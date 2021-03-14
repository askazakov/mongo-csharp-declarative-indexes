namespace MongoDB.DeclarativeIndexes
{
    public class CollectionIndexes
    {
        public CollectionIndexes(string collectionName, params Index[] indexes)
        {
            CollectionName = collectionName;
            Indexes = indexes;
        }

        public string CollectionName { get; }

        public Index[] Indexes { get; }
    }
}