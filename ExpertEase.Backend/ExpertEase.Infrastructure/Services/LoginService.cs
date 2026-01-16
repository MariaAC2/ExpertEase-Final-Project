using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Services;
using ExpertEase.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ExpertEase.Infrastructure.Services;

/// <summary>
/// Inject the required service configuration from the application.json or environment variables.
/// </summary>
public class LoginService(IOptions<JwtConfiguration> jwtConfiguration) : ILoginService
{
    private readonly JwtConfiguration _jwtConfiguration = jwtConfiguration.Value;
    
    public string GetToken(UserDto user, DateTime issuedAt, TimeSpan expiresIn)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtConfiguration.Key);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.FullName))
            claims.Add(new Claim(ClaimTypes.Name, user.FullName));

        if (!string.IsNullOrWhiteSpace(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        if (!string.IsNullOrWhiteSpace(user.Role.ToString()))
            claims.Add(new Claim(ClaimTypes.Role, user.Role.ToString()));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            IssuedAt = issuedAt,
            Expires = issuedAt.Add(expiresIn),
            Issuer = _jwtConfiguration.Issuer,
            Audience = _jwtConfiguration.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
    }

    
    // public string GetToken(UserDTO user, DateTime issuedAt, TimeSpan expiresIn)
    // {
    //     var tokenHandler = new JwtSecurityTokenHandler();
    //     var key = Encoding.ASCII.GetBytes(_jwtConfiguration.Key);
    //
    //     var claims = new Dictionary<string, object>();
    //     
    //     if (!string.IsNullOrWhiteSpace(user.FullName))
    //         claims.Add(ClaimTypes.Name, user.FullName);
    //
    //     if (!string.IsNullOrWhiteSpace(user.Email))
    //         claims.Add(ClaimTypes.Email, user.Email);
    //     
    //     if (!string.IsNullOrWhiteSpace(user.Role.ToString()))
    //         claims.Add(ClaimTypes.Role, user.Role.ToString());
    //
    //     var tokenDescriptor = new SecurityTokenDescriptor
    //     {
    //         Subject = new ClaimsIdentity(new[]
    //         {
    //             new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
    //         }),
    //         Claims = claims,
    //         IssuedAt = issuedAt,
    //         Expires = issuedAt.Add(expiresIn),
    //         Issuer = _jwtConfiguration.Issuer,
    //         Audience = _jwtConfiguration.Audience,
    //         SigningCredentials = new SigningCredentials(
    //             new SymmetricSecurityKey(key),
    //             SecurityAlgorithms.HmacSha256Signature)
    //     };
    //
    //     return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
    // }
}
