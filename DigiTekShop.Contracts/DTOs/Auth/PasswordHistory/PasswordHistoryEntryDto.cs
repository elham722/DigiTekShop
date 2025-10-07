using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.DTOs.Auth.PasswordHistory
{
    public sealed record PasswordHistoryEntryDto(
        DateTime ChangedAt
    );
}
