using System.Threading.Tasks;
using Grpc.Core;
using JetBrains.Annotations;
using Nanover.Protocol.Trajectory;

namespace Nanover.Grpc.Tests.Trajectory
{
    /// <summary>
    /// Simple implementation of a TrajectoryService that returns a single provided
    /// FrameData when SubscribeLatestFrames is called
    /// </summary>
    internal class QueueTrajectoryService : TrajectoryService.TrajectoryServiceBase,
                                            IBindableService
    {
        private readonly FrameData[] frameData;

        public int Delay { get; set; }

        public QueueTrajectoryService(params FrameData[] data)
        {
            frameData = data;
        }

        public override async Task SubscribeLatestFrames(GetFrameRequest request,
                                                         [NotNull]
                                                         IServerStreamWriter<GetFrameResponse>
                                                             responseStream,
                                                         ServerCallContext context)
        {
            var i = 0u;
            foreach (var frame in frameData)
            {
                if (i > 0u && Delay > 0)
                    await Task.Delay(Delay);
                await responseStream.WriteAsync(new GetFrameResponse
                                                    { FrameIndex = i++, Frame = frame });
            }
        }

        public ServerServiceDefinition BindService()
        {
            return TrajectoryService.BindService(this);
        }
    }
}