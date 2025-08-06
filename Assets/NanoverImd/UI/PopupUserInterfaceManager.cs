using Cysharp.Threading.Tasks;
using Nanover.Frontend.Controllers;
using Nanover.Frontend.Input;
using Nanover.Frontend.UI;
using Nanover.Frontend.XR;
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

        private void Start()
        {
            Assert.IsNotNull(menuPrefab, "Missing menu prefab");

            UpdatePressedInBackground().Forget();

            async UniTask UpdatePressedInBackground()
            {
                var openMenu = new DirectButton();
                openMenu.Pressed += ShowMenu;
                openMenu.Released += CloseMenu;

                while (true)
                {
                    try
                    {
                        var joystick = characteristics.GetFirstDevice().GetJoystickValue(CommonUsages.primary2DAxis) ?? Vector2.zero;
                        var pressed = Mathf.Abs(joystick.y) > .5f;

                        if (pressed && !openMenu.IsPressed)
                            openMenu.Press();
                        else if (!pressed && openMenu.IsPressed)
                            openMenu.Release();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }

                    await UniTask.DelayFrame(1);
                }
            }
        }

        [SerializeField]
        private Vector3 offset;

        private void ShowMenu()
        {
            if (!controllers.WouldBecomeCurrentMode(mode))
                return;
            
            GotoScene(menuPrefab);
            
            SceneUI.transform.position = SceneUI.GetComponent<PhysicalCanvasInput>()
                                                .Controller.transform.position + offset;
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
    }
}