using UnityEngine;
using Nanover.Frontend.XR;
using UnityEngine.XR;
using NanoverImd.Interaction;

public class LineModeToggler : MonoBehaviour
{
    private Nanover.Frontend.Input.IButton menuButton;
    private bool menuButtonPrevPressed;


    [SerializeField] GameObject[] ObjectsToActivate;
    [SerializeField] GameObject[] ObjectsToDeactivate;
    [SerializeField] MonoBehaviour[] ScriptsToEnable;
    [SerializeField] MonoBehaviour[] ScriptsToDisable;

    //[SerializeField] ReferenceLineManager referenceLineManager;
    //[SerializeField] InteractionTrailsManager interactionTrailsManager;
    //[SerializeField] GameObject userInterface;

    // readonly
    [SerializeField] bool isExtendedModeEnabled = false;

    //[SerializeField] string DRAWING_DISABLED = "<b>Press [menu] to enable draw mode";
    //[SerializeField] string DRAWING_INSTRUCTIONS = "<b>Hold [A]</b> to draw a line\r\n<b>Press [A]</b> to add points to the line\r\n<b>Press [B]</b> to delete the line\r\n\r\n<b>Press [Y]</b> to reset trail\r\n<b>Press [X]</b> to position destiny\r\n\r\n<b>Press [menu]</b> to disable drawing mode";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        menuButton = InputDeviceCharacteristics.Left.WrapUsageAsButton(CommonUsages.menuButton);
        UpdateStates();
    }

    // Update is called once per frame
    void Update()
    {
        if (menuButtonPrevPressed && !menuButton.IsPressed)
        {
            isExtendedModeEnabled = !isExtendedModeEnabled;
            UpdateStates();
            //userInterface.SetActive(isExtendedModeEnabled);
            //referenceLineManager.enabled = isExtendedModeEnabled;
            //interactionTrailsManager.enabled = isExtendedModeEnabled;
        }
        menuButtonPrevPressed = menuButton.IsPressed;
    }

    void UpdateStates()
    {
        for (int i = 0; i < ObjectsToActivate.Length; i++)
        {
            ObjectsToActivate[i].SetActive(isExtendedModeEnabled);
        }
        for (int i = 0; i < ScriptsToEnable.Length; i++)
        {
            ScriptsToEnable[i].enabled = isExtendedModeEnabled;
        }

        for (int i = 0; i < ObjectsToDeactivate.Length; i++)
        {
            ObjectsToDeactivate[i].SetActive(!isExtendedModeEnabled);
        }
        for (int i = 0; i < ScriptsToDisable.Length; i++)
        {
            ScriptsToDisable[i].enabled = !isExtendedModeEnabled;
        }
    }
}
