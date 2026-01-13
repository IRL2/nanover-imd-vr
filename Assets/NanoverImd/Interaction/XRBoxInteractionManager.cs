using Nanover.Core.Math;
using Nanover.Frontend.Controllers;
using Nanover.Frontend.Input;
using Nanover.Frontend.Manipulation;
using Nanover.Frontend.XR;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR;

namespace NanoverImd.Interaction
{
    /// <summary>
    /// Translates XR input into interactions the box in NanoverImd.
    /// </summary>
    public class XRBoxInteractionManager : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField]
        private NanoverImdSimulation simulation;

        [SerializeField]
        private ControllerManager controllerManager;

        [SerializeField]
        private ControllerInputMode targetMode;
#pragma warning restore 0649

        private AttemptableManipulator leftManipulator;
        private IButton leftButton;
        
        private AttemptableManipulator rightManipulator;
        private IButton rightButton;
        
        private void OnEnable()
        {
            Assert.IsNotNull(simulation);
            Assert.IsNotNull(controllerManager);

            controllerManager.LeftController.ControllerReset += SetupLeftManipulator;
            controllerManager.RightController.ControllerReset += SetupRightManipulator;
            
            SetupLeftManipulator();
            SetupRightManipulator();
        }

        private void OnDisable()
        {
            controllerManager.LeftController.ControllerReset -= SetupLeftManipulator;
            controllerManager.RightController.ControllerReset -= SetupRightManipulator;
        }

        private void SetupLeftManipulator()
        {
            CreateManipulator(ref leftManipulator, 
                              ref leftButton,
                              controllerManager.LeftController,
                              InputDeviceCharacteristics.Left);
        }

        private void SetupRightManipulator()
        {
            CreateManipulator(ref rightManipulator, 
                              ref rightButton,
                              controllerManager.RightController,
                              InputDeviceCharacteristics.Right);
        }

        private void CreateManipulator(ref AttemptableManipulator manipulator,
                                       ref IButton button,
                                       VrController controller,
                                       InputDeviceCharacteristics characteristics)
        {
            // End manipulations if controller has been removed/replaced
            if (manipulator != null)
            {
                manipulator.EndActiveManipulation();
                button.Pressed -= manipulator.AttemptManipulation;
                button.Released -= manipulator.EndActiveManipulation;
                manipulator = null;
            }

            if (!controller.IsControllerActive)
                return;

            var controllerPoser = controller.GripPose;
            manipulator = new AttemptableManipulator(controllerPoser, AttemptGrabSpace);

            button = characteristics.WrapUsageAsButton(CommonUsages.gripButton, () => controllerManager.CurrentInputMode == targetMode && gameObject.activeInHierarchy);
            button.Pressed += manipulator.AttemptManipulation;
            button.Released += manipulator.EndActiveManipulation;
        }

        private IActiveManipulation AttemptGrabSpace(UnitScaleTransformation grabberPose)
        {
            if (simulation.InLocalPlayback)
                return null;

            // there is presently only one grabbable space
            return simulation.ManipulableSimulationSpace.StartGrabManipulation(grabberPose);
        }
    }
}