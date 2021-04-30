using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VcogBookmark.Shared.Models;

namespace VcogBookmarkServer.Controllers
{
    [Route("account")]
    public class AccountController : ControllerBase
    {
        private readonly AuthOptions _authOptions;

        private readonly List<Person> _people = new List<Person>
        {
            new Person("BookmarksSharedServerUser", "5b&5wXcYOH6SQ^zj", "user"),
        };

        public AccountController(IOptions<AuthOptions> authOptions)
        {
            _authOptions = authOptions.Value;
        }
 
        [HttpPost("get-token")]
        public IActionResult Token([FromForm]string username, [FromForm]string password)
        {
            var person = _people.FirstOrDefault(x => x.Login == username && x.Password == password);
            if (person == null)
            {
                return BadRequest(new { errorText = "Invalid username or password." });
            }
 
            var now = DateTime.UtcNow;
            var accessJwt = new JwtSecurityToken(
                issuer: _authOptions.Issuer,
                audience: _authOptions.Audience,
                notBefore: now,
                expires: now.AddMonths(1),
                claims: new []
                {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, person.Login),
                    new Claim(ClaimsIdentity.DefaultRoleClaimType, person.Role),
                },
                signingCredentials: new SigningCredentials(_authOptions.GetSymmetricSecurityKey(),SecurityAlgorithms.HmacSha256));
            
            var encodedAccessJwt = new JwtSecurityTokenHandler().WriteToken(accessJwt);
 
            return Ok(encodedAccessJwt);
        }
    }
}