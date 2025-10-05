using Microsoft.AspNetCore.Mvc;
using webapi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using webapi.Services;

namespace webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private IBlobService _blobService;

        public AccountController(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,  
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            IBlobService blobService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _blobService = blobService;
        }


        [HttpPost]
        [Route("token")]
        public async Task<ActionResult> Login(LoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };

                var currentUser = await _userManager.FindByNameAsync(user.UserName);

                if (currentUser != null && await _userManager.CheckPasswordAsync(currentUser, model.Password))
                {
                    var userRoles = await _userManager.GetRolesAsync(currentUser);

                    var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, currentUser.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                    foreach (var userRole in userRoles)
                    {
                        authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                    }

                    var token = GetToken(authClaims);

                    return Ok(new
                    {
                        token = new JwtSecurityTokenHandler().WriteToken(token),
                        expiration = token.ValidTo,
                        name = currentUser.FirstName,
                        lastname = currentUser.LastName,
                        role = userRoles,
                });
                }
            }
            catch(System.Net.WebException error)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Status = "Error", Message = error.Message });
            }
            
            return Unauthorized();
        }

        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return token;
        }

        [Authorize]
        [HttpGet("user-details")]
        public async Task<ActionResult> GetUserDetails()
        {
            var identity = User.Identity as ClaimsIdentity;
            var currentUser = await _userManager.FindByNameAsync(identity.Name);
            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var status = "";
            var user = new
            {
                currentUser.FirstName,
                currentUser.LastName,
                currentUser.Email,
                currentUser.UserName,
                currentUser.Background,
                currentUser.Dob,
                currentUser.PhoneNumber,
                currentUser.Cover,
                currentUser.Avatar,
                status,
                userRoles
            };
            return Ok(user);
        }

        [Authorize]
        [HttpGet("get-user-dob")]
        public async Task<ActionResult> GetUserDob()
        {
            var identity = User.Identity as ClaimsIdentity;
            var currentUser = await _userManager.FindByNameAsync(identity.Name);
            return Ok(currentUser.Dob);
        }

        private ActionResult GetErrorResult(IdentityResult result)
        {
            if (result == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            if (!result.Succeeded)
            {
                if (result.Errors != null)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }

                if (ModelState.IsValid)
                {
                    // No ModelState errors are available to send, so just return an empty BadRequest.
                    return BadRequest();
                }

                return BadRequest(ModelState);
            }

            return null;
        }
    }
}

