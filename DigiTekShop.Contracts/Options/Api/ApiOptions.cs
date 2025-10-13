using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Options.Api
{
    public sealed class ApiOptions
    {
        public string Title { get; init; } = "DigiTekShop API";
        public string Version { get; init; } = "v1";
        public string Description { get; init; } = "";
        public int MaxPageSize { get; init; } = 100;
        public int DefaultPageSize { get; init; } = 10;
    }

}
