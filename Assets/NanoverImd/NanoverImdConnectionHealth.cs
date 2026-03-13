using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Text = TMPro.TextMeshProUGUI;

namespace NanoverImd
{
    public sealed class NanoverImdConnectionHealth : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField]
        private NanoverImdSimulation simulation;
        [SerializeField]
        private Text label;
#pragma warning restore 0649

        public bool ReportToState = false;

        public int Frame;
        public int State;
        public int RTT;
        public int FPS;

        private List<float> dts = new List<float>();

        private void OnEnable()
        {
            StartCoroutine(UpdateFPS());

            IEnumerator UpdateFPS()
            {
                while (true)
                {
                    dts.Add(Time.smoothDeltaTime);
                    while (dts.Count > 30)
                        dts.RemoveAt(0);
                    yield return new WaitForSeconds(1/30f);
                }
            }
        }

        private void Update()
        {
            UpdateSuggestedParameters();

            Frame = CheckTimes(simulation.Trajectory.MessageReceiveTimes);
            State = CheckTimes(simulation.Multiplayer.MessageReceiveTimes);
            RTT = Mathf.RoundToInt(simulation.Multiplayer.LastIndexRTT * 1000);
            FPS = Mathf.RoundToInt(dts.Count/dts.Sum());

            if (ReportToState)
            {
                var id = simulation.Multiplayer.AccessToken;

                simulation.Multiplayer.SetSharedState($"report.connection.health.{id}", new Dictionary<string, object>
                {
                    { "frame", Frame },
                    { "state", State },
                    { "rtt", RTT },
                    { "fps", FPS },
                });
            }

            label.text = $"Frame: {Frame}/s, State: {State}/s, RTT: {RTT}ms";

            int CheckTimes(IEnumerable<float> times)
            {
                var count = 0;
                var now = Time.realtimeSinceStartup;

                foreach (var time in times.Reverse())
                {
                    if (now - time < 1)
                    {
                        count += 1;
                    } 
                }

                return count;
            }
        }

        private void UpdateSuggestedParameters()
        {
            const string reportKey = "suggested.report.connection.health";

            if (simulation.Multiplayer.GetSharedState(reportKey) is bool report)
            {
                ReportToState = report;
            }
        }
    }
}