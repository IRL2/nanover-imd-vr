using UnityEngine;

/// <summary>
/// Disable the UIRaycastingInteraction gameobjects from the given controllers when this component is enabled.
/// Returns them to their initial state when disabled.
/// </summary>
public class DisableRaycastingInteraction : MonoBehaviour
{
    [SerializeField]
    private GameObject raycasterInteractorRight;

    [SerializeField]
    private GameObject raycasterInteractorLeft;

    private bool initialStateRight;
    private bool initialStateLeft;

    void OnEnable()
    {
        initialStateRight = raycasterInteractorRight.activeInHierarchy;
        initialStateLeft = raycasterInteractorLeft.activeInHierarchy;

        raycasterInteractorRight.SetActive(false);
        raycasterInteractorLeft.SetActive(false);
    }

    private void OnDisable()
    {
        raycasterInteractorRight.SetActive(initialStateRight);
        raycasterInteractorLeft.SetActive(initialStateLeft);
    }
}
