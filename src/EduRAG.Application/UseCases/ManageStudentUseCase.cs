using EduRAG.Application.Common;
using EduRAG.Application.DTOs;
using EduRAG.Application.Interfaces;
using EduRAG.Domain.Entities;
using EduRAG.Domain.Enums;

namespace EduRAG.Application.UseCases;

public class ManageStudentUseCase
{
    private readonly IUserRepository               _userRepo;
    private readonly IStudentPermissionRepository  _permRepo;

    public ManageStudentUseCase(IUserRepository userRepo, IStudentPermissionRepository permRepo)
    {
        _userRepo = userRepo;
        _permRepo = permRepo;
    }

    public async Task<Result<UserDto>> CreateStudentAsync(CreateStudentRequest req)
    {
        var existing = await _userRepo.GetByEmailAsync(req.Email);
        if (existing is not null)
            return Result<UserDto>.Failure("Email already registered.");

        var student = new AppUser
        {
            Email        = req.Email.ToLowerInvariant(),
            FullName     = req.FullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password, 11),
            Role         = UserRole.Student,
            ClassId      = req.ClassId,
        };

        await _userRepo.CreateAsync(student);

        return Result<UserDto>.Success(new(
            student.Id, student.Email, student.FullName,
            student.Role, student.IsActive, student.CreatedAt,
            student.LastLoginAt, student.ClassId));
    }

    public async Task<Result<UserDto>> UpdateStudentAsync(Guid studentId, UpdateStudentRequest req)
    {
        var student = await _userRepo.GetByIdAsync(studentId);
        if (student is null || student.Role != UserRole.Student)
            return Result<UserDto>.Failure("Student not found.");

        var emailLower = req.Email.ToLowerInvariant();
        if (!string.Equals(student.Email, emailLower, StringComparison.Ordinal))
        {
            var existing = await _userRepo.GetByEmailAsync(emailLower);
            if (existing is not null)
                return Result<UserDto>.Failure("Email already in use by another account.");
        }

        student.FullName = req.FullName;
        student.Email    = emailLower;
        student.ClassId  = req.ClassId;
        student.IsActive = req.IsActive;

        if (!string.IsNullOrWhiteSpace(req.NewPassword))
            student.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword, 11);

        await _userRepo.UpdateAsync(student);

        return Result<UserDto>.Success(new(
            student.Id, student.Email, student.FullName,
            student.Role, student.IsActive, student.CreatedAt,
            student.LastLoginAt, student.ClassId));
    }

    public async Task<Result<bool>> DeactivateStudentAsync(Guid studentId)
    {
        var student = await _userRepo.GetByIdAsync(studentId);
        if (student is null || student.Role != UserRole.Student)
            return Result<bool>.Failure("Student not found.");

        if (!student.IsActive)
            return Result<bool>.Failure("Student is already deactivated.");

        student.IsActive = false;
        await _userRepo.UpdateAsync(student);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> SetPermissionsAsync(Guid studentId, SetStudentPermissionsRequest req)
    {
        var student = await _userRepo.GetByIdAsync(studentId);
        if (student is null || student.Role != UserRole.Student)
            return Result<bool>.Failure("Student not found.");

        await _permRepo.SetPermissionsAsync(studentId, req.SubjectIds);
        return Result<bool>.Success(true);
    }
}
