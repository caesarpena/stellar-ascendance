using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Common_Utils;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using webapi.Models;
using webapi.Services;
using Azure;

namespace webapi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UploadEncodeAndStreamFilesController : ControllerBase
    {
        // Set this variable to true if you want to authenticate Interactively through the browser using your Azure user account
        private const bool UseInteractiveAuth = false;

        private readonly UserManager<ApplicationUser> _userManager;

        private IBlobService _blobService;
        public UploadEncodeAndStreamFilesController(UserManager<ApplicationUser> userManager,
            IBlobService blobService)
        {
            _userManager = userManager;
            _blobService = blobService;
        }

        private static ConfigWrapper GetConfig()
        {
            ConfigWrapper config = new(new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables() // parses the values from the optional .env file at the solution root
                .Build());
            
            return config;
        }

        [Authorize]
        [HttpPost("upload-file"), DisableRequestSizeLimit]
        public async Task<ActionResult> UploadFile()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var currentUser = await _userManager.FindByEmailAsync(claimsIdentity?.Name);
            string toReturn = null;
            string inputAssetName = $"input-{currentUser.Id}";
            IFormFile file = Request.Form.Files[0];

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (file == null)
            {
                return BadRequest();
            }
            try
            {
                var result = await _blobService.UploadFileBlobAsync(inputAssetName, file.OpenReadStream(), file.ContentType, file.FileName);
                toReturn = result.AbsoluteUri;

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Status = "Error", Message = ex.Message });
            }

            return Ok(new { toReturn });
        }
    }
}