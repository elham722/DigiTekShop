using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.DTOs.Auth.Token
{
    public record RefreshRequestDto(string RefreshToken, string? DeviceId, string? Ip, string? UserAgent);
}
