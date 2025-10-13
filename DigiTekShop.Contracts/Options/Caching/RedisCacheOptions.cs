using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Options.Caching
{
    public sealed class RedisCacheOptions
    {
        public string InstanceName { get; init; } = "digitek:";
    }
}
