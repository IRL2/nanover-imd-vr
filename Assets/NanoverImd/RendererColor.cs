using UnityEngine;

namespace NanoverImd
{
    [RequireComponent(typeof(Renderer))]
    public class RendererColor : MonoBehaviour
    {
        private Material _material;
        private Material material
        {
            get
            {
                if (_material == null)
                {
                    var renderer = GetComponent<Renderer>();
                    _material = new Material(renderer.sharedMaterial);
                    renderer.sharedMaterial = _material;
                    _material.color = Color.cyan;
                }
                return _material;
            }
        }

        public Color Color
        {
            get => material.GetColor("_EmissionColor");
            set => material.SetColor("_EmissionColor", value);
        }
    }
}