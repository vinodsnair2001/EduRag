using EduRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduRAG.Infrastructure.Persistence.Configurations;

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> b)
    {
        b.ToTable("Subjects");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasDefaultValue(string.Empty);
        b.Property(x => x.IsActive).HasDefaultValue(true);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
        b.HasOne(x => x.Class)
         .WithMany(c => c.Subjects)
         .HasForeignKey(x => x.ClassId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
