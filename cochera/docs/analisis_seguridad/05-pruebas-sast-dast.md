# 05 — Pruebas de Seguridad (SAST / DAST)

## 5.1 Introducción

Este documento describe las herramientas y procedimientos recomendados para ejecutar pruebas de seguridad estáticas (SAST) y dinámicas (DAST) sobre el sistema Cochera Inteligente.

> **Actualización Marzo 2026:** Se actualizaron las configuraciones de prueba para reflejar la implementación de ASP.NET Core Identity, se añadieron nuevos escenarios de prueba para autenticación/autorización, y se ajustaron los resultados esperados.

---

## 5.2 Pruebas Estáticas (SAST)

### 5.2.1 Herramientas Recomendadas

| Herramienta | Tipo | Lenguaje | Propósito |
|-------------|------|----------|-----------|
| **Roslyn Analyzers** | SAST | C# | Análisis de código en tiempo de compilación |
| **SonarQube** | SAST | C#, SQL | Análisis de calidad y seguridad |
| **dotnet-security-guard** | SAST | C# | Reglas específicas de seguridad para .NET |
| **Semgrep** | SAST | C#, C++ | Análisis de patrones de seguridad |
| **CodeQL** | SAST | C# | Análisis semántico de seguridad (GitHub) |
| **Snyk** | SCA | NuGet | Análisis de dependencias vulnerables |

### 5.2.2 Configuración de Roslyn Security Analyzers

```xml
<!-- En cada .csproj -->
<ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.0.0" />
    <PackageReference Include="SecurityCodeScan.VS2019" Version="5.6.7" />
</ItemGroup>

<!-- .editorconfig -->
[*.cs]
dotnet_diagnostic.SCS0005.severity = warning  # Weak random
dotnet_diagnostic.SCS0018.severity = warning  # Path traversal
dotnet_diagnostic.SCS0026.severity = warning  # SQL injection
dotnet_diagnostic.SCS0029.severity = warning  # XSS
```

### 5.2.3 Hallazgos SAST Esperados (Actualizado)

| Regla | Archivo | Descripción | Estado |
|-------|---------|-------------|--------|
| ~~SCS0005~~ | ~~UsuarioActualService.cs~~ | ~~Auth bypass~~ | ✅ Resuelto — ya no hay suplantación |
| SCS0016 | appsettings.json | Hardcoded connection string | ❌ Se detectará |
| SCS0029 | Login.razor | Posible XSS en error display | ⚠️ Verificar |
| SCS0032 | MqttConsumerService.cs | Insecure deserialization | ❌ Se detectará |
| CA2000 | MqttConsumerService.cs | Disposable not disposed | ❌ Se detectará |

### 5.2.4 Análisis de Dependencias (SCA)

```bash
# Verificar vulnerabilidades conocidas en NuGet packages
dotnet list package --vulnerable --include-transitive

# Con Snyk CLI
snyk test --file=Cochera.sln

# Con dotnet-outdated  
dotnet tool install -g dotnet-outdated-tool
dotnet outdated Cochera.sln
```

**Paquetes a monitorear especialmente:**

| Paquete | Versión | Notas |
|---------|---------|-------|
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.x | 🆕 Nuevo — verificar parches |
| MQTTnet | 4.3.3 | Verificar actualizaciones |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.x | Verificar CVEs |
| Radzen.Blazor | 5.x | Componentes UI de terceros |

---

## 5.3 Pruebas Dinámicas (DAST)

### 5.3.1 Herramientas Recomendadas

| Herramienta | Tipo | Propósito |
|-------------|------|-----------|
| **OWASP ZAP** | DAST | Escaneo automatizado web |
| **Burp Suite** | DAST | Interceptación y análisis de tráfico |
| **Nuclei** | DAST | Templates de vulnerabilidades conocidas |
| **sqlmap** | DAST | Inyección SQL |
| **testssl.sh** | Infra | Evaluación de TLS |
| **Wireshark** | Network | Captura de tráfico MQTT |

### 5.3.2 Configuración OWASP ZAP

