namespace Cochera.Application.DTOs;

public class DashboardDto
{
    public EstadoCocheraDto EstadoActual { get; set; } = new();
    public int TotalSesionesHoy { get; set; }
    public decimal RecaudacionHoy { get; set; }
    public decimal RecaudacionMes { get; set; }
    public int TotalEventosHoy { get; set; }
    public List<EventoSensorDto> UltimosEventos { get; set; } = new();
    public List<SesionEstacionamientoDto> SesionesActivas { get; set; } = new();
    public List<ResumenPorHora> UsoPorHora { get; set; } = new();
    public List<ResumenPorDia> UsoPorDia { get; set; } = new();
}

public class ResumenPorHora
{
    public int Hora { get; set; }
    public int CantidadSesiones { get; set; }
    public decimal Recaudacion { get; set; }
}

public class ResumenPorDia
{
    public DateTime Fecha { get; set; }
    public int CantidadSesiones { get; set; }
    public decimal Recaudacion { get; set; }
}
