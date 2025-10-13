using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Abstractions.Events.Outbox
{
    public interface IAppOutboxBridge
    {
        Task EnqueueAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default);
    }
}
