using EduRAG.Application.Common;
using EduRAG.Application.DTOs;
using EduRAG.Application.Interfaces;
using EduRAG.Domain.Entities;

namespace EduRAG.Application.UseCases;

public class ManageSubjectUseCase
{
    private readonly ISubjectRepository _repo;

    public ManageSubjectUseCase(ISubjectRepository repo) => _repo = repo;

    public async Task<Result<SubjectDto>> CreateAsync(int classId, CreateSubjectRequest req)
    {
        var entity = new Subject { Name = req.Name, Description = req.Description, ClassId = classId };
        var created = await _repo.CreateAsync(entity);
        return Result<SubjectDto>.Success(Map(created));
    }

    public async Task<Result<SubjectDto>> UpdateAsync(int id, UpdateSubjectRequest req)
    {
        var entity = new Subject { Id = id, Name = req.Name, Description = req.Description, IsActive = req.IsActive };
        var updated = await _repo.UpdateAsync(entity);
        return Result<SubjectDto>.Success(Map(updated));
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        await _repo.DeleteAsync(id);
        return Result<bool>.Success(true);
    }

    private static SubjectDto Map(Subject s) => new(s.Id, s.Name, s.Description, s.ClassId, s.IsActive);
}
