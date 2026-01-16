using System.ComponentModel;
using Cysharp.Threading.Tasks;
using Nanover.Frontend.Controllers;
using Nanover.Frontend.Input;
using Nanover.Frontend.UI;
using Nanover.Frontend.XR;
using OVR.OpenVR;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR;

namespace NanoverImd.UI
{
    /// <summary>
    /// A <see cref="UserInterfaceManager"/> that only shows the UI while a cursor is held down.
    /// </summary>
    public class PopupUserInterfaceManager : UserInterfaceManager
    {
        [SerializeField]
        private GameObject menuPrefab;

        [SerializeField]
        private bool clickOnMenuClosed = true;

        [SerializeField]
        private ControllerManager controllers;
        
        [SerializeField]
        private UiInputMode mode;

        [SerializeField]
        private InputDeviceCharacteristics characteristics;

        private bool uiVisible;

        [SerializeField]
        private Nanover.Frontend.Input.IButton menuButton;

        private bool menuButtonPrevPressed;

        private void Start()
        {
            Assert.IsNotNull(menuPrefab, "Missing menu prefab");

            uiVisible = false;
            menuButtonPrevPressed = false;

            UpdatePressedInBackground().Forget();

            async UniTask UpdatePressedInBackground()
            {
                while (true)
                {
                    try
                    {
                        var pressed = characteristics.GetFirstDevice().GetButtonPressed(CommonUsages.menuButton) == true;

                        if (pressed && !menuButtonPrevPressed)
                        {
                            ToggleMenu();
                        }

                        menuButtonPrevPressed = pressed;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }

                    await UniTask.DelayFrame(1);
                }
            }
        }

        private void ShowMenu()
        {
            if (!controllers.WouldBecomeCurrentMode(mode))
                return;

            GotoScene(menuPrefab);

            SceneUI.transform.position = Camera.main.transform.position +
                                         Vector3.down * 0.2f +
                                         Camera.main.transform.forward * 0.7f;

            SceneUI.transform.rotation =
                Quaternion.LookRotation(SceneUI.transform.position - Camera.main.transform.position,
                                        Vector3.up);
        }

        private void CloseMenu()
        {
            if (clickOnMenuClosed)
                WorldSpaceCursorInput.TriggerClick();
            CloseScene();
        }

        private void ToggleMenu()
        {
            uiVisible = SceneUI.transform.gameObject.activeInHierarchy;
            uiVisible = !uiVisible;

            if (uiVisible)
                ShowMenu();
            else
                CloseMenu();
        }
    }
}