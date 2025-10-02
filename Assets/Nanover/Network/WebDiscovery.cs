using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace WebDiscovery
{
    [Serializable]
    public class DiscoveryInfo
    {
        public string name;
        public string https;
        public string ws;
    }

    [Serializable]
    public class DiscoveryEntry
    {
        public string code;
        public DiscoveryInfo info;
    }

    [Serializable]
    public class DiscoveryListing
    {
        public List<DiscoveryEntry> list;
    }

    public static class WebsocketDiscovery
    {
        public static async UniTask<List<DiscoveryEntry>> DiscoverWebsocketServers(string endpoint)
        {
            var request = UnityWebRequest.Get(endpoint);
            await request.SendWebRequest();

            var json = request.downloadHandler.text;
            json = "{\"list\":" + json + "}";

            var listing = JsonUtility.FromJson<DiscoveryListing>(json);
            return listing.list;
        }
    }
}
