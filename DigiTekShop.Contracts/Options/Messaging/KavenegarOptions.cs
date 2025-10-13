using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Options.Messaging
{
    public sealed class KavenegarOptions
    {
        public string ApiKey { get; init; } = "";
        public string BaseUrl { get; init; } = "https://api.kavenegar.com/v1";
        public string DefaultSender { get; init; } = "2000660110";
        public string OtpTemplate { get; init; } = "login-otp";
        public int TimeoutSeconds { get; init; } = 10;
    }
}
