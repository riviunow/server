using Domain.Entities.SingleIdEntities;
using Domain.Interfaces;
using Microsoft.Extensions.Options;
using Shared.Config;
using Shared.Constants;
using Shared.Types;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace Application.UseCases.JWT
{
    public class JWTPairResponse
    {
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
    }

    public class GenerateTokenPairUseCase : IUseCase<JWTPairResponse, User>
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IConfiguration _configuration;

        public GenerateTokenPairUseCase(IOptions<JwtSettings> jwtOptions, IConfiguration configuration)
        {
            _jwtSettings = jwtOptions.Value;
            _configuration = configuration;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<Result<JWTPairResponse>> Execute(User user)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            try
            {
                string accessToken = AccessToken(user);
                string refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

                return Result<JWTPairResponse>.Done(new JWTPairResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                });
            }
            catch
            {
                return Result<JWTPairResponse>.Fail(ErrorMessage.UnknownError);
            }
        }

        private string AccessToken(User user)
        {
            var claims = new Claim[]
        {
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };

            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(int.Parse(_configuration["JwtSettings:AccessTokenExpiryInHours"])),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = creds,
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            var accessToken = tokenHandler.WriteToken(token);

            return accessToken;
        }
    }
}
