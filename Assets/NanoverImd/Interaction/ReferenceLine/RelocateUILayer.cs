using Nanover.Visualisation;
using UnityEngine;
using UnityEngine.UIElements;

public class RelocateUILayer : MonoBehaviour
{
    [SerializeField] private BoxVisualiser boxVisualiser;

    void OnEnable()
    {
        ResetLocation();
    }

    public void ResetLocation()
    {
        Vector3 position = new Vector3();
        position.x = - boxVisualiser.GetBox().xAxis.x + 1.0f;
        position.y = 0;//boxVisualiser.GetBox().yAxis.y;
        position.z = boxVisualiser.GetBox().zAxis.z;
        transform.localPosition = position;

        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(rt.sizeDelta.x,
                                    boxVisualiser.GetBox().axesMagnitudes.y);

        Debug.Log($"RelocateUILayer: Relocating UI layer relative to simbox at {position}");
    }
}
