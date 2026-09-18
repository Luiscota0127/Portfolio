using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using AspireApp.ApiService.Data;
using AspireApp.ApiService.Services;
using AspireApp.ApiService.Models;

namespace AspireApp.ApiService.Tests;

public class TokenServiceTests
{
    private IConfiguration CreateConfiguration()
    {
        var dict = new Dictionary<string,string?>
        {
            ["Jwt:Key"] = "TestKeyForSigningTokensDontUseInProd",
            ["Jwt:Issuer"] = "AspireAppTests",
            ["Jwt:Audience"] = "AspireAppClients",
            ["Jwt:TokenLifetimeMinutes"] = "60"
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public async Task CreateAndValidateRefreshToken_Works()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ApplicationDbContext(options);
        var config = CreateConfiguration();
        var service = new TokenService(config, db);

        var user = new ApplicationUser { UserName = "tester", Email = "tester@example.com" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var refresh = await service.CreateRefreshTokenAsync(user);
        Assert.False(string.IsNullOrEmpty(refresh));

        var (valid, foundUser) = await service.ValidateRefreshTokenAsync(refresh);
        Assert.True(valid);
        Assert.NotNull(foundUser);
        Assert.Equal(user.Email, foundUser!.Email);

        await service.RevokeRefreshTokenAsync(refresh);
        var (valid2, _) = await service.ValidateRefreshTokenAsync(refresh);
        Assert.False(valid2);
    }
}
