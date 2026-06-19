using EduRAG.Application.Common;
using EduRAG.Application.DTOs;
using EduRAG.Application.Interfaces;
using EduRAG.Domain.Entities;
using EduRAG.Domain.Enums;
using System.Security.Cryptography;
using System.Threading.Channels;

namespace EduRAG.Application.UseCases;

public class UploadMaterialUseCase
{
    private readonly IStudyMaterialRepository _materialRepo;
    private readonly IFileStorageService      _storage;
    private readonly Channel<Guid>            _queue;

    public UploadMaterialUseCase(
        IStudyMaterialRepository materialRepo,
        IFileStorageService storage,
        Channel<Guid> queue)
    {
        _materialRepo = materialRepo;
        _storage      = storage;
        _queue        = queue;
    }

    public async Task<Result<UploadMaterialResponse>> ExecuteAsync(
        Stream fileStream, string fileName, long fileSize,
        int classId, int subjectId, int? chapterId, Guid uploadedById)
    {
        if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return Result<UploadMaterialResponse>.Failure("Only PDF files are accepted.");

        if (fileSize > 50 * 1024 * 1024)
            return Result<UploadMaterialResponse>.Failure("File size exceeds 50 MB limit.");

        var hash = await ComputeHashAsync(fileStream);
        fileStream.Position = 0;

        var existing = await _materialRepo.GetByHashAsync(hash);
        if (existing is not null)
            return Result<UploadMaterialResponse>.Failure("This file has already been uploaded.");

        var storedPath = await _storage.SaveAsync(fileStream, fileName, classId, subjectId, chapterId);

        var material = new StudyMaterial
        {
            OriginalFileName     = fileName,
            StoredFilePath       = storedPath,
            ContentHash          = hash,
            FileSizeBytes        = fileSize,
            ClassId              = classId,
            SubjectId            = subjectId,
            ChapterId            = chapterId,
            UploadedById         = uploadedById,
            VectorizationStatus  = VectorizationStatus.Pending,
        };

        var created = await _materialRepo.CreateAsync(material);
        await _queue.Writer.WriteAsync(created.Id);

        return Result<UploadMaterialResponse>.Success(new(created.Id, created.VectorizationStatus));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id)
    {
        var material = await _materialRepo.GetByIdAsync(id);
        if (material is null)
            return Result<bool>.Failure("Material not found.");

        await _storage.DeleteAsync(material.StoredFilePath);
        await _materialRepo.DeleteAsync(id);
        return Result<bool>.Success(true);
    }

    private static async Task<string> ComputeHashAsync(Stream stream)
    {
        using var sha256 = SHA256.Create();
        var bytes = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
