using EduRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;

namespace EduRAG.Infrastructure.Persistence.Configurations;

public class MaterialChunkConfiguration : IEntityTypeConfiguration<MaterialChunk>
{
    public void Configure(EntityTypeBuilder<MaterialChunk> b)
    {
        b.ToTable("MaterialChunks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Content).IsRequired();
        b.Property(x => x.PageNumber).HasDefaultValue(1);
        b.Property(x => x.Embedding)
            .HasConversion(
                v => new Vector(v),
                v => v.Memory.ToArray(),
                new ValueComparer<float[]>(
                    (a, b) => a != null && b != null && a.SequenceEqual(b),
                    v => v.Aggregate(0, (h, e) => HashCode.Combine(h, e.GetHashCode())),
                    v => v.ToArray()))
            .HasColumnType("vector(768)");
        b.HasIndex(x => new { x.ClassId, x.SubjectId });
        b.HasOne(x => x.Material)
         .WithMany(m => m.Chunks)
         .HasForeignKey(x => x.MaterialId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
