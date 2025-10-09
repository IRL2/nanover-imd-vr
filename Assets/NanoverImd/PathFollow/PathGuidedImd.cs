using System;
using Nanover.Frame;
using Nanover.Frontend.XR;
using Nanover.Visualisation;
using NanoverImd.Interaction;
using UnityEngine;
using UnityEngine.XR;

namespace NanoverImd.PathFollower
{
    public class PathGuidedImd : MonoBehaviour
    {
        public LineModeToggler lineModeToggler;
        public SynchronisedFrameSource frameSource;
        public ReferenceLineManager pathManager;
        public InteractionTrailsManager trailsManager;
        public LineManager lineManager;
        public PathFollower pathFollower;

        public bool IsFollowing => pathFollower.enabled;

        private void Start()
        {
            var cancelButton = InputDeviceCharacteristics.Right.WrapUsageAsButton(CommonUsages.secondaryButton);

            cancelButton.Pressed += () =>
            {
                CancelFollower();
            };
        }

        private void Update()
        {
            CheckUserInteracted();
        }

        private void CheckUserInteracted()
        {
            if (!IsFollowing
                && GetSingleInteractedAtom() is int atom
                && GetLastReferencePath() is { } path)
            {
                StartFollower(path, atom);
            }

            LineRenderer GetLastReferencePath()
            {
                // get all line renderers inside the linesManager game object
                LineRenderer[] lines = lineManager.GetComponentsInChildren<LineRenderer>();

                // find the last line renderer that has "reference" in its name
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    if (lines[i].name.Contains("reference"))
                    {
                        return lines[i];
                    }
                }
                return null;
            }

            int? GetSingleInteractedAtom()
            {
                if (frameSource?.CurrentFrame?.Data is { } data)
                    if (data.TryGetValue(FrameData.ForcesUserIndex, out var interactedAtomsObj)
                    && interactedAtomsObj is object[] interactedAtoms
                    && interactedAtoms.Length == 1)
                {
                    return Convert.ToInt32(interactedAtoms[0]);
                }

                return null;
            }
        }

        public void OnDisconnect()
        {
            gameObject.SetActive(false);

            Clear();
        }

        public void OnEnable()
        {
            lineModeToggler.EnableExtendedMode(true);
        }

        public void OnDisable()
        {
            lineModeToggler.EnableExtendedMode(false);

            CancelFollower();
        }

        public void Clear()
        {
            pathManager.OnDisconnect();
            trailsManager.OnDisconnect();
            lineManager.RemoveAllLines();

            CancelFollower();
        }

        public void StartFollower(LineRenderer path, int atomId)
        {
            Debug.Log($"PathGuidedImd: Start following atom {atomId} on path name {path.name}");
            pathFollower.testLine = path;
            pathFollower.AtomId = atomId;
            pathFollower.enabled = true;
        }

        public void CancelFollower()
        {
            pathFollower.enabled = false;
            pathFollower.Reset();
        }
    }
}
