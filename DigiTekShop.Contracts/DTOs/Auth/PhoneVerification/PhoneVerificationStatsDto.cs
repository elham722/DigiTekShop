using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.DTOs.Auth.PhoneVerification
{
    public class PhoneVerificationStatsDto
    {
        public int TotalCodes { get; set; }

        public int VerifiedCodes { get; set; }

        public int ExpiredCodes { get; set; }

        public int FailedAttempts { get; set; }

        public DateTime? LastVerificationAt { get; set; }

        public double SuccessRate => TotalCodes > 0 ? (double)VerifiedCodes / TotalCodes * 100 : 0;
    }
}
