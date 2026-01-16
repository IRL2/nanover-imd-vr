using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nanover.Visualisation.Node.Protein
{
    public class DsspAlgorithm
    {
        [ThreadStatic]
        private static Dictionary<int, int> residueIndexToOrdinal = new Dictionary<int, int>();

        /// <summary>
        /// Get ResidueData for amino acid residues, storing locations of alpha carbons etc.
        /// </summary>
        /// <param name="aminoAcidResidues"></param>
        /// <param name="atomResidues"></param>
        /// <param name="atomNames"></param>
        /// <returns></returns>
        public static SecondaryStructureResidueData[] GetResidueData(IReadOnlyCollection<int> aminoAcidResidues,
                                                   int[] atomResidues,
                                                   string[] atomNames)
        {
            var list = new SecondaryStructureResidueData[aminoAcidResidues.Count];
            var resDataIndex = 0;
            residueIndexToOrdinal.Clear();
            foreach (var resIndex in aminoAcidResidues)
            {
                var data = new SecondaryStructureResidueData
                {
                    ResidueIndex = resIndex,
                    AlphaCarbonIndex = -1,
                    NitrogenIndex = -1,
                    CarbonIndex = -1,
                    OxygenIndex = -1,
                    HydrogenIndex = -1
                };
                data.ordinal = resDataIndex++;
                list[data.ordinal] = data;
                residueIndexToOrdinal[resIndex] = data.ordinal;
            }

            for (var i = 0; i < atomResidues.Length; i++)
            {
                if (GetResidueData(atomResidues[i]) is not { } residue)
                    continue;

                switch (atomNames[i])
                {
                    case "CA":
                        residue.AlphaCarbonIndex = i;
                        break;
                    case "N":
                        residue.NitrogenIndex = i;
                        break;
                    case "C":
                        residue.CarbonIndex = i;
                        break;
                    case "O":
                        residue.OxygenIndex = i;
                        break;
                    case "H":
                        residue.HydrogenIndex = i;
                        break;
                }
            }

            // Check for missing oxygen and guess (O is sometimes OC1/OC2)
            for (var i = atomResidues.Length - 1; i >= 0; i--)
            {
                if (GetResidueData(atomResidues[i]) is not { } residue
                 || residue.OxygenIndex >= 0
                 || atomNames[i][0] != 'O')
                    continue;

                residue.OxygenIndex = i;
            }

            SecondaryStructureResidueData GetResidueData(int residueIndex)
            {
                if (!residueIndexToOrdinal.TryGetValue(residueIndex, out int ordinal))
                    return null;

                return list[ordinal];
            }

            return list;
        }

        public static void UpdateResidueAtomPositions(Vector3[] atomPositions,
                                                      IList<SecondaryStructureResidueData> residues)
        {
            foreach (var residue in residues)
            {
                if (!residue.HasEssentialNamedAtoms)
                    continue;

                residue.AlphaCarbonPosition = atomPositions[residue.AlphaCarbonIndex];
                residue.NitrogenPosition = atomPositions[residue.NitrogenIndex];
                residue.OxygenPosition = atomPositions[residue.OxygenIndex];
                residue.CarbonPosition = atomPositions[residue.CarbonIndex];
                if (residue.HydrogenIndex >= 0)
                {
                    residue.HydrogenPosition = atomPositions[residue.HydrogenIndex];
                }
                else if (residue.ordinal > 0)
                {
                    var nextRes = residue;
                    var prevRes = residues[nextRes.ordinal - 1];
                    // Calculate hydrogen position (optional, could use actual set hydrogen positions)
                    if (Vector3.Distance(prevRes.CarbonPosition, nextRes.NitrogenPosition) <= 0.2)
                        nextRes.HydrogenPosition = prevRes.CarbonPosition - prevRes.OxygenPosition;
                    else
                        nextRes.HydrogenPosition = nextRes.OxygenPosition - nextRes.CarbonPosition;

                    nextRes.HydrogenPosition = nextRes.HydrogenPosition.normalized * 0.1008f +
                                               nextRes.NitrogenPosition;
                }
                else
                {
                    residue.HydrogenPosition = residue.OxygenPosition - residue.CarbonPosition;
                    residue.HydrogenPosition = residue.HydrogenPosition.normalized * 0.1008f +
                                               residue.NitrogenPosition;
                }
            }
        }

        private static void Initialise(IEnumerable<SecondaryStructureResidueData> residues)
        {
            foreach (var residue in residues)
            {
                residue.AcceptorHydrogenBondEnergy = 1e10;
                residue.AcceptorHydrogenBondResidue = null;
                residue.DonorHydrogenBondEnergy = 1e10;
                residue.DonorHydrogenBondResidue = null;
                residue.SecondaryStructure = SecondaryStructureAssignment.None;
                residue.Pattern = SecondaryStructurePattern.None;
            }
        }

        private static bool IsBonded(SecondaryStructureResidueData res1,
                                     SecondaryStructureResidueData res2,
                                     float cutoff,
                                     out float energy)
        {
            energy = 0.42f * 0.20f * 33.20f *
                     (1f / Vector3.Distance(res1.OxygenPosition, res2.NitrogenPosition) +
                      1f / Vector3.Distance(res1.CarbonPosition, res2.HydrogenPosition) -
                      1f / Vector3.Distance(res1.OxygenPosition, res2.HydrogenPosition) -
                      1f / Vector3.Distance(res1.CarbonPosition, res2.NitrogenPosition));
            return energy < cutoff;
        }


        public static void CalculateSecondaryStructure(IReadOnlyList<SecondaryStructureResidueData> residues,
                                                       DsspOptions options)
        {
            Initialise(residues);

            // Calculate bond energies for all residues which are spaced at least 3 apart
            CalculateHydrogenBonds(residues, options);

            // Calculate N turns
            IdentifyTurns(residues);

            IdentifyAntiparallelBridges(residues);

            IdentifyParallelBridges(residues);

            AssignHelix(residues, SecondaryStructurePattern.FiveTurn, 5,
                        SecondaryStructureAssignment.PiHelix);

            AssignHelix(residues, SecondaryStructurePattern.FourTurn, 4,
                        SecondaryStructureAssignment.AlphaHelix);

            AssignBetaSheets(residues);

            AssignHelix(residues, SecondaryStructurePattern.ThreeTurn, 3,
                        SecondaryStructureAssignment.ThreeTenHelix);

            ExpandSheets(residues);

            /*
             
            for (var i = 0; i < residues.Count; i++)
            {
                var res1 = residues[i];
                switch (res1.SecondaryStructure)
                {
                    case 'g':
                    case 'G':
                    case 'i':
                    case 'I':
                        secstruc = char.ToUpper(res1.SecondaryStructure);
                        bool swap = false;
                        if (i > 0)
                        {
                            swap = secstruc != char.ToUpper(residues[i - 1].SecondaryStructure);
                        }
                        else
                        {
                            swap = true;
                        }

                        if (i < residues.Count - 1)
                        {
                            swap = swap && (secstruc != char.ToUpper(residues[i + 1].SecondaryStructure));
                        }

                        if (swap) residues[i].SecondaryStructure = 't';
                        break;
                }
            }
            

            CheckSingleTurn(residues, 5, SecondaryStructurePattern.FiveTurn);
            CheckSingleTurn(residues, 4, SecondaryStructurePattern.FourTurn);
            CheckSingleTurn(residues, 3, SecondaryStructurePattern.ThreeTurn);
            
            */
        }

        private static void ExpandSheets(IReadOnlyList<SecondaryStructureResidueData> residues)
        {
            var ids = new List<int>();
            for (var i = 0; i < residues.Count; i++)
            {
                var res = residues[i];
                if (res.SecondaryStructure != SecondaryStructureAssignment.None)
                    continue;
                if (i > 0 && residues[i - 1].SecondaryStructure ==
                    SecondaryStructureAssignment.Sheet)
                    ids.Add(i);
                else if (i < residues.Count - 1 &&
                         residues[i + 1].SecondaryStructure == SecondaryStructureAssignment.Sheet)
                    ids.Add(i);
            }

            foreach (var i in ids)
                residues[i].SecondaryStructure = SecondaryStructureAssignment.Sheet;
        }

        private static void AssignBetaSheets(IReadOnlyList<SecondaryStructureResidueData> residues)
        {
            foreach (var residue in residues)
            {
                if (residue.SecondaryStructure != SecondaryStructureAssignment.None)
                    continue;
                if (PatternHasFlag(residue.Pattern, SecondaryStructurePattern.AntiparallelBridge) 
                 || PatternHasFlag(residue.Pattern, SecondaryStructurePattern.ParallelBridge))
                    residue.SecondaryStructure = SecondaryStructureAssignment.Sheet;
            }
        }

        private static void IdentifyParallelBridges(IReadOnlyList<SecondaryStructureResidueData> residues)
        {
// Parallel Bridge
            // n---c   n---c   res-1   n---c   n---c   ---->
            //              o         /
            //               \       /
            //                \     o
            // n---c   n---c   res-2   n---c   n---c   ---->
            for (var i = 1; i < residues.Count; i++)
            {
                var res1 = residues[i];
                var res2 = residues[i - 1].AcceptorHydrogenBondResidue;
                if (res2?.AcceptorHydrogenBondResidue == null)
                    continue;
                if (res2.AcceptorHydrogenBondResidue.ordinal - res1.ordinal == 1)
                {
                    res1.Pattern |= SecondaryStructurePattern.ParallelBridge;
                    res2.Pattern |= SecondaryStructurePattern.ParallelBridge;
                }
            }

            // Parallel Bridge
            // n---c   n---c   res-1   n---c   n---c   ---->
            //                /     o
            //               /       \
            //              o         \
            // n---c   n---c   res-2   n---c   n---c   ---->
            foreach (var res1 in residues)
            {
                if (res1.AcceptorHydrogenBondResidue == null)
                    continue;
                if (res1.DonorHydrogenBondResidue == null)
                    continue;
                if (res1.AcceptorHydrogenBondResidue.ordinal -
                    res1.DonorHydrogenBondResidue.ordinal == 2)
                {
                    var res2 = residues[res1.DonorHydrogenBondResidue.ordinal + 1];
                    res1.Pattern |= SecondaryStructurePattern.ParallelBridge;
                    res2.Pattern |= SecondaryStructurePattern.ParallelBridge;
                }
            }
        }

        private static void IdentifyAntiparallelBridges(IReadOnlyList<SecondaryStructureResidueData> residues)
        {
            // Antiparrallel Bridge
            // c---n   c---n   res-1   c---n
            //                 o   |
            //                 |   |
            //                 |   o
            // n---c   n---c   n---c   n---c
            foreach (var res1 in residues)
            {
                var res2 = res1.AcceptorHydrogenBondResidue;
                if (res2 != null)
                    if (res2.AcceptorHydrogenBondResidue == res1)
                    {
                        res1.Pattern |= SecondaryStructurePattern.AntiparallelBridge;
                        res2.Pattern |= SecondaryStructurePattern.AntiparallelBridge;
                    }
            }

            // Antiparrallel Bridge
            // n---c   n---c   res-1   res-3   n---c
            //                 o               |    
            //                 |               |
            //                 |               o
            // c---n   c---n   c---n   res-4   res-2
            for (var i = 0; i < residues.Count - 2; i++)
            {
                var res1 = residues[i];
                if (res1.AcceptorHydrogenBondResidue == null)
                    continue;

                var res2 = residues[i + 2].DonorHydrogenBondResidue;
                if (res2 == null)
                    continue;

                if (res1.AcceptorHydrogenBondResidue.ordinal - res2.ordinal == 2)
                {
                    var res4 = residues[res2.ordinal + 1];
                    var res3 = residues[i + 1];

                    res3.Pattern |= SecondaryStructurePattern.AntiparallelBridge;
                    res4.Pattern |= SecondaryStructurePattern.AntiparallelBridge;
                }
            }
        }

        private static void IdentifyTurns(IReadOnlyList<SecondaryStructureResidueData> residues)
        {
            foreach (var residue in residues)
            {
                if (residue.AcceptorHydrogenBondResidue == null)
                    continue;

                var count = residue.AcceptorHydrogenBondResidue.ordinal - residue.ordinal;

                switch (count)
                {
                    case 3:
                        residue.Pattern |= SecondaryStructurePattern.ThreeTurn;
                        break;
                    case 4:
                        residue.Pattern |= SecondaryStructurePattern.FourTurn;
                        break;
                    case 5:
                        residue.Pattern |= SecondaryStructurePattern.FiveTurn;
                        break;
                }
            }
        }

        private static void CalculateHydrogenBonds(IReadOnlyList<SecondaryStructureResidueData> residues,
                                                   DsspOptions options)
        {
            for (var i = 0; i < residues.Count - 3; i++)
            {
                var res1 = residues[i];
                for (var j = i + 3; j < residues.Count; j++)
                {
                    var res2 = residues[j];
                    var dist =
                        Vector3.SqrMagnitude(res1.AlphaCarbonPosition - res2.AlphaCarbonPosition);
                    if (dist > 0.64f)
                    {
                        var jump = (int) ((Mathf.Sqrt(dist) - 0.8f) / 0.5f);
                        j += jump;
                        continue;
                    }

                    if (IsBonded(res1, res2, options.Cutoff, out var energy) &&
                        energy < res1.AcceptorHydrogenBondEnergy)
                    {
                        res1.AcceptorHydrogenBondResidue = res2;
                        res1.AcceptorHydrogenBondEnergy = energy;
                        res2.DonorHydrogenBondResidue = res1;
                        res2.DonorHydrogenBondEnergy = energy;
                    }

                    if (IsBonded(res2, res1, options.Cutoff, out energy) &&
                        energy < res2.AcceptorHydrogenBondEnergy)
                    {
                        res2.AcceptorHydrogenBondResidue = res1;
                        res2.AcceptorHydrogenBondEnergy = energy;
                        res1.DonorHydrogenBondResidue = res2;
                        res1.DonorHydrogenBondEnergy = energy;
                    }
                }
            }
        }

        private static void AssignHelix(IReadOnlyList<SecondaryStructureResidueData> residues,
                                        SecondaryStructurePattern pattern,
                                        int length,
                                        SecondaryStructureAssignment assignment)
        {
            for (var i = 0; i < residues.Count - 1; i++)
            {
                var res1 = residues[i];

                if (PatternHasFlag(res1.Pattern, pattern) && PatternHasFlag(residues[i + 1].Pattern, pattern))
                {
                    for (var slot = 1; slot <= length; slot++)
                    {
                        var res2 = residues[i + slot];
                        if (res2.SecondaryStructure != SecondaryStructureAssignment.None) continue;
                        res2.SecondaryStructure = assignment;
                    }
                }
            }
        }

        // This exists because the C# built-in allocates memory every call
        public static bool PatternHasFlag(SecondaryStructurePattern value, SecondaryStructurePattern flag)
        {
            return ((int)value & (int)flag) != 0;
        }

        private static void CheckSingleTurn(IReadOnlyList<SecondaryStructureResidueData> residues,
                                            int turnLength,
                                            SecondaryStructurePattern pattern)
        {
            /* single 5-turns */
            for (var i = 0; i < residues.Count; i++)
            {
                var res1 = residues[i];
                if (PatternHasFlag(res1.Pattern, SecondaryStructurePattern.FiveTurn))
                {
                    if (i > 0 && PatternHasFlag(residues[i - 1]
                                 .Pattern, SecondaryStructurePattern.FiveTurn))
                        continue;
                    if (i < residues.Count - 1 && PatternHasFlag(residues[i + 1].Pattern, SecondaryStructurePattern.FiveTurn))
                        continue;

                    for (var slot = 1; slot <= 5; slot++)
                    {
                        var res2 = residues[i + slot];
                        if (res2.SecondaryStructure == SecondaryStructureAssignment.None)
                            res2.SecondaryStructure = SecondaryStructureAssignment.Turn;
                    }
                }
            }
        }
    }
}