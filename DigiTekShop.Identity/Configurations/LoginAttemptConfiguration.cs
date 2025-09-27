namespace DigiTekShop.Identity.Configurations;
    internal class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
    {
        public void Configure(EntityTypeBuilder<LoginAttempt> builder)
        {
            // Configure primary key
            builder.HasKey(la => la.Id);

            // Configure properties
            builder.Property(la => la.UserId)
                .IsRequired(false);

            builder.Property(la => la.AttemptedAt)
                .IsRequired();

            builder.Property(la => la.Status)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(la => la.IpAddress)
                .HasMaxLength(45) // IPv6 max length
                .IsRequired(false);

            builder.Property(la => la.UserAgent)
                .HasMaxLength(1000)
                .IsRequired(false);

            // Configure relationships
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(la => la.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure indexes
            builder.HasIndex(la => la.UserId)
                .HasDatabaseName("IX_LoginAttempts_UserId");

            builder.HasIndex(la => la.Status)
                .HasDatabaseName("IX_LoginAttempts_Status");

            builder.HasIndex(la => la.IpAddress)
                .HasDatabaseName("IX_LoginAttempts_IpAddress");

            builder.HasIndex(la => la.AttemptedAt)
                .HasDatabaseName("IX_LoginAttempts_AttemptedAt");

            builder.HasIndex(la => new { la.UserId, la.AttemptedAt })
                .HasDatabaseName("IX_LoginAttempts_UserId_AttemptedAt");

            builder.HasIndex(la => new { la.IpAddress, la.AttemptedAt })
                .HasDatabaseName("IX_LoginAttempts_IpAddress_AttemptedAt");
        }
    }