using System;
using System.Collections.Generic;
using System.Linq;

namespace Nanover.Core
{
    /// <summary>
    /// Useful extensions for using dictionaries.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Deconstruct method for <see cref="KeyValuePair{TKey,TValue}" />, so
        /// dictionaries can be used with tuples.
        /// </summary>
        public static void Deconstruct<TKey, TValue>(
            this KeyValuePair<TKey, TValue> kvp,
            out TKey key,
            out TValue value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }

        /// <summary>
        /// Try to get a value out of a dictionary of type T, returning false if the key is not present or the present value is of the wrong type.
        /// </summary>
        public static bool TryGetValue<T>(this IDictionary<string, object> dictionary, string key, out T value)
        {
            if (dictionary.TryGetValue(key, out var potentialValue) 
                && potentialValue is T valueOfCorrectType)
            {
                value = valueOfCorrectType;
                return true;
            }

            value = default;
            return false;
        }

        public static bool TryGetStructure(this IDictionary<string, object> dictionary, string key, out IDictionary<string, object> value)
        {
            if (dictionary.TryGetValue(key, out var potentialValue))
            {
                if (potentialValue is IDictionary<string, object> dict1)
                {
                    value = dict1;
                    return true;
                }
                else if (potentialValue is IDictionary<object, object> dict2)
                {
                    value = dict2.StringifyKeys();
                    return true;
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Get an item of the dictionary of the given type <typeparamref name="T" />,
        /// returning the default if the key is not present and throwing an exception if
        /// there is a value for the given key, but the type is incompatible.
        /// </summary>
        public static T GetValueOrDefault<T>(this IDictionary<string, object> dictionary,
                                             string id, T defaultValue = default)
        {
            if (dictionary.TryGetValue(id, out var value))
            {
                if (value is T cast)
                    return cast;
                if (value is null)
                    return defaultValue;
                throw new InvalidOperationException(
                    $"Value with id {id} is of incompatible type {value?.GetType()}");
            }

            return defaultValue;
        }

        /// <summary>
        /// Get an array in the dictionary of the given type <typeparamref name="T"/>
        /// returning an empty array if the key is not present and throwing an exception if
        /// there is an array for the given key, but the type is incompatible.
        /// </summary>
        public static T[] GetArrayOrEmpty<T>(this IDictionary<string, object> dictionary,
                                             string id)
        {
            return dictionary.GetValueOrDefault<T[]>(id) ?? new T[0];
        }

        public static IDictionary<string, TValue> StringifyKeys<TValue>(this IDictionary<object, TValue> dictionary)
        {
            return dictionary.ToDictionary(pair => pair.Key.ToString(), pair => pair.Value);
        }
    }
}