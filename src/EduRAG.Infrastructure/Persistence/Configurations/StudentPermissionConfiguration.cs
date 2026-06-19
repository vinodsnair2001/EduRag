using EduRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduRAG.Infrastructure.Persistence.Configurations;

public class StudentPermissionConfiguration : IEntityTypeConfiguration<StudentPermission>
{
    public void Configure(EntityTypeBuilder<StudentPermission> b)
    {
        b.ToTable("StudentPermissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.GrantedAt).HasDefaultValueSql("NOW()");

        b.HasOne(x => x.Student)
         .WithMany(u => u.SubjectPermissions)
         .HasForeignKey(x => x.StudentId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Subject)
         .WithMany(s => s.StudentPermissions)
         .HasForeignKey(x => x.SubjectId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.StudentId, x.SubjectId }).IsUnique();
    }
}
