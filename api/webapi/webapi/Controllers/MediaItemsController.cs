using webapi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using webapi.Data;
using webapi.Utilities;
using webapi.Authentication;
using webapi.Services;
using Azure;

namespace webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaItemsController : ControllerBase
    {
        private readonly MediaItemsContext _mediaITemsDb;
        private IBlobService _blobService;


        private readonly UserManager<ApplicationUser> _userManager;
        public MediaItemsController(MediaItemsContext mediaITemsDb,
            UserManager<ApplicationUser> userManager,
            IBlobService blobService)

        {
            _userManager = userManager;
            _mediaITemsDb = mediaITemsDb;
            _blobService = blobService;

        }

        /// <summary>
        /// create a media item
        /// @currentUser the id of the user attemting to create a new mediItem
        /// @model - the information to create a new media item
        /// the method will create a new entry in the database with the information provided by the user
        /// @folderId - If a @model.folderId !=null this means that the media item will have a parent otherwise will be at root
        /// media items can be of @type "folder" or "file"
        /// if  @type == "file" the file in question will be uploaded to the azure blob and assign the reference to @azureUrl
        /// you can find the upload method at "UploadEncodeAndStreamFilesController.UploadFile()"
        /// </summary> 
        [Authorize]
        [HttpPost("create-media-item")]
        public async Task<ActionResult> CreateMediaItem(MediaItem model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var currentUser = await _userManager.FindByEmailAsync(claimsIdentity?.Name);

            var fm = new MediaItem();
            fm.userId = currentUser.Id;
            fm.name = model.name;
            fm.description = model.description;
            fm.type = model.type;
            fm.mediaType = model.mediaType;
            fm.modifiedAt = model.modifiedAt;
            fm.createdAt = model.createdAt;
            fm.folderId = model.folderId;
            fm.size = model.size;
            fm.azureUrl = model.azureUrl;

            try
            {
                _mediaITemsDb.MediaItems.Add(fm);
                _mediaITemsDb.SaveChanges();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Status = "Error", Message = ex.Message });

            }

            return Ok(StatusCodes.Status200OK);
        }

        /// <summary>
        /// after a media item type "file" is created, an azure function is triggered to extract "thumbnail" which is a frame of the video and uploads it to the same azure container /thumbnails
        /// after the thumbnail image is uploaded to the azure container/thumbnails, the azure functions sends a call to this control to add the azure url of the thumbnail to the db entry 
        /// that was created for the media item that triggered the azure function
        /// @Thumbnail - the information to update the media item
        /// </summary> 
        [ApiKeyAuth]
        [HttpPost("add-media-item-thumbnail")]
        public ActionResult AddThumbnailUrl(Thumbnail model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            MediaItem? result = _mediaITemsDb?.MediaItems.Where(s => s.azureUrl == model.videoAzureUrl).FirstOrDefault();

            if (result != null)
            {
                result.ThumbnailUrl = model.thumbnailAzureUrl;

                try
                {
                    _mediaITemsDb?.SaveChanges();
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new { Status = "Error", Message = ex.Message });
                }
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Status = "Error", Message = $"Error: No media item found for this azureURL {model.videoAzureUrl}" });
            }

            return Ok(StatusCodes.Status200OK);
        }

        /// <summary>
        /// get the media items of a user
        /// @currentUser
        /// @id - the id of the media item that the user wants to access
        /// the method will retrieve all the media items where its folderId is equal to the provided itemId
        /// @folderId - the folderId of the item which the passed 'itemId' belonds to. The method will use it 
        /// to retrieve the parent of requested itemId in order to track the path in the frontend
        /// </summary>
        [Authorize]
        [HttpGet("get-media-items")]
        public async Task<ActionResult> GetItems(string? id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var claimsIdentity = User.Identity as ClaimsIdentity;
            ApplicationUser? currentUser = await _userManager.FindByEmailAsync(claimsIdentity?.Name);
            MediaItem[] allResult = _mediaITemsDb.MediaItems.Where(s => s.userId == currentUser.Id).ToArray();
            MediaItem[] itemResult = Array.FindAll(allResult, item => item.folderId == id);
            MediaItem[] folders = Array.FindAll(itemResult, element => element.type == "folder");
            MediaItem[] files = Array.FindAll(itemResult, element => element.type == "file");

            // Figure out the path and attach it to the response
            // Prepare the empty paths array
            MediaItem[] pathItems = ExtensionMethods.DeepClone(allResult);
            //FileManager[] path = new FileManager[] {};
            List<MediaItem> path = new List<MediaItem>();

            // Prepare the current folder
            MediaItem? currentFolder = null;

            // Get the current folder and add it as the first entry
            if (!String.IsNullOrEmpty(id))
            {
                currentFolder = Array.Find(pathItems, item => item.Id == id);
                path.Add(currentFolder);
            }

            // Start traversing and storing the folders as a path array
            // until we hit null on the folder id

            while (!String.IsNullOrEmpty(currentFolder?.folderId))
            {
                currentFolder = Array.Find(pathItems, item => item.Id == currentFolder.folderId);

                if (currentFolder != null)
                {
                    path.Add(currentFolder);
                }

            }

            path.Reverse();

            return Ok(new { folders, files, path });
        }

        [Authorize]
        [HttpGet("get-media-item-by-id")]
        public async Task<ActionResult> GetMediaItemById(string Id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            MediaItem result = _mediaITemsDb.MediaItems.Where(s => s.Id == Id).FirstOrDefault();
            return Ok(result);
        }

        /// <summary>
        /// get the media item based on id
        /// @id - the id of the media item that the user wants to access
        /// the method will retrieve the media item where its @id is equal to the provided item id
        /// and change and save the entry in the database
        /// </summary>
        [Authorize]
        [HttpPatch("patch-media-item")]
        public async Task<ActionResult> PatchMediaItem(MediaItem model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            MediaItem[] mediaItemChilds = _mediaITemsDb.MediaItems.Where(s => s.folderId == model.Id || s.folderId == null).ToArray();
            MediaItem result = _mediaITemsDb.MediaItems.Where(s => s.Id == model.Id).FirstOrDefault();

            if (result == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Status = "Error", Message = "The instance of the object could not be retrieved" });
            }
            //check if another media item has the same name at the specified directory
            bool nameExist = Array.Exists(mediaItemChilds, element => element.name == model.name);

            if (nameExist && result.name != model.name)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Status = "Error", Message = "The name " + model.name + " already exist in directory" });
            }
            if (result.name != model.name)
            {
                result.name = model.name;

            }
            if (result.description != model.description)
            {
                result.description = model.description;

            }

            result.modifiedAt = model.modifiedAt;

            try
            {
                _mediaITemsDb.SaveChanges();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Status = "Error", Message = ex.Message });

            }

            return Ok(StatusCodes.Status200OK);
        }

        /// <summary>
        /// get the media item based on id
        /// @id - the id of the media item that the user wants to access
        /// the method will retrieve the media item where its @id is equal to the provided item id
        /// and delete the entry and all its childs from the database
        /// a trigger was defined in the db to excute "instead delete" with logic to perform the action above
        /// </summary>
        /// 
        [Authorize]
        [HttpDelete("delete-media-item")]
        public async Task<ActionResult> DeleteMediaItem(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { Status = "Error", Message = "Missing Id." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var claimsIdentity = User.Identity as ClaimsIdentity;
            var currentUser = await _userManager.FindByEmailAsync(claimsIdentity?.Name);
            if (currentUser == null)
                return Unauthorized(new { Status = "Error", Message = "User not found." });

            var item = await _mediaITemsDb.MediaItems.FirstOrDefaultAsync(s => s.Id == id);
            if (item == null || !string.Equals(item.userId, currentUser.Id, StringComparison.OrdinalIgnoreCase))
                return NotFound(new { Status = "Error", Message = $"Media item '{id}' not found." });

            var container = GetContainerNameForUser(currentUser.Id);

            // FILE: delete blob (+thumbnail) then DB row
            if (string.Equals(item.type, "file", StringComparison.OrdinalIgnoreCase))
            {
                var del = await DeleteFileAndThumbnailAsync(container, item);
                if (del.Status == "Error")
                    return StatusCode(StatusCodes.Status500InternalServerError, del);

                _mediaITemsDb.MediaItems.Remove(item);
                await _mediaITemsDb.SaveChangesAsync();
                return NoContent();
            }

            // FOLDER: gather descendants, delete blobs for file descendants, then delete rows
            if (string.Equals(item.type, "folder", StringComparison.OrdinalIgnoreCase))
            {
                //Compute subtree BEFORE DB changes (so we know which blobs to remove)
                var descendants = await GetDescendantsAsync(currentUser.Id, item.Id);
                var fileDescendants = descendants.Where(d => string.Equals(d.type, "file", StringComparison.OrdinalIgnoreCase)).ToList();

                //Delete all file blobs (+thumbnails). Fail fast if any deletion errors.
                foreach (var f in fileDescendants)
                {
                    var del = await DeleteFileAndThumbnailAsync(container, f);
                    if (del.Status == "Error")
                        return StatusCode(StatusCodes.Status500InternalServerError,
                            new { Status = "Error", Message = $"Blob delete failed for '{f.name}': {del.Message}" });
                }
                using var tx = await _mediaITemsDb.Database.BeginTransactionAsync();
                try
                {
                    // I have a DB trigger cascades (media_asset_parent_deleted), so delete the folder only
                    _mediaITemsDb.MediaItems.Remove(item);

                    await _mediaITemsDb.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new { Status = "Error", Message = ex.Message });
                }

                return NoContent();
            }

            // Unknown type
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Status = "Error", Message = $"Unsupported media item type '{item.type}'." });
        }


        private async Task<FileOperationResult> DeleteFile(string currentUserId, MediaItem file)
        {
            try
            {
                string inputAssetName = $"input-{currentUserId}";


                // Delete thumbnail if the file has a video extension
                if (IsVideoFile(file.name))
                {
                    string thumbnailFileName = $"thumbnails/{Path.GetFileNameWithoutExtension(file.name)}_thumbnail.png";

                    var isThumbnailDeleted = await _blobService.DeleteBlobIfExistsAsync(inputAssetName, thumbnailFileName);

                    if (!isThumbnailDeleted)
                    {
                        return new FileOperationResult { Status = "Error", Message = $"Thumbnail for file '{file.name}' not found." };
                    }
                }

                // Delete the main file
                var isFileDeleted = await _blobService.DeleteBlobIfExistsAsync(inputAssetName, file.name);

                if (!isFileDeleted)
                {
                    return new FileOperationResult { Status = "Error", Message = $"File '{file.name}' not found." };
                }

                return new FileOperationResult { Status = "Success", Message = $"File '{file.name}' and its thumbnail deleted successfully." };
            }
            catch (Exception ex)
            {
                return new FileOperationResult { Status = "Error", Message = ex.Message };
            }
        }


        public class FileOperationResult
        {
            public string? Status { get; set; }
            public string? Message { get; set; }
        }

        private bool IsVideoFile(string fileName)
        {
            //logic to check if the file has a video extension
            var videoExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", /* add more extensions if needed */ };
            return videoExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }


        private string GetContainerNameForUser(string userId) => $"input-{userId}";

        private static string? TryGetBlobNameFromUrl(string? url, string containerName)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;

            // AbsolutePath like: /input-<userid>/thumbnails/file_thumb.png
            var path = uri.AbsolutePath.TrimStart('/'); // input-<userid>/thumbnails/...
            if (path.StartsWith(containerName + "/", StringComparison.OrdinalIgnoreCase))
                return path.Substring(containerName.Length + 1); // thumbnails/file_thumb.png

            // Fallback: use whatever after leading slash
            return path;
        }

        private async Task<List<MediaItem>> GetDescendantsAsync(string userId, string rootFolderId)
        {
            // Load all items for this user once; traverse in memory
            var all = await _mediaITemsDb.MediaItems
                .Where(m => m.userId == userId)
                .AsNoTracking()
                .ToListAsync();

            var childrenByParent = all
                .Where(x => !string.IsNullOrEmpty(x.folderId))
                .GroupBy(x => x.folderId!)
                .ToDictionary(g => g.Key, g => g.ToList());

            var stack = new Stack<string>();
            stack.Push(rootFolderId);

            var result = new List<MediaItem>();

            while (stack.Count > 0)
            {
                var parentId = stack.Pop();
                if (!childrenByParent.TryGetValue(parentId, out var kids)) continue;

                foreach (var child in kids)
                {
                    result.Add(child);
                    if (string.Equals(child.type, "folder", StringComparison.OrdinalIgnoreCase))
                        stack.Push(child.Id);
                }
            }

            return result;
        }

        private async Task<FileOperationResult> DeleteFileAndThumbnailAsync(string containerName, MediaItem file)
        {
            try
            {
                // MAIN FILE
                // Prefer azureUrl parsing; fallback to file.name
                var fileBlobName = TryGetBlobNameFromUrl(file.azureUrl, containerName) ?? file.name;
                if (string.IsNullOrWhiteSpace(fileBlobName))
                    return new FileOperationResult { Status = "Error", Message = $"No blob name for file '{file.name}'." };

                var mainDeleted = await _blobService.DeleteBlobIfExistsAsync(containerName, fileBlobName);
                if (!mainDeleted)
                    return new FileOperationResult { Status = "Error", Message = $"Main blob for '{file.name}' not found." };

                // THUMBNAIL (if any)
                if (!string.IsNullOrWhiteSpace(file.ThumbnailUrl))
                {
                    var thumbBlobName = TryGetBlobNameFromUrl(file.ThumbnailUrl, containerName);
                    if (!string.IsNullOrWhiteSpace(thumbBlobName))
                    {
                        // best-effort: if not found, don't fail the whole op
                        await _blobService.DeleteBlobIfExistsAsync(containerName, thumbBlobName);
                    }
                }

                return new FileOperationResult { Status = "Success", Message = $"Deleted '{file.name}' (+ thumbnail if present)." };
            }
            catch (Exception ex)
            {
                return new FileOperationResult { Status = "Error", Message = ex.Message };
            }
        }


    }
}