using Nanover.Frontend.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Nanover.Core.Math;
using UnityEngine;
using UnityEngine.XR;
using Cysharp.Threading.Tasks;

namespace Nanover.Frontend.XR
{
    /// <summary>
    /// Extensions for Unity's XR system to provide convenient querying of
    /// XR input values e.g device pose and button states.
    /// </summary>
    public static partial class UnityXRExtensions
    {
        [ThreadStatic]
        private static List<InputDevice> devices = new List<InputDevice>();

        public static InputDevice GetFirstDevice(this InputDeviceCharacteristics characteristics)
        {
            InputDevices.GetDevicesWithCharacteristics(characteristics, devices);
            return devices.FirstOrDefault();
        }

        /// <summary>
        /// Return the pose matrix for a given InputDevice, if available.
        /// </summary>
        public static Transformation? GetSinglePose(this InputDevice device)
        {
            if (device.isValid
             && device.TryGetFeatureValue(CommonUsages.devicePosition, out var position)
             && device.TryGetFeatureValue(CommonUsages.deviceRotation, out var rotation))
                return new Transformation(position, rotation, Vector3.one);

            return null;
        }

        public static IPosedObject WrapAsPosedObject(this InputDeviceCharacteristics characteristics)
        {
            var devices = new List<InputDevice>();
            var wrapper = new DirectPosedObject();

            UpdatePoseInBackground().Forget();

            async UniTask UpdatePoseInBackground()
            {
                while (true)
                {
                    wrapper.SetPose(GetDevice().GetSinglePose());
                    await UniTask.DelayFrame(1);
                }
            }

            InputDevice GetDevice()
            {
                InputDevices.GetDevicesWithCharacteristics(characteristics, devices);
                return devices.FirstOrDefault();
            }

            return wrapper;
        }

        public static bool? GetButtonPressed(this InputDevice device, InputFeatureUsage<bool> usage)
        {
            if (device.isValid
             && device.TryGetFeatureValue(usage, out var pressed))
                return pressed;

            return null;
        }

        /// <summary>
        /// Wrap an InputDeviceCharacteristics and InputFeatureUsage into a single
        /// button object that can be used to track a single button state. It will
        /// continously poll the corresponding feature of the first matching
        /// InputDevice. If a predicate function is provider then the button is
        /// forced into released when the predicate is false.
        /// </summary>
        public static IButton WrapUsageAsButton(this InputDeviceCharacteristics characteristics, 
                                                InputFeatureUsage<bool> usage,
                                                Func<bool> predicate = null)
        {
            var wrapper = new DirectButton();

            UpdatePressedInBackground().Forget();

            async UniTask UpdatePressedInBackground()
            {
                while (true)
                {
                    var pressed = characteristics.GetFirstDevice().GetButtonPressed(usage) ?? wrapper.IsPressed;
                    pressed &= predicate?.Invoke() ?? true; 

                    if (pressed && !wrapper.IsPressed)
                        wrapper.Press();
                    else if (!pressed && wrapper.IsPressed)
                        wrapper.Release();

                    await UniTask.DelayFrame(1);
                }
            }

            return wrapper;
        }

        public static Vector2? GetJoystickValue(this InputDevice device, InputFeatureUsage<Vector2> usage)
        {
            if (device.isValid
             && device.TryGetFeatureValue(usage, out var value))
                return value;

            return null;
        }
    }
}