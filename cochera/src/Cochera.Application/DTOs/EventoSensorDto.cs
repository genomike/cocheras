using Cochera.Domain.Enums;

namespace Cochera.Application.DTOs;

public class EventoSensorDto
{
    public int Id { get; set; }
    public TipoEvento TipoEvento { get; set; }
    public string EventoOriginal { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public string TimestampESP32 { get; set; } = string.Empty;
    public string EstadoCajon1 { get; set; } = string.Empty;
    public string EstadoCajon2 { get; set; } = string.Empty;
    public int CajonesLibres { get; set; }
    public int CajonesOcupados { get; set; }
    public bool CocheraLlena { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string TipoEventoTexto => TipoEvento switch
    {
        TipoEvento.SistemaIniciado => "Sistema Iniciado",
        TipoEvento.MovimientoEntrada => "Movimiento Entrada",
        TipoEvento.MovimientoEntradaBloqueado => "Entrada Bloqueada",
        TipoEvento.VehiculoSalio => "Vehículo Salió",
        TipoEvento.CajonOcupado => "Cajón Ocupado",
        TipoEvento.CajonLiberado => "Cajón Liberado",
        TipoEvento.ParpadeoIniciado => "Parpadeo Iniciado",
        TipoEvento.ParpadeoTimeout => "Parpadeo Timeout",
        TipoEvento.CocheraLlena => "Cochera Llena",
        _ => "Desconocido"
    };
}

// DTO para recibir mensajes MQTT desde ESP32
public class MensajeSensorMqtt
{
    public string evento { get; set; } = string.Empty;
    public string detalle { get; set; } = string.Empty;
    public string timestamp { get; set; } = string.Empty;
    public string cajon1 { get; set; } = string.Empty;
    public string cajon2 { get; set; } = string.Empty;
    public int libres { get; set; }
    public int ocupados { get; set; }
    public bool lleno { get; set; }
}
