using Cochera.Application.DTOs;
using Cochera.Application.Interfaces;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.DependencyInjection;

namespace Cochera.Web.Services;

public class UsuarioActualService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ProtectedSessionStorage _sessionStorage;
    private UsuarioDto? _usuarioActual;
    private bool _initialized = false;
    
    public event Action? OnUsuarioCambiado;
    
    private const string StorageKey = "usuario_actual_id";
    
    public UsuarioActualService(IServiceProvider serviceProvider, ProtectedSessionStorage sessionStorage)
    {
        _serviceProvider = serviceProvider;
        _sessionStorage = sessionStorage;
    }
    
    public UsuarioDto? UsuarioActual => _usuarioActual;
    
    public bool EsAdmin => _usuarioActual?.EsAdmin ?? false;
    
    public bool EstaLogueado => _usuarioActual != null;
    
    public int? UsuarioId => _usuarioActual?.Id;
    
    public bool Initialized => _initialized;
    
    /// <summary>
    /// Inicializa el servicio cargando el usuario desde el storage del navegador.
    /// Debe llamarse desde OnAfterRenderAsync de los componentes.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;
        
        try
        {
            var result = await _sessionStorage.GetAsync<int>(StorageKey);
            if (result.Success && result.Value > 0)
            {
                using var scope = _serviceProvider.CreateScope();
                var usuarioService = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
                _usuarioActual = await usuarioService.GetByIdAsync(result.Value);
            }
        }
        catch
        {
            // Ignorar errores de storage (puede fallar en prerendering)
        }
        finally
        {
            _initialized = true;
        }
    }
    
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
        
        // Persistir en el storage del navegador
        try
        {
            if (_usuarioActual != null)
            {
                await _sessionStorage.SetAsync(StorageKey, _usuarioActual.Id);
            }
        }
        catch
        {
            // Ignorar errores de storage
        }
        
        OnUsuarioCambiado?.Invoke();
    }
    
    public async Task CerrarSesionAsync()
    {
        _usuarioActual = null;
        
        try
        {
            await _sessionStorage.DeleteAsync(StorageKey);
        }
        catch
        {
            // Ignorar errores de storage
        }
        
        OnUsuarioCambiado?.Invoke();
    }
    
    // Método legacy para compatibilidad
    public void CerrarSesion()
    {
        _usuarioActual = null;
        OnUsuarioCambiado?.Invoke();
    }
}
