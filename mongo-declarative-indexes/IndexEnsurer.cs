using System.Collections.Generic;
using System.Linq;

namespace MongoDB.DeclarativeIndexes
{
    public class IndexEnsurer
    {
        private static readonly IndexIgnoringNameEqualityComparer IndexIgnoringNameEqualityComparer =
            new IndexIgnoringNameEqualityComparer();

        private readonly IDatabase _database;

        public IndexEnsurer(IDatabase database)
        {
            _database = database;
        }

        public EnsurerContinuation Begin(params CollectionIndexes[] indexes)
        {
            var targetIndexesByCollectionName = indexes.ToDictionary(i => i.CollectionName, i => i.Indexes);

            var existingIndexes = _database.ListCollectionNames()
                .Select(name => new CollectionIndexes(name, GetExistingIndexes(name))).ToList();

            var extraIndexes = existingIndexes
                .Select(i => new CollectionIndexes(i.CollectionName,
                    i.Indexes
                        .Where(x =>
                            !targetIndexesByCollectionName
                                .ContainsKey(i.CollectionName) ||
                            !targetIndexesByCollectionName[i.CollectionName]
                                .Contains(x, IndexIgnoringNameEqualityComparer)).ToArray())).ToList();

            foreach (var collectionIndex in extraIndexes)
            foreach (var index in collectionIndex.Indexes)
                _database.DropOneIndex(collectionIndex.CollectionName, index.Name);

            var existingIndexesByCollectionName = existingIndexes.ToDictionary(i => i.CollectionName, i => i.Indexes);

            var missingIndexes = indexes.Select(collectionIndex =>
                new CollectionIndexes(collectionIndex.CollectionName,
                    GetMissingIndexes(existingIndexesByCollectionName,
                        collectionIndex))).ToList();


            return new EnsurerContinuation(_database, missingIndexes, extraIndexes);
        }

        public void Ensure(params CollectionIndexes[] indexes)
        {
            var continuation = Begin(indexes);
            continuation.Continue();
        }

        private Index[] GetExistingIndexes(string collectionName)
        {
            return _database.ListIndexes(collectionName).Select(Index.FromDb).Where(IsNotIdIndex).ToArray();
        }

        private static bool IsNotIdIndex(Index index)
        {
            return index.Keys.Select(k => k.Field).Any(k => k != "_id");
        }

        private static Index[] GetMissingIndexes(IReadOnlyDictionary<string, Index[]> existingIndexesByCollectionName,
            CollectionIndexes collectionIndex)
        {
            return collectionIndex.Indexes
                .Where(i => !existingIndexesByCollectionName.ContainsKey(collectionIndex.CollectionName) ||
                            !existingIndexesByCollectionName[collectionIndex.CollectionName]
                                .Contains(i, IndexIgnoringNameEqualityComparer)).ToArray();
        }
    }

    internal class IndexIgnoringNameEqualityComparer : IEqualityComparer<Index>
    {
        public bool Equals(Index x, Index y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Keys.SequenceEqual(y.Keys) && x.Unique == y.Unique;
        }

        public int GetHashCode(Index obj)
        {
            unchecked
            {
                return ((obj.Keys != null ? obj.Keys.GetHashCode() : 0) * 397) ^ obj.Unique.GetHashCode();
            }
        }
    }

    public sealed class EnsurerContinuation
    {
        private readonly IDatabase _database;
        private readonly List<CollectionIndexes> _extraIndexes;

        private readonly List<CollectionIndexes> _missingIndexes;

        internal EnsurerContinuation(IDatabase database, List<CollectionIndexes> missingIndexes,
            List<CollectionIndexes> extraIndexes)
        {
            _missingIndexes = missingIndexes;
            _extraIndexes = extraIndexes;
            _database = database;
        }


        public void Rollback()
        {
            CreateIndexes(_extraIndexes);
        }

        public void Continue()
        {
            CreateIndexes(_missingIndexes);
        }

        private void CreateIndexes(IEnumerable<CollectionIndexes> indexes)
        {
            foreach (var collectionIndex in indexes)
                _database.CreateManyIndexes(collectionIndex.CollectionName, collectionIndex.Indexes);
        }
    }
}