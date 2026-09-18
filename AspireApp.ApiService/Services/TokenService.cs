using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using AspireApp.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using AspireApp.ApiService.Data;

namespace AspireApp.ApiService.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _dbContext;

    public TokenService(IConfiguration configuration, ApplicationDbContext dbContext)
    {
        _configuration = configuration;
        _dbContext = dbContext;
    }

    public string CreateToken(ApplicationUser user)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = jwtSection.GetValue<string>("Key");
        // fallback to a development key when configuration is missing (tests / dev local)
        if (string.IsNullOrEmpty(key)) key = "ReplaceThisWithASecretKeyForDev";
        var issuer = jwtSection.GetValue<string>("Issuer") ?? "AspireApp";
        var audience = jwtSection.GetValue<string>("Audience") ?? "AspireAppClients";
        var lifetime = jwtSection.GetValue<int?>("TokenLifetimeMinutes") ?? 60;

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new Claim("displayName", user.DisplayName ?? string.Empty)
        };

        // Ensure signing key has sufficient length: derive a 32-byte key via SHA256 when needed
        byte[] keyBytesRaw = Encoding.UTF8.GetBytes(key);
        byte[] keyBytes;
        if (keyBytesRaw.Length < 32)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            keyBytes = sha.ComputeHash(keyBytesRaw);
        }
        else
        {
            keyBytes = keyBytesRaw;
        }

        var signingKey = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(lifetime),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> CreateRefreshTokenAsync(ApplicationUser user)
    {
        var refresh = new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.UtcNow.AddDays(30),
            UserId = user.Id,
            Revoked = false
        };

        _dbContext.RefreshTokens.Add(refresh);
        await _dbContext.SaveChangesAsync();
        return refresh.Token;
    }

    public async Task<(bool Valid, ApplicationUser? User)> ValidateRefreshTokenAsync(string refreshToken)
    {
        var token = await _dbContext.RefreshTokens.Include(r => r.User).FirstOrDefaultAsync(r => r.Token == refreshToken);
        if (token == null) return (false, null);
        if (token.Revoked) return (false, null);
        if (token.Expires < DateTime.UtcNow) return (false, null);
        return (true, token.User);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var token = await _dbContext.RefreshTokens.FirstOrDefaultAsync(r => r.Token == refreshToken);
        if (token == null) return;
        token.Revoked = true;
        await _dbContext.SaveChangesAsync();
    }
}
