using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nanover.Core
{
    /// <summary>
    /// Utility methods for reflection not included in C#.
    /// </summary>
    public static class ReflectionUtility
    {
        /// <summary>
        /// Get the field in a given type or its ancestors of the given name, with the
        /// given <see cref="BindingFlags" />.
        /// </summary>
        public static FieldInfo GetFieldInSelfOrParents(this Type type,
                                                        string name,
                                                        BindingFlags flags)
        {
            while (type != null && type != typeof(object))
            {
                var field = type.GetField(name, flags);
                if (field != null)
                    return field;
                type = type.BaseType;
            }

            return null;
        }

        /// <summary>
        /// Get all fields in a given type and its ancestors, with the given
        /// <see cref="BindingFlags" />.
        /// </summary>
        public static IEnumerable<FieldInfo> GetFieldsInSelfOrParents(this Type type,
                                                                      BindingFlags flags)
        {
            while (type != null && type != typeof(object))
            {
                foreach (var field in type.GetFields(flags))
                    yield return field;
                type = type.BaseType;
            }
        }

        /// <summary>
        /// Get the property in a given type or its ancestors of the given name, with the
        /// given <see cref="BindingFlags" />.
        /// </summary>
        public static PropertyInfo GetPropertyInSelfOrParents(this Type type,
                                                              string name,
                                                              BindingFlags flags)
        {
            while (type != null && type != typeof(object))
            {
                var property = type.GetProperty(name, flags);
                if (property != null)
                    return property;
                type = type.BaseType;
            }

            return null;
        }
    }
}