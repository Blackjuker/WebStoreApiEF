using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using WebStoreApiEF.Models;
using WebStoreApiEF.Services;

namespace WebStoreApiEF.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly ApplicationDbContext context;
        private readonly EmailSender emailSender;

        public AccountController(IConfiguration configuration,ApplicationDbContext context,EmailSender emailSender) 
        {
            this.configuration = configuration;
            this.context = context;
            this.emailSender = emailSender;
        }

        //[HttpGet("TestToken")]
        //public IActionResult TestToken()
        //{
        //    User user = new User() {
        //     Id =2,
        //     Role="admin"
        //    };

        //    var jwt = CreateJWToken(user);

        //    var response = new {jwToken = jwt};
        //    return Ok(response);
        //}
        private string CreateJWToken(User user)
        {
            List<Claim> claims = new List<Claim>()
            {
                new Claim("id",""+user.Id),
                new Claim("role",user.Role)
            };

            string strKey = configuration["JwtSettings:Key"]!;

            //symitric key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(strKey));
            // signing cresidential
            var creds = new SigningCredentials(key,SecurityAlgorithms.HmacSha512);

            var token = new JwtSecurityToken(
                    issuer: configuration["JwtSettings:Issuer"],
                    audience: configuration["JwtSettings:Audience"],
                    claims:claims,
                    expires:DateTime.Now.AddDays(1),
                    signingCredentials:creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }

        [Authorize]
        [HttpGet("AuthorizeAuthenticatedUsers")]
        public IActionResult AuthorizeAuthenticatedUsers()
        {
            return Ok("Welcome the new user");
        }

        [Authorize]
        [HttpGet("GetTokenClaims")]
        public IActionResult GetTokenClaims()
        {
            var identity = User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                Dictionary<string, string> claims = new Dictionary<string, string>();

                foreach (Claim claim in identity.Claims)
                {
                    claims.Add(claim.Type, claim.Value);
                }

                return Ok(claims);
            }

           return Ok();
        }

        [HttpPost("Register")]
        public IActionResult CreateUser(UserDto userDto)
        {
            // First vérification if an email is already used

            var emailCount = context.Users.Count(u => u.Email == userDto.Email);

            if (emailCount > 0)
            {
                ModelState.AddModelError("User", "This email is already used");
                return BadRequest(ModelState);
            }

            // encrypt the password 
            var passwordHasher = new PasswordHasher<User>();
            var encryptedPassword = passwordHasher.HashPassword(new User(), userDto.Password);

            //create new Account 
            User user = new User()
            {
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Email = userDto.Email,
                Phone = userDto.Phone ?? "",
                Address = userDto.Address,
                Password = encryptedPassword,
                Role ="client",
                CreatedAt = DateTime.Now
            };

            context.Users.Add(user);

            context.SaveChanges();

            var jwt = CreateJWToken(user);

            UserProfileDto userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = DateTime.Now
            };

            var response = new
            {
                Token = jwt,
                User = userProfileDto
            };

            return Ok(response);
        }


        [HttpPost("Login")]
        public IActionResult Login(string email,string password)
        {
            var user = context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                ModelState.AddModelError("Error", "Invalid password or username");
                return BadRequest(ModelState);
            }

            // verify password

            var passwordHacher = new PasswordHasher<User>();
            var result = passwordHacher.VerifyHashedPassword(new User(),user.Password,password);

            if(result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("Error", "Wrong password");
                return BadRequest(ModelState);
            }

            var jwt = CreateJWToken(user);


            UserProfileDto userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = DateTime.Now
            };

            var response = new
            {
                Token = jwt,
                User = userProfileDto
            };

            return Ok(response);

        }

        [HttpPost("ForgotPassword")]
        public IActionResult ForgotPassword(string email)
        {
            var user = context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                return NotFound();
            }

            //delete any old request password
            var oldPasswordReset = context.PasswordResets.FirstOrDefault(r=>r.Email == email);

            if (oldPasswordReset != null)
            {
                context.Remove(oldPasswordReset);
            }

            // create Password Reset Token
            string token = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString();

            var passwordReset = new PasswordReset()
            {
                Email = email,
                Token = token,
                CreatedAt = DateTime.Now
            };

            context.PasswordResets.Add(passwordReset);
            context.SaveChanges();

            string emailSubjet = "Password Reset";
            string username = user.FirstName + " " + user.LastName;
            string emailMessage = "Dear " + username + "\n" + "We received your password reset request.\n " + " Please copy the following token and paste it in the Password Reset Form:\n" + token + "\n\n" + "Best Regards\n";
            //emailSender.SendSimpleMessage();
            return Ok("Reset Link sent to your Email\n"+ token);
        }

        [Authorize]
        [HttpPost("ResetPassword")]
        public IActionResult ResetPassword(string token,string password)
        {
            var pwrReset = context.PasswordResets.FirstOrDefault(r => r.Token == token);
            if (pwrReset == null)
            {
                ModelState.AddModelError("Token", "Wrong or Expired Token");
                return BadRequest(ModelState);
            }

            var user = context.Users.FirstOrDefault(u => u.Email == pwrReset.Email);

            if (user == null)
            {
                ModelState.AddModelError("Token", "Wrong or Expired Token");
                return BadRequest(ModelState);
            }

            //encrypt password
            var passwordHasher = new PasswordHasher<User>();
            string encryptedPassword = passwordHasher.HashPassword(new User(), password);

            // Save the encrypted password
            user.Password = encryptedPassword;

            //delete the Token
            context.PasswordResets.Remove(pwrReset);

            context.SaveChanges();

            return Ok();
        }

        [Authorize]
        [HttpGet("Profile")]
        public IActionResult GetProfile()
        {
            int id = JwtReader.GetUserId(User);

            var user = context.Users.Find( id);

            if (user == null)
            {
                return Unauthorized();
            }

            var userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return Ok(userProfileDto); 
        }

        [Authorize]
        [HttpPut("UpdateProfile")]
        public IActionResult UpdateProfile(UserProfileUpdateDto userProfileUpdateDto)
        {

            int id = JwtReader.GetUserId(User);

            var user = context.Users.Find( id);

            if(user == null)
            {
                return Unauthorized();
            }

            //update the user profile
            user.FirstName = userProfileUpdateDto.FirstName;
            user.LastName = userProfileUpdateDto.LastName;
            user.Email = userProfileUpdateDto.Email;
            user.Phone = userProfileUpdateDto.Phone??"";
            user.Address = userProfileUpdateDto.Address;

            context.SaveChanges();

            var userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return Ok(userProfileDto);
        }

        [Authorize]
        [HttpPut("UpdatePassword")]
        public IActionResult UpdatePassword([Required,MinLength(8),MaxLength(100)]string password)
        {
            int id = JwtReader.GetUserId(User);

            var user = context.Users.Find(id);

            if (user == null)
            {
                return Unauthorized();
            }

            //encrypt password 
            var passwordHasher = new PasswordHasher<User>();
            string encryptepPass = passwordHasher.HashPassword(new User(), password);

            //Update password
            user.Password = encryptepPass;

            context.SaveChanges();

            return Ok();
        }
        
    }
}
