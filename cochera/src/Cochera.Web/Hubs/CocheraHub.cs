using Cochera.Application.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace Cochera.Web.Hubs;

public class CocheraHub : Hub
{
    public async Task NuevoEvento(EventoSensorDto evento)
    {
        await Clients.All.SendAsync("RecibirEvento", evento);
    }

    public async Task CambioEstado(EstadoCocheraDto estado)
    {
        await Clients.All.SendAsync("RecibirEstado", estado);
    }

    public async Task NuevaSesion(SesionEstacionamientoDto sesion)
    {
        await Clients.All.SendAsync("RecibirNuevaSesion", sesion);
    }

    public async Task SesionFinalizada(SesionEstacionamientoDto sesion)
    {
        await Clients.All.SendAsync("RecibirSesionFinalizada", sesion);
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
