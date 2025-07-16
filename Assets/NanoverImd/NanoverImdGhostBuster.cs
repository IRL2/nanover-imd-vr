using UnityEngine;
using System.Collections.Generic;

namespace NanoverImd
{
    [DisallowMultipleComponent]
    public sealed class NanoverImdGhostBuster : MonoBehaviour
    {
        private static readonly string[] prefixes = 
        {
            "update.index",
            "avatar",
            "playarea",
        };

#pragma warning disable 0649
        [SerializeField]
        private NanoverImdSimulation simulation;
        [SerializeField]
        private float timeout = 10f;
#pragma warning restore 0649

        private Dictionary<string, float> userIdLastSeen = new Dictionary<string, float>();

        private void Start()
        {
            simulation.Multiplayer.SharedStateDictionaryKeyUpdated += OnKeyUpdate;
        }

        private void Update()
        {
            var now = Time.realtimeSinceStartup;
            var removes = new HashSet<string>();

            foreach (var (id, time) in userIdLastSeen)
            {
                if (now - time > timeout)
                {
                    ClearUser(id);
                    removes.Add(id);
                }
            }

            foreach (var key in simulation.Interactions.Keys)
            {
                object owner;
                var interaction = simulation.Interactions.GetValue(key);
                if (interaction.Other.TryGetValue("owner.id", out owner) && owner is string owner_)
                {
                    if (!userIdLastSeen.ContainsKey(owner_))
                    {
                        userIdLastSeen.Add(owner_, Time.realtimeSinceStartup);
                    }
                }
            }

            foreach (var id in removes)
            {
                userIdLastSeen.Remove(id);
            }
        }

        private void ClearUser(string id)
        {    
            foreach (var prefix in prefixes)
            {
                simulation.Multiplayer.RemoveSharedStateKey($"{prefix}.{id}");
            }


            foreach (var key in simulation.Interactions.Keys)
            {
                object owner;
                var interaction = simulation.Interactions.GetValue(key);
                if (interaction.Other.TryGetValue("owner.id", out owner) && (owner as string) == id)
                {
                    simulation.Interactions.RemoveValue(key);
                }
            }
        }

        private void OnKeyUpdate(string key, object value) {
            if (key.StartsWith("update.index."))
            {
                var id = key.Substring("update.index.".Length);
                userIdLastSeen[id] = Time.realtimeSinceStartup;
            }
        }
    }
}