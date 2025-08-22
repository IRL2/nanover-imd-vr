using Nanover.Visualisation;
using System.Collections;
using UnityEngine;

public class RelocateUILayer : MonoBehaviour
{
    [SerializeField] private BoxVisualiser boxVisualiser;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Material boxMaterial;

    private Color initialBoxColor;

    private void OnEnable()
    {
        initialBoxColor = boxMaterial.GetColor("_Color");
        StartCoroutine(ResetLocation());
    }

    private IEnumerator ResetLocation()
    {
        canvasGroup.alpha = 0f;

        yield return new WaitForSeconds(.2f);
        Vector3 position = new Vector3();
        position.x = 0f;// - boxVisualiser.GetBox().xAxis.x + 0.3f;
        position.y = 0f;//boxVisualiser.GetBox().yAxis.y;
        position.z = boxVisualiser.GetBox().zAxis.z - 0.3f;
        transform.localPosition = position;

        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(boxVisualiser.GetBox().axesMagnitudes.x,
                                   boxVisualiser.GetBox().axesMagnitudes.y);

        rt.localRotation = Quaternion.Euler(0, 0, 0);


        // an opening door effect
        for (float i = 0; i <= 1; i+=0.05f)
        {
            float angle = Mathf.Lerp(-90f, -30f, i);
            rt.localRotation = Quaternion.Euler(0, angle, 0);
            canvasGroup.alpha = i;
            boxMaterial.SetColor("_Color", Color.Lerp(initialBoxColor, Color.gray8, i));
            yield return null; // wait for the next frame
        }

        Debug.Log($"RelocateUILayer: Relocating UI layer relative to simbox at {position}");
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        canvasGroup.alpha = 0f;
        boxMaterial.SetColor("_Color", initialBoxColor);
        //StartCoroutine(Dissapear());
    }

    private IEnumerator Dissapear()
    {
        RectTransform rt = GetComponent<RectTransform>();
        float initialY = rt.localRotation.eulerAngles.y;

        for (float i = initialY; i > -90; i++)
        {
            rt.localRotation = Quaternion.Euler(0, i, 0);
            canvasGroup.alpha = (i + initialY) / 60f; // from 1 to 0
            yield return null; // wait for the next frame
        }
    }
}

