using Cochera.Application.DTOs;
using Cochera.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Cochera.Web.Services;

public class UsuarioActualService
{
    private readonly IServiceProvider _serviceProvider;
    private UsuarioDto? _usuarioActual;
    
    public event Action? OnUsuarioCambiado;
    
    public UsuarioActualService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public UsuarioDto? UsuarioActual => _usuarioActual;
    
    public bool EsAdmin => _usuarioActual?.EsAdmin ?? false;
    
    public bool EstaLogueado => _usuarioActual != null;
    
    public int? UsuarioId => _usuarioActual?.Id;
    
    public async Task<IEnumerable<UsuarioDto>> GetTodosUsuariosAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var usuarioService = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
        return await usuarioService.GetAllAsync();
    }
    
    public async Task CambiarUsuarioAsync(int usuarioId)
    {
        using var scope = _serviceProvider.CreateScope();
        var usuarioService = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
        _usuarioActual = await usuarioService.GetByIdAsync(usuarioId);
        OnUsuarioCambiado?.Invoke();
    }
    
    public void CerrarSesion()
    {
        _usuarioActual = null;
        OnUsuarioCambiado?.Invoke();
    }
}
