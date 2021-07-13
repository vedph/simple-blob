using Fusi.DbManager;
using Fusi.DbManager.PgSql;
using SimpleBlob.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace SimpleBlob.PgSql.Test
{
    // https://github.com/xunit/xunit/issues/1999

    [CollectionDefinition(nameof(NonParallelResourceCollection),
        DisableParallelization = true)]
    public class NonParallelResourceCollection { }

    [Collection(nameof(NonParallelResourceCollection))]
    public sealed class PgSqlSimpleBlobStoreTest
    {
        private const string DB = "blob-test";
        private const string CS =
            "User ID=postgres;Password=postgres;Host=localhost;Port=5432;Database={0};";
        private readonly IDbManager _dbManager;
        private readonly PgSqlSimpleBlobStore _store;

        public PgSqlSimpleBlobStoreTest()
        {
            _dbManager = new PgSqlDbManager(CS);
            _store = new PgSqlSimpleBlobStore(string.Format(CS, DB));
        }

        private void Init()
        {
            if (!_dbManager.Exists(DB))
                _dbManager.CreateDatabase(DB, _store.GetSchema(), null);
            else
                _dbManager.ClearDatabase(DB);
        }

        [Fact]
        public void AddItem_NotExisting_Added()
        {
            Init();
            BlobItem item = new BlobItem
            {
                Id = "alpha/1",
                UserId = "zeus"
            };
            _store.AddItem(item);

            BlobItem item2 = _store.GetItem(item.Id);
            Assert.NotNull(item2);
            Assert.Equal(item.Id, item2.Id);
            Assert.Equal(item.UserId, item2.UserId);
        }

        [Fact]
        public void AddItem_Existing_Updated()
        {
            Init();
            BlobItem item = new BlobItem
            {
                Id = "alpha/1",
                UserId = "zeus"
            };
            _store.AddItem(item);

            item.UserId = "hera";
            _store.AddItem(item);

            BlobItem item2 = _store.GetItem(item.Id);
            Assert.NotNull(item2);
            Assert.Equal(item.Id, item2.Id);
            Assert.Equal(item.UserId, item2.UserId);
        }

        [Fact]
        public void DeleteItem_NotExisting_Nope()
        {
            Init();
            _store.DeleteItem("notexisting");
            Assert.Null(_store.GetItem("notexisting"));
        }

        [Fact]
        public void DeleteItem_Existing_Deleted()
        {
            Init();
            BlobItem item = new BlobItem
            {
                Id = "alpha/1",
                UserId = "zeus"
            };
            _store.AddItem(item);

            _store.DeleteItem(item.Id);
            Assert.Null(_store.GetItem(item.Id));
        }

        private static IList<byte> ReadStreamToEnd(Stream stream)
        {
            using BinaryReader reader = new BinaryReader(stream);
            byte[] buf;
            List<byte> bytes = new List<byte>();
            do
            {
                buf = reader.ReadBytes(8192);
                bytes.AddRange(buf);
            } while (buf.Length == 8192);
            return bytes;
        }

        [Fact]
        public void SetContent_NotExisting_False()
        {
            Init();

            byte[] buf = Encoding.UTF8.GetBytes("Hello world");
            BlobItemContent content = new BlobItemContent
            {
                ItemId = "notexisting",
                UserId = "zeus",
                Content = new MemoryStream(buf)
            };
            Assert.False(_store.SetContent(content));
        }

        [Fact]
        public void SetContent_Existing_True()
        {
            Init();
            BlobItem item = new BlobItem
            {
                Id = "alpha/1",
                UserId = "zeus"
            };
            _store.AddItem(item);

            byte[] buf = Encoding.UTF8.GetBytes("Hello world");
            BlobItemContent content = new BlobItemContent
            {
                ItemId = item.Id,
                UserId = "zeus",
                Content = new MemoryStream(buf),
                MimeType = "text/plain"
            };
            Assert.True(_store.SetContent(content));

            BlobItemContent content2 = _store.GetContent(item.Id);
            byte[] buf2 = ReadStreamToEnd(content2.Content).ToArray();
            Assert.Equal(buf.Length, buf2.Length);
            for (int i = 0; i < buf.Length; i++)
                Assert.Equal(buf[i], buf2[i]);
        }

        [Fact]
        public void AddProperties_NotExisting_False()
        {
            Init();
            Assert.False(
                _store.AddProperties("notexisting", new List<BlobItemProperty>
            {
                new BlobItemProperty
                {
                    ItemId = "notexisting",
                    Name = "title",
                    Value = "Hello world"
                }
            }));
        }

        [Fact]
        public void AddProperties_Existing_True()
        {
            Init();
            BlobItem item = new BlobItem
            {
                Id = "alpha/1",
                UserId = "zeus"
            };
            _store.AddItem(item);

            Assert.True(
                _store.AddProperties(item.Id, new List<BlobItemProperty>
            {
                new BlobItemProperty
                {
                    ItemId = item.Id,
                    Name = "title",
                    Value = "Hello world"
                },
                new BlobItemProperty
                {
                    ItemId = item.Id,
                    Name = "note",
                    Value = "A note"
                }
            }));

            IList<BlobItemProperty> properties = _store.GetProperties(item.Id);
            Assert.Equal(2, properties.Count);

            BlobItemProperty prop = properties[0];
            Assert.Equal(item.Id, prop.ItemId);
            Assert.Equal("note", prop.Name);
            Assert.Equal("A note", prop.Value);

            prop = properties[1];
            Assert.Equal(item.Id, prop.ItemId);
            Assert.Equal("title", prop.Name);
            Assert.Equal("Hello world", prop.Value);
        }

        [Fact]
        public void AddProperties_ExistingSameName_True()
        {
            Init();
            BlobItem item = new BlobItem
            {
                Id = "alpha/1",
                UserId = "zeus"
            };
            _store.AddItem(item);
            _store.AddProperties(item.Id, new List<BlobItemProperty>
            {
                new BlobItemProperty
                {
                    ItemId = item.Id,
                    Name = "title",
                    Value = "First title"
                }
            });

            Assert.True(
                _store.AddProperties(item.Id, new List<BlobItemProperty>
            {
                new BlobItemProperty
                {
                    ItemId = item.Id,
                    Name = "title",
                    Value = "Second title"
                },
                new BlobItemProperty
                {
                    ItemId = item.Id,
                    Name = "note",
                    Value = "A note"
                }
            }));

            IList<BlobItemProperty> properties = _store.GetProperties(item.Id);
            Assert.Equal(3, properties.Count);

            BlobItemProperty prop = properties[0];
            Assert.Equal(item.Id, prop.ItemId);
            Assert.Equal("note", prop.Name);
            Assert.Equal("A note", prop.Value);

            prop = properties[1];
            Assert.Equal(item.Id, prop.ItemId);
            Assert.Equal("title", prop.Name);
            Assert.Equal("First title", prop.Value);

            prop = properties[2];
            Assert.Equal(item.Id, prop.ItemId);
            Assert.Equal("title", prop.Name);
            Assert.Equal("Second title", prop.Value);
        }

        [Fact]
        public void SetProperties_NotExisting_False()
        {
            Init();
            Assert.False(
                _store.SetProperties("notexisting", new List<BlobItemProperty>
            {
                new BlobItemProperty
                {
                    ItemId = "notexisting",
                    Name = "title",
                    Value = "Hello world"
                }
            }));
        }

        [Fact]
        public void SetProperties_Existing_True()
        {
            Init();
            BlobItem item = new BlobItem
            {
                Id = "alpha/1",
                UserId = "zeus"
            };
            _store.AddItem(item);
            _store.AddProperties(item.Id, new List<BlobItemProperty>
            {
                new BlobItemProperty
                {
                    ItemId = item.Id,
                    Name = "fate",
                    Value = "I will be deleted"
                }
            });

            Assert.True(
                _store.SetProperties(item.Id, new List<BlobItemProperty>
            {
                new BlobItemProperty
                {
                    ItemId = item.Id,
                    Name = "title",
                    Value = "Hello world"
                },
                new BlobItemProperty
                {
                    ItemId = item.Id,
                    Name = "note",
                    Value = "A note"
                }
            }));

            IList<BlobItemProperty> properties = _store.GetProperties(item.Id);
            Assert.Equal(2, properties.Count);

            BlobItemProperty prop = properties[0];
            Assert.Equal(item.Id, prop.ItemId);
            Assert.Equal("note", prop.Name);
            Assert.Equal("A note", prop.Value);

            prop = properties[1];
            Assert.Equal(item.Id, prop.ItemId);
            Assert.Equal("title", prop.Name);
            Assert.Equal("Hello world", prop.Value);
        }

        [Fact]
        public void DeleteProperties_NotExisting_Nope()
        {
            Init();
            _store.DeleteProperties("notexisting");
            Assert.Empty(_store.GetProperties("notexisting"));
        }

        [Fact]
        public void DeleteProperties_Existing_Deleted()
        {
            Init();
            BlobItem item = new BlobItem
            {
                Id = "alpha/1",
                UserId = "zeus"
            };
            _store.AddItem(item);
            _store.AddProperties(item.Id, new List<BlobItemProperty>
            {
                new BlobItemProperty
                {
                    ItemId = item.Id,
                    Name = "title",
                    Value = "Hello world"
                },
                new BlobItemProperty
                {
                    ItemId = item.Id,
                    Name = "note",
                    Value = "A note"
                }
            });

            _store.DeleteProperties(item.Id);

            Assert.Empty(_store.GetProperties(item.Id));
        }
    }
}
