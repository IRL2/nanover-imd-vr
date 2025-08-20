using UnityEngine;
using Nanover.Frontend.XR;
using UnityEngine.XR;
using UnityEditor;
using Unity.Collections;

public class LineModeToggler : MonoBehaviour
{
    private Nanover.Frontend.Input.IButton menuButton;

    [SerializeField] GameObject[] ObjectsToActivate;
    [SerializeField] GameObject[] ObjectsToDeactivate;
    [SerializeField] MonoBehaviour[] ScriptsToEnable;
    [SerializeField] MonoBehaviour[] ScriptsToDisable;

    [SerializeField] bool isExtendedModeEnabled = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        menuButton = InputDeviceCharacteristics.Left.WrapUsageAsButton(CommonUsages.menuButton);
        UpdateStates();

        menuButton.Pressed += () => EnableExtendedMode(!isActiveAndEnabled);
    }

    public void EnableExtendedMode(bool state = true)
    {
        isExtendedModeEnabled = state;
        UpdateStates();
    }

    /// <summary>
    /// update 
    /// </summary>
    public void UpdateStates()
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
