using EduRAG.Application.Interfaces;
using EduRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduRAG.Infrastructure.Persistence.Repositories;

public class StudentPermissionRepository : IStudentPermissionRepository
{
    private readonly AppDbContext _ctx;
    public StudentPermissionRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task SetPermissionsAsync(Guid studentId, IEnumerable<int> subjectIds)
    {
        var existing = await _ctx.StudentPermissions
            .Where(p => p.StudentId == studentId)
            .ToListAsync();

        _ctx.StudentPermissions.RemoveRange(existing);

        foreach (var subjectId in subjectIds.Distinct())
        {
            _ctx.StudentPermissions.Add(new StudentPermission
            {
                StudentId = studentId,
                SubjectId = subjectId,
            });
        }

        await _ctx.SaveChangesAsync();
    }

    public async Task<IEnumerable<StudentPermission>> GetByStudentIdAsync(Guid studentId)
        => await _ctx.StudentPermissions
            .Where(p => p.StudentId == studentId)
            .ToListAsync();
}
