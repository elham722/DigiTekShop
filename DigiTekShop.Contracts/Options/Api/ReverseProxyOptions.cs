using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Options.Api
{
    public sealed class ReverseProxyOptions
    {
        public bool EnableForwardedHeaders { get; init; } = true;
        public List<string> KnownProxies { get; init; } = new();
        public List<string> KnownNetworks { get; init; } = new();
        public int? ForwardLimit { get; init; }
    }
}
