using UnityEngine;

namespace NanoverImd
{
    [SerializeField]
    [RequireComponent(typeof(Renderer))]
    public class RendererColor : MonoBehaviour
    {
        [SerializeField]
        private string target = "_EmissionColor";

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
                    _material.color = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, _material.color.a);
                }
                return _material;
            }
        }

        public Color Color
        {
            get => material.GetColor(target);
            set => material.SetColor(target, new Color(value.r, value.g, value.b, _material.color.a));
        }
    }
}