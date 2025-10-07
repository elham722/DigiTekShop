using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.DTOs.Auth.SecurityEvent
{
    public class SecurityEventStatsDto
    {
        public int TotalEvents { get; set; }
        public int UnresolvedEvents { get; set; }
        public int HighSeverityEvents { get; set; }
        public int MediumSeverityEvents { get; set; }
        public int LowSeverityEvents { get; set; }
        public Dictionary<string, int> EventsByType { get; set; } = new();
        public Dictionary<string, int> EventsByIp { get; set; } = new();
    }
}
