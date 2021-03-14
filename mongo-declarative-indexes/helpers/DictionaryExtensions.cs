using System;
using System.Collections.Generic;

namespace MongoDB.DeclarativeIndexes.helpers
{
    // get rid of after drop netstandard2.0 support
    internal static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue defaultValue = default)
            where TKey : notnull
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));

            return dictionary.TryGetValue(key, out var value)
                ? value
                : defaultValue;
        }
    }
}