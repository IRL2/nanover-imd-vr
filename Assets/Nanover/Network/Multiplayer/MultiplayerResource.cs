using System;
using Cysharp.Threading.Tasks;

namespace Nanover.Network.Multiplayer
{
    /// <summary>
    /// Represents a multiplayer resource that is shared across the multiplayer
    /// service.
    /// </summary>
    /// <typeparam name="TValue">The type to interpret the value of this key as.</typeparam>
    public class MultiplayerResource<TValue>
    {
        /// <summary>
        /// Is the current value a local value that is pending being sent to the server.
        /// </summary>
        private bool localValuePending = false;
        
        /// <summary>
        /// Create a multiplayer resource.
        /// </summary>
        /// <param name="session">The multiplayer session that will provide this value.</param>
        /// <param name="key">The key that identifies this resource in the dictionary.</param>
        /// <param name="objectToValue">
        /// An optional converter for converting the value in
        /// the dictionary to an appropriate value.
        /// </param>
        /// <param name="valueToObject">
        /// An optional converter for converting the value
        /// provided to one suitable for serialisation to protobuf.
        /// </param>
        public MultiplayerResource(MultiplayerSession session,
                                   string key,
                                   Converter<object, TValue> objectToValue = null,
                                   Converter<TValue, object> valueToObject = null)
        {
            this.session = session;
            ResourceKey = key;
            LockState = MultiplayerResourceLockState.Unlocked;
            session.SharedStateDictionaryKeyUpdated += SharedStateDictionaryKeyUpdated;
            this.objectToValue = objectToValue;
            this.valueToObject = valueToObject;
            CopyRemoteValueToLocal();
        }

        private readonly Converter<object, TValue> objectToValue;

        private readonly Converter<TValue, object> valueToObject;

        /// <summary>
        /// Convert the value provided to one suitable for serialisation to protobuf.
        /// </summary>
        private object ValueToObject(TValue value)
        {
            return valueToObject != null ? valueToObject(value) : value;
        }

        /// <summary>
        /// Convert the value in the dictionary to an appropriate value.
        /// </summary>
        private TValue ObjectToValue(object obj)
        {
            if (objectToValue != null)
                return objectToValue(obj);
            if (obj is TValue v)
                return v;
            return default;
        }

        /// <summary>
        /// Callback for when the shared value is changed.
        /// </summary>
        public event Action RemoteValueChanged;

        /// <summary>
        /// Callback for when the value is changed, either remotely or locally.
        /// </summary>
        public event Action ValueChanged;

        /// <summary>
        /// Callback for when a lock request is accepted.
        /// </summary>
        public event Action LockAccepted;

        /// <summary>
        /// Callback for when a lock request is rejected.
        /// </summary>
        public event Action LockRejected;

        /// <summary>
        /// Callback for when a lock is released.
        /// </summary>
        public event Action LockReleased;

        private void SharedStateDictionaryKeyUpdated(string key, object value)
        {
            if (key == ResourceKey)
            {
                CopyRemoteValueToLocal();
                RemoteValueChanged?.Invoke();
            }
        }

        private TValue GetRemoteValue()
        {
            return ObjectToValue(session.GetSharedState(ResourceKey));
        }

        /// <summary>
        /// Copy the locally set version of this value to the multiplayer service.
        /// </summary>
        private void CopyLocalValueToRemote()
        {
            sentUpdateIndex = session.NextUpdateIndex;
            session.SetSharedState(ResourceKey, ValueToObject(value));
        }
        
        private TValue value;

        /// <summary>
        /// The key which identifies this resource.
        /// </summary>
        public readonly string ResourceKey;

        /// <summary>
        /// The current state of the lock on this resource.
        /// </summary>
        public MultiplayerResourceLockState LockState { get; private set; }

        private MultiplayerSession session;

        private int sentUpdateIndex = -1;

        /// <summary>
        /// Value of this resource. Mirrors the value in the remote dictionary, unless a
        /// local change is in progress.
        /// </summary>
        public TValue Value => value;

        /// <summary>
        /// Obtain a lock on this resource.
        /// </summary>
        public void ObtainLock()
        {
            ObtainLockAsync().Forget();
        }

        /// <summary>
        /// Release the lock on this resource.
        /// </summary>
        public void ReleaseLock()
        {
            CopyRemoteValueToLocal();
            ReleaseLockAsync().Forget();
        }

        private async UniTask ObtainLockAsync()
        {
            LockState = MultiplayerResourceLockState.Pending;
            var success = await session.LockResource(ResourceKey);
            LockState = success
                            ? MultiplayerResourceLockState.Locked
                            : MultiplayerResourceLockState.Unlocked;
            if (success)
                OnLockAccepted();
            else
                OnLockRejected();
        }

        private void OnLockAccepted()
        {
            if (localValuePending)
            {
                localValuePending = false;
                CopyLocalValueToRemote();
            }
            LockAccepted?.Invoke();
        }

        private void OnLockRejected()
        {
            localValuePending = false;
            sentUpdateIndex = -1;
            CopyRemoteValueToLocal();
            LockRejected?.Invoke();
        }

        private async UniTask ReleaseLockAsync()
        {
            if (LockState != MultiplayerResourceLockState.Unlocked)
            {
                localValuePending = false;
                LockState = MultiplayerResourceLockState.Unlocked;
                CopyRemoteValueToLocal();
                LockReleased?.Invoke();
                await session.ReleaseResource(ResourceKey);
            }
        }

        /// <summary>
        /// Set the local value of this key, and try to lock this resource to send it to
        /// everyone.
        /// If it is rejected, the value will revert to the default.
        /// </summary>
        public void UpdateValueWithLock(TValue value)
        {
            SetLocalValue(value);
            switch (LockState)
            {
                case MultiplayerResourceLockState.Unlocked:
                    ObtainLock();
                    return;
                case MultiplayerResourceLockState.Pending:
                    return;
                case MultiplayerResourceLockState.Locked:
                    CopyLocalValueToRemote();
                    return;
            }
        }

        private void SetLocalValue(TValue value)
        {
            this.value = value;
            localValuePending = true;
            ValueChanged?.Invoke();
        }
        
        /// <summary>
        /// Copy the remote value to this value.
        /// </summary>
        private void CopyRemoteValueToLocal()
        {
            if (!session.IsOpen)
                return;
            // If we don't have a local change, and we are up to date with the server
            if (!localValuePending && sentUpdateIndex <= session.LastReceivedIndex)
            {
                value = GetRemoteValue();
                ValueChanged?.Invoke();
            }
        }

    }
}