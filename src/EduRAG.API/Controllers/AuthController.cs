using EduRAG.Application.DTOs;
using EduRAG.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduRAG.API.Controllers;

[ApiController, Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthUseCase _auth;
    public AuthController(AuthUseCase auth) => _auth = auth;

    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var result = await _auth.LoginAsync(req);
        if (!result.IsSuccess) return Unauthorized(new { message = result.Error });
        return Ok(result.Value);
    }

    [HttpPost("register"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var result = await _auth.RegisterAsync(req);
        if (!result.IsSuccess) return BadRequest(new { message = result.Error });
        return Ok(new { message = "User registered successfully." });
    }
}
