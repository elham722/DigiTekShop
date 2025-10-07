using DigiTekShop.SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.DTOs.Auth.SecurityEvent
{
    public sealed class SecurityEventCreateDto
    {
        public SecurityEventType EventType { get; init; }
        public Guid? UserId { get; init; }
        public string? IpAddress { get; init; }
        public string? UserAgent { get; init; }
        public string? DeviceId { get; init; }
        public string? MetadataJson { get; init; }
    }
}
