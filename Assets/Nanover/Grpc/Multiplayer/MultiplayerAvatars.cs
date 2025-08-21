using System.Collections.Generic;
using System.Linq;

namespace Nanover.Grpc.Multiplayer
{
    /// <summary>
    /// A collection of multiplayer avatars stored in the shared state.
    /// </summary>
    public class MultiplayerAvatars : MultiplayerCollection<MultiplayerAvatar>
    {
        internal MultiplayerAvatars(MultiplayerSession session) : base(session)
        {
            Multiplayer.MultiplayerJoined += OnMultiplayerJoined;
        }

        private void OnMultiplayerJoined()
        {
            LocalAvatar = new MultiplayerAvatar()
            {
                ID = Multiplayer.AccessToken
            };
        }

        /// <inheritdoc cref="MultiplayerCollection{TItem}.KeyPrefix"/>
        protected override string KeyPrefix => "avatar.";

        /// <inheritdoc cref="MultiplayerCollection{TItem}.ParseItem"/>
        protected override bool ParseItem(string key, object value, out MultiplayerAvatar parsed)
        {
            if (IsMapping(value))
            {
                parsed = Core.Serialization.Serialization.FromDataStructure<MultiplayerAvatar>(value);
                parsed.ID = key.Remove(0, KeyPrefix.Length);
                return true;
            }

            parsed = default;
            return false;
        }

        /// <inheritdoc cref="MultiplayerCollection{TItem}.SerializeItem"/>
        protected override object SerializeItem(MultiplayerAvatar item)
        {
            return Core.Serialization.Serialization.ToDataStructure(item);
        }
        
        /// <summary>
        /// A list of <see cref="MultiplayerAvatar"/> which are not the current player.
        /// </summary>
        public IEnumerable<MultiplayerAvatar> OtherPlayerAvatars =>
            Values.Where(avatar => avatar.ID != Multiplayer.AccessToken);

        /// <summary>
        /// The <see cref="MultiplayerAvatar"/> which is the local player, and hence
        /// not controlled by the shared state dictionary.
        /// </summary>
        public MultiplayerAvatar LocalAvatar = new MultiplayerAvatar();

        private string LocalAvatarId => $"avatar.{Multiplayer.AccessToken}";
        
        /// <summary>
        /// Add your local avatar to the shared state dictionary
        /// </summary>
        public void FlushLocalAvatar()
        {
            UpdateValue(LocalAvatarId, LocalAvatar);
        }

        internal void CloseClient()
        {
            RemoveValue(LocalAvatarId);
        }
    }
}
