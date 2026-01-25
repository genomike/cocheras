using Cochera.Domain.Enums;

namespace Cochera.Domain.Entities;

public class EventoSensor : BaseEntity
{
    public TipoEvento TipoEvento { get; set; }
    public string EventoOriginal { get; set; } = string.Empty; // Nombre del evento ESP32
    public string Detalle { get; set; } = string.Empty;
    public string TimestampESP32 { get; set; } = string.Empty;
    public string EstadoCajon1 { get; set; } = string.Empty;
    public string EstadoCajon2 { get; set; } = string.Empty;
    public int CajonesLibres { get; set; }
    public int CajonesOcupados { get; set; }
    public bool CocheraLlena { get; set; }
    public string JsonOriginal { get; set; } = string.Empty; // JSON crudo del mensaje
}
