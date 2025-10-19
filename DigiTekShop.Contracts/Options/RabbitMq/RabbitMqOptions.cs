namespace DigiTekShop.Contracts.Options.RabbitMq
{
    public sealed class RabbitMqOptions
    {
        public string HostName { get; init; } = "localhost";
        public int Port { get; init; } = 5672;
        public string VirtualHost { get; init; } = "/";
        public string UserName { get; init; } = "guest";
        public string Password { get; init; } = "guest";
        public string Exchange { get; init; } = "digitekshop.exchange";
        public string ExchangeType { get; init; } = "topic";
        public bool Durable { get; init; } = true;
        public string Queue { get; init; } = "integration-events.q";
        public int Prefetch { get; init; } = 16;
        public string? Dlx { get; init; } = "digitekshop.dlx";
        public string? Dlq { get; init; } = "integration-events.dlq";
        public bool UseDlq { get; init; } = true;
        public bool UseSsl { get; init; } = false;
    }

}
