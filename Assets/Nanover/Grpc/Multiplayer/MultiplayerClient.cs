using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using JetBrains.Annotations;
using Nanover.Grpc.Stream;
using Nanover.Protocol.State;
using Value = Google.Protobuf.WellKnownTypes.Value;

namespace Nanover.Grpc.Multiplayer
{
    /// <summary>
    /// Wraps a <see cref="Multiplayer.MultiplayerClient" /> and
    /// provides access to avatars and the shared key/value store on the server
    /// over a <see cref="GrpcConnection" />.
    /// </summary>
    public class MultiplayerClient :
        GrpcClient<State.StateClient>
    {
        // Chosen as an acceptable minimum rate that should ideally be 
        // explicitly increased.
        private const float DefaultUpdateInterval = 1f / 30f;

        public MultiplayerClient([NotNull] GrpcConnection connection) : base(connection)
        {
        }
        
        /// <summary>
        /// Starts an <see cref="IncomingStream{StateUpdate}" /> on 
        /// which the server provides updates to the shared key/value store at 
        /// the requested time interval (in seconds).
        /// </summary>
        /// <remarks>
        /// Corresponds to the SubscribeStateUpdates gRPC call.
        /// </remarks>
        public IncomingStream<StateUpdate> SubscribeStateUpdates(float updateInterval = DefaultUpdateInterval)
        {
            var request = new SubscribeStateUpdatesRequest
            {
                UpdateInterval = updateInterval,
            };

            return GetIncomingStream(Client.SubscribeStateUpdates, request);
        }
        
        public async UniTask<bool> UpdateState(string token, Dictionary<string, object> updates, List<string> removals)
        {
            var request = new UpdateStateRequest()
            {
                AccessToken = token,
                Update = CreateStateUpdate(updates, removals)
            };

            var response = await Client.UpdateStateAsync(request, cancellationToken: GetCancellationToken());

            return response.Success;
        }
        
        public async UniTask<bool> UpdateLocks(string token, IDictionary<string, float> toAcquire, IEnumerable<string> toRemove)
        {
            var request = new UpdateLocksRequest
            {
                AccessToken = token,
                LockKeys = CreateLockUpdate(toAcquire, toRemove)
            };

            var response = await Client.UpdateLocksAsync(request, cancellationToken: GetCancellationToken());

            return response.Success;
        }

        private Struct CreateLockUpdate(IDictionary<string, float> toAcquire, IEnumerable<string> toRelease)
        {
            var str = toAcquire.ToProtobufStruct();
            foreach(var releasedkey in toRelease)
                str.Fields[releasedkey] = Value.ForNull();
            return str;
        }

        private static StateUpdate CreateStateUpdate(IDictionary<string, object> updates, IEnumerable<string> removals)
        {
            var update = new StateUpdate();
            var updatesAsStruct = updates.ToProtobufStruct();
            update.ChangedKeys = updatesAsStruct;
            foreach (var removal in removals)
                update.ChangedKeys.Fields[removal] = Value.ForNull();
            return update;
        }
    }
}
