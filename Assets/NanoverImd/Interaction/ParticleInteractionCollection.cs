using System.Collections.Generic;
using Nanover.Core.Serialization;
using Nanover.Grpc.Multiplayer;

namespace NanoverImd.Interaction
{
    /// <summary>
    /// A collection of interactions involved in iMD.
    /// </summary>
    public class ParticleInteractionCollection : MultiplayerCollection<ParticleInteraction>
    {
        public ParticleInteractionCollection(MultiplayerSession session) : base(session)
        {
        }

        /// <inheritdoc cref="MultiplayerCollection{TItem}.KeyPrefix"/>
        protected override string KeyPrefix => "interaction.";
        
        /// <inheritdoc cref="MultiplayerCollection{TItem}.ParseItem"/>
        protected override bool ParseItem(string key, object value, out ParticleInteraction parsed)
        {
            if (value is Dictionary<string, object> dict)
            {
                parsed = Serialization.FromDataStructure<ParticleInteraction>(dict);
                return true;
            }

            parsed = default;
            return false;
        }
        
        /// <inheritdoc cref="MultiplayerCollection{TItem}.SerializeItem"/>
        protected override object SerializeItem(ParticleInteraction item)
        {
            return Serialization.ToDataStructure(item);
        }
    }
}