using System;
using System.Collections.Generic;
using Nanover.Frame;
using Nanover.Visualisation.Properties;
using Nanover.Visualisation.Properties.Collections;
using Nanover.Visualisation.Property;
using UnityEngine;

namespace Nanover.Visualisation.Node.Protein
{
    /// <summary>
    /// Calculates secondary structure using the DSSP Algorithm.
    /// </summary>
    [Serializable]
    public class SecondaryStructureNode
    {
        #region Input Properties

        /// <summary>
        /// Array of atomic positions. This should contains the atoms which are relevant to
        /// the protein backbone.
        /// </summary>
        public IProperty<Vector3[]> AtomPositions => atomPositions;

        /// <inheritdoc cref="AtomPositions" />
        [SerializeField]
        private Vector3ArrayProperty atomPositions = new Vector3ArrayProperty();

        /// <summary>
        /// Array of residue indices which may appear in
        /// <see cref="PeptideResidueSequences" /> for each atom.
        /// </summary>
        public IProperty<int[]> AtomResidues => atomResidues;

        /// <inheritdoc cref="AtomResidues" />
        [SerializeField]
        private IntArrayProperty atomResidues = new IntArrayProperty();

        /// <summary>
        /// Array of atom names. Each amino acid should have atoms named 'CA', 'C', 'N' and
        /// 'O'.
        /// </summary>
        public IProperty<string[]> AtomNames => atomNames;

        /// <inheritdoc cref="AtomNames" />
        [SerializeField]
        private StringArrayProperty atomNames = new StringArrayProperty();

        /// <summary>
        /// Number of residues involved. The maximum index referenced in both
        /// <see cref="AtomResidues" /> and <see cref="PeptideResidueSequences" /> should
        /// be less than this value.
        /// </summary>
        public IProperty<int> ResidueCount => residueCount;

        /// <inheritdoc cref="ResidueCount" />
        [SerializeField]
        private IntProperty residueCount = new IntProperty();

        /// <summary>
        /// Array of residue indices that indicate which residues are involved in a protein
        /// chain.
        /// </summary>
        public IProperty<IReadOnlyList<int>[]> PeptideResidueSequences => peptideResidueSequences;

        /// <inheritdoc cref="PeptideResidueSequences" />
        [SerializeField]
        private SelectionArrayProperty peptideResidueSequences = new SelectionArrayProperty();

        /// <summary>
        /// Options to configure the DSSP algorithm.
        /// </summary>
        public DsspOptions DsspOptions
        {
            get => dsspOptions;
            set => dsspOptions = value;
        }

        /// <inheritdoc cref="DsspOptions" />
        [SerializeField]
        private DsspOptions dsspOptions = new DsspOptions();

        #endregion

        #region Output Properties

        /// <summary>
        /// Secondary structure assignments for each residue. The size of this array will
        /// be equal to <see cref="ResidueCount" />, with residues that are not in one of
        /// the peptide chains provided in <see cref="PeptideResidueSequences" /> being
        /// given the assignment <see cref="SecondaryStructureAssignment.None" />
        /// </summary>
        public IReadOnlyProperty<SecondaryStructureAssignment[]> ResidueSecondaryStructure =>
            residueSecondaryStructure;

        /// <inheritdoc cref="ResidueSecondaryStructure" />
        private readonly SecondaryStructureArrayProperty residueSecondaryStructure =
            new SecondaryStructureArrayProperty();

        /// <summary>
        /// Array of calculated hydrogen bonds, based on indices of atoms in the
        /// <see cref="AtomPositions" />.
        /// </summary>
        public IReadOnlyProperty<BondPair[]> HydrogenBonds => hydrogenBonds;

        /// <inheritdoc cref="HydrogenBonds" />
        private BondArrayProperty hydrogenBonds = new BondArrayProperty();

        #endregion

        #region State Management 

        /// <summary>
        /// Does the secondary structure require recalculating.
        /// </summary>
        private bool needRecalculate = true;

        /// <summary>
        /// Set of residue data (positions of hydrogen-bonding involved atoms) for each
        /// residue in each sequence specified in <see cref="PeptideResidueSequences" />.
        /// </summary>
        private List<SecondaryStructureResidueData[]> sequenceResidueData =
            new List<SecondaryStructureResidueData[]>();

        #endregion
        
        public bool IsInputValid => peptideResidueSequences.HasNonNullValue()
                                 && residueCount.HasNonNullValue();

        public bool AreResiduesDirty => atomResidues.IsDirty || peptideResidueSequences.IsDirty ||
                                        atomNames.IsDirty || residueCount.IsDirty;

        public bool AreResiduesValid => atomResidues.HasNonEmptyValue() &&
                                        peptideResidueSequences.HasNonEmptyValue() &&
                                        atomNames.HasNonEmptyValue();

        public void Refresh()
        {
            if (IsInputValid)
            {
                if (AreResiduesDirty)
                {
                    if (AreResiduesValid)
                        UpdateResidues();

                    atomResidues.IsDirty = false;
                    peptideResidueSequences.IsDirty = false;
                    atomNames.IsDirty = false;
                    residueCount.IsDirty = false;
                }

                if (atomPositions.IsDirty)
                    UpdatePositions();

                if (needRecalculate || Time.frameCount % 30 == 0)
                {
                    CalculateSecondaryStructure();
                    CalculateHydrogenBonds();
                    needRecalculate = false;
                }
            }
        }

        private void UpdateResidues()
        {
            sequenceResidueData.Clear();
            foreach (var sequence in peptideResidueSequences.Value)
                sequenceResidueData.Add(
                    DsspAlgorithm.GetResidueData(sequence, atomResidues, atomNames));

            needRecalculate = true;
        }

        private void CalculateSecondaryStructure()
        {
            foreach (var peptideSequence in sequenceResidueData)
                DsspAlgorithm.CalculateSecondaryStructure(peptideSequence, dsspOptions);

            residueSecondaryStructure.Resize(residueCount.Value);

            foreach (var sequence in sequenceResidueData)
            foreach (var data in sequence)
                residueSecondaryStructure.Value[data.ResidueIndex] = data.SecondaryStructure;

            residueSecondaryStructure.MarkValueAsChanged();
        }

        private BondPair[] hydrogrenBondsArray = new BondPair[0];

        private void CalculateHydrogenBonds()
        {
            int count = 0;
            foreach (var sequence in sequenceResidueData)
                foreach (var data in sequence)
                    if (data.DonorHydrogenBondResidue != null)
                        count += 1;

            if (count != hydrogrenBondsArray.Length)
                hydrogrenBondsArray = new BondPair[count];

            int next = 0;
            foreach (var sequence in sequenceResidueData)
                foreach (var data in sequence)
                    if (data.DonorHydrogenBondResidue != null)
                        hydrogrenBondsArray[next++] = new BondPair(data.OxygenIndex, data.DonorHydrogenBondResidue.NitrogenIndex);

            hydrogenBonds.Value = hydrogrenBondsArray;
        }

        private void UpdatePositions()
        {
            foreach (var t in sequenceResidueData)
                DsspAlgorithm.UpdateResidueAtomPositions(atomPositions.Value, t);

            needRecalculate = true;
        }
    }
}