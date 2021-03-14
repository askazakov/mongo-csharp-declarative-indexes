using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.DeclarativeIndexes.helpers;

namespace MongoDB.DeclarativeIndexes
{
    public class Index
    {
        public Index(bool unique = false, string name = null, params Key[] keys)
        {
            Keys = keys;
            Unique = unique;
            Name = name;
        }

        public Key[] Keys { get; }
        public bool Unique { get; }
        public string Name { get; }

        public static Index FromDb(Dictionary<string, object> dbDocument)
        {
            /*
             * {
             *   "v": 2,
             *   "key": {"field_name": "type"},
             *   "name": "index_name",
             *   "ns": "{namespace}
             * }
             */
            var dbKeys = (IEnumerable<KeyValuePair<string, object>>) dbDocument["key"];
            return new Index(keys: dbKeys.Select(x => new Key(x.Key, IndexType.Ascending)).ToArray(),
                             name: (string) dbDocument.GetValueOrDefault("name"));
        }

        public Dictionary<string, object> ToDb()
        {
            return Keys.ToDictionary(k => k.Field, k => ConvertIndexTypeToDb(k.IndexType));
        }

        private static object ConvertIndexTypeToDb(IndexType indexType)
        {
            // ReSharper disable once HeapView.BoxingAllocation
            return indexType switch
            {
                IndexType.Ascending => 1,
                IndexType.Descending => -1,
                // IndexType.Text => "text",
                _ => throw new ArgumentOutOfRangeException(nameof(indexType), indexType, null)
            };
        }
    }

    public class Key
    {
        public Key(string field, IndexType indexType)
        {
            Field = field;
            IndexType = indexType;
        }

        public string Field { get; }

        public IndexType IndexType { get; }

        protected bool Equals(Key other)
        {
            return Field == other.Field && IndexType == other.IndexType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Key) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Field != null ? Field.GetHashCode() : 0) * 397) ^ (int) IndexType;
            }
        }
    }

    public enum IndexType
    {
        Ascending,
        Descending,
        // Text
    }
}