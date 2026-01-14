using Cysharp.Threading.Tasks;
using PolyType;
using System;
using System.Collections.Generic;
using System.Linq;
using CommandArguments = System.Collections.Generic.Dictionary<string, object>;
using CommandReturn = System.Collections.Generic.Dictionary<string, object>;
using Nanover.Core;

namespace WebSocketTypes
{ 
    public class RecordingIndexEntry
    {
        [PropertyShape(Name = "offset")]
        public uint Offset;

        [PropertyShape(Name = "length")]
        public uint Length;

        [PropertyShape(Name = "metadata")]
        public Dictionary<string, object> Metadata;

        public uint? Timestamp => Metadata.TryGetValue("timestamp", out var value) ? Convert.ToUInt32(value) : null;

        public bool ContainsFrame => Metadata.TryGetValue("types", out IList<object> types) && types.Contains("frame");
        public bool ContainsState => Metadata.TryGetValue("types", out IList<object> types) && types.Contains("state");

        public override string ToString() => $"RecordingIndexEntry(Offset={Offset}, Length={Length}, Timestamp={Timestamp})";
    }

    public partial class StateUpdate
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
        public CommandUpdate? CommandUpdate;

        public override string ToString() => $"Message(FrameUpdate={FrameUpdate}, StateUpdate={StateUpdate}, CommandUpdate={CommandUpdate})";
    }

    public interface WebSocketMessageSource
    {
        bool Connected { get; }

        UniTask<CommandReturn> RunCommand(string name, CommandArguments args = null);

        event Action<Message> OnMessage;
    }

    [GenerateShapeFor(typeof(byte[]))]
    [GenerateShapeFor(typeof(byte[][]))]
    [GenerateShapeFor(typeof(object[]))]
    [GenerateShapeFor(typeof(HashSet<string>))]
    [GenerateShapeFor(typeof(List<object>))]
    [GenerateShapeFor(typeof(Message))]
    [GenerateShapeFor(typeof(RecordingIndexEntry))]
    [GenerateShapeFor(typeof(List<RecordingIndexEntry>))]
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