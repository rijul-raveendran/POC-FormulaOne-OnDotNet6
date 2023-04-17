using FormulaOne.Configurations;
using FormulaOne.Data;
using FormulaOne.Data.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RestSharp;
using RestSharp.Authenticators;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace FormulaOne.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly JwtConfig _jwtConfig;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthenticationController(UserManager<IdentityUser> userManager, IOptionsMonitor<JwtConfig> optionsMonitor, IConfiguration configuration)
        {
            _userManager = userManager;
            _jwtConfig = optionsMonitor.CurrentValue;
            _configuration = configuration;

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
                UserName = registrationRequestDto.Name,
                EmailConfirmed = false
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

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
            var emailBody = "Please confirm your email by clicking on the link below: " +
                $"<a href=\"#URL#\">Confirm Email</a>";


            //sample format https://localhost:44300/api/authentication/confirmemail?userid={newUser.Id}&code={code}
            var callBack_url = Request.Scheme + "://" + Request.Host + Url.Action(nameof(ConfirmEmail), "Authentication", new { userId = newUser.Id, code = code });

            var body = emailBody.Replace("#URL#", callBack_url);

            // Send email
            var result = SendEmail(body, newUser.Email);

            if (result)
            {
                return Ok("Please confirm the email");
            }

            return Ok("Please request an email verification link");


            // Generate JWT token when user added to table
            //var jwtToken = GenerateJwtToken(newUser);



            //return Ok(new AuthResult()
            //{
            //    IsSuccess = true,
            //    Token = jwtToken
            //});
        }

        [Route(nameof(ConfirmEmail))]
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if(userId == null || code == null)
            {
                return BadRequest(new AuthResult()
                {
                    IsSuccess = false,
                    Errors = new List<string>() { "Invalid Email confirmation url" }
                });
            }

            var user = _userManager.FindByIdAsync(userId);

            if(user == null)
            {
                return BadRequest(new AuthResult()
                {
                    IsSuccess = false,
                    Errors = new List<string>() { "Invalid Email parameters" }
                });
            }

            //code = Encoding.UTF8.GetString(Convert.FromBase64String(code));
            var result = await _userManager.ConfirmEmailAsync(user.Result, code);
            var confirmationStatusMessage = result.Succeeded ? "Thank you for confirming the email"
                : "Your email is not confirmed, please try again later";

            return Ok(confirmationStatusMessage);

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

            if (!existing_user.EmailConfirmed)
            {
                return BadRequest(new AuthResult
                {
                    IsSuccess = false,

                    Errors = new List<string>() { "Please confirm your email" }
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

        private bool SendEmail(string emailBody ,string email)
        {
            var emailAddress = _configuration["MailGun:EmailAddress"];
            var restClientOptions = new RestClientOptions("https://api.mailgun.net/v3")
            {
                // Replace appsettings.json with original api key
                Authenticator = new HttpBasicAuthenticator("api", _configuration["MailGun:ApiKey"])
            };

            // create client
            var client = new RestClient(restClientOptions);
            // create request
            var request = new RestRequest("", Method.Post);

            request.AddParameter("domain", "sandboxddca8e8c38db41ad93a9204b24827ca0.mailgun.org", ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";
            request.AddParameter ("from", "Formula One Admin <mailgun@sandboxddca8e8c38db41ad93a9204b24827ca0.mailgun.org>");
            request.AddParameter("to", emailAddress); //Mailgain supports only Authorized recepients
            request.AddParameter ("subject", $"Email Confirmation" + email);
            request.AddParameter ("text", emailBody);
            request.Method = Method.Post;
            var response = client.Execute(request);

            return response.IsSuccessful;

        }


    }
}
