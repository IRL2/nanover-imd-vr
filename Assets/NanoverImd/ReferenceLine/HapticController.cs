using UnityEngine;
using UnityEngine.XR;

public class HapticController : MonoBehaviour
{
    private InputDevice rightHandDevice, leftHandDevice;
    private UnityEngine.XR.HapticCapabilities rightHapticCapabilities, leftHapticCapabilities;

    public const int RIGHT_HAND = 0;
    public const int LEFT_HAND = 1;

    private bool? rightHandEnabled;
    private bool? leftHandEnabled = false;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (rightHandEnabled == null)
        {
            TryToEnableRightHaptics();
        }

        if (leftHandEnabled == null)
        {
            TryToEnablLeftHaptics();
        }
    }

    public void SendHaptic(int hand, float amplitude, float duration)
    {
        if (rightHandDevice.isValid && rightHapticCapabilities.supportsImpulse)
        {
            rightHandDevice.SendHapticImpulse(0, amplitude, duration);
        }
        else
        {
            UnityEngine.Debug.LogWarning("Right hand device is not valid or does not support haptic impulses.");
        }
    }

    private void TryToEnableRightHaptics()
    {
        rightHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        rightHandDevice.TryGetHapticCapabilities(out rightHapticCapabilities);
        if (!rightHapticCapabilities.supportsImpulse)
        {
            rightHandEnabled = false;
            UnityEngine.Debug.LogWarning("Right hand device does not support haptic impulses.");
        }
        else
        {
            rightHandEnabled = true;
            rightHandDevice.SendHapticImpulse(0, .5f, .1f); // Test haptic feedback
            UnityEngine.Debug.Log("Right hand device supports haptic impulses.");
        }
    }

    private void TryToEnablLeftHaptics()
    {
        leftHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        leftHandDevice.TryGetHapticCapabilities(out leftHapticCapabilities);
        if (!leftHapticCapabilities.supportsImpulse)
        {
            leftHandEnabled = false;
            UnityEngine.Debug.LogWarning("Right hand device does not support haptic impulses.");
        }
        else
        {
            leftHandEnabled = true;
            leftHandDevice.SendHapticImpulse(0, .5f, .1f); // Test haptic feedback
            UnityEngine.Debug.Log("Right hand device supports haptic impulses.");
        }
    }
}
