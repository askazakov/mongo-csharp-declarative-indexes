[![NuGet Status](https://img.shields.io/nuget/v/MongoDB.DeclarativeIndexes)](https://www.nuget.org/packages/MongoDB.DeclarativeIndexes)

MongoDB.DeclarativeIndexes - it is a small helper for declarative description of indexes for MongoDB.
Just list desired indexes and `IndexEnsurer` creates missing indexes and drops excess ones.


```c#
IndexEnsurer ensurer = ...
ensurer.Ensure(new CollectionIndexes("collection_name",
                                     new Index(keys: new Key("field_name", IndexType.Ascending)),
                                     new Index(keys: new[] // compound index
                                     {
                                         new Key("first_field", IndexType.Ascending),
                                         new Key("second_field", IndexType.Descending)
                                     })),
               new CollectionIndexes("another_collection",
                                     new Index(keys: new Key("a", IndexType.Ascending))));
```

You don't need write explicit `CreateOne`, `CreateMany` or `DropOne` anymore.

`IndexEnsurer` also provides `Begin` method for two-step process. This is useful in case when you want do something
between drop excess indexes and create new ones – for example you could drop non-unique index, normalize db documents
and then create unique index:

```c#
// there is collection with non-unique index

// declare unique indexes
var continuation = ensurer.Begin(new[]
{
    new CollectionIndexes("collection",
                          new Index(keys: new Key("field", IndexType.Ascending),
                                    unique: true))
});

// do some stuff with 'collection'

// and continue with indexes
continuation.Continue();
```

`EnsurerContinuation` can rollback if something goes wrong and you want to abort index dropping:

```c#
var continuation = ensurer.Begin(new[]
{
    new CollectionIndexes("collection",
                          new Index(keys: new Key("field", IndexType.Ascending),
                                    unique: true))
});
try {
    // do some stuff 
}
catch (Exception)
{
    continuation.Rollback();
    throw
}

continuation.Continue();
```

To avoid dependency on [MongoDB.Driver](https://www.nuget.org/packages/MongoDB.Driver/) (or any other mongodb library)
MongoDB.DeclarativeIndexes uses IoC – `IndexEnsurer` takes `IDatabase` abstraction as constructor argument:
```c#
public interface IDatabase
{
    IEnumerable<string> ListCollectionNames();
    
    IEnumerable<Dictionary<string, object>> ListIndexes(string collectionName);

    void CreateManyIndexes(string collectionName, IEnumerable<Index> indexes);

    void DropOneIndex(string collectionName, string indexName);
}
```
So you need to implement it with real mongodb client:
```c#
using MongoDB.Bson;  // from MongoDB.Driver
using MongoDB.Driver;

...

class Adapter : IDatabase
{
    private readonly IMongoDatabase _mongoDatabase;

    public Adapter(IMongoDatabase mongoDatabase)
    {
        _mongoDatabase = mongoDatabase;
    }

    public void CreateManyIndexes(string collectionName, IEnumerable<Index> indexes)
    {
        var collection = _mongoDatabase.GetCollection<BsonDocument>(collectionName);
        var createIndexModels =
            indexes.Select(i => new CreateIndexModel<BsonDocument>(KeysDefinitionFrom(i), OptionsFrom(i)));
        collection.Indexes.CreateMany(createIndexModels);
    }

    public IEnumerable<Dictionary<string, object>> ListIndexes(string collectionName)
    {
        var collection = _mongoDatabase.GetCollection<BsonDocument>(collectionName);
        return collection.Indexes.List().ToEnumerable().Select(index => index.ToDictionary());
    }

    public IEnumerable<string> ListCollectionNames()
    {
        return _mongoDatabase.ListCollectionNames().ToEnumerable();
    }

    public void DropOneIndex(string collectionName, string indexName)
    {
        _mongoDatabase.GetCollection<BsonDocument>(collectionName).Indexes.DropOne(indexName);
    }

    private static CreateIndexOptions OptionsFrom(Index index)
    {
        return new CreateIndexOptions {Unique = index.Unique};
    }

    private static IndexKeysDefinition<BsonDocument> KeysDefinitionFrom(Index index)
    {
        return new BsonDocumentIndexKeysDefinition<BsonDocument>(new BsonDocument(index.ToDb()));
    }
}

...

    var client = new MongoClient("mongodb://localhost:27017");
    var database = client.GetDatabase("test");

    var adapter = new Adapter(database);

    var ensurer = new IndexEnsurer(adapter);
```