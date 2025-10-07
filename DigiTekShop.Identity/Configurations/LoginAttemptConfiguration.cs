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
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(la => la.AttemptedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(la => la.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(la => la.IpAddress)
            .HasMaxLength(45)
            .IsRequired(false);

        builder.Property(la => la.UserAgent)
            .HasMaxLength(512)
            .IsRequired(false);

        
        builder.HasIndex(la => new { la.UserId, la.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_UserId_AttemptedAt");

        
        builder.HasIndex(la => la.LoginNameOrEmail)
            .HasDatabaseName("IX_LoginAttempts_LoginNameOrEmail");

        
        builder.HasIndex(la => la.IpAddress)
            .HasDatabaseName("IX_LoginAttempts_IpAddress");

       
        builder.HasIndex(la => new { la.Status, la.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_Status_AttemptedAt");

        
        builder.HasIndex(la => new { la.IpAddress, la.Status, la.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_IpAddress_Status_AttemptedAt");
    }
}