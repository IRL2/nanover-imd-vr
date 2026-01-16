using Cysharp.Threading.Tasks;
using Essd;
using Nanover.Frontend.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using WebDiscovery;

namespace NanoverImd.UI.Scene
{
    public class DiscoverServers : MonoBehaviour
    {
        private Client client;
        private List<ServiceHub> hubs = new List<ServiceHub>();
        private List<DiscoveryEntry> websockets = new List<DiscoveryEntry>();

        [SerializeField]
        private Sprite localServerIcon;

        [SerializeField]
        private Sprite remoteServerIcon;

        [SerializeField]
        private NanoverImdApplication application;

        [SerializeField]
        private DynamicMenu menu;

        private void OnEnable()
        {
            Refresh();
        }

        /// <summary>
        /// Returns either the <see cref="ServiceHub"/> with a localhost IP address or the first hub.
        /// </summary>
        private ServiceHub SelectBestService(IGrouping<string, ServiceHub> group)
        {
            foreach (var hub in group)
            {
                if (IsLocalhost(hub))
                    return hub;
            }

            return group.First();
        }

        private bool IsLocalhost(ServiceHub hub)
        {
            return hub.Address.Equals("127.0.0.1") || hub.Address.Equals("localhost");
        }

        private UniTask? currentSearchTask = null;

        [SerializeField]
        private UnityEvent startSearch;

        [SerializeField]
        private UnityEvent endSearch;

        public async UniTask SearchAsync()
        {
            currentSearchTask = client.StartSearch().AsUniTask();
            startSearch?.Invoke();
            await UniTask.WhenAny(UniTask.Delay(500), currentSearchTask.Value);
            await client.StopSearch();
            currentSearchTask = null;
            endSearch?.Invoke();
            RefreshHubs();
        }

        public void Refresh()
        {
            if (currentSearchTask != null)
            {
                client.StopSearch().AsUniTask().Forget();
                currentSearchTask = null;
                endSearch?.Invoke();
            }

            client = new Client();
            hubs.Clear();
            client.ServiceFound += (obj, args) => hubs.Add(args);
            SearchAsync().Forget();
        }

        public void RefreshHubs()
        {
            hubs = hubs.GroupBy(hub => hub.Id)
                       .Select(SelectBestService)
                       .ToList();

            menu.ClearChildren();
            foreach (var hub in hubs)
            {
                var local = hub.Address.Equals("127.0.0.1") || hub.Address.Equals("localhost");
                menu.AddItem(hub.Name, local ? localServerIcon : remoteServerIcon,
                                () => application.Connect(hub), hub.Address); 
            }
            foreach (var entry in websockets)
            {
                menu.AddItem(entry.info.name, remoteServerIcon, () => application.Connect(entry), entry.info.ws);
            }
        }
    }
}