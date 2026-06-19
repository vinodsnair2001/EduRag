using EduRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduRAG.Infrastructure.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> b)
    {
        b.ToTable("ChatMessages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Content).IsRequired();
        b.Property(x => x.SentAt).HasDefaultValueSql("NOW()");
        b.HasOne(x => x.Session)
         .WithMany(s => s.Messages)
         .HasForeignKey(x => x.SessionId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
