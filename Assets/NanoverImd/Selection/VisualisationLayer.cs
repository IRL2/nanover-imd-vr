using System;
using System.Collections.Generic;
using System.Linq;
using Nanover.Core.Math;
using Nanover.Visualisation.Property;
using UnityEngine;

namespace NanoverImd.Selection
{
    /// <summary>
    /// A group of <see cref="VisualisationSelection" />s which are mutually exclusive.
    /// </summary>
    /// <remarks>
    /// Selections within a visualisation layer display an atom precisely once,
    /// with the selection settings overwriting one another in the order that they are
    /// established.
    /// For example, a <see cref="VisualisationLayer" /> with selections [1,2,3] and
    /// [3,4,5] will display atoms [1,2] in the first (lower) selection and atoms
    /// [3,4,5] in the second (upper) selection.
    /// </remarks>
    public class VisualisationLayer : MonoBehaviour
    {
        [SerializeField]
        private VisualisationScene scene;

        /// <summary>
        /// The <see cref="VisualisationScene" /> to which this layer belongs.
        /// </summary>
        public VisualisationScene Scene
        {
            get => scene;
            set => scene = value;
        }

        /// <summary>
        /// The set of selections that form this layer.
        /// </summary>
        public IReadOnlyList<VisualisationSelection> Selections => selections;

        private void Awake()
        {
            Scene = GetComponentInParent<VisualisationScene>();
        }

        private float _prevAtomCount = 0;

        public bool GetInteractable(bool[] lookup)
        {
            // default to interactable
            for (int i = 0; i < lookup.Length; ++i)
                lookup[i] = true;

            try
            {
                for (int i = 0; i < selections.Count; i++)
                {
                    var selection = selections[i];
                    var ignored = selection.Selection.InteractionMethod == ParticleSelection.InteractionMethodNone;
                    var hidden = selection.Selection.HideRenderer;

                    if (!selection.FilteredIndices.HasValue)
                        continue;

                    foreach (int id in selection.FilteredIndices)
                    {
                        lookup[id] = !ignored && !hidden;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        private void Update()
        {
            // TODO: trigger this from frame topology change
            if (_prevAtomCount != Scene.ParticleCount)
            {
                RefreshAllSelections();
                _prevAtomCount = Scene.ParticleCount;
            }
        }

        private readonly List<VisualisationSelection> selections =
            new List<VisualisationSelection>();

        [SerializeField]
        private VisualisationSelection selectionPrefab;

        /// <summary>
        /// Refresh all selections in this layer. 
        /// </summary>
        public void RefreshAllSelections()
        {
            OnSelectionUpdated(selections[selections.Count - 1]);
        }

        /// <summary>
        /// Add a selection to this visualisation, based upon a
        /// <see cref="ParticleSelection" />.
        /// </summary>
        public VisualisationSelection AddSelection(ParticleSelection selection)
        {
            var visualisationSelection = Instantiate(selectionPrefab, transform);
            visualisationSelection.Selection = selection;
            visualisationSelection.gameObject.name = selection.Name;
            visualisationSelection.SelectionUpdated +=
                () => OnSelectionUpdated(visualisationSelection);

            
            if (selections.Any())
            {
                var insertIndex = 1;
                foreach (var existingSelection in selections.Skip(1))
                    if (string.CompareOrdinal(existingSelection.Selection.ID, selection.ID) <= 0)
                        insertIndex++;
                selections.Insert(insertIndex, visualisationSelection);
            }
            else
            {
                selections.Add(visualisationSelection);
            }

            
            OnSelectionUpdated(visualisationSelection);
            return visualisationSelection;
        }

        /// <summary>
        /// Refresh all selections that are lower down the layer than the given selection.
        /// </summary>
        private void OnSelectionUpdated(VisualisationSelection selection)
        {
            var index = selections.IndexOf(selection);
            if (index < 0)
                throw new ArgumentException(
                    "Tried to update selection not in layer.");
            for (var i = index; i >= 0; i--)
                selections[i].CalculateFilteredIndices(
                    i == selections.Count - 1 ? null : selections[i + 1],
                    Scene.ParticleCount);
        }

        /// <summary>
        /// Either update an existing selection with the given key or add a new selection
        /// with the given key.
        /// </summary>
        public void UpdateOrCreateSelection(string key, object value)
        {
            if (!(value is Dictionary<string, object> dict))
                return;
            foreach (var selection in selections)
                if (selection.Selection.ID == key)
                {
                    if (selection.Selection.ID == ParticleSelection.RootSelectionId)
                    {
                        dict.Remove(ParticleSelection.KeySelected);
                    }

                    selection.Selection.UpdateFromObject(dict);
                    selection.gameObject.name = selection.Selection.Name;
                    return;
                }

            var newSelection = new ParticleSelection(dict);
            var selec = AddSelection(newSelection);
            selec.UpdateVisualiser();
        }

        /// <summary>
        /// Remove the selection with the given key.
        /// </summary>
        public void RemoveSelection(string key)
        {
            var selection = selections.FirstOrDefault(s => s.Selection.ID == key);
            if (selection == null)
                return;

            Destroy(selection.gameObject);
            selections.Remove(selection);

            OnSelectionUpdated(selections.Last());
        }

        /// <summary>
        /// Find the selection on this layer which contains the particle of the given
        /// index.
        /// </summary>
        public VisualisationSelection GetSelectionForParticle(int particleIndex)
        {
            for (var i = selections.Count - 1; i >= 0; i--)
            {
                var selection = selections[i];
                if (!selection.FilteredIndices.HasNonNullValue())
                    return selection;
                if (SearchAlgorithms.BinarySearch(particleIndex, selection.FilteredIndices.Value))
                    return selection;
            }

            return null;
        }
    }
}