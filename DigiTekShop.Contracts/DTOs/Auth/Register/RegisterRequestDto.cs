using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.DTOs.Auth.Register
{
    public record RegisterRequestDto(
        string Email,
        string Password,
        string ConfirmPassword,
        string? PhoneNumber,
        bool AcceptTerms,
        string? DeviceId = null,
        string? UserAgent = null,
        string? Ip = null
    );
}
