using DigiTekShop.SharedKernel.Enums.Outbox;

namespace DigiTekShop.Persistence.Models;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }  
    public string Type { get; set; } = default!; 
    public string Payload { get; set; } = default!; 
    public string? CorrelationId { get; set; }    
    public string? CausationId { get; set; }     
    public DateTime? ProcessedAtUtc { get; set; }
    public int Attempts { get; set; }
    public OutboxStatus Status { get; set; } 
    public string? Error { get; set; }        
}

