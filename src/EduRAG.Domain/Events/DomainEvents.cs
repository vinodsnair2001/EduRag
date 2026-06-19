namespace EduRAG.Domain.Events;

public record MaterialUploadedEvent(Guid MaterialId, int ClassId, int SubjectId);
public record VectorizationCompletedEvent(Guid MaterialId, int ChunkCount);
public record ChatSessionStartedEvent(Guid SessionId, Guid UserId);
