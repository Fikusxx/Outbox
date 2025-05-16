using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OutboxProcessor.Database;

internal sealed class OutboxTypeConfiguration : IEntityTypeConfiguration<Outbox>
{
    public void Configure(EntityTypeBuilder<Outbox> builder)
    {
        builder.ToTable("outbox");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Time);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Content).HasColumnName("content").HasColumnType("jsonb");
        builder.Property(x => x.Time).HasColumnName("time");
    }
}