namespace Cochera.Infrastructure.Mqtt;

public class MqttSettings
{
    public string Server { get; set; } = "192.168.100.16";
    public int Port { get; set; } = 1883;
    public string Username { get; set; } = "esp32";
    public string Password { get; set; } = "123456";
    public string Topic { get; set; } = "cola_sensores";
    public string ClientId { get; set; } = "CocheraWorker";
}
