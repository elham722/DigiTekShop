using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.DTOs.Auth.UserDevice
{
    public sealed class UserDeviceDto
    {
        public string UserId { get; init; } = default!;

        public Guid? DeviceId { get; init; }

        public string? DeviceName { get; init; }

        public string? Platform { get; init; }

        public string? IpAddress { get; init; }

        public string? UserAgent { get; init; }

        public bool IsTrusted { get; init; }

        public bool IsRevoked { get; init; }

        public DateTime CreatedAt { get; init; }
        public DateTime? LastLoginAt { get; init; }
    }
}
