using Nanover.Core.Math;
using UnityEngine;

namespace Nanover.Visualisation
{
    /// <summary>
    /// Visualiser a box (as represented by an <see cref="AffineTransformation" />).
    /// </summary>
    [ExecuteAlways]
    public class BoxVisualiser : MonoBehaviour
    {
        /// <summary>
        /// The box to visualise.
        /// </summary>
        [SerializeField]
        private AffineTransformation box;

        /// <summary>
        /// The width of the edges of the box.
        /// </summary>
        [Range(0, 0.2f)]
        [SerializeField]
        private float width = 0.1f;

        /// <summary>
        /// The mesh to draw each edge as.
        /// </summary>
        [SerializeField]
        private Mesh mesh;

        /// <summary>
        /// The material to render the edges as.
        /// </summary>
        [SerializeField]
        private Material material;

        /// <summary>
        /// The midpoints of the edges of the box, in the local box coordinates.
        /// </summary>
        private readonly Vector3[] axesMidpoints =
        {
            new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 1),
            new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0),
            new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 1), new Vector3(0, 0, 0.5f),
            new Vector3(1, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 1, 0.5f)
        };

        /// <summary>
        /// The length of each edge of the box, in the local box coordinates.
        /// </summary>
        private readonly Vector3[] axesLength =
        {
            Vector3.right, Vector3.right, Vector3.right, Vector3.right, Vector3.up, Vector3.up,
            Vector3.up, Vector3.up, Vector3.forward, Vector3.forward, Vector3.forward,
            Vector3.forward
        };

        private void Update()
        {
            for (var i = 0; i < 12; i++)
            {
                var offset = box.TransformPoint(axesMidpoints[i]);
                var size = Vector3.Scale(axesLength[i], box.axesMagnitudes);
                var x = box.xAxis.normalized;
                var y = box.yAxis.normalized;
                var z = box.zAxis.normalized;
                var transformation = new AffineTransformation(x * (size.x + width),
                                                              y * (size.y + width),
                                                              z * (size.z + width),
                                                              offset);
                var matrix = transformation.matrix;
                Graphics.DrawMesh(mesh, transform.localToWorldMatrix * matrix, material, 0);
            }
        }

        /// <summary>
        /// Set the box to be visualised.
        /// </summary>
        public void SetBox(AffineTransformation transformation)
        {
            box = transformation;
        }

        /// <summary>
        /// Get the box transform to be visualised.
        /// </summary>
        public AffineTransformation GetBox()
        {
            return box;
        }
    }
}