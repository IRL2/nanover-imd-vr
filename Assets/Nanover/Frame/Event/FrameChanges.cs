using System.Collections.Generic;

namespace Nanover.Frame.Event
{
    /// <summary>
    /// A record of known changes to a Nanover frame. Everything is assumed
    /// unchanged unless explicitly set.
    /// </summary>
    public class FrameChanges
    {
        private bool haveAllChanged = false;
        
        private readonly HashSet<string> changed  = new HashSet<string>();

        private FrameChanges()
        {
            
        }
        
        /// <summary>
        /// A <see cref="FrameChanges"/> where no key is marked as having changed.
        /// </summary>
        public static FrameChanges None => new FrameChanges()
        {
            haveAllChanged = false
        };

        /// <summary>
        /// A <see cref="FrameChanges"/> where all keys are marked as having changed.
        /// </summary>
        public static FrameChanges All => new FrameChanges
        {
            haveAllChanged = true
        };

        /// <summary>
        /// A <see cref="FrameChanges"/> where only the provided keys are marked as having changed.
        /// </summary>
        public static FrameChanges WithChanges(params string[] keys)
        {
            var changes = None;
            foreach (var key in keys)
                changes.MarkAsChanged(key);
            return changes;
        }

        /// <summary>
        /// Indicates whether any keys have been changed in comparison to a
        /// previous frame.
        /// </summary>
        public bool HasAnythingChanged => haveAllChanged || changed.Count > 0;

        /// <summary>
        /// Merge another <see cref="FrameChanges" />, such that the update
        /// state reflects the combination of the two.
        /// </summary>
        public void MergeChanges(FrameChanges otherChanges)
        {
            haveAllChanged = this.haveAllChanged || otherChanges.haveAllChanged;
            changed.UnionWith(otherChanges.changed);
        }

        /// <summary>
        /// Check if the field with the given id as having been changed from the previous
        /// frame.
        /// </summary>
        public bool HasChanged(string id)
        {
            return haveAllChanged || changed.Contains(id);
        }

        /// <summary>
        /// Mark the field with the given id as having been changed from the previous
        /// frame.
        /// </summary>
        public void MarkAsChanged(string id)
        {
            changed.Add(id);
        }

        public override string ToString() => 
            haveAllChanged ? "FrameChanges.All" : 
            changed.Count == 0 ? "FrameChanges.None" :
            $"FrameChanges.WithChanges({string.Join(", ", changed)})";
    }
}