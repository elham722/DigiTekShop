using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.DTOs.Auth.ResetPassword
{
    public record ChangePasswordRequestDto(Guid UserId, string CurrentPassword, string NewPassword);
}
