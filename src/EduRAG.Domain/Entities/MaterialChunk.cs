namespace EduRAG.Domain.Entities;

public class MaterialChunk
{
    public Guid    Id         { get; set; } = Guid.NewGuid();
    public Guid    MaterialId { get; set; }
    public int     ClassId    { get; set; }
    public int     SubjectId  { get; set; }
    public int?    ChapterId  { get; set; }
    public string  Content    { get; set; } = string.Empty;
    public int     ChunkIndex { get; set; }
    public int     PageNumber { get; set; } = 1;
    public float[] Embedding  { get; set; } = Array.Empty<float>();
    public StudyMaterial Material { get; set; } = null!;
}
