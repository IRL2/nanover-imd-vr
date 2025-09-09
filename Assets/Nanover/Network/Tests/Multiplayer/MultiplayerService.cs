using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Nanover.Core;
using Nanover.Core.Collections;
using Nanover.Protocol.State;
using UnityEngine;

namespace Nanover.Grpc.Tests.Multiplayer
{
    public class MultiplayerService : State.StateBase, IBindableService
    {
        private ObservableDictionary<string, Value> resources
            = new ObservableDictionary<string, Value>();

        private Dictionary<string, string> locks = new Dictionary<string, string>();

        public IDictionary<string, Value> Resources => resources;

        public IReadOnlyDictionary<string, string> Locks => locks;

        public override Task<UpdateLocksResponse> UpdateLocks(
            UpdateLocksRequest request,
            ServerCallContext context)
        {
            var token = request.AccessToken;
            Debug.Log($"Lock with {token}");
            
            foreach (var requestKey in request.LockKeys.Fields.Keys)
                if (locks.ContainsKey(requestKey) && locks[requestKey] != token)
                    return Task.FromResult(new UpdateLocksResponse
                    {
                        Success = false
                    });
            foreach (var (key, lockTime) in request.LockKeys.Fields)
            {
                if (lockTime.KindCase == Value.KindOneofCase.NullValue)
                {
                    locks.Remove(key);
                }
                else
                {
                    locks[key] = token;
                }
            }

            return Task.FromResult(new UpdateLocksResponse
            {
                Success = true
            });
        }

        public override Task<UpdateStateResponse> UpdateState(
            UpdateStateRequest request,
            ServerCallContext context)
        {
            var token = request.AccessToken;
            foreach (var requestKey in request.Update.ChangedKeys.Fields.Keys)
                if (locks.ContainsKey(requestKey) && locks[requestKey] != token)
                    return Task.FromResult(new UpdateStateResponse
                    {
                        Success = false
                    });
            foreach (var (key, value) in request.Update.ChangedKeys.Fields)
            {
                if (value.KindCase == Value.KindOneofCase.NullValue)
                {
                    resources.Remove(key);
                }
                else
                {
                    resources[key] = value;
                }
            }

            return Task.FromResult(new UpdateStateResponse
            {
                Success = true
            });
        }

        public override async Task SubscribeStateUpdates(SubscribeStateUpdatesRequest request,
                                                         IServerStreamWriter<StateUpdate>
                                                             responseStream,
                                                         ServerCallContext context)
        {
            var millisecondTiming = (int) (request.UpdateInterval * 1000);
            var update = new StateUpdate
            {
                ChangedKeys = new Struct()
            };

            void ResourcesOnCollectionChanged(object sender,
                                              NotifyCollectionChangedEventArgs e)
            {
                var (changes, removals) = e.AsChangesAndRemovals<string>();

                foreach (var change in changes)
                    update.ChangedKeys.Fields[change] = resources[change];
                foreach (var removal in removals)
                    update.ChangedKeys.Fields[removal] = Value.ForNull();
            }

            resources.CollectionChanged += ResourcesOnCollectionChanged;
            while (true)
            {
                await Task.Delay(millisecondTiming);
                if (update.ChangedKeys.Fields.Any())
                {
                    var toSend = update;
                    update = new StateUpdate
                    {
                        ChangedKeys = new Struct()
                    };
                    await responseStream.WriteAsync(toSend);
                }
            }
        }

        public ServerServiceDefinition BindService()
        {
            return State.BindService(this);
        }

        public void SetValueDirect(string key, object value)
        {
            this.resources[key] = value.ToProtobufValue();
        }
        
        public void RemoveValueDirect(string key)
        {
            this.resources.Remove(key);
        }
    }
}