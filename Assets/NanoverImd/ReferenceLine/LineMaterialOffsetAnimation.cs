using System.Collections;
using UnityEngine;

public class ReferenceLineAnimator : MonoBehaviour
{
    [SerializeField]
    private Material material;
    
    string textureName = "_MainTex";

    float offset = 0.0f;

    Vector2 horizontalVector = new Vector2 (1, 0);

    [SerializeField]
    [Range(0.0f, 1.0f)]
    float speed = 0.2f; // Speed of the texture offset

    private Color transparentColor = new Color(0, 0, 0, 0);


    void Start()
    {
        textureName = material.GetTexturePropertyNames()[0];
    }

    // Update is called once per frame
    void LateUpdate()
    {
        material.SetTextureOffset(textureName, horizontalVector * offset);
        offset -= Time.deltaTime * speed; // Adjust speed as needed
        offset %= 1.0f; // Keep offset within [0, 1] range to avoid overflow
    }

    private void OnEnable()
    {
        //StartCoroutine(FadeIn()); // not yet, need to prevent flashing
    }

    private IEnumerator FadeIn()
    {
        for (float i = 0; i <= 1; i += 0.05f)
        {
            material.SetColor("_Color", Color.Lerp(transparentColor, Color.white, i));
            yield return null;
        }
    }
}
