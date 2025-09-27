namespace DigiTekShop.Identity.Configurations;
    internal class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            // Configure primary key
            builder.HasKey(al => al.Id);

            // Configure properties
            builder.Property(al => al.UserId)
                .IsRequired();

            builder.Property(al => al.Action)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(al => al.EntityName)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(al => al.EntityId)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(al => al.OldValue)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            builder.Property(al => al.NewValue)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            builder.Property(al => al.Timestamp)
                .IsRequired();

            builder.Property(al => al.IsSuccess)
                .IsRequired();

            builder.Property(al => al.ErrorMessage)
                .HasMaxLength(2000)
                .IsRequired(false);

            builder.Property(al => al.Severity)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(al => al.IpAddress)
                .HasMaxLength(45) // IPv6 max length
                .IsRequired(false);

            builder.Property(al => al.UserAgent)
                .HasMaxLength(1000)
                .IsRequired(false);

            // Configure indexes
            builder.HasIndex(al => al.UserId)
                .HasDatabaseName("IX_AuditLogs_UserId");

            builder.HasIndex(al => al.Action)
                .HasDatabaseName("IX_AuditLogs_Action");

            builder.HasIndex(al => al.EntityName)
                .HasDatabaseName("IX_AuditLogs_EntityName");

            builder.HasIndex(al => al.EntityId)
                .HasDatabaseName("IX_AuditLogs_EntityId");

            builder.HasIndex(al => al.Timestamp)
                .HasDatabaseName("IX_AuditLogs_Timestamp");

            builder.HasIndex(al => al.IsSuccess)
                .HasDatabaseName("IX_AuditLogs_IsSuccess");

            builder.HasIndex(al => al.Severity)
                .HasDatabaseName("IX_AuditLogs_Severity");

            builder.HasIndex(al => new { al.UserId, al.Timestamp })
                .HasDatabaseName("IX_AuditLogs_UserId_Timestamp");
        }
    }