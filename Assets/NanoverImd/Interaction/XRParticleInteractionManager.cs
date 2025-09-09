using Nanover.Core.Math;
using Nanover.Frontend.Controllers;
using Nanover.Frontend.Input;
using Nanover.Frontend.Manipulation;
using Nanover.Frontend.XR;
using Nanover.Network.Multiplayer;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR;

namespace NanoverImd.Interaction
{
    /// <summary>
    /// Translates XR input into interactions with particles in NanoverIMD.
    /// </summary>
    public class XRParticleInteractionManager : MonoBehaviour
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
                              InputDeviceCharacteristics.Left,
                              MultiplayerAvatar.LeftHandName);
        }

        private void SetupRightManipulator()
        {
            CreateManipulator(ref rightManipulator, 
                              ref rightButton,
                              controllerManager.RightController,
                              InputDeviceCharacteristics.Right,
                              MultiplayerAvatar.RightHandName);
        }
        
        private void CreateManipulator(ref AttemptableManipulator manipulator,
                                       ref IButton button,
                                       VrController controller,
                                       InputDeviceCharacteristics characteristics,
                                       string label = default)
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

            var toolPoser = controller.HeadPose;
            manipulator = new AttemptableManipulator(toolPoser, (pose) => AttemptGrabObjectUser(pose, label));

            button = characteristics.WrapUsageAsButton(CommonUsages.triggerButton, () => controllerManager.CurrentInputMode == targetMode);
            button.Pressed += manipulator.AttemptManipulation;
            button.Released += manipulator.EndActiveManipulation;

            var b = button;
            toolPoser.PoseChanged += () =>
            {
                var targetting = (controllerManager.CurrentInputMode == targetMode);
                var unpressed = (!b.IsPressed);

                if (targetting && unpressed && toolPoser.Pose is { } pose)
                    simulation.ManipulableParticles.HoverParticleGrab(pose.AsUnitTransformWithoutScale());
            };

        }

        private IActiveManipulation AttemptGrabObject(UnitScaleTransformation grabberPose)
        {
            // there is presently only one grabbable set of objects
            return simulation.ManipulableParticles.StartParticleGrab(grabberPose);
        }

        private IActiveManipulation AttemptGrabObjectUser(UnitScaleTransformation grabberPose, string label)
        {
            // there is presently only one grabbable set of objects
            if (simulation.ManipulableParticles.StartParticleGrab(grabberPose) is not { } grab)
                return null;

            grab.OwnerId = simulation.Multiplayer.AccessToken;
            grab.Label = label;
              
            return grab;
        }
    }
}