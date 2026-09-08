using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MicrobloggingApp.API.Helpers;

namespace MicrobloggingApp.Tests
{
    public class JwtTokenHelperTests
    {
        private const string SecretKey = "01234567890123456789012345678901";
        private const string Issuer = "MicrobloggingApp";
        private const string Audience = "MicrobloggingApp.Client";

        [Fact]
        public void GenerateToken_UsesConfiguredIssuerAudienceAndUsername()
        {
            var helper = new JwtTokenHelper(CreateConfiguration());

            var token = helper.GenerateToken("aqib");
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);

            Assert.Equal("aqib", principal.FindFirstValue(ClaimTypes.Name));
        }

        [Fact]
        public void Constructor_ThrowsWhenIssuerIsMissing()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:SecretKey"] = SecretKey,
                    ["JwtSettings:Audience"] = Audience
                })
                .Build();

            var exception = Assert.Throws<InvalidOperationException>(() => new JwtTokenHelper(configuration));

            Assert.Equal("JwtSettings:Issuer is required.", exception.Message);
        }

        private static IConfiguration CreateConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:SecretKey"] = SecretKey,
                    ["JwtSettings:Issuer"] = Issuer,
                    ["JwtSettings:Audience"] = Audience
                })
                .Build();
        }
    }
}
