using System;
using System.Collections.Generic;
using Nanover.Core;
using Nanover.Frame;
using Nanover.Frame.Event;
using Nanover.Visualisation.Property;

namespace Nanover.Visualisation.Node.Adaptor
{
    /// <summary>
    /// Visualisation node which reads frames using <see cref="IFrameConsumer" /> and
    /// dynamically provides the frame's data as properties for the visualisation
    /// system.
    /// </summary>
    /// <remarks>
    /// This visualisation node acts as the bridge between the underlying trajectory
    /// and the visualisation system.
    /// </remarks>
    [Serializable]
    public class FrameAdaptorNode : BaseAdaptorNode, IFrameConsumer
    {
        private List<string> propertyOverrides = new List<string>();

        /// <summary>
        /// Add a property with the given type and name to this adaptor that is not
        /// affected by the frame.
        /// </summary>
        public IProperty<TValue> AddOverrideProperty<TValue>(string name)
        {
            var prop = base.GetOrCreateProperty<TValue>(name) as IProperty<TValue>;
            propertyOverrides.Add(name);
            return prop;
        }
        
        /// <summary>
        /// Remove a property with the given type and name from this adaptor that is not
        /// affected by the frame.
        /// </summary>
        public void RemoveOverrideProperty<TValue>(string name)
        {
            propertyOverrides.Remove(name);
            GetPropertyValueFromFrame(name, GetOrCreateProperty<TValue>(name) as IProperty);
        }

        /// <inheritdoc cref="BaseAdaptorNode.OnCreateProperty{T}" />
        protected override void OnCreateProperty<T>(string key, IProperty<T> property)
        {
            GetPropertyValueFromFrame(key, property);
        }

        /// <summary>
        /// Update a property by getting the value with the given key from the current
        /// frame, ignoring if this property is marked as an override.
        /// </summary>
        private void GetPropertyValueFromFrame(string key, IProperty property)
        {
            if (propertyOverrides.Contains(key))
                return;
            
            if (FrameSource?.CurrentFrame != null
             && FrameSource.CurrentFrame.Data.TryGetValue(key, out var value))
            {
                try
                {
                    property.TrySetValue(value);
                }
                catch (Exception e)
                {
                    throw new ArgumentException($"Couldn't copy property {key}", e);
                }
            }
            else
            {
                property.UndefineValue();
            }
        }

        /// <summary>
        /// Callback for when the frame is changed. Updates the output properties
        /// selectively depending on if the field is marked as having changed.
        /// </summary>
        private void OnFrameUpdated(IFrame frame, FrameChanges changes)
        {
            if (frame == null)
                return;

            foreach (var (key, property) in Properties)
            {
                if (changes.HasChanged(key))
                    GetPropertyValueFromFrame(key, property);
            }
        }

        private ITrajectorySnapshot source;

        /// <inheritdoc cref="IFrameConsumer.FrameSource" />
        public ITrajectorySnapshot FrameSource
        {
            get => source;
            set
            {
                if (source != null)
                    source.FrameChanged -= OnFrameUpdated;
                source = value;
                if (source != null)
                {
                    source.FrameChanged += OnFrameUpdated;
                    OnFrameUpdated(source.CurrentFrame, FrameChanges.All);
                }
            }
        }
    }
}