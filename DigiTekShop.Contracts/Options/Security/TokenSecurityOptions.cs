using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Options.Security
{
    public sealed class TokenSecurityOptions
    {
        public bool TokenReplayDetectionEnabled { get; init; } = true;
        public bool TokenRotationSecurityEnabled { get; init; } = true;
        public int MaxActiveTokensPerUser { get; init; } = 5;
        public int MaxActiveTokensPerDevice { get; init; } = 1;
    }
}
