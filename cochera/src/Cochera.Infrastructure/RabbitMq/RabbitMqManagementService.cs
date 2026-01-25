using RabbitMQ.Client;
using Microsoft.Extensions.Options;
using Cochera.Infrastructure.Mqtt;

namespace Cochera.Infrastructure.RabbitMq;

public class RabbitMqManagementService
{
    private readonly MqttSettings _settings;

    public RabbitMqManagementService(IOptions<MqttSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task PurgeClientQueueAsync(string clientId)
    {
        var factory = new ConnectionFactory()
        {
            HostName = _settings.Server,
            Port = 5672, // Puerto AMQP (no MQTT)
            UserName = _settings.Username,
            Password = _settings.Password
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        // La cola MQTT se crea con formato: mqtt-subscription-{ClientId}qos{QoS}
        var queueName = $"mqtt-subscription-{clientId}qos1";
        
        try
        {
            var purged = channel.QueuePurge(queueName);
            await Task.CompletedTask; // Para hacer el método async
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al purgar cola {queueName}: {ex.Message}", ex);
        }
    }

    public async Task<uint> GetQueueMessageCountAsync(string clientId)
    {
        var factory = new ConnectionFactory()
        {
            HostName = _settings.Server,
            Port = 5672,
            UserName = _settings.Username,
            Password = _settings.Password
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        var queueName = $"mqtt-subscription-{clientId}qos1";
        
        try
        {
            var result = channel.QueueDeclarePassive(queueName);
            return result.MessageCount;
        }
        catch
        {
            return 0;
        }
    }
}