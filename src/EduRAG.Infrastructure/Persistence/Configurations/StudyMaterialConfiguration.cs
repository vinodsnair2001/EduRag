using EduRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduRAG.Infrastructure.Persistence.Configurations;

public class StudyMaterialConfiguration : IEntityTypeConfiguration<StudyMaterial>
{
    public void Configure(EntityTypeBuilder<StudyMaterial> b)
    {
        b.ToTable("StudyMaterials");
        b.HasKey(x => x.Id);
        b.Property(x => x.OriginalFileName).HasMaxLength(500).IsRequired();
        b.Property(x => x.StoredFilePath).IsRequired();
        b.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.FileSizeBytes).HasDefaultValue(0L);
        b.Property(x => x.VectorizationStatus).IsRequired();
        b.Property(x => x.UploadedAt).HasDefaultValueSql("NOW()");
        b.HasOne(x => x.Subject)
         .WithMany(s => s.Materials)
         .HasForeignKey(x => x.SubjectId)
         .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Chapter)
         .WithMany(c => c.Materials)
         .HasForeignKey(x => x.ChapterId)
         .IsRequired(false)
         .OnDelete(DeleteBehavior.SetNull);
    }
}
