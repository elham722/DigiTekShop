using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.DTOs.Auth.Login
{
    public record LoginRequestDto(string Email, string Password, string? DeviceId, string? UserAgent, string? Ip);
}
