namespace Nanover.Network.Multiplayer
{
    /// <summary>
    /// The state of a lock on a multiplayer resource.
    /// </summary>
    public enum MultiplayerResourceLockState
    {
        /// <summary>
        /// The state of the resource is not known, and shouldn't be altered.
        /// </summary>
        Unlocked,
        /// <summary>
        /// A request to obtain a lock has been sent, and we are awaiting the result.
        /// </summary>
        Pending,
        /// <summary>
        /// The lock has been accepted and we have a lock on the object.
        /// </summary>
        Locked
    }
}