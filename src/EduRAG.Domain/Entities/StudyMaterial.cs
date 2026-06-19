using EduRAG.Domain.Enums;

namespace EduRAG.Domain.Entities;

public class StudyMaterial
{
    public Guid     Id                   { get; set; } = Guid.NewGuid();
    public string   OriginalFileName     { get; set; } = string.Empty;
    public string   StoredFilePath       { get; set; } = string.Empty;
    public string   ContentHash          { get; set; } = string.Empty;
    public long     FileSizeBytes        { get; set; }
    public int      ClassId              { get; set; }
    public int      SubjectId            { get; set; }
    public int?     ChapterId            { get; set; }
    public Guid     UploadedById         { get; set; }
    public DateTime UploadedAt           { get; set; } = DateTime.UtcNow;
    public VectorizationStatus VectorizationStatus { get; set; } = VectorizationStatus.Pending;
    public string?  VectorizationError   { get; set; }
    public Subject  Subject              { get; set; } = null!;
    public Chapter? Chapter              { get; set; }
    public ICollection<MaterialChunk> Chunks { get; set; } = new List<MaterialChunk>();
}