```yaml
# zap-config.yaml (Actualizado para sistema con Identity)
env:
  contexts:
    - name: "Cochera Inteligente"
      urls:
        - "https://localhost:7XXX"
      includePaths:
        - "https://localhost:7XXX/.*"
      excludePaths:
        - "https://localhost:7XXX/_framework/.*"
        - "https://localhost:7XXX/_content/.*"
      authentication:
        method: "form"
        parameters:
          loginPageUrl: "https://localhost:7XXX/login"
          loginRequestUrl: "https://localhost:7XXX/auth/login"
          loginRequestData: "username={%username%}&password={%password%}"
          # NOTA: El formulario no tiene AntiForgery token
        verification:
          method: "response"
          loggedInRegex: "Cochera\\.Auth"  # Cookie de sesión
          loggedOutRegex: "Iniciar sesión"
      users:
        - name: "admin"
          credentials:
            username: "admin"
            password: "Admin12345"
        - name: "usuario_1"
          credentials:
            username: "usuario_1"
            password: "Usuario12345"
  
  # Políticas de escaneo específicas
  policy:
    defaultPolicy:
      parameterHandling:
        enablePerformanceOptimizations: true
      activeScanRules:
        - id: 40012  # XSS Reflected
          threshold: "medium"
        - id: 40014  # XSS Persistent
          threshold: "medium"
        - id: 40018  # SQL Injection
          threshold: "low"
        - id: 10202  # Missing Anti-CSRF Tokens
          threshold: "medium"
```

### 5.3.3 Checklist de Pruebas DAST (Actualizado)

#### Autenticación (🆕 Nuevos tests post-Identity)

| # | Prueba | Resultado Esperado | Estado |
|---|--------|-------------------|--------|
| D-01 | Acceso a `/admin/dashboard` sin cookie | ✅ Redirige a `/login` | 🔒 Nuevo |
| D-02 | Acceso a `/usuario/estacionamiento` sin cookie | ✅ Redirige a `/login` | 🔒 Nuevo |
| D-03 | Login con credenciales incorrectas | ✅ Redirige a `/login?error=1` | 🔒 Nuevo |
| D-04 | Login con credenciales correctas | ✅ Set-Cookie: Cochera.Auth, redirect a `/` | 🔒 Nuevo |
| D-05 | Acceso a `/admin/dashboard` con rol User | ⚠️ Verificar redirect a `/access-denied` | 🔒 Nuevo |
| D-06 | Logout invalida cookie del servidor | ✅ Cookie eliminada, redirige a `/login` | 🔒 Nuevo |
| D-07 | Cookie expirada (>8h) redirecciona a login | ✅ Debe redirigir | 🔒 Nuevo |
| D-08 | Fuerza bruta en `/auth/login` (100 intentos) | ⚠️ Sin rate limiting — vulnerable | ❌ Falla |
| D-09 | CSRF en `/auth/login` (POST desde otro origin) | ⚠️ DisableAntiforgery — vulnerable | ❌ Falla |

#### SignalR Hub

| # | Prueba | Resultado Esperado | Estado |
|---|--------|-------------------|--------|
| D-10 | Conexión WebSocket sin cookie | ⚠️ Se conecta (clase sin `[Authorize]`) | ❌ Falla |
| D-11 | `UnirseComoAdmin` sin rol Admin | ✅ HubException | 🔒 Nuevo |
| D-12 | `UnirseComoUsuario(2)` siendo usuario_1 | ✅ HubException "Acceso denegado" | 🔒 Nuevo |
| D-13 | `NuevoEvento` sin autenticación | ⚠️ Se ejecuta (método sin `[Authorize]`) | ❌ Falla |
| D-14 | `CambioEstado` sin autenticación | ⚠️ Se ejecuta | ❌ Falla |

#### MQTT / IoT

| # | Prueba | Resultado Esperado | Estado |
|---|--------|-------------------|--------|
| D-15 | Captura Wireshark en puerto 1883 | ❌ Tráfico visible en texto plano | ❌ Falla |
| D-16 | Inyectar mensaje JSON malformado vía MQTT | ❌ Se procesa sin validación | ❌ Falla |
| D-17 | Conexión MQTT con credenciales del firmware | ❌ Se conecta (esp32/123456) | ❌ Falla |

#### Infraestructura

| # | Prueba | Resultado Esperado | Estado |
|---|--------|-------------------|--------|
| D-18 | Verificar headers de seguridad HTTP | ❌ Faltan CSP, X-Frame-Options, etc. | ❌ Falla |
| D-19 | Acceso PostgreSQL con postgres/postgres | ❌ Acceso total (superusuario) | ❌ Falla |
| D-20 | Verificar HSTS header | ⚠️ Solo en producción (UseHsts) | ⚠️ Parcial |

