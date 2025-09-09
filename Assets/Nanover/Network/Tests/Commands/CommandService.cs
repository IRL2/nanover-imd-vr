using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Nanover.Protocol.Command;

namespace Nanover.Grpc.Tests.Commands
{
    internal class CommandService : Command.CommandBase, IBindableService
    {
        public event Action<string, Struct> ReceivedCommand;

        public override Task<CommandReply> RunCommand(CommandMessage request, ServerCallContext context)
        {
            ReceivedCommand?.Invoke(request.Name, request.Arguments);
            return Task.FromResult(new CommandReply());
        }

        public override Task<GetCommandsReply> GetCommands(GetCommandsRequest request, ServerCallContext context)
        {
            return Task.FromResult(new GetCommandsReply());
        }

        public ServerServiceDefinition BindService()
        {
            return Command.BindService(this);
        }
    }
}