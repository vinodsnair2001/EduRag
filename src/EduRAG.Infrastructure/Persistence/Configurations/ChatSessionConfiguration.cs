using EduRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduRAG.Infrastructure.Persistence.Configurations;

public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> b)
    {
        b.ToTable("ChatSessions");
        b.HasKey(x => x.Id);
        b.Property(x => x.StartedAt).HasDefaultValueSql("NOW()");
        b.HasOne(x => x.User)
         .WithMany(u => u.ChatSessions)
         .HasForeignKey(x => x.UserId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
