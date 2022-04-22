using Fusi.Tools.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleBlob.Api.Models;
using SimpleBlob.Core;

namespace SimpleBlobApi.Controllers
{
    /// <summary>
    /// Items.
    /// </summary>
    /// <seealso cref="ControllerBase" />
    [ApiController]
    public sealed class ItemController : ControllerBase
    {
        private readonly ISimpleBlobStore _store;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemController"/> class.
        /// </summary>
        /// <param name="store">The store.</param>
        public ItemController(ISimpleBlobStore store)
        {
            _store = store;
        }

        /// <summary>
        /// Gets the items matching the specified filter.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>Page of matching items.</returns>
        [HttpGet("api/items")]
        [Authorize(Roles = "browser,admin")]
        [ProducesResponseType(200)]
        public ActionResult<DataPage<BlobItem>> GetItems(
            [FromQuery] BlobItemFilterModel filter)
        {
            DataPage<BlobItem> page = _store.GetItems(filter.ToFilter());
            return Ok(page);
        }

        /// <summary>
        /// Adds or updates the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        [HttpPost("api/items")]
        [Authorize(Roles = "writer,browser,admin")]
        [ProducesResponseType(200)]
        public ActionResult AddItem([FromBody] BlobItemModel item)
        {
            BlobItem i = item.ToItem();
            i.UserId = User.Identity.Name;
            _store.AddItem(i);

            return CreatedAtRoute("GetItem", new
            {
                id = item.Id
            }, i);
        }

        /// <summary>
        /// Gets the item with the specified ID.
        /// </summary>
        /// <param name="id">The item's identifier.</param>
        /// <returns>The item or 404 if not found.</returns>
        [HttpGet("api/items/{id}", Name = "GetItem")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public ActionResult<BlobItem> GetItem([FromRoute] string id)
        {
            BlobItem item = _store.GetItem(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        /// <summary>
        /// Deletes the item with the specified ID.
        /// </summary>
        /// <param name="id">The identifier.</param>
        [HttpDelete("api/items/{id}")]
        [Authorize(Roles = "writer,browser,admin")]
        public void DeleteItem([FromRoute] string id)
        {
            _store.DeleteItem(id);
        }
    }
}
