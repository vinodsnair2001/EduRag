using EduRAG.Application.Common;
using EduRAG.Application.DTOs;
using EduRAG.Application.Interfaces;
using EduRAG.Domain.Entities;
using BCrypt.Net;

namespace EduRAG.Application.UseCases;

public class AuthUseCase
{
    private readonly IUserRepository _userRepo;
    private readonly IJwtService     _jwt;

    public AuthUseCase(IUserRepository userRepo, IJwtService jwt)
    {
        _userRepo = userRepo;
        _jwt      = jwt;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest req)
    {
        var user = await _userRepo.GetByEmailAsync(req.Email);
        if (user is null || !user.IsActive)
            return Result<LoginResponse>.Failure("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Result<LoginResponse>.Failure("Invalid credentials.");

        await _userRepo.UpdateLastLoginAsync(user.Id);
        var token = _jwt.GenerateToken(user);
        return Result<LoginResponse>.Success(new(token, user.Role.ToString(), user.FullName, user.Id));
    }

    public async Task<Result<bool>> RegisterAsync(RegisterRequest req)
    {
        var existing = await _userRepo.GetByEmailAsync(req.Email);
        if (existing is not null)
            return Result<bool>.Failure("Email already registered.");

        var user = new AppUser
        {
            Email        = req.Email.ToLowerInvariant(),
            FullName     = req.FullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password, 11),
            Role         = req.Role,
        };

        await _userRepo.CreateAsync(user);
        return Result<bool>.Success(true);
    }
}
