using DigiTekShop.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations;

public class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.ToTable("LoginAttempts");

        builder.HasKey(la => la.Id);

        builder.Property(la => la.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(la => la.UserId)
            .IsRequired(false);

        builder.Property(la => la.LoginNameOrEmail)
            .HasMaxLength(256).IsUnicode(false).IsRequired(false);


        builder.Property(la => la.LoginNameOrEmailNormalized)
            .HasMaxLength(256).IsUnicode(false).IsRequired(false);


        builder.Property(la => la.AttemptedAt)
            .IsRequired()
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(la => la.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(la => la.IpAddress)
            .HasMaxLength(45)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(la => la.UserAgent)
            .HasMaxLength(512)
            .IsRequired(false);


        builder.HasIndex(la => new { la.UserId, la.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_UserId_AttemptedAt")
            .IncludeProperties(la => new { la.Status, la.IpAddress });

        builder.HasIndex(la => la.LoginNameOrEmailNormalized)
            .HasFilter("[LoginNameOrEmailNormalized] IS NOT NULL")
            .HasDatabaseName("IX_LoginAttempts_LoginNameOrEmailNorm");

        builder.HasIndex(la => la.IpAddress)
            .HasDatabaseName("IX_LoginAttempts_IpAddress");

        builder.HasIndex(la => new { la.Status, la.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_Status_AttemptedAt")
            .IncludeProperties(la => new { la.UserId, la.IpAddress });

        builder.HasIndex(la => new { la.IpAddress, la.Status, la.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_Ip_Status_AttemptedAt");
    }
}