using Cochera.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;

namespace Cochera.Infrastructure.Mqtt;

public class MqttConsumerService : IMqttConsumerService, IDisposable
{
    private readonly MqttSettings _settings;
    private readonly ILogger<MqttConsumerService> _logger;
    private IMqttClient? _mqttClient;
    private bool _disposed;

    public event Func<MensajeSensorMqtt, string, Task>? OnMensajeRecibido;
    public bool IsConnected => _mqttClient?.IsConnected ?? false;

    public MqttConsumerService(IOptions<MqttSettings> settings, ILogger<MqttConsumerService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_settings.Server, _settings.Port)
            .WithCredentials(_settings.Username, _settings.Password)
            .WithClientId($"{_settings.ClientId}_{Guid.NewGuid():N}")
            .WithCleanSession()
            .Build();

        _mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            try
            {
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                _logger.LogInformation("Mensaje recibido en topic {Topic}: {Payload}", 
                    e.ApplicationMessage.Topic, payload);

                var mensaje = JsonSerializer.Deserialize<MensajeSensorMqtt>(payload, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (mensaje != null && OnMensajeRecibido != null)
                {
                    await OnMensajeRecibido.Invoke(mensaje, payload);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar mensaje MQTT");
            }
        };

        _mqttClient.DisconnectedAsync += async e =>
        {
            _logger.LogWarning("Desconectado del broker MQTT. Intentando reconectar...");
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            
            try
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await _mqttClient.ConnectAsync(options, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reconectar con MQTT");
            }
        };

        _mqttClient.ConnectedAsync += async e =>
        {
            _logger.LogInformation("Conectado al broker MQTT {Server}:{Port}", _settings.Server, _settings.Port);
            
            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(_settings.Topic)
                .Build(), cancellationToken);
            
            _logger.LogInformation("Suscrito al topic: {Topic}", _settings.Topic);
        };

        try
        {
            await _mqttClient.ConnectAsync(options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al conectar con MQTT broker");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_mqttClient?.IsConnected == true)
        {
            await _mqttClient.DisconnectAsync(cancellationToken: cancellationToken);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _mqttClient?.Dispose();
            _disposed = true;
        }
    }
}
