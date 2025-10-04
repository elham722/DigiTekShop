using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.DTOs.Register
{
    public record RegisterRequest(
        string Email,
        string Password,
        string ConfirmPassword,
        string? PhoneNumber,
        bool AcceptTerms,
        string? DeviceInfo = null
    );
}
