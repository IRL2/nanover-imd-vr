using Nanover.Frontend.Controllers;
using Nanover.Frontend.XR;
using NanoverImd;
using UnityEngine;
using UnityEngine.XR;

public class InteractionStrengthController : MonoBehaviour
{
    [SerializeField]
    private NanoverImdSimulation simulation;

    [SerializeField]
    private VrController controller;

    [SerializeField]
    private float maximumInteractionStrength;

    [SerializeField]
    private float minimumInteractionStrength;

    private float scaleTick;
    private float scaleTime;

    private void Update()
    {
        if (simulation.InLocalPlayback)
            return;

        var joystick = InputDeviceCharacteristics.Right.GetFirstDevice().GetJoystickValue(CommonUsages.primary2DAxis) ?? Vector2.zero;

        var increase = joystick.x > .5f;
        var decrease = joystick.x < -.5f;
        var isScaling = increase || decrease;

        scaleTime = isScaling ? scaleTime + Time.deltaTime : 0;
        scaleTick = isScaling ? scaleTick + Time.deltaTime : 0;

        var sign = isScaling ? Mathf.Sign(joystick.x) : 0;
        var change = sign * 1;

        if (scaleTick > .1f) {
            change *= Mathf.Pow(2, Mathf.FloorToInt(scaleTime));

            Scale = (int) Mathf.Clamp(Scale + change,
                                      minimumInteractionStrength,
                                      maximumInteractionStrength);

            controller.PushNotification($"{(int) Scale}x");
            scaleTick -= .1f;
        }
    }

    private float Scale
    {
        get => simulation.ManipulableParticles.ForceScale;
        set => simulation.ManipulableParticles.ForceScale = value;
    }
}