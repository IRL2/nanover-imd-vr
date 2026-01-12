using Nanover.Frontend.UI;
using Nerdbank.MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Networking;
using WebSocketTypes;

namespace NanoverImd.UI.Scene
{
    public class ListDemos : MonoBehaviour
    {
        [Serializable]
        private class DemoListing
        {
            public string Name;
            public string URL;
        }

        [SerializeField]
        private List<DemoListing> demos;

        [SerializeField]
        private NanoverImdApplication application;

        [SerializeField]
        private DynamicMenu menu;

        [SerializeField]
        private Sprite demoIcon;

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            RefreshDemos();
        }

        [ContextMenu("Load First Demo")]
        public void LoadFirstDemo()
        {
            LoadDemo(demos[0].URL);
        }

        public void LoadDemo(string url)
        {
            var dest = Path.Combine(Application.persistentDataPath, "demo.nanover.zip");
            var index = Path.Combine(Application.persistentDataPath, "index.msgpack");
            var messages = Path.Combine(Application.persistentDataPath, "messages.msgpack");

            var request = UnityWebRequest.Get(url);
            var handler = new DownloadHandlerFile(dest);
            handler.removeFileOnAbort = true;
            request.downloadHandler = handler;

            var operation = request.SendWebRequest();

            operation.completed += (_) =>
            {
                Debug.LogError(request.downloadedBytes);
                Debug.LogError(dest);

                ZipFile.ExtractToDirectory(dest, Application.persistentDataPath, overwriteFiles: true);

                var reader = new NanoverRecordingReader(index, messages);
                application.Simulation.ConnectRecordingReader(reader);
            };
        }

        public void RefreshDemos()
        {
            menu.ClearChildren();
            foreach (var demo in demos)
            {
                menu.AddItem(demo.Name, demoIcon, () => LoadDemo(demo.URL));
            }
        }
    }
}