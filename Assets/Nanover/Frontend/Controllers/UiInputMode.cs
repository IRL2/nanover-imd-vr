using UnityEngine;
using UnityEngine.XR;

namespace Nanover.Frontend.Controllers
{
    public class UiInputMode : ControllerInputMode
    {
#pragma warning disable 0649
        [SerializeField]
        private GameObject gizmo;

        [SerializeField]
        private VrController.ControllerAnchor anchor;

        [SerializeField]
        private int priority;

        [SerializeField]
        private bool bothHands;
#pragma warning restore 0649

        public override int Priority => priority;

        public override void OnModeStarted()
        {
            Controllers.DominantHandChanged += ControllersOnDominantHandChanged;
        }

        private void ControllersOnDominantHandChanged()
        {
            // Need to update controllers when dominant hand changes, to correctly show Gizmo and
            // make other hand transparent
            SetupController(Controllers.LeftController, InputDeviceCharacteristics.Left);
            SetupController(Controllers.RightController, InputDeviceCharacteristics.Right);
        }

        public override void OnModeEnded()
        {
            Controllers.DominantHandChanged -= ControllersOnDominantHandChanged;
            Controllers.LeftController.RenderModel.SetColor(Color.white);
            Controllers.RightController.RenderModel.SetColor(Color.white);
        }

        public override void SetupController(VrController controller,
                                             InputDeviceCharacteristics inputSource)
        {
            if (controller.IsControllerActive)
            {
                var dominant = (Controllers.DominantHand & inputSource) != InputDeviceCharacteristics.None || bothHands;
                if (dominant)
                    controller.InstantiateCursorGizmo(gizmo, anchor);
                else
                    controller.InstantiateCursorGizmo(null, anchor);
                if (dominant)
                    controller.RenderModel.SetColor(Color.white);
                else
                    controller.RenderModel.SetColor(new Color(1f, 1f, 1f, 0.2f));
            }
        }
    }
}