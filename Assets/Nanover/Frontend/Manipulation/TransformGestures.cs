using Nanover.Core.Math;
using UnityEngine;

namespace Nanover.Frontend.Manipulation
{
    /// <summary>
    /// Gesture that ties an object's position and orientation to a control
    /// point's  position and orientation. Updating the control point
    /// transformation yields an updated object transformation such that the
    /// object has moved to preserve the relative position and orientation of
    /// the object and control point.
    /// Example usage: A single hand grabbing a box, the box then moves and
    /// rotates with the motion of the hand.
    /// </summary>
    public sealed class OnePointTranslateRotateGesture
    {
        private Transformation controlPointToObject;

        /// <summary>
        /// Begin the gesture with an initial object transform and initial control
        /// point transform. Throughout the gesture the relationship between these
        /// transforms will remain invariant.
        /// </summary>
        public void BeginGesture(Transformation objectTransformation,
                                 UniformScaleTransformation controlTransformation)
        {
            controlPointToObject = controlTransformation.TransformationTo(objectTransformation);
        }

        /// <summary>
        /// Return an object transform that is the initial object transform
        /// altered in the same manner as the gesture transformation since the
        /// gesture began.
        /// The relative coordinates of the gesture within the object space
        /// remains invariant.
        /// </summary>
        public Transformation UpdateControlPoint(UniformScaleTransformation controlTransformation)
        {
             return controlTransformation.TransformBy(controlPointToObject);
        }
    }

    /// <summary>
    /// Gesture that ties an object's position, orientation, and scale to the
    /// positions of two control points. Updating the control point matrices yields
    /// an updated object matrix such that the object has moved to preserve the
    /// local positions of the control points within the object.
    /// Example usage: Two hands grab corners of a box, and as the hands move
    /// the box stretches and pivots with the motion of the hands.
    /// </summary>
    public sealed class TwoPointTranslateRotateScaleGesture
    {
        private readonly OnePointTranslateRotateGesture onePointGesture =
            new OnePointTranslateRotateGesture();

        private Quaternion? midRotation;

        /// <summary>
        /// Begin the gesture with an initial object transformation and initial
        /// transformations for the two control points. Throughout the gesture
        /// the positions of the control points within object space will remain
        /// invariant.
        /// </summary>
        public void BeginGesture(Transformation objectTransformation,
                                 UnitScaleTransformation controlTransformation1,
                                 UnitScaleTransformation controlTransformation2)
        {
            midRotation = null;
            var midpointTransformation = ComputeMidpointTransformation(controlTransformation1,
                                                                       controlTransformation2);
            onePointGesture.BeginGesture(objectTransformation, midpointTransformation);
        }

        /// <summary>
        /// Return an updated object transformation to account for the changes
        /// in the control point transformations.
        /// </summary>
        public Transformation UpdateControlPoints(UnitScaleTransformation controlTransformation1,
                                                  UnitScaleTransformation controlTransformation2)
        {
            var midpointTransformation = ComputeMidpointTransformation(controlTransformation1,
                                                                       controlTransformation2);
            var objectTransformation = onePointGesture.UpdateControlPoint(midpointTransformation);
            
            return objectTransformation;
        }

        /// <summary>
        /// Compute a transformation that represents the "average" of two input
        /// transformations.
        /// </summary>
        private UniformScaleTransformation ComputeMidpointTransformation(
            UnitScaleTransformation controlTransformation1,
            UnitScaleTransformation controlTransformation2)
        {
            // position the origin between the two control points
            var position1 = controlTransformation1.position;
            var position2 = controlTransformation2.position;
            var position = Vector3.LerpUnclamped(position1, position2, 0.5f);

            // base the scale on the separation between the two control points
            var scale = Vector3.Distance(position1, position2);

            // choose an orientation where the line between the two control points
            // is one axis, and another axis is roughly the compromise between the
            // two up vectors of the control points
            var rotation1 = controlTransformation1.rotation;
            var rotation2 = controlTransformation2.rotation;

            // try to retain continuity between updates, by preferring a mid
            // rotation towards the previous midrotation
            if (midRotation.HasValue)
            {
                var a = Quaternion.SlerpUnclamped(rotation1, midRotation.Value, 0.5f);
                var b = Quaternion.SlerpUnclamped(rotation1, midRotation.Value, 0.5f);
                midRotation = Quaternion.SlerpUnclamped(a, b, 0.5f);
            }
            else
            {
                midRotation = Quaternion.SlerpUnclamped(rotation1, rotation2, 0.5f);
            }

            var up = midRotation.Value * Vector3.forward;
            var right = (position2 - position1).normalized;

            var rotation = Quaternion.LookRotation(right, up);

            return new UniformScaleTransformation(position, rotation, scale);
        }
    }
}