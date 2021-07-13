using Fusi.Tools.Data;
using System;
using System.Collections.Generic;

namespace SimpleBlob.Core
{
    /// <summary>
    /// The simple BLOB store interface.
    /// </summary>
    public interface ISimpleBlobStore : IDisposable
    {
        /// <summary>
        /// Gets the code for the store schema.
        /// </summary>
        /// <returns>Code.</returns>
        public string GetSchema();

        /// <summary>
        /// Gets the specified page of items.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>The page.</returns>
        DataPage<BlobItem> GetItems(BlobItemFilter filter);

        /// <summary>
        /// Adds or updates the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        void AddItem(BlobItem item);

        /// <summary>
        /// Gets the item with the specified identifier.
        /// </summary>
        /// <param name="id">The item's identifier.</param>
        /// <returns>The item, or null if not found.</returns>
        BlobItem GetItem(string id);

        /// <summary>
        /// Deletes the item with the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        void DeleteItem(string id);

        /// <summary>
        /// Sets the content for the item with the specified identifier.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>True if set, false if item not found.</returns>
        bool SetContent(BlobItemContent content);

        /// <summary>
        /// Gets the content of the item with the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>The content, or null if not found.</returns>
        BlobItemContent GetContent(string id);

        /// <summary>
        /// Gets the properties of the item with the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Properties.</returns>
        IList<BlobItemProperty> GetProperties(string id);

        /// <summary>
        /// Adds the properties.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="properties">The properties.</param>
        /// <returns>True if added, false if target item not found.</returns>
        bool AddProperties(string id, IList<BlobItemProperty> properties);

        /// <summary>
        /// Sets the properties.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="properties">The properties.</param>
        /// <returns>True if set, false if target item not found.</returns>
        bool SetProperties(string id, IList<BlobItemProperty> properties);

        /// <summary>
        /// Deletes the properties.
        /// </summary>
        /// <param name="id">The identifier.</param>
        void DeleteProperties(string id);
    }
}
