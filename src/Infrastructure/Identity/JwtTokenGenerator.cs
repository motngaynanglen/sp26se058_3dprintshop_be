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
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Infrastructure.Identity;
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        // 1. Lấy thông số từ appsettings.json
        var secretKey = _configuration["JwtSettings:Secret"];
        var issuer = _configuration["JwtSettings:Issuer"];
        var audience = _configuration["JwtSettings:Audience"];

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 2. Định nghĩa các "thẻ tên" (Claims) đính kèm vào Token
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, user.Role) // Quan trọng để phân quyền [Authorize(Roles = "Admin")]
        };

        // 3. Tạo Token
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddHours(12), // Token có hạn 12 tiếng... chắc đủ
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
