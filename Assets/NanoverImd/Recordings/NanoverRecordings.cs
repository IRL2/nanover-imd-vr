using Nanover.Core;
using Nerdbank.MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Analytics;
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

        public static async Task<NanoverRecordingReader> LoadDemo(string url)
        {
            var dest = Path.Combine(Application.persistentDataPath, "demo.nanover.zip");
            var index = Path.Combine(Application.persistentDataPath, "index.msgpack");
            var messages = Path.Combine(Application.persistentDataPath, "messages.msgpack");

            var request = UnityWebRequest.Get(url);
            var handler = new DownloadHandlerFile(dest);
            handler.removeFileOnAbort = true;
            request.downloadHandler = handler;

            await request.SendWebRequest();

            ZipFile.ExtractToDirectory(dest, Application.persistentDataPath, overwriteFiles: true);

            return new NanoverRecordingReader(index, messages);
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

    public class NanoverRecordingPlayback
    {
        const float SecondsToMicroseconds = 1000f * 1000f;
        const float MicrosecondsToSeconds = 1 / SecondsToMicroseconds;

        public NanoverRecordingReader Reader { get; }
        public bool IsPaused { get; private set; }
        public float PlaybackTime { get; private set; }
        public event Action PlaybackReset;
        public event Action<Message> PlaybackMessage;

        public float FirstTime { get; }
        public float LastTime { get; }
        public float Duration => LastTime - FirstTime;

        private int prevEntryIndex = -1;

        public NanoverRecordingPlayback(NanoverRecordingReader reader)
        {
            Reader = reader;

            FirstTime = (reader[0].Timestamp ?? 0) * MicrosecondsToSeconds;
            LastTime = (reader[^1].Timestamp ?? 0) * MicrosecondsToSeconds;
        }

        public void Play() => IsPaused = false;
        public void Pause() => IsPaused = true;
        public void AdvanceBySeconds(float seconds) => SeekToSeconds(PlaybackTime + seconds);

        public void Reset()
        {
            prevEntryIndex = -1;
            StepOneFrame();
        }

        public void SeekToSeconds(float seconds)
        {
            var prev = PlaybackTime;
            var next = seconds % Duration;

            if (next < PlaybackTime)
            {
                Reset();
                SeekToSeconds(seconds);
            }
            else
            {
                for (int i = prevEntryIndex + 1; i < Reader.Count; i++)
                {
                    var time = Reader[i].Timestamp * MicrosecondsToSeconds;
                    if (time < next)
                        StepOneEntry();
                }

                PlaybackTime = next;
            }
        }

        public RecordingIndexEntry StepOneEntry()
        {
            var index = (prevEntryIndex + 1) % Reader.Count;
            var entry = Reader[index];

            PlaybackTime = (entry.Timestamp ?? 0) * MicrosecondsToSeconds;

            if (index == 0)
                PlaybackReset?.Invoke();

            if (entry.ContainsFrame || entry.ContainsState)
            {
                var message = Reader.GetMessage(entry);
                PlaybackMessage?.Invoke(message);
            }

            prevEntryIndex = index;

            return entry;
        }

        public void StepOneFrame()
        {
            for (int i = 0; i < Reader.Count; ++i)
            {
                if (StepOneEntry().ContainsFrame)
                    break;
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
