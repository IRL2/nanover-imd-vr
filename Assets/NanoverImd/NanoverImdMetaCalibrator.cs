using UnityEngine;
using Meta.XR.BuildingBlocks;

namespace NanoverImd
{
    [DisallowMultipleComponent]
    public sealed class NanoverImdMetaCalibrator : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField]
        private NanoverImdApplication application;
        [SerializeField]
        private SpatialAnchorCoreBuildingBlock spatialAnchors;
        [SerializeField]
        private GameObject colocationAnchor;
#pragma warning restore 0649

        private GameObject anchor;

        public void Setup(Vector3 pointA, Vector3 pointB)
        {
            var center = Vector3.LerpUnclamped(pointA, pointB, 0.5f);
            var normal = Vector3.Cross(Vector3.up, pointB - pointA).normalized;
            var rotation = Quaternion.LookRotation(normal, Vector3.up);

            colocationAnchor.transform.SetLocalPositionAndRotation(center, rotation);
            spatialAnchors.InstantiateSpatialAnchor(colocationAnchor, center, rotation);

            spatialAnchors.OnAnchorCreateCompleted.AddListener((anchor, result) =>
            {
                this.anchor = anchor.gameObject;
                anchor.gameObject.SetActive(true);

                if (result != OVRSpatialAnchor.OperationResult.Success)
                {
                    DebugPanel.Instance.AddText($"FAILED ANCHORS {result}");
                }
            });
        }

        private void Update()
        {
            if (anchor == null)
                return;

            var center = anchor.transform.position;
            var normal = anchor.transform.forward;
            application.CalibratedSpace.CalibrateFromTwoControlPoints(center, center + normal);
        }
    }
}