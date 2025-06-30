using System;
using Nanover.Frontend.Input;
using UnityEngine;

namespace Nanover.Frontend.Controllers
{
    /// <summary>
    /// The persistent script to represent a left or right controller in VR.
    /// </summary>
    /// <remarks>
    /// This class exposes various poses (such as cursor and grip points) in a way that
    /// the posed object always exists, and is automatically linked to the active
    /// controller if there is one.
    /// </remarks>
    public class VrController : MonoBehaviour
    {
        [SerializeField]
        private NanoverRenderModel renderModel;

        public NanoverRenderModel RenderModel => renderModel;

        public enum ControllerAnchor
        {
            Cursor,
            Grip,
            Head,
        }

        private ControllerPivot cursor;
        private ControllerPivot grip;
        private ControllerPivot head;

        private DirectPosedObject cursorPose = new DirectPosedObject();
        private DirectPosedObject gripPose = new DirectPosedObject();
        private DirectPosedObject headPose = new DirectPosedObject();

        /// <summary>
        /// Indicate the controller has been reset (connected or disconnected).
        /// </summary>
        /// <param name="controller"></param>
        public void ResetController(VrControllerPrefab controller)
        {
            IsControllerActive = controller != null;

            controllerPrefab = controller;

            SetupPose(ref cursor, controller?.Cursor, cursorPose, OnCursorPoseChanged);
            SetupPose(ref grip, controller?.Grip, gripPose, OnGripPoseChanged);
            SetupPose(ref head, controller?.Head, headPose, OnHeadPoseChanged);

            ControllerReset?.Invoke();
        }

        private VrControllerPrefab controllerPrefab;

        public void PushNotification(string text)
        {
            controllerPrefab.PushNotification(text);
        }

        private void SetupPose(ref ControllerPivot pivot,
                               ControllerPivot newPivot,
                               DirectPosedObject posedObject,
                               Action onPoseChanged)
        {
            if (IsControllerActive)
            {
                if (pivot != null)
                    pivot.PoseChanged -= onPoseChanged;
                pivot = newPivot;
                if (pivot != null)
                    pivot.PoseChanged += onPoseChanged;
            }
            else
            {
                if (pivot != null)
                    pivot.PoseChanged -= onPoseChanged;
                pivot = null;
                posedObject.SetPose(null);
            }
        }

        private void OnCursorPoseChanged()
        {
            cursorPose.SetPose(cursor.Pose);
        }

        private void OnGripPoseChanged()
        {
            gripPose.SetPose(grip.Pose);
        }

        private void OnHeadPoseChanged()
        {
            headPose.SetPose(head.Pose);
        }

        /// <summary>
        /// The pose marking the location of a gripped hand.
        /// </summary>
        public IPosedObject GripPose => gripPose;

        /// <summary>
        /// The pose marking the location where the bulk of the controller is
        /// </summary>
        public IPosedObject HeadPose => headPose;

        /// <summary>
        /// The cursor point where tools should be centered.
        /// </summary>
        public IPosedObject CursorPose => cursorPose;

        /// <summary>
        /// Is the controller currently active?
        /// </summary>
        public bool IsControllerActive { get; private set; } = false;

        public event Action ControllerReset;

        private GameObject cursorGizmo = null;

        /// <summary>
        /// Set the current gizmo at the end of the controller.
        /// </summary>
        /// <param name="interactionGizmo">A <see cref="GameObject"/> representing the gizmo at the end of the controller, or null if there should be no gizmo.</param>
        public void InstantiateCursorGizmo(GameObject interactionGizmo, ControllerAnchor anchor)
        {
            if (cursorGizmo != null)
                Destroy(cursorGizmo);
            if (cursor != null && interactionGizmo != null)
            {
                var pivot =
                    anchor == ControllerAnchor.Grip ? grip.transform :
                    anchor == ControllerAnchor.Cursor ? cursor.transform :
                    anchor == ControllerAnchor.Head ? head.transform : 
                    cursor.transform;


                cursorGizmo = Instantiate(interactionGizmo, pivot);
            }
        }
    }
}