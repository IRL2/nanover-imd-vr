using Cysharp.Threading.Tasks;
using PolyType;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CommandArguments = System.Collections.Generic.Dictionary<string, object>;
using CommandReturn = System.Collections.Generic.Dictionary<string, object>;

namespace WebSocketTypes
{ 
    public class StateUpdate
    {
        [PropertyShape(Name = "updates")]
        public Dictionary<string, object> Updates = new Dictionary<string, object>();

        [PropertyShape(Name = "removals")]
        public HashSet<string> Removals = new HashSet<string>();
    }

    public partial class CommandRequest
    {
        [PropertyShape(Name = "id")]
        public int Id;

        [PropertyShape(Name = "name")]
        public string? Name;

        [PropertyShape(Name = "arguments")]
        public CommandArguments? Arguments;
    }

    public partial class CommandUpdate
    {
        [PropertyShape(Name = "request")]
        public CommandRequest Request;

        [PropertyShape(Name = "response")]
        public CommandReturn Response;
    }

    public partial class Message
    {
        [PropertyShape(Name = "frame")]
        public Dictionary<string, object>? FrameUpdate;

        [PropertyShape(Name = "state")]
        public StateUpdate? StateUpdate;

        [PropertyShape(Name = "command")]
        public List<CommandUpdate>? CommandUpdates;
    }

    public interface WebSocketMessageSource
    {
        UniTask<CommandReturn> RunCommand(string name, CommandArguments args = null);

        event Action<Message> OnMessage;
    }

    [GenerateShapeFor(typeof(byte[]))]
    [GenerateShapeFor(typeof(byte[][]))]
    [GenerateShapeFor(typeof(object[]))]
    [GenerateShapeFor(typeof(HashSet<string>))]
    [GenerateShapeFor(typeof(List<object>))]
    [GenerateShapeFor(typeof(Message))]
    public partial class Witness { }

    public static class ObjectExtensions
    {
        public static object StringifyStructureKeys(this object structure)
        {
            if (structure is IDictionary<object, object> dict)
                return dict.ToDictionary(pair => pair.Key.ToString(), pair => StringifyStructureKeys(pair.Value));
            if (structure is IDictionary<string, object> dict2)
                return dict2.ToDictionary(pair => pair.Key, pair => StringifyStructureKeys(pair.Value));
            return structure;
        }
    }
}