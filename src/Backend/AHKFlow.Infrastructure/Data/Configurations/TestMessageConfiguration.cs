using AHKFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AHKFlow.Infrastructure.Data.Configurations
{
    public class TestMessageConfiguration : IEntityTypeConfiguration<TestMessage>
    {
        public void Configure(EntityTypeBuilder<TestMessage> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Message)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(t => t.Category)
                .HasMaxLength(100);

            builder.HasData(new TestMessage
            {
                Id = 1,
                Message = "hello test record",
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Category = "test"
            });
        }
    }
}
