using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.SharedKernel.DomainShared.Events
{
    public interface IIntegrationEventHandler<T>
    {
        Task HandleAsync(T evt, CancellationToken ct);
    }
}
