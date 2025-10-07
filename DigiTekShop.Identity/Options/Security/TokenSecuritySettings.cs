using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Identity.Options.Security
{
    public class TokenSecuritySettings
    {
        public bool TokenReplayDetectionEnabled { get; init; } = true;

        public bool TokenRotationSecurityEnabled { get; init; } = true;

        public int MaxActiveTokensPerUser { get; init; } = 5;

        public int MaxActiveTokensPerDevice { get; init; } = 1;
    }

}
