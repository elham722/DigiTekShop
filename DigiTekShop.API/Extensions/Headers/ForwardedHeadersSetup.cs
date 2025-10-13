using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

namespace DigiTekShop.API.Extensions.Headers
{
    public static class ForwardedHeadersSetup
    {
        public static IServiceCollection AddForwardedHeadersSupport(
            this IServiceCollection services,
            IConfiguration config)
        {
            var section = config.GetSection("ReverseProxy");
            var enable = section.GetValue("EnableForwardedHeaders", defaultValue: true);
            if (!enable) return services;

            services.PostConfigure<ForwardedHeadersOptions>(opts =>
            {
                opts.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto |
                    ForwardedHeaders.XForwardedHost;

              
                var trustAll = section.GetValue("TrustAll", false);

                
                var proxies = section.GetSection("KnownProxies").Get<string[]>() ?? Array.Empty<string>();
                var networks = section.GetSection("KnownNetworks").GetChildren().ToList();

                if (trustAll)
                {
                    opts.KnownProxies.Clear();
                    opts.KnownNetworks.Clear();
                }
                else if (proxies.Length > 0 || networks.Count > 0)
                {
                    opts.KnownProxies.Clear();
                    opts.KnownNetworks.Clear();

                    foreach (var ipStr in proxies)
                    {
                        if (IPAddress.TryParse(ipStr, out var parsed))
                            opts.KnownProxies.Add(parsed);
                    }

                    foreach (var n in networks)
                    {
                        var prefix = n.GetValue<string>("Prefix");
                        var len = n.GetValue<int?>("PrefixLength") ?? 24;
                        if (IPAddress.TryParse(prefix, out var ip))
                            opts.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(ip, len));
                    }
                }
               
                var limit = section.GetValue<int?>("ForwardLimit");
                if (limit.HasValue)
                    opts.ForwardLimit = limit.Value;

                opts.RequireHeaderSymmetry = false;
            });

            return services;
        }

        public static IApplicationBuilder UseForwardedHeadersSupport(
            this IApplicationBuilder app,
            IConfiguration config)
        {
            if (config.GetValue("ReverseProxy:EnableForwardedHeaders", true))
                app.UseForwardedHeaders();

            return app;
        }
    }
}
