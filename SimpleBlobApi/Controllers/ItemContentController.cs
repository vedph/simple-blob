using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SimpleBlob.Core;

namespace SimpleBlobApi.Controllers
{
    /// <summary>
    /// Item's content.
    /// </summary>
    /// <seealso cref="ControllerBase" />
    [ApiController]
    public sealed class ItemContentController : ControllerBase
    {
        private readonly ISimpleBlobStore _store;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContentController"/>
        /// class.
        /// </summary>
        /// <param name="store">The store.</param>
        public ItemContentController(ISimpleBlobStore store)
        {
            _store = store;
        }

        /// <summary>
        /// Uploads the BLOB item's content.
        /// </summary>
        /// <param name="file">The form file to upload.</param>
        /// <param name="mimeType">The MIME type.</param>
        /// <param name="id">The BLOB item's identifier.</param>
        [HttpPost("items/{id}/content")]
        public IActionResult UploadContent(IFormFile file,
            [FromQuery] string mimeType,
            [FromQuery] string id)
        {
            _store.SetContent(new BlobItemContent
            {
                ItemId = id,
                MimeType = mimeType,
                Content = file.OpenReadStream(),
                UserId = User.Identity.Name
            });
            return CreatedAtRoute("DownloadContent", new
            {
                id
            });
        }

        /// <summary>
        /// Downloads the BLOB item's content.
        /// </summary>
        /// <param name="id">The item's identifier.</param>
        /// <returns>Content.</returns>
        [HttpGet("items/{id}/content", Name = "DownloadContent")]
        public FileResult DownloadContent([FromRoute] string id)
        {
            BlobItemContent item = _store.GetContent(id);
            return File(item.Content, item.MimeType);
        }
    }
}
