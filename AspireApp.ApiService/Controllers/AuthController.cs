using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using AspireApp.ApiService.Models;
using AspireApp.ApiService.Services;

namespace AspireApp.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;

    public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = new ApplicationUser { UserName = model.Email, Email = model.Email, DisplayName = model.DisplayName };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        var token = _tokenService.CreateToken(user);
        var refresh = await _tokenService.CreateRefreshTokenAsync(user);
        return Ok(new AuthResponse { Token = token, RefreshToken = refresh, UserId = user.Id, Email = user.Email });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null) return Unauthorized();

        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
        if (!result.Succeeded) return Unauthorized();

        var token = _tokenService.CreateToken(user);
        var refresh = await _tokenService.CreateRefreshTokenAsync(user);
        return Ok(new AuthResponse { Token = token, RefreshToken = refresh, UserId = user.Id, Email = user.Email });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest model)
    {
        if (string.IsNullOrEmpty(model.RefreshToken)) return BadRequest();

        var (valid, user) = await _tokenService.ValidateRefreshTokenAsync(model.RefreshToken);
        if (!valid || user == null) return Unauthorized();

        // revoke old, issue new
        await _tokenService.RevokeRefreshTokenAsync(model.RefreshToken);
        var newJwt = _tokenService.CreateToken(user);
        var newRefresh = await _tokenService.CreateRefreshTokenAsync(user);
        return Ok(new AuthResponse { Token = newJwt, RefreshToken = newRefresh, UserId = user.Id, Email = user.Email });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest model)
    {
        if (string.IsNullOrEmpty(model.RefreshToken)) return BadRequest();
        await _tokenService.RevokeRefreshTokenAsync(model.RefreshToken);
        return NoContent();
    }
}

public record RegisterRequest(string Email, string Password, string? DisplayName);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record AuthResponse(string Token, string? RefreshToken, string? UserId, string? Email);
