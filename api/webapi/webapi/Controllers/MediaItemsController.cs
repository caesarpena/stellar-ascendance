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
                    new { Status = "Error", Message = "The name "+ model.name+" already exist in directory" });
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
        [Authorize]
        [HttpDelete("delete-media-item")]
        public async Task<ActionResult> DeleteMediaItem(string? id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var currentUser = await _userManager.FindByEmailAsync(claimsIdentity?.Name);
            MediaItem? file = _mediaITemsDb?.MediaItems.Where(s => s.Id == id).FirstOrDefault();

            if (file != null)
            {
                try
                {
                    
                    FileOperationResult azResult = await DeleteFile(currentUser.Id, file);

                    if (azResult.Status == "Error")
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError,
                        new { Status = "Error", Message = $"Can not delete {file.name}" });
                    }

                    _mediaITemsDb?.MediaItems.Remove(file);

                    if (_mediaITemsDb != null)
                    {
                        await _mediaITemsDb.SaveChangesAsync();
                    }
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
                new { Status = "Error", Message = $"Can not find file {file.name}" });
            }

            return Ok(StatusCodes.Status200OK);
        }

/*
        [Authorize]
        [HttpDelete("delete-thumbnail")]
        public async Task<ActionResult> DeleteThumbnail(string? fileName) 
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var currentUser = await _userManager.FindByEmailAsync(claimsIdentity?.Name);

            try
            {
                FileOperationResult azResult = await DeleteFile(currentUser.Id, fileName);

                if (azResult.Status == "Error")
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Status = "Error", Message = $"Can not delete {fileName}" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Status = "Error", Message = ex.Message });
            }

            return Ok(StatusCodes.Status200OK);
        }*/

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
    }
}