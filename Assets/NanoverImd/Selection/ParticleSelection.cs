using System;
using System.Collections.Generic;
using Nanover.Core;

namespace NanoverImd.Selection
{
    /// <summary>
    /// A selection containing a group of particles.
    /// </summary>
    public class ParticleSelection
    {
        /// <summary>
        /// The unique identifier for this selection.
        /// </summary>
        public string ID { get; }

        /// <summary>
        /// The set of indices that are contained in this selection. When null, this
        /// represents a selection containing everything.
        /// </summary>
        public IReadOnlyList<int> Selection => selection;

        /// <summary>
        /// The user-facing name of this selection.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// A set of arbitrary properties associated with this selection.
        /// </summary>
        public IDictionary<string, object> Properties => properties;

        private List<int> selection = null;

        private Dictionary<string, object> properties = new Dictionary<string, object>();

        public const string KeyName = "name";
        public const string KeyProperties = "properties";
        public const string KeyId = "id";
        public const string KeySelected = "selected";
        public const string KeyParticleIds = "particle_ids";

        public const string KeyHideProperty = "nanover.rendering.hide";
        public const string KeyRendererProperty = "nanover.rendering.renderer";
        public const string KeyInteractionMethod = "nanover.interaction.method";
        public const string KeyResetVelocities = "nanover.interaction.velocity_reset";

        public const string InteractionMethodSingle = "single";
        public const string InteractionMethodGroup = "group";
        public const string InteractionMethodNone = "none";

        public const string RootSelectionId = "selection.root";
        public const string RootSelectionName = "Base";

        public const string SelectionIdPrefix = "selection.";

        /// <summary>
        /// Callback for when the selection is altered.
        /// </summary>
        public event Action SelectionUpdated;

        /// <summary>
        /// Create a selection with the given ID, that contains all atoms.
        /// </summary>
        public ParticleSelection(string id)
        {
            ID = id;
        }

        /// <summary>
        /// Create a selection from a dictionary representation of the selection.
        /// </summary>
        public ParticleSelection(Dictionary<string, object> obj) : this(obj[KeyId] as string)
        {
            UpdateFromObject(obj);
        }

        /// <summary>
        /// Update this selection based upon a dictionary representation.
        /// </summary>
        public void UpdateFromObject(Dictionary<string, object> obj)
        {
            Name = obj.GetValueOrDefault(KeyName, "Unnamed Selection");
            properties = obj.GetValueOrDefault(KeyProperties, new Dictionary<string, object>());
            var selectedDict = obj.GetValueOrDefault(KeySelected, new Dictionary<string, object>());
            if (selectedDict != null)
            {
                var ids = selectedDict.GetValueOrDefault<IReadOnlyList<object>>(KeyParticleIds,
                                                                                null);

                if (ids == null)
                {
                    selection = null; // Selects everything
                }
                else
                {
                    selection = selection ?? new List<int>();
                    selection.Clear();
                    foreach (var id in ids)
                        selection.Add(Convert.ToInt32(id));
                    selection.Sort();
                }
            }
            else
            {
                selection = null; // Selects everything
            }

            SelectionUpdated?.Invoke();
        }

        /// <summary>
        /// Create a selection representing the shared root selection.
        /// </summary>
        /// <remarks> 
        /// The root selection, containing all atoms, is denoted with a <c>null</c> selection.
        /// </remarks>
        public static ParticleSelection CreateRootSelection()
        {
            return new ParticleSelection(RootSelectionId)
            {
                Name = RootSelectionName,
                selection = null // Selects everything
            };
        }

        /// <summary>
        /// Should this selection not have a visualiser?
        /// </summary>
        public bool HideRenderer => Properties.GetValueOrDefault(KeyHideProperty, false);

        /// <summary>
        /// A string or dictionary that describes the visualiser for this selection.
        /// </summary>
        public object Renderer => Properties.GetValueOrDefault<object>(KeyRendererProperty, null);

        /// <summary>
        /// The type of interaction that should occur using this selection.
        /// </summary>
        public string InteractionMethod =>
            Properties.GetValueOrDefault(KeyInteractionMethod, InteractionMethodSingle);

        /// <summary>
        /// Should the velocities be reset after this selection is interacted with.
        /// </summary>
        public bool ResetVelocities => Properties.GetValueOrDefault(KeyResetVelocities, false);
    }
}