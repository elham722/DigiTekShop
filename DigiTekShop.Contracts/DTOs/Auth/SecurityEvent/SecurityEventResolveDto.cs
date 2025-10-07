using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.DTOs.Auth.SecurityEvent
{
    public sealed class SecurityEventResolveDto
    {
        public Guid EventId { get; init; }
        public string ResolvedBy { get; init; } = default!;
        public string? ResolutionNotes { get; init; }
    }
}
