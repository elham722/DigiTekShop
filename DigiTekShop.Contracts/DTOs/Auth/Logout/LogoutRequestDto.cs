using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.DTOs.Auth.Logout
{
    public record LogoutRequestDto(
        string? RefreshToken = null,
        string? AccessToken = null
    );
}
