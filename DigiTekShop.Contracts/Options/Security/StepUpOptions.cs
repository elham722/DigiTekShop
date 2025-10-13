using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Options.Security
{
    public sealed class StepUpOptions
    {
        public bool Enabled { get; init; } = true;
        public bool RequiredForNewDevices { get; init; } = true;
        public bool RequiredForSensitiveOperations { get; init; } = true;
        public TimeSpan StepUpValidityDuration { get; init; } = TimeSpan.FromHours(1);
        public int MaxStepUpAttempts { get; init; } = 3;
        public TimeSpan StepUpLockoutDuration { get; init; } = TimeSpan.FromMinutes(15);
    }
}
