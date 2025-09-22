using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Identity.Models
{
    public class AuditLog
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string UserId { get; private set; } = default!;
        public string Action { get; private set; } = default!;
        public string? Metadata { get; private set; }
        public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

        private AuditLog() { } // EF Core
        public AuditLog(string userId, string action, string? metadata = null)
        {
            UserId = userId;
            Action = action;
            Metadata = metadata;
        }
    }
}