---

## 5.4 Pruebas Específicas de Autenticación (🆕 Manual)

### 5.4.1 Test: Flujo Completo de Login

```bash
# 1. Verificar que /admin/dashboard redirige a /login
curl -v -L https://localhost:7XXX/admin/dashboard 2>&1 | grep -i "location"
# Esperado: Location: /login?returnUrl=%2Fadmin%2Fdashboard

# 2. Login con credenciales válidas
curl -v -X POST https://localhost:7XXX/auth/login \
  -d "username=admin&password=Admin12345&returnUrl=/" \
  -c cookies.txt
# Esperado: Set-Cookie: Cochera.Auth=...; path=/; httponly

# 3. Verificar acceso con cookie
curl -v https://localhost:7XXX/admin/dashboard -b cookies.txt
# Esperado: 200 OK con contenido del dashboard

# 4. Logout
curl -v https://localhost:7XXX/logout -b cookies.txt -c cookies.txt
# Esperado: Cookie invalidada, redirect a /login
```

### 5.4.2 Test: Bypass de Autorización SignalR

```javascript
// Usando un cliente JavaScript (navegador DevTools)
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/cocherahub")  // Sin cookie de autenticación
    .build();

await connection.start();
console.log("Conectado:", connection.connectionId); // ⚠️ Se conecta

// Test: Invocar método protegido
try {
    await connection.invoke("UnirseComoAdmin"); // Debería fallar
} catch (e) {
    console.log("Error esperado:", e); // ✅ HubException
}

// Test: Invocar método sin protección
await connection.invoke("NuevoEvento", { /* dto falso */ }); // ⚠️ Se ejecuta
```

### 5.4.3 Test: Validación de returnUrl

```bash
# Intentar open redirect
curl -X POST https://localhost:7XXX/auth/login \
  -d "username=admin&password=Admin12345&returnUrl=https://evil.com"
# Esperado: Redirige a "/" (validación rechaza URLs absolutas)

# returnUrl relativa pero sin /
curl -X POST https://localhost:7XXX/auth/login \
  -d "username=admin&password=Admin12345&returnUrl=evil"
# Esperado: Redirige a "/" (validación requiere que empiece con /)
```

---

## 5.5 Pipeline CI/CD de Seguridad Propuesto

```yaml
# .github/workflows/security.yml
name: Security Scan
on: [push, pull_request]

jobs:
  sast:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0'
      - run: dotnet build --warnaserror  # Roslyn analyzers
      - name: Run Semgrep
        uses: returntocorp/semgrep-action@v1
        with:
          config: >-
            p/csharp
            p/owasp-top-ten

  sca:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - run: dotnet list package --vulnerable --include-transitive
      - name: Snyk
        uses: snyk/actions/dotnet@master
        env:
          SNYK_TOKEN: ${{ secrets.SNYK_TOKEN }}

  dast:
    runs-on: ubuntu-latest
    needs: [sast]
    steps:
      - name: OWASP ZAP Scan
        uses: zaproxy/action-full-scan@v0.7.0
        with:
          target: 'https://localhost:7XXX'
          config: zap-config.yaml
```

---

## 5.6 Resultados Esperados Post-Remediación

### Comparación Antes vs Después

| Categoría | Tests Pre-Auth | Tests Post-Auth | Mejora |
|-----------|---------------|-----------------|--------|
| Auth — Acceso sin credenciales | ❌ 0/5 pass | ✅ 5/7 pass | +5 |
| Auth — Login flow | N/A | ✅ 4/4 pass | +4 nuevos |
| Auth — Brute force protection | N/A | ❌ 0/1 pass | Pendiente |
| SignalR — Auth methods | ❌ 0/3 pass | ✅ 2/5 pass | +2 |
| MQTT — Seguridad | ❌ 0/3 pass | ❌ 0/3 pass | Sin cambio |
| Headers — Seguridad | ❌ 0/2 pass | ❌ 0/2 pass | Sin cambio |
| Infra — BD seguridad | ❌ 0/2 pass | ❌ 0/2 pass | Sin cambio |
| **Total** | **0/15** | **11/24** | **+11** |

---

*Anterior: [04 — Propuesta de Mejoras](04-propuesta-mejoras.md)*
*Siguiente: [06 — Gestión de Vulnerabilidades](06-gestion-vulnerabilidades.md)*
