using Cochera.Application.DTOs;

namespace Cochera.Infrastructure.Mqtt;

public interface IMqttConsumerService
{
    event Func<MensajeSensorMqtt, string, Task>? OnMensajeRecibido;
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    bool IsConnected { get; }
}
