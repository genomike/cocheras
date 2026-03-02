using Cochera.Application.DTOs;
using Cochera.Application.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cochera.Web.Services;

public class UsuarioActualService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private UsuarioDto? _usuarioActual;
    private bool _initialized = false;
    
    public event Action? OnUsuarioCambiado;
    
    public UsuarioActualService(IServiceProvider serviceProvider, AuthenticationStateProvider authenticationStateProvider)
    {
        _serviceProvider = serviceProvider;
        _authenticationStateProvider = authenticationStateProvider;
    }
    
    public UsuarioDto? UsuarioActual => _usuarioActual;
    
    public bool EsAdmin => _usuarioActual?.EsAdmin ?? false;
    
    public bool EstaLogueado => _usuarioActual != null;
    
    public int? UsuarioId => _usuarioActual?.Id;
    
    public bool Initialized => _initialized;
    
    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        await RefrescarDesdeIdentidadAsync();
        _initialized = true;
    }
    
    public async Task<IEnumerable<UsuarioDto>> GetTodosUsuariosAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var usuarioService = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
        return await usuarioService.GetAllAsync();
    }

    public async Task RefrescarDesdeIdentidadAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var userPrincipal = authState.User;

        if (userPrincipal?.Identity?.IsAuthenticated != true)
        {
            _usuarioActual = null;
            OnUsuarioCambiado?.Invoke();
            return;
        }

        var codigo = userPrincipal.Identity?.Name;
        if (string.IsNullOrWhiteSpace(codigo))
        {
            _usuarioActual = null;
            OnUsuarioCambiado?.Invoke();
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var usuarioService = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
        _usuarioActual = await usuarioService.GetByCodigoAsync(codigo);
        OnUsuarioCambiado?.Invoke();
    }

    public async Task CambiarUsuarioAsync(int usuarioId)
    {
        await RefrescarDesdeIdentidadAsync();
    }

    public async Task CerrarSesionAsync()
    {
        _usuarioActual = null;
        _initialized = false;
        OnUsuarioCambiado?.Invoke();
    }
    
    // Método legacy para compatibilidad
    public void CerrarSesion()
    {
        _usuarioActual = null;
        OnUsuarioCambiado?.Invoke();
    }
}
