using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Options.Security
{
    public sealed class SecurityOptions
    {
        public BruteForceOptions BruteForce { get; init; } = new();
        public StepUpOptions StepUp { get; init; } = new();
        public TokenSecurityOptions TokenSecurity { get; init; } = new();
    }
}
