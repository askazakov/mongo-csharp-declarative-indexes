using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.DeclarativeIndexes;
using NSubstitute;
using Xunit;
using Index = MongoDB.DeclarativeIndexes.Index;

namespace mongo_declarative_indexes.Tests
{
    public class IndexEnsurerShould
    {
        [Fact]
        public void Ensure_CreatesMissingIndexes()
        {
            var database = Substitute.For<IDatabase>();
            var ensurer = new IndexEnsurer(database);
            var expectedIndexes = new[] {new Index(keys: new Key("field", IndexType.Ascending))};
            ensurer.Ensure(new CollectionIndexes("testCollection", expectedIndexes));

            database.Received().CreateManyIndexes("testCollection",
                                                  Arg.Is<Index[]>(actualIndexes =>
                                                                      actualIndexes.SequenceEqual(expectedIndexes)));
        }

        [Fact]
        public void Ensure_DropsExtraIndexes()
        {
            var database = Substitute.For<IDatabase>();
            var extraIndex = new Dictionary<string, object>
            {
                {"v", 2},
                {"key", new Dictionary<string, object> {{"field", 1}}},
                {"name", "field_1"},
                {"ns", "test.collections"}
            };
            database.ListCollectionNames().Returns(new[] {"collectionName"});
            database.ListIndexes("collectionName").Returns(new[] {extraIndex});

            var ensurer = new IndexEnsurer(database);
            ensurer.Ensure(Array.Empty<CollectionIndexes>());

            database.DidNotReceiveWithAnyArgs().CreateManyIndexes(default, default);
            database.Received().DropOneIndex("collectionName", "field_1");
        }

        [Fact]
        public void Ensure_DoesNothingWithIdIndex()
        {
            var database = Substitute.For<IDatabase>();
            var idIndex = new Dictionary<string, object>
            {
                {"v", 2},
                {"key", new Dictionary<string, object> {{"_id", 1}}},
                {"name", "_id_"},
                {"ns", "test.collections"}
            };
            database.ListCollectionNames().Returns(new[] {"collectionName"});
            database.ListIndexes("collectionName").Returns(new[] {idIndex});

            var ensurer = new IndexEnsurer(database);
            ensurer.Ensure(Array.Empty<CollectionIndexes>());

            database.DidNotReceiveWithAnyArgs().CreateManyIndexes(default, default);
            database.DidNotReceiveWithAnyArgs().DropOneIndex(default, default);
        }

        [Fact]
        public void Ensure_DropsExtraAndCreatesMissingIndexes()
        {
            var database = Substitute.For<IDatabase>();
            var extraIndex = new Dictionary<string, object>
            {
                {"v", 2},
                {"key", new Dictionary<string, object> {{"field", 1}}},
                {"name", "field_1"},
                {"ns", "test.collections"}
            };
            var remainingDbIndex = new Dictionary<string, object>
            {
                {"v", 2},
                {"key", new Dictionary<string, object> {{"another_field", 1}}},
                {"name", "another_field_1"},
                {"ns", "test.collections"}
            };
            database.ListCollectionNames().Returns(new[] {"collectionName"});
            database.ListIndexes("collectionName").Returns(new[] {extraIndex, remainingDbIndex});


            var expectedCreatedIndexes = new[] {new Index(keys: new Key("yet_another_field", IndexType.Descending))};
            var ensurer = new IndexEnsurer(database);
            var remainingIndex = new Index(keys: new Key("another_field", IndexType.Ascending));
            ensurer.Ensure(new CollectionIndexes("testCollection",
                                                 expectedCreatedIndexes.Append(remainingIndex).ToArray()));
            database.Received().DropOneIndex("collectionName", "field_1");
            database.Received().CreateManyIndexes("testCollection",
                                                  Arg.Is<Index[]>(actualIndexes =>
                                                                      actualIndexes
                                                                          .SequenceEqual(expectedCreatedIndexes)));
        }
    }
}