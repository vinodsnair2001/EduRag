using EduRAG.Application.Common;
using EduRAG.Application.DTOs;
using EduRAG.Application.Interfaces;
using EduRAG.Domain.Entities;

namespace EduRAG.Application.UseCases;

public class ManageChapterUseCase
{
    private readonly IChapterRepository _repo;

    public ManageChapterUseCase(IChapterRepository repo) => _repo = repo;

    public async Task<Result<ChapterDto>> CreateAsync(int subjectId, CreateChapterRequest req)
    {
        var entity = new Chapter { Title = req.Title, OrderIndex = req.OrderIndex, SubjectId = subjectId };
        var created = await _repo.CreateAsync(entity);
        return Result<ChapterDto>.Success(Map(created));
    }

    public async Task<Result<ChapterDto>> UpdateAsync(int id, UpdateChapterRequest req)
    {
        var entity = new Chapter { Id = id, Title = req.Title, OrderIndex = req.OrderIndex, IsActive = req.IsActive };
        var updated = await _repo.UpdateAsync(entity);
        return Result<ChapterDto>.Success(Map(updated));
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        await _repo.DeleteAsync(id);
        return Result<bool>.Success(true);
    }

    private static ChapterDto Map(Chapter c) => new(c.Id, c.Title, c.OrderIndex, c.SubjectId, c.IsActive, HasPdf: false);
}
