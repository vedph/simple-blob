﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SimpleBlob.Api.Models;
using SimpleBlob.Core;

namespace SimpleBlobApi.Controllers;

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
    [Authorize(Roles = "admin,browser,writer")]
    [HttpPost("api/contents/{id}")]
    public IActionResult UploadContent(
        // https://stackoverflow.com/questions/52294830/iformfile-is-always-null-when-receiving-file-from-console-app
        [FromForm(Name = "file")] IFormFile file,
        [FromForm] string mimeType,
        [FromForm] string id)
    {
        _store.SetContent(new BlobItemContent
        {
            ItemId = id,
            MimeType = mimeType,
            Content = file.OpenReadStream(),
            UserId = User.Identity.Name
        });
        // mind the final null!
        // https://stackoverflow.com/questions/68397822/asp-net-5-web-api-createdataction-post-returns-500
        return CreatedAtAction(nameof(DownloadContent), new
        {
            id = id
        }, null);
    }

    /// <summary>
    /// Downloads the BLOB item's content.
    /// </summary>
    /// <param name="id">The item's identifier.</param>
    /// <returns>Content.</returns>
    [Authorize]
    [ActionName("DownloadContent")]
    [HttpGet("api/contents/{id}")]
    public ActionResult DownloadContent([FromRoute] string id)
    {
        BlobItemContent item = _store.GetContent(id, false);
        if (item == null) return NotFound();
        return File(item.Content, item.MimeType);
    }

    /// <summary>
    /// Gets the BLOB item's content metadata.
    /// </summary>
    /// <param name="id">The item's identifier.</param>
    /// <returns>Metadata. If the item was not found, an empty metadata
    /// object is returned rather than 404.</returns>
    [Authorize]
    [HttpGet("api/contents/{id}/meta")]
    [ProducesResponseType(200)]
    public ActionResult<BlobItemContentMetaModel> GetContentMetadata(
        [FromRoute] string id)
    {
        BlobItemContent item = _store.GetContent(id, true);
        return Ok(item != null
            ? new BlobItemContentMetaModel
            {
                ItemId = item.ItemId,
                MimeType = item.MimeType,
                Hash = item.Hash,
                Size = item.Size,
                UserId = item.UserId,
                DateModified = item.DateModified
            } : new BlobItemContentMetaModel());
    }
}
