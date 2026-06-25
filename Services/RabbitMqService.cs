using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace App.Services;

// Minimal RabbitMQ publisher. The connection is established lazily on first
// publish and failures are swallowed (logged, not thrown) so the app still
// runs when no broker is configured/available — handy for local dev. Drop the
// resilience if you want publishing to be a hard dependency.
public class RabbitMqService : IAsyncDisposable {
    private readonly IConfiguration _config;
    private readonly ILogger<RabbitMqService> _log;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqService(IConfiguration config, ILogger<RabbitMqService> log) {
        _config = config;
        _log = log;
    }

    private async Task<IChannel?> GetChannelAsync(string queue, CancellationToken ct) {
        if (_channel is { IsOpen: true }) return _channel;

        await _gate.WaitAsync(ct);
        try {
            if (_channel is { IsOpen: true }) return _channel;

            var factory = new ConnectionFactory {
                HostName = _config["RabbitMQ:HostName"] ?? "localhost",
                UserName = _config["RabbitMQ:Username"] ?? "guest",
                Password = _config["RabbitMQ:Password"] ?? "guest",
            };
            _connection = await factory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
            await _channel.QueueDeclareAsync(queue, durable: true, exclusive: false,
                autoDelete: false, arguments: null, cancellationToken: ct);
            return _channel;
        } finally {
            _gate.Release();
        }
    }

    // Publish any JSON-serializable payload to a durable queue. Returns false
    // (and logs) instead of throwing when the broker is unreachable.
    public async Task<bool> PublishAsync(string queue, object payload, CancellationToken ct = default) {
        try {
            var channel = await GetChannelAsync(queue, ct);
            if (channel is null) return false;
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
            await channel.BasicPublishAsync(exchange: "", routingKey: queue,
                body: body.AsMemory(), cancellationToken: ct);
            return true;
        } catch (Exception ex) {
            _log.LogWarning(ex, "RabbitMQ publish to '{Queue}' failed (broker unavailable?)", queue);
            return false;
        }
    }

    public async ValueTask DisposeAsync() {
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
