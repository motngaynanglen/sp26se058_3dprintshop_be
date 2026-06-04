using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Models;

namespace sp26se058_3dprintshop_be.Infrastructure.Identity;
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(UserIdentity user)
    {
        var secretKey = RequireJwtSetting("Secret");
        var issuer = RequireJwtSetting("Issuer");
        var audience = RequireJwtSetting("Audience");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 2. Định nghĩa các "thẻ tên" (Claims) đính kèm vào Token
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, user.Role) // Quan trọng để phân quyền [Authorize(Roles = "Customer")]
        };

        // 3. Tạo Token
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string RequireJwtSetting(string key)
    {
        var value = _configuration[$"JwtSettings:{key}"]?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"JwtSettings:{key} is required.");
        }

        if (key == "Secret" && Encoding.UTF8.GetByteCount(value) < 32)
        {
            throw new InvalidOperationException("JwtSettings:Secret must be at least 32 bytes for HS256.");
        }

        return value;
    }
}
