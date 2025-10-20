namespace DigiTekShop.Identity.Configurations;

internal sealed class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistory>
{
    public void Configure(EntityTypeBuilder<PasswordHistory> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(ph => ph.PasswordHash)
            .IsRequired()
            .HasMaxLength(500)    
            .IsUnicode(false);

        builder.Property(ph => ph.ChangedAtUtc)
            .HasColumnType("datetime2(3)")
            .IsRequired();

        builder.HasIndex(ph => ph.UserId).HasDatabaseName("IX_PasswordHistory_User");

        builder.HasIndex(ph => new { ph.UserId, ph.ChangedAtUtc })
            .HasDatabaseName("IX_PasswordHistory_User_ChangedAtUtc");

        builder.HasOne(x => x.User)
            .WithMany(u => u.PasswordHistories)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}