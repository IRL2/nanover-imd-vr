using Nanover.Core;
using Nerdbank.MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using WebSocketTypes;

namespace NanoverImd
{
    public static class NanoverRecordings
    {
        [Serializable]
        public class DemoListing
        {
            public string Name;
            public string URL;
        }

        [Serializable]
        private class Container
        {
            [Serializable]
            public class FileListing
            {
                public string name;
                public string download_url;
            }

            public List<FileListing> listing;
        }

        public static async Task<List<DemoListing>> FetchDemosListing()
        {
            const string suffix = ".nanover.zip";

            var request = UnityWebRequest.Get("https://api.github.com/repos/IRL2/nanover-imd-vr-demo-recordings/contents/");
            var operation = request.SendWebRequest();

            await operation;

            var wrapped = $"{{\"listing\":{operation.webRequest.downloadHandler.text}}}";
            var container = JsonUtility.FromJson<Container>(wrapped);

            return container.listing
                .Where(entry => entry.name.EndsWith(suffix))
                .Select(entry => new DemoListing { Name = entry.name.Replace(suffix, ""), URL = entry.download_url })
                .ToList();
        }

        public static async IAsyncEnumerable<Message> PlaybackOnce(this NanoverRecordingReader reader)
        {
            var prev = default(RecordingIndexEntry);

            foreach (var entry in reader)
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

                yield return reader.GetMessage(entry);
            }
        }
    }

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

        public RecordingIndexEntry this[int index] => indexEntries[index];
        public int Count => indexEntries.Count;
        public IEnumerator<RecordingIndexEntry> GetEnumerator() => indexEntries.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
