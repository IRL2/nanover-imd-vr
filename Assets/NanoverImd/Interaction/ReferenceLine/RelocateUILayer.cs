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
        position.x = - 0.3f;// - boxVisualiser.GetBox().xAxis.x + 0.3f;
        position.y = 0;//boxVisualiser.GetBox().yAxis.y;
        position.z = boxVisualiser.GetBox().zAxis.z - 0.3f;
        transform.localPosition = position;

        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(boxVisualiser.GetBox().axesMagnitudes.x,
                                   boxVisualiser.GetBox().axesMagnitudes.y);

        //rt.pivot = new Vector2(0, 1);
        //rt.Rotate(Vector3.right, 30f);

        Debug.Log($"RelocateUILayer: Relocating UI layer relative to simbox at {position}");
    }
}
