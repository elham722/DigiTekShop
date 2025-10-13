using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Options.Messaging
{
    public sealed class SmsOptions
    {
        public bool Enabled { get; init; }
        public string Provider { get; init; } = "Kavenegar";
    }

}
