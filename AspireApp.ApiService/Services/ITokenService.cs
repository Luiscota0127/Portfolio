using AspireApp.ApiService.Models;

namespace AspireApp.ApiService.Services;

public interface ITokenService
{
    string CreateToken(ApplicationUser user);
    Task<string> CreateRefreshTokenAsync(ApplicationUser user);
    Task<(bool Valid, ApplicationUser? User)> ValidateRefreshTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
}
