using EduRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduRAG.Infrastructure.Persistence.Configurations;

public class ChapterConfiguration : IEntityTypeConfiguration<Chapter>
{
    public void Configure(EntityTypeBuilder<Chapter> b)
    {
        b.ToTable("Chapters");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.OrderIndex).HasDefaultValue(0);
        b.Property(x => x.IsActive).HasDefaultValue(true);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
        b.HasOne(x => x.Subject)
         .WithMany(s => s.Chapters)
         .HasForeignKey(x => x.SubjectId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
