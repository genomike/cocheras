using Cochera.Domain.Enums;

namespace Cochera.Application.DTOs;

public class SesionEstacionamientoDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public int CajonId { get; set; }
    public int CajonNumero { get; set; }
    public DateTime HoraEntrada { get; set; }
    public DateTime? HoraSalida { get; set; }
    public int MinutosEstacionado { get; set; }
    public decimal TarifaPorMinuto { get; set; }
    public decimal MontoTotal { get; set; }
    public EstadoSesion Estado { get; set; }
    public string EstadoTexto => Estado switch
    {
        EstadoSesion.Activa => "Activa",
        EstadoSesion.Finalizada => "Finalizada",
        EstadoSesion.Cancelada => "Cancelada",
        _ => "Desconocido"
    };
    public bool TienePago { get; set; }
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
