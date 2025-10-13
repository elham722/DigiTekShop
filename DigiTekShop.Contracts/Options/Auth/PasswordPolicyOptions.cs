using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Options.Auth
{
    public sealed class PasswordPolicyOptions
    {
        public int MinLength { get; init; } = 12;
        public int RequiredCategoryCount { get; init; } = 3;
        public int MaxRepeatedChar { get; init; } = 3;
        public int HistoryDepth { get; init; } = 5;
        public bool ForbidUserNameFragments { get; init; } = true;
        public bool ForbidEmailLocalPart { get; init; } = true;
        public List<string> Blacklist { get; init; } = new();
    }
}
