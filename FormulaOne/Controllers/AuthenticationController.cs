using FormulaOne.Configurations;
using FormulaOne.Data;
using FormulaOne.Data.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FormulaOne.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly JwtConfig _jwtConfig;
        private readonly UserManager<IdentityUser> _userManager;

        public AuthenticationController(UserManager<IdentityUser> userManager, IOptionsMonitor<JwtConfig> optionsMonitor)
        {
            _userManager = userManager;
            _jwtConfig = optionsMonitor.CurrentValue;
        }

        [HttpPost]
        [Route(nameof(Register))]
        public async Task<IActionResult> Register([FromBody] UserRegistrationRequestDto registrationRequestDto)
        {
            //Validate incoming request
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResult
                {
                    IsSuccess = false,

                    Errors = new List<string>() { "Invalid Request" }
                });
            }

            var existing_user = await _userManager.FindByEmailAsync(registrationRequestDto.Email);
            if (existing_user != null)
            {
                return BadRequest(new AuthResult
                {
                    IsSuccess = false,

                    Errors = new List<string>() { "User Already Exists" }
                });
            }

            //Create a user
            var newUser = new IdentityUser()
            {
                Email = registrationRequestDto.Email,
                UserName = registrationRequestDto.Name
            };

            // Add user to the table
            var isCreated = await _userManager.CreateAsync(newUser, registrationRequestDto.Password);

            if(!isCreated.Succeeded)
            {
                return BadRequest(new AuthResult()
                {
                    IsSuccess = isCreated.Succeeded,
                    Errors = isCreated.Errors.Select(x => x.Description).ToList()
,               });
            }

            // Generate JWT token when user added to table
            var jwtToken = GenerateJwtToken(newUser);

            return Ok(new AuthResult()
            {
                IsSuccess = true,
                Token = jwtToken
            });
        }

        [HttpPost]
        [Route(nameof(Login))]
        public async Task<IActionResult> Login([FromBody] UserLoginRequestDto loginRequestDto)
        {
            //Validate incoming request
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResult
                {
                    IsSuccess = false,

                    Errors = new List<string>() { "Invalid Request" }
                });
            }

            var existing_user = await _userManager.FindByEmailAsync(loginRequestDto.Email);
            if (existing_user == null)
            {
                return BadRequest(new AuthResult
                {
                    IsSuccess = false,

                    Errors = new List<string>() { "Invalid Credentials" }
                });
            }

            var isPasswordCorrect = await _userManager.CheckPasswordAsync(existing_user, loginRequestDto.Password);

            if(!isPasswordCorrect)
            {
                return BadRequest(new AuthResult
                {
                    IsSuccess = false,

                    Errors = new List<string>() { "Invalid Credentials" }
                });
            }

            // Generate JWT token 
            var jwtToken = GenerateJwtToken(existing_user);

            return Ok(new AuthResult()
            {
                IsSuccess = true,
                Token = jwtToken
            });
        }

        private string GenerateJwtToken(IdentityUser user)
        {
            // handler for creating token
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            // Get the security key
            var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);

            // Token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("Id", user.Id),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email), //Sub is unique id
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                                        //used by the refresh token
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.ToUniversalTime().ToString())

                }),

                Expires = DateTime.Now.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var securityToken = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(securityToken);

            return jwtToken;
        }
    }
}
