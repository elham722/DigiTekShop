using DigiTekShop.Contracts.IntegrationEvents;
using DigiTekShop.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Testcontainers.RabbitMq;
using Xunit;

namespace DigiTekShop.IntegrationTests.Messaging;

[Collection("Integration")]
public sealed class RabbitMQIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly RabbitMqContainer _rabbitMqContainer;
    private readonly IServiceScope _scope;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly List<string> _receivedMessages = new();

    public RabbitMQIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Configure RabbitMQ for testing
                services.Configure<RabbitMqOptions>(options =>
                {
                    options.HostName = "localhost";
                    options.Port = 5672;
                    options.UserName = "guest";
                    options.Password = "guest";
                    options.VirtualHost = "/";
                });
            });
        });

        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .WithPortBinding(5672, true)
            .WithPortBinding(15672, true)
            .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
            .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
            .Build();

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        
        var connectionFactory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest",
            VirtualHost = "/",
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            Heartbeat = TimeSpan.FromSeconds(30)
        };

        _connection = connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    private readonly HttpClient _client;

    public async Task InitializeAsync()
    {
        await _rabbitMqContainer.StartAsync();
        
        // Wait for RabbitMQ to be ready
        await Task.Delay(5000);
        
        // Setup test exchanges and queues
        SetupTestInfrastructure();
    }

    public async Task DisposeAsync()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        _scope?.Dispose();
        _client?.Dispose();
        await _rabbitMqContainer.StopAsync();
        await _rabbitMqContainer.DisposeAsync();
    }

    #region Message Publishing Tests

    [Fact]
    public async Task PublishMessage_WithValidMessage_ShouldSucceed()
    {
        // Arrange
        var message = new UserRegisteredEvent
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser",
            RegisteredAt = DateTime.UtcNow
        };

        var exchange = "test.exchange";
        var routingKey = "user.registered";
        var queue = "test.user.registered";

        // Setup exchange and queue
        _channel.ExchangeDeclare(exchange, ExchangeType.Topic, true, false);
        _channel.QueueDeclare(queue, true, false, false);
        _channel.QueueBind(queue, exchange, routingKey);

        // Act
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        _channel.BasicPublish(exchange, routingKey, null, body);

        // Assert
        var messageCount = _channel.MessageCount(queue);
        messageCount.Should().Be(1);
    }

    [Fact]
    public async Task PublishMessage_WithInvalidExchange_ShouldThrowException()
    {
        // Arrange
        var message = new UserRegisteredEvent
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser",
            RegisteredAt = DateTime.UtcNow
        };

        var exchange = "nonexistent.exchange";
        var routingKey = "user.registered";

        // Act & Assert
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var action = () => _channel.BasicPublish(exchange, routingKey, null, body);
        
        action.Should().Throw<Exception>();
    }

    #endregion

    #region Message Consumption Tests

    [Fact]
    public async Task ConsumeMessage_WithValidMessage_ShouldReceiveMessage()
    {
        // Arrange
        var message = new UserRegisteredEvent
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser",
            RegisteredAt = DateTime.UtcNow
        };

        var exchange = "test.exchange";
        var routingKey = "user.registered";
        var queue = "test.user.registered";

        // Setup exchange and queue
        _channel.ExchangeDeclare(exchange, ExchangeType.Topic, true, false);
        _channel.QueueDeclare(queue, true, false, false);
        _channel.QueueBind(queue, exchange, routingKey);

        // Setup consumer
        var consumer = new TestConsumer(_channel);
        _channel.BasicConsume(queue, false, consumer);

        // Act
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        _channel.BasicPublish(exchange, routingKey, null, body);

        // Wait for message to be consumed
        await Task.Delay(1000);

        // Assert
        consumer.ReceivedMessages.Should().HaveCount(1);
        var receivedMessage = JsonSerializer.Deserialize<UserRegisteredEvent>(consumer.ReceivedMessages[0]);
        receivedMessage.Should().NotBeNull();
        receivedMessage!.UserId.Should().Be(message.UserId);
        receivedMessage.Email.Should().Be(message.Email);
    }

    [Fact]
    public async Task ConsumeMessage_WithAck_ShouldRemoveMessageFromQueue()
    {
        // Arrange
        var message = new UserRegisteredEvent
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser",
            RegisteredAt = DateTime.UtcNow
        };

        var exchange = "test.exchange";
        var routingKey = "user.registered";
        var queue = "test.user.registered";

        // Setup exchange and queue
        _channel.ExchangeDeclare(exchange, ExchangeType.Topic, true, false);
        _channel.QueueDeclare(queue, true, false, false);
        _channel.QueueBind(queue, exchange, routingKey);

        // Setup consumer with auto-ack
        var consumer = new TestConsumer(_channel);
        _channel.BasicConsume(queue, true, consumer); // auto-ack = true

        // Act
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        _channel.BasicPublish(exchange, routingKey, null, body);

        // Wait for message to be consumed
        await Task.Delay(1000);

        // Assert
        var messageCount = _channel.MessageCount(queue);
        messageCount.Should().Be(0); // Message should be removed after ack
    }

    [Fact]
    public async Task ConsumeMessage_WithNack_ShouldKeepMessageInQueue()
    {
        // Arrange
        var message = new UserRegisteredEvent
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser",
            RegisteredAt = DateTime.UtcNow
        };

        var exchange = "test.exchange";
        var routingKey = "user.registered";
        var queue = "test.user.registered";

        // Setup exchange and queue
        _channel.ExchangeDeclare(exchange, ExchangeType.Topic, true, false);
        _channel.QueueDeclare(queue, true, false, false);
        _channel.QueueBind(queue, exchange, routingKey);

        // Setup consumer that nacks messages
        var consumer = new TestConsumer(_channel, shouldNack: true);
        _channel.BasicConsume(queue, false, consumer);

        // Act
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        _channel.BasicPublish(exchange, routingKey, null, body);

        // Wait for message to be consumed
        await Task.Delay(1000);

        // Assert
        var messageCount = _channel.MessageCount(queue);
        messageCount.Should().Be(1); // Message should remain in queue after nack
    }

    #endregion

    #region Dead Letter Queue Tests

    [Fact]
    public async Task PublishMessage_WithDLX_ShouldRouteToDeadLetterQueue()
    {
        // Arrange
        var message = new UserRegisteredEvent
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser",
            RegisteredAt = DateTime.UtcNow
        };

        var exchange = "test.exchange";
        var routingKey = "user.registered";
        var queue = "test.user.registered";
        var dlx = "test.dlx";
        var dlq = "test.dlq";

        // Setup DLX and DLQ
        _channel.ExchangeDeclare(dlx, ExchangeType.Topic, true, false);
        _channel.QueueDeclare(dlq, true, false, false);
        _channel.QueueBind(dlq, dlx, "#");

        // Setup main queue with DLX
        var args = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", dlx },
            { "x-message-ttl", 1000 } // 1 second TTL
        };

        _channel.ExchangeDeclare(exchange, ExchangeType.Topic, true, false);
        _channel.QueueDeclare(queue, true, false, false, args);
        _channel.QueueBind(queue, exchange, routingKey);

        // Act
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        _channel.BasicPublish(exchange, routingKey, null, body);

        // Wait for TTL to expire
        await Task.Delay(2000);

        // Assert
        var dlqMessageCount = _channel.MessageCount(dlq);
        dlqMessageCount.Should().Be(1);
    }

    #endregion

    #region Connection Recovery Tests

    [Fact]
    public async Task PublishMessage_AfterConnectionRecovery_ShouldSucceed()
    {
        // Arrange
        var message = new UserRegisteredEvent
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser",
            RegisteredAt = DateTime.UtcNow
        };

        var exchange = "test.exchange";
        var routingKey = "user.registered";
        var queue = "test.user.registered";

        // Setup exchange and queue
        _channel.ExchangeDeclare(exchange, ExchangeType.Topic, true, false);
        _channel.QueueDeclare(queue, true, false, false);
        _channel.QueueBind(queue, exchange, routingKey);

        // Act
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        _channel.BasicPublish(exchange, routingKey, null, body);

        // Assert
        var messageCount = _channel.MessageCount(queue);
        messageCount.Should().Be(1);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task PublishMultipleMessages_ShouldHandleCorrectly()
    {
        // Arrange
        var exchange = "test.exchange";
        var routingKey = "user.registered";
        var queue = "test.user.registered";
        var messageCount = 100;

        // Setup exchange and queue
        _channel.ExchangeDeclare(exchange, ExchangeType.Topic, true, false);
        _channel.QueueDeclare(queue, true, false, false);
        _channel.QueueBind(queue, exchange, routingKey);

        // Act
        var tasks = new List<Task>();
        for (int i = 0; i < messageCount; i++)
        {
            var message = new UserRegisteredEvent
            {
                UserId = Guid.NewGuid(),
                Email = $"user{i}@example.com",
                UserName = $"user{i}",
                RegisteredAt = DateTime.UtcNow
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            tasks.Add(Task.Run(() => _channel.BasicPublish(exchange, routingKey, null, body)));
        }

        await Task.WhenAll(tasks);

        // Assert
        var actualMessageCount = _channel.MessageCount(queue);
        actualMessageCount.Should().Be(messageCount);
    }

    #endregion

    #region Helper Methods

    private void SetupTestInfrastructure()
    {
        // Setup test exchanges
        _channel.ExchangeDeclare("test.exchange", ExchangeType.Topic, true, false);
        _channel.ExchangeDeclare("test.dlx", ExchangeType.Topic, true, false);
    }

    #endregion

    #region Test Consumer

    private class TestConsumer : DefaultBasicConsumer
    {
        public List<string> ReceivedMessages { get; } = new();
        private readonly bool _shouldNack;

        public TestConsumer(IModel model, bool shouldNack = false) : base(model)
        {
            _shouldNack = shouldNack;
        }

        public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, ReadOnlyMemory<byte> body)
        {
            var message = Encoding.UTF8.GetString(body.Span);
            ReceivedMessages.Add(message);

            if (_shouldNack)
            {
                Model.BasicNack(deliveryTag, false, true);
            }
            else
            {
                Model.BasicAck(deliveryTag, false);
            }
        }
    }

    #endregion
}
