using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nanover.Grpc
{
    /// <summary>
    /// Utility methods for converting from protobuf data structures to C# objects.
    /// </summary>
    public static class Conversions
    {
        /// <summary>
        /// Retrieve a Vector3 from a list of objects, optionally
        /// starting from a given offset in the array.
        /// </summary>
        public static Vector3 GetVector3(this IReadOnlyList<object> values,
                                         int offset = 0)
        {
            return new Vector3(Convert.ToSingle(values[0 + offset]),
                               Convert.ToSingle(values[1 + offset]),
                               Convert.ToSingle(values[2 + offset]));
        }

        /// <summary>
        /// Retrieve a Quaternion from a list of objects, optionally
        /// starting from a given offset in the array.
        /// </summary>
        public static Quaternion GetQuaternion(this IReadOnlyList<object> values,
                                               int offset = 0)
        {
            return new Quaternion(Convert.ToSingle(values[0 + offset]),
                                  Convert.ToSingle(values[1 + offset]),
                                  Convert.ToSingle(values[2 + offset]),
                                  Convert.ToSingle(values[3 + offset]));
        }
    }
}