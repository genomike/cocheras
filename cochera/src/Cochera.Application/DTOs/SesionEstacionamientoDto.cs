using Cochera.Domain.Enums;

namespace Cochera.Application.DTOs;

public class SesionEstacionamientoDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public string UsuarioCodigo { get; set; } = string.Empty;
    public int CajonId { get; set; }
    public int CajonNumero { get; set; }
    public DateTime HoraEntrada { get; set; }
    public DateTime? HoraSalida { get; set; }
    public decimal TarifaPorMinuto { get; set; }
    public EstadoSesion Estado { get; set; }
    public bool TienePago { get; set; }
    public DateTime? FechaPago { get; set; }
    public MetodoPago? MetodoPago { get; set; }

    // Propiedades calculadas
    public int MinutosEstacionado => CalcularMinutos();
    public decimal MontoTotal => CalcularMonto();
    
    public string EstadoTexto => Estado switch
    {
        EstadoSesion.Activa => "Activa",
        EstadoSesion.PendientePago => "Pendiente de Pago",
        EstadoSesion.Finalizada => "Finalizada",
        EstadoSesion.Cancelada => "Cancelada",
        _ => "Desconocido"
    };

    public string TiempoFormateado
    {
        get
        {
            var minutos = MinutosEstacionado;
            var horas = minutos / 60;
            var mins = minutos % 60;
            return horas > 0 ? $"{horas}h {mins}m" : $"{mins}m";
        }
    }

    private int CalcularMinutos()
    {
        var fin = HoraSalida ?? DateTime.UtcNow;
        var diferencia = fin - HoraEntrada;
        // Fracción de minuto cuenta como minuto completo
        var minutos = (int)diferencia.TotalMinutes;
        if (diferencia.Seconds > 0 || diferencia.Milliseconds > 0)
            minutos++;
        return Math.Max(1, minutos); // Mínimo 1 minuto
    }

    private decimal CalcularMonto()
    {
        return MinutosEstacionado * TarifaPorMinuto;
    }
}

public class IniciarSesionRequest
{
    public int UsuarioId { get; set; }
    public int CajonId { get; set; }
}

public class FinalizarSesionRequest
{
    public int SesionId { get; set; }
    public MetodoPago MetodoPago { get; set; }
}

public class SolicitudPagoDto
{
    public int SesionId { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public int CajonNumero { get; set; }
    public DateTime HoraEntrada { get; set; }
    public int MinutosEstacionado { get; set; }
    public decimal MontoTotal { get; set; }
    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
}

public class ConfirmacionPagoDto
{
    public int SesionId { get; set; }
    public int UsuarioId { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public DateTime FechaPago { get; set; } = DateTime.UtcNow;
}
