using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

namespace DigiTekShop.API.Extensions
{
    public static class ForwardedHeadersSetup
    {
        public static IServiceCollection AddForwardedHeadersSupport(this IServiceCollection services,
            IConfiguration config)
        {
            var section = config.GetSection("ReverseProxy");
            var enable = section.GetValue("EnableForwardedHeaders", defaultValue: true);

            if (!enable) return services;

            services.PostConfigure<ForwardedHeadersOptions>(opts =>
            {
                // همه Forwardedها را بپذیر
                opts.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto |
                    ForwardedHeaders.XForwardedHost;

                // اگر پشت پراکسی هستی، KnownProxies/Networks را تنظیم کن
                opts.KnownProxies.Clear();
                opts.KnownNetworks.Clear();

                foreach (var ip in section.GetSection("KnownProxies").Get<string[]>() ?? Array.Empty<string>())
                    if (IPAddress.TryParse(ip, out var parsed))
                        opts.KnownProxies.Add(parsed);

                foreach (var n in section.GetSection("KnownNetworks").GetChildren())
                {
                    var prefix = n.GetValue<string>("Prefix");
                    var len = n.GetValue<int?>("PrefixLength") ?? 24;
                    if (IPAddress.TryParse(prefix, out var ip))
                        opts.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(ip, len));
                }

                var limit = section.GetValue<int?>("ForwardLimit");
                if (limit.HasValue) opts.ForwardLimit = limit;
                opts.RequireHeaderSymmetry = false; // بعضی CDNها هدرها را کامل ست نمی‌کنند
            });

            return services;
        }

        public static IApplicationBuilder UseForwardedHeadersSupport(this IApplicationBuilder app,
            IConfiguration config)
        {
            if (config.GetValue<bool>("ReverseProxy:EnableForwardedHeaders", true))
                app.UseForwardedHeaders();

            return app;
        }
    }
}
