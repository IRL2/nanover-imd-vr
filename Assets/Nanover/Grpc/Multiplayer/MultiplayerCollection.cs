using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nanover.Grpc.Multiplayer
{
    /// <summary>
    /// A collection of keys in the shared state dictionary with a given prefix, which are parsed
    /// into objects of type <typeparamref name="TItem"/>. It automatically manages local changes
    /// and removals, so they are reflected immediately and remain until a more recent update is
    /// received from the server.
    /// </summary>
    /// <typeparam name="TItem">The type to convert keys of this type into.</typeparam>
    public abstract class MultiplayerCollection<TItem>
    {
        public static bool IsMapping(object @object)
        {
            return @object is IDictionary<string, object> || @object is IDictionary<object, object>;
        }

        /// <summary>
        /// Callback for when a key is updated in the collection, either remotely or by the
        /// client.
        /// </summary>
        /// <remarks>
        /// Updating a key using <see cref="UpdateValue(string)"/> invokes this event when
        /// both called initially and when the updated value is received from the server.
        /// This is required behaviour, as the server could have modified the key it was
        /// sent.
        /// </remarks>
        public event Action<string> KeyUpdated;
        
        /// <summary>
        /// Callback for when a key is removed in the collection, either remotely or by the
        /// client.
        /// </summary>
        /// <remarks>
        /// Removing a key using <see cref="RemoveValue(string)"/> invokes this event when
        /// both called initially and when the act of removal is received from the server.
        /// </remarks>
        public event Action<string> KeyRemoved;

        /// <summary>
        /// The prefix that marks a key as belonging to this collection.
        /// </summary>
        protected abstract string KeyPrefix { get; }

        /// <summary>
        /// The <see cref="MultiplayerSession"/> this collection reflects.
        /// </summary>
        protected MultiplayerSession Multiplayer { get; }
        
        /// <summary>
        /// Parse a received item into the correct type for this collection.
        /// </summary>
        /// <param name="key">The key of the item.</param>
        /// <param name="value">The value as a C# object, consisting of dictionaries,
        /// lists and primitive values.</param>
        /// <param name="parsed">The parsed item, or the default if parsing was successful</param>
        /// <returns>Was the parse successful? If not, the key is ignored, and is removed
        /// if it already existed.</returns>
        protected abstract bool ParseItem(string key, object value, out TItem parsed);

        /// <summary>
        /// Convert an item to a C# form of lists, dictionaries and primitives, which can be
        /// serialized.
        /// </summary>
        /// <param name="item">The item to serialize.</param>
        /// <returns>A C# object consisting of lists, dictionaries and primitives.</returns>
        protected abstract object SerializeItem(TItem item);
        
        /// <summary>
        /// Create a collection based upon the given <see cref="MultiplayerSession"/>.
        /// </summary>
        /// <param name="session">The session which allows reading and writing of the
        /// shared state.</param>
        protected MultiplayerCollection(MultiplayerSession session)
        {
            Multiplayer = session;
            Multiplayer.SharedStateDictionaryKeyUpdated += OnKeyUpdated;
            Multiplayer.SharedStateDictionaryKeyRemoved += OnKeyRemoved;
        }

        /// <summary>
        /// Get the current value for a given key. If a local change has been made, this changed
        /// value is returned until an up-to-date server reply is received. Otherwise, the latest
        /// value received from the server is returned.
        /// </summary>
        /// <param name="key">The key in the shared state.</param>
        /// <returns>The latest modified value or lastest server value, whichever is
        /// more recent.</returns>
        /// <exception cref="KeyNotFoundException">The key is not in the collection.</exception>
        public TItem GetValue(string key)
        {
            key = ValidateKey(key);
            if (localRemovals.ContainsKey(key))
                throw new KeyNotFoundException($"Key removed: {key}");
            if (localChanges.ContainsKey(key))
                return localChanges[key].Item2;
            return multiplayerState[key];
        }

        /// <summary>
        /// Collection of all keys currently in the collection, taking into account local
        /// changes and removals which may not have been acknowledged by the server yet.
        /// </summary>
        public IReadOnlyCollection<string> Keys
        {
            get
            {
                var keys = new HashSet<string>();
                foreach (var key in localChanges.Keys)
                    keys.Add(key);
                foreach (var key in multiplayerState.Keys)
                    keys.Add(key);
                foreach (var key in localRemovals.Keys)
                    keys.Remove(key);
                return keys;
            }
        }
        
        /// <summary>
        /// Collection of all values currently in the collection, taking into account local
        /// changes and removals which may not have been acknowledged by the server yet.
        /// </summary>
        public IEnumerable<TItem> Values
        {
            get
            {
                foreach (var key in Keys)
                    yield return GetValue(key);
            }
        }
        
        /// <summary>
        /// Update a value with the given key using the provided value.
        /// </summary>
        /// <param name="key">The key to update.</param>
        /// <param name="value">The value to add to the shared state under the given key.</param>
        public void UpdateValue(string key, TItem value)
        {
            key = ValidateKey(key);
            if (localRemovals.ContainsKey(key))
                localRemovals.Remove(key);
            localChanges[key] = (Multiplayer.NextUpdateIndex, value);
            Multiplayer.SetSharedState(key, SerializeItem(value));
            KeyUpdated?.Invoke(key);
        }
        
        /// <summary>
        /// Remove a value with the given key from the shared state dictionary.
        /// </summary>
        /// <param name="key">The key to remove from the dictionary.</param>
        public void RemoveValue(string key)
        {
            key = ValidateKey(key);
            if (!multiplayerState.ContainsKey(key) && !localChanges.ContainsKey(key))
                return;
            if (localChanges.ContainsKey(key))
                localChanges.Remove(key);
            localRemovals[key] = Multiplayer.NextUpdateIndex;
            Multiplayer.RemoveSharedStateKey(key);
            KeyRemoved?.Invoke(key);
        }

        /// <summary>
        /// Does this collection contain the given key, taking into account local
        /// changes and removals which may not have been acknowledged by the server yet.
        /// </summary>
        public bool ContainsKey(string key)
        {
            return Keys.Contains(key);
        }

        private Dictionary<string, TItem> multiplayerState = new Dictionary<string, TItem>();
        
        private Dictionary<string, (int, TItem)> localChanges = new Dictionary<string, (int, TItem)>();
        
        private Dictionary<string, int> localRemovals = new Dictionary<string, int>();

        private void OnKeyUpdated(string key, object value)
        {
            if (key.StartsWith(KeyPrefix))
            {
                if(ParseItem(key, value, out var item))
                    CreateOrUpdateItemFromServer(key, item);
                else
                    RemoveItemFromServer(key);
            }
        }

        private void OnKeyRemoved(string key)
        {
            if (key.StartsWith(KeyPrefix))
            {
                RemoveItemFromServer(key);
            }
        }
        
        private void CreateOrUpdateItemFromServer(string key, TItem value)
        {
            if (!multiplayerState.ContainsKey(key))
                multiplayerState.Add(key, value);
            else
                multiplayerState[key] = value;
            if (localChanges.ContainsKey(key))
            {
                var sentUpdateIndex = localChanges[key].Item1;
                if (sentUpdateIndex <= Multiplayer.LastReceivedIndex)
                {
                    localChanges.Remove(key);
                    KeyUpdated?.Invoke(key);
                }
            }
            else
            {
                KeyUpdated?.Invoke(key);
            }
        }

        private void RemoveItemFromServer(string key)
        {
            multiplayerState.Remove(key);
            if (!localChanges.ContainsKey(key))
            {
                KeyRemoved?.Invoke(key);
            }
            localRemovals.Remove(key);
        }

        private string ValidateKey(string key)
        {
            if (!key.StartsWith(KeyPrefix))
                return KeyPrefix + key;
            return key;
        }
    }
}