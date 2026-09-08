using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MicrobloggingApp.API.Helpers
{
    public class JwtTokenHelper
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;

        public JwtTokenHelper(IConfiguration configuration)
        {
            var settings = configuration.GetSection("JwtSettings");
            _secretKey = settings["SecretKey"] ?? throw new InvalidOperationException("JwtSettings:SecretKey is required.");
            _issuer = settings["Issuer"] ?? throw new InvalidOperationException("JwtSettings:Issuer is required.");
            _audience = settings["Audience"] ?? throw new InvalidOperationException("JwtSettings:Audience is required.");
        }

        public string GenerateToken(string username)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
