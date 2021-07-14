using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleBlob.Api.Models;
using SimpleBlob.Core;
using SimpleBlobApi.Models;
using System.Collections.Generic;
using System.Linq;

namespace SimpleBlobApi.Controllers
{
    /// <summary>
    /// Item's properties.
    /// </summary>
    [ApiController]
    public sealed class ItemPropertyController : ControllerBase
    {
        private readonly ISimpleBlobStore _store;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemPropertyController"/>
        /// class.
        /// </summary>
        /// <param name="store">The store.</param>
        public ItemPropertyController(ISimpleBlobStore store)
        {
            _store = store;
        }

        /// <summary>
        /// Gets the properties of the item with the specified ID.
        /// </summary>
        /// <param name="id">The item identifier.</param>
        /// <returns>Array of properties.</returns>
        [HttpGet("api/items/{id}/properties", Name = "GetProperties")]
        [Authorize]
        [ProducesResponseType(200)]
        public ActionResult<BlobItemProperty[]> GetProperties([FromRoute] string id)
        {
            BlobItemProperty[] props = _store.GetProperties(id).ToArray();
            return Ok(props);
        }

        /// <summary>
        /// Adds the specified properties to a BLOB item.
        /// </summary>
        /// <param name="model">The model with item ID and properties.</param>
        [HttpPost("api/items/{id}/properties/add")]
        [Authorize(Roles = "writer,browser,admin")]
        [ProducesResponseType(200)]
        public ActionResult AddProperties(
            [FromBody] BlobItemPropertiesModel model)
        {
            IList<BlobItemProperty> props = model.ToProperties();
            _store.AddProperties(model.ItemId, props);
            return CreatedAtRoute("GetProperties", new
            {
                id = model.ItemId
            }, props);
        }

        /// <summary>
        /// Sets the specified properties for a BLOB item.
        /// </summary>
        /// <param name="model">The model with item ID and properties.</param>
        [HttpPost("api/items/{id}/properties/set")]
        [Authorize(Roles = "writer,browser,admin")]
        [ProducesResponseType(200)]
        public ActionResult SetProperties(
            [FromBody] BlobItemPropertiesModel model)
        {
            IList<BlobItemProperty> props = model.ToProperties();
            _store.SetProperties(model.ItemId, props);
            return CreatedAtRoute("GetProperties", new
            {
                id = model.ItemId
            }, props);
        }

        /// <summary>
        /// Deletes all the properties of the BLOB item with the specified ID.
        /// </summary>
        /// <param name="id">The item's identifier.</param>
        [HttpDelete("api/items/{id}/properties")]
        [Authorize(Roles = "writer,browser,admin")]
        public void DeleteProperties([FromRoute] string id)
        {
            _store.DeleteProperties(id);
        }
    }
}
