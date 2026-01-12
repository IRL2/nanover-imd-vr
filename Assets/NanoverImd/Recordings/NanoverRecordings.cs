using Nanover.Core;
using Nerdbank.MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Analytics;
using WebSocketTypes;

namespace NanoverImd
{
    public class NanoverRecordingReader : IReadOnlyList<RecordingIndexEntry>
    {
        private readonly string indexPath;
        private readonly string messagesPath;

        private readonly MessagePackSerializer serializer;
        private readonly List<RecordingIndexEntry> indexEntries;

        public NanoverRecordingReader(string indexPath, string messagesPath)
        {
            this.indexPath = indexPath;
            this.messagesPath = messagesPath;

            serializer = new MessagePackSerializer().WithDynamicObjectConverter();

            var bytes = File.ReadAllBytes(indexPath);
            indexEntries = serializer.Deserialize<List<RecordingIndexEntry>>(bytes, Witness.ShapeProvider)!;
        }

        public Message GetMessage(RecordingIndexEntry entry)
        {
            var bytes = new byte[entry.Length];

            using (var stream = File.OpenRead(messagesPath))
            {
                stream.Seek(entry.Offset, SeekOrigin.Begin);
                stream.Read(bytes);

                return serializer.Deserialize<Message>(bytes, Witness.ShapeProvider)!; 
            }
        }

        public async IAsyncEnumerable<Message> PlaybackOnce()
        {
            var prev = default(RecordingIndexEntry);

            foreach (var entry in this)
            {
                if (prev?.Timestamp is { } prevTimestamp && entry.Timestamp is { } nextTimestamp)
                {
                    var delay = nextTimestamp - prevTimestamp;
                    await Task.Delay(Convert.ToInt32(delay/1000));
                }

                prev = entry;

                if (!entry.Metadata.TryGetValue("types", out IList<object> types)
                || (!types.Contains("frame") && !types.Contains("state")))
                    continue;

                yield return GetMessage(entry);
            }
        }

        public RecordingIndexEntry this[int index] => indexEntries[index];
        public int Count => indexEntries.Count;
        public IEnumerator<RecordingIndexEntry> GetEnumerator() => indexEntries.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
