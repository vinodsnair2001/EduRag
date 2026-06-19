using EduRAG.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace EduRAG.Application.DTOs;

public record SendMessageRequest([Required][MaxLength(4000)] string Content);

public record ChatMessageDto(Guid Id, string Content, MessageRole Role, DateTime SentAt);

public record CreateSessionResponse(Guid SessionId);

public record CreateSessionRequest(int ClassId, int SubjectId, int[] ChapterIds);

public record ChunkSearchResult(Guid ChunkId, string Content, int PageNumber, double Score);
