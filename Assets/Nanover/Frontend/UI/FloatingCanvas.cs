﻿using UnityEngine;
using UnityEngine.Assertions;
 using UnityEngine.XR;

 namespace Nanover.Frontend.UI
{
    public class FloatingCanvas : MonoBehaviour
    {
        [SerializeField]
        private float distance;

        [SerializeField]
        private float verticalOffset;

        [SerializeField]
        private float pitchRotation = 0.0f;

#pragma warning disable 0109
        [SerializeField]
        private new Camera camera;
#pragma warning restore 0109

        [SerializeField]
        private float smoothTime;

        [SerializeField]
        private float acceptableDistance = 0.1f;

        [SerializeField]
        private float unacceptableDistance = 0.8f;

        private bool isMoving = true;

        private void Start()
        {
            Assert.IsNotNull(camera);
            InputTracking.trackingAcquired += InputTrackingOntrackingAcquired;
            UpdatePosition(true);
        }

        private void InputTrackingOntrackingAcquired(XRNodeState obj)
        {
            if (obj.nodeType == XRNode.Head)
            {
                UpdatePosition(true);
            }
        }

        private Vector3 velocity = Vector3.zero;

        private void Update()
        {
            UpdatePosition();
        }

        private void UpdatePosition(bool force = false)
        {
            var cameraPos = camera.transform.position;
            var cameraForward = camera.transform.forward;

            cameraForward.y = 0;
            cameraForward = cameraForward.normalized;

            var currentPosition = transform.position;
            var idealPosition = cameraPos + cameraForward * distance + verticalOffset * Vector3.up;

            var currentFacing = currentPosition - cameraPos;
            currentFacing.y = pitchRotation;
            
            if (force)
            {
                transform.position = idealPosition;
                transform.rotation = Quaternion.LookRotation(currentFacing, Vector3.up);
            }
            else
            {
                var distance = Vector3.Distance(currentPosition, idealPosition);

                if (distance > unacceptableDistance && !isMoving)
                    isMoving = true;
                else if (distance < acceptableDistance && isMoving)
                    isMoving = false;

                if (isMoving)
                {
                    transform.position = Vector3.SmoothDamp(currentPosition,
                                                            idealPosition,
                                                            ref velocity,
                                                            smoothTime);
                    transform.rotation = Quaternion.LookRotation(currentFacing, Vector3.up);
                }
            }

           
        }
    }
}