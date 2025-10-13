using System.ComponentModel.DataAnnotations;

namespace DigiTekShop.SharedKernel.DomainShared.Events;

public sealed class OutboxEvent
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    public string EventData { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string AggregateId { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string AggregateType { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public int RetryCount { get; set; }

    public string? ErrorMessage { get; set; }

    public bool IsProcessed => ProcessedAt.HasValue;

    public bool ShouldRetry => !IsProcessed && RetryCount < 3;
}

