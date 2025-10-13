using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Options.Messaging
{
    public sealed class SmtpSettings
    {
        public string Host { get; init; } = "";
        public int Port { get; init; } = 587;
        public string Username { get; init; } = "";
        public string Password { get; init; } = "";
        public bool EnableSsl { get; init; } = true;
        public bool UseDefaultCredentials { get; init; }
        public int TimeoutMs { get; init; } = 10000;

        public string FromEmail { get; init; } = "";
        public string FromName { get; init; } = "DigiTekShop";
        public string ReplyToEmail { get; init; } = "";
        public int MaxRetryAttempts { get; init; } = 3;
        public int RetryDelayMs { get; init; } = 1000;
    }
}
