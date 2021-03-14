using System.Collections.Generic;

namespace MongoDB.DeclarativeIndexes
{
    /*
     * Abstraction for eliminate dependency on MongoDB.Driver
     */
    public interface IDatabase
    {
        void CreateManyIndexes(string collectionName, IEnumerable<Index> indexes);

        IEnumerable<Dictionary<string, object>> ListIndexes(string collectionName);

        IEnumerable<string> ListCollectionNames();

        void DropOneIndex(string collectionName, string indexName);
    }
}