using EduRAG.Application.Common;
using EduRAG.Application.DTOs;
using EduRAG.Application.Interfaces;
using EduRAG.Domain.Entities;

namespace EduRAG.Application.UseCases;

public class ManageClassUseCase
{
    private readonly IClassRepository _repo;

    public ManageClassUseCase(IClassRepository repo) => _repo = repo;

    public async Task<Result<ClassDto>> CreateAsync(CreateClassRequest req)
    {
        var entity = new Class { Name = req.Name, Grade = req.Grade };
        var created = await _repo.CreateAsync(entity);
        return Result<ClassDto>.Success(Map(created));
    }

    public async Task<Result<ClassDto>> UpdateAsync(int id, UpdateClassRequest req)
    {
        var entity = new Class { Id = id, Name = req.Name, Grade = req.Grade, IsActive = req.IsActive };
        var updated = await _repo.UpdateAsync(entity);
        return Result<ClassDto>.Success(Map(updated));
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        await _repo.DeleteAsync(id);
        return Result<bool>.Success(true);
    }

    private static ClassDto Map(Class c) => new(c.Id, c.Name, c.Grade, c.IsActive, c.CreatedAt);
}
