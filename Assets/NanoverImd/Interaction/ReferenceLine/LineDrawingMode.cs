using UnityEngine;
using Nanover.Frontend.XR;
using UnityEngine.XR;
using NanoverImd.Interaction;

public class LineDrawingMode : MonoBehaviour
{
    private Nanover.Frontend.Input.IButton menuButton;
    private bool menuButtonPrevPressed;

    [SerializeField] ReferenceLineManager referenceLineManager;
    [SerializeField] InteractionTrailsManager interactionTrailsManager;
    [SerializeField] GameObject userInterface;

    [SerializeField] bool isExtendedModeEnabled = false;

    [SerializeField] string DRAWING_DISABLED = "<b>Press [menu] to enable draw mode";
    [SerializeField] string DRAWING_INSTRUCTIONS = "<b>Hold [A]</b> to draw a line\r\n<b>Press [A]</b> to add points to the line\r\n<b>Press [B]</b> to delete the line\r\n\r\n<b>Press [Y]</b> to reset trail\r\n<b>Press [X]</b> to position destiny\r\n\r\n<b>Press [menu]</b> to disable drawing mode";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        menuButton = InputDeviceCharacteristics.Left.WrapUsageAsButton(CommonUsages.menuButton);
        userInterface.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (menuButtonPrevPressed && !menuButton.IsPressed)
        {
            isExtendedModeEnabled = !isExtendedModeEnabled;

            userInterface.SetActive(isExtendedModeEnabled);
            referenceLineManager.enabled = isExtendedModeEnabled;
            interactionTrailsManager.enabled = isExtendedModeEnabled;
        }
        menuButtonPrevPressed = menuButton.IsPressed;
    }
}
