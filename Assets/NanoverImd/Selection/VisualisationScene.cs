using System.Collections.Generic;
using MessagePackTesting;
using Nanover.Visualisation;
using Nanover.Visualisation.Components.Adaptor;
using Nanover.Visualisation.Property;
using NanoverImd.Interaction;
using UnityEngine;

namespace NanoverImd.Selection
{
    /// <summary>
    /// A set of layers and selections that are used to render a frame using multiple
    /// visualisers.
    /// </summary>
    /// <remarks>
    /// It contains a <see cref="FrameAdaptor" /> to which each visualiser will be
    /// linked.
    /// </remarks>
    public class VisualisationScene : MonoBehaviour
    {
        /// <summary>
        /// The <see cref="VisualisationLayer" />s that make up this scene.
        /// </summary>
        private readonly List<VisualisationLayer> layers = new List<VisualisationLayer>();

        [SerializeField]
        private NanoverImdSimulation simulation;

        [SerializeField]
        private SynchronisedFrameSource frameSource;

        [SerializeField]
        private InteractableScene interactableScene;

        /// <inheritdoc cref="InteractableScene.InteractedParticles"/>
        public IReadOnlyProperty<int[]> InteractedParticles
            => interactableScene.InteractedParticles;

        /// <inheritdoc cref="FrameAdaptor" />
        /// <remarks>
        /// This is automatically generated on <see cref="Start()" />.
        /// </remarks>
        private FrameAdaptor frameAdaptor;

        /// <summary>
        /// The <see cref="FrameAdaptor" /> that exposes all the data present in the frame
        /// in a way that is compatible with the visualisation system.
        /// </summary>
        public FrameAdaptor FrameAdaptor => frameAdaptor;

        [SerializeField]
        private VisualisationLayer layerPrefab;

        /// <summary>
        /// The number of particles in the current frame, or 0 if no frame is present.
        /// </summary>
        public int ParticleCount => frameSource.CurrentFrame?.ParticleCount ?? 0;

        /// <summary>
        /// The root selection of the scene.
        /// </summary>
        private ParticleSelection rootSelection;

        private const string BaseLayerName = "Base Layer";

        private VisualisationLayer BaseLayer => layers[0];

        /// <summary>
        /// Remove all the visualisation layer.
        /// </summary>
        public void ClearLayers()
        {
            foreach (var layer in layers)
            {
                Destroy(layer.gameObject);
            }
            layers.Clear();
        }
        
        /// <summary>
        /// Create a visualisation layer with the given name.
        /// </summary>
        public VisualisationLayer AddLayer(string name)
        {
            var layer = Instantiate(layerPrefab, transform);
            layer.gameObject.name = name;
            layers.Add(layer);
            return layer;
        }

        private const string HighlightedParticlesKey = "highlighted.particles";

        private void OnEnable()
        {
            frameAdaptor = gameObject.AddComponent<FrameAdaptor>();
            frameAdaptor.FrameSource = frameSource;
            frameAdaptor.Node.AddOverrideProperty<int[]>(HighlightedParticlesKey).LinkedProperty = InteractedParticles; 

            simulation.Multiplayer.SharedStateDictionaryKeyUpdated +=
                MultiplayerOnSharedStateDictionaryKeyChanged;
            simulation.Multiplayer.SharedStateDictionaryKeyRemoved +=
                MultiplayerOnSharedStateDictionaryKeyRemoved;
            ClearLayers();
            var baseLayer = AddLayer(BaseLayerName);
            rootSelection = ParticleSelection.CreateRootSelection();
            var baseRenderableSelection = baseLayer.AddSelection(rootSelection);
            baseRenderableSelection.UpdateVisualiser();
        }

        private void OnDisable()
        {
            simulation.Multiplayer.SharedStateDictionaryKeyUpdated -=
                MultiplayerOnSharedStateDictionaryKeyChanged;
            simulation.Multiplayer.SharedStateDictionaryKeyRemoved -=
                MultiplayerOnSharedStateDictionaryKeyRemoved;
            
            Destroy(frameAdaptor);
        }

        /// <summary>
        /// Callback for when a key is removed from the multiplayer shared state.
        /// </summary>
        private void MultiplayerOnSharedStateDictionaryKeyRemoved(string key)
        {
            if (key == ParticleSelection.RootSelectionId)
            {
                rootSelection.UpdateFromObject(new Dictionary<string, object>
                {
                    [ParticleSelection.KeyName] = ParticleSelection.RootSelectionName,
                    [ParticleSelection.KeyId] = ParticleSelection.RootSelectionId
                });
            }
            else if (key.StartsWith(ParticleSelection.SelectionIdPrefix))
            {
                // TODO: Work out which layer the selection is on.
                BaseLayer.RemoveSelection(key);
            }
        }

        /// <summary>
        /// Callback for when a key is modified in the multiplayer shared state.
        /// </summary>
        private void MultiplayerOnSharedStateDictionaryKeyChanged(string key, object value)
        {
            if (key.StartsWith(ParticleSelection.SelectionIdPrefix))
            {
                // TODO: Work out which layer the selection is on.
                BaseLayer.UpdateOrCreateSelection(key, value.StringifyStructureKeys());
            }
        }

        /// <summary>
        /// Get the selection in the base layer which contains the particle.
        /// </summary>
        public VisualisationSelection GetSelectionForParticle(int particleIndex)
        {
            return BaseLayer.GetSelectionForParticle(particleIndex);
        }

        public bool GetInteractable(bool[] lookup) => BaseLayer.GetInteractable(lookup);
    }
}