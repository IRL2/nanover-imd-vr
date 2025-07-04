using UnityEngine;

namespace NanoverImd
{
    [DisallowMultipleComponent]
    public sealed class NanoverImdMetaCalibrator : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField]
        private NanoverImdApplication application;
        [SerializeField]
        private GameObject colocationAnchor;
#pragma warning restore 0649

        public Transform referenceAnchor;
        public Transform referencePointA;
        public Transform referencePointB;

        private OVRSpatialAnchor anchor;

        public void Clear()
        {
            colocationAnchor.SetActive(false);

            if (anchor != null)
                Destroy(anchor);
        }

        public void Setup(Vector3 pointA, Vector3 pointB)
        {
            Clear();

            var center = Vector3.LerpUnclamped(pointA, pointB, 0.5f);
            var normal = Vector3.Cross(Vector3.up, pointB - pointA).normalized;
            var rotation = Quaternion.LookRotation(normal, Vector3.up);

            colocationAnchor.transform.SetLocalPositionAndRotation(center, rotation);
            colocationAnchor.SetActive(true);

            anchor = colocationAnchor.AddComponent<OVRSpatialAnchor>();

            referencePointA.SetParent(colocationAnchor.transform, worldPositionStays: true);
            referencePointB.SetParent(colocationAnchor.transform, worldPositionStays: true);
        }

        private void Update()
        {
            if (anchor == null)
                return;

            var center = colocationAnchor.transform.position;
            var normal = colocationAnchor.transform.forward;
            application.CalibratedSpace.CalibrateFromTwoControlPoints(center, center + normal);
        }
    }
}