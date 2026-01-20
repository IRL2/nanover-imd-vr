using Cysharp.Threading.Tasks;
using Nanover.Frontend.UI;
using System.Collections.Generic;
using UnityEngine;

namespace NanoverImd.UI.Scene
{
    public class ListDemos : MonoBehaviour
    {
        [SerializeField]
        private List<NanoverRecordings.DemoListing> demos;

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
            NanoverRecordings
                .FetchDemosListing()
                .AsUniTask()
                .ContinueWith((listing) =>
            {
                demos = listing;
                RefreshDemos();
            });
        }

        public void RefreshDemos()
        {
            menu.ClearChildren();
            foreach (var demo in demos)
            {
                menu.AddItem(
                    demo.Name, 
                    demoIcon, 
                    () => NanoverRecordings.LoadDemo(demo.URL).AsUniTask().ContinueWith(application.Simulation.ConnectRecordingReader)
                );
            }
        }
    }
}