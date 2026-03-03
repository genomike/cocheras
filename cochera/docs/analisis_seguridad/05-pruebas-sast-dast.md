# 05 — Pruebas de Seguridad (SAST / DAST)

## 5.1 Introducción

Este documento describe las herramientas y procedimientos recomendados para ejecutar pruebas de seguridad estáticas (SAST) y dinámicas (DAST) sobre el sistema Cochera Inteligente.

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

### 5.2.3 Hallazgos SAST Esperados

| Regla | Archivo | Descripción | Estado |
|-------|---------|-------------|--------|
| SCS0016 | appsettings.json | Hardcoded connection string con credenciales | [FALLA] Se detectará |
| SCS0029 | Login.razor | Posible XSS en visualización de error | [RIESGO]️ Verificar |
| SCS0032 | MqttConsumerService.cs | Deserialización insegura de JSON MQTT | [FALLA] Se detectará |
| CA2000 | MqttConsumerService.cs | Disposable no gestionado correctamente | [FALLA] Se detectará |
| CA5394 | CocheraDbContext.cs | Uso potencial de Guid.NewGuid() para seguridad | [RIESGO]️ Verificar |

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

**Paquetes a monitorear:**

| Paquete | Versión | Riesgo |
|---------|---------|--------|
| MQTTnet | 4.3.3 | Librería IoT de nicho — verificar CVEs |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.x | Driver BD — monitorear parches |
| Radzen.Blazor | 5.x | Componentes UI de terceros — superficie amplia |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.x | Verificar parches de seguridad |

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
# zap-config.yaml
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
        verification:
          method: "response"
          loggedInRegex: "Cochera\\.Auth"
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
  
  policy:
    defaultPolicy:
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

### 5.3.3 Checklist de Pruebas DAST

#### Autenticación y Autorización

| # | Prueba | Resultado Esperado | Estado |
|---|--------|-------------------|--------|
| D-01 | Acceso a `/admin/dashboard` sin cookie | Redirige a `/login` | [OK] Pass |
| D-02 | Acceso a `/usuario/estacionamiento` sin cookie | Redirige a `/login` | [OK] Pass |
| D-03 | Login con credenciales incorrectas | Redirige a `/login?error=1` | [OK] Pass |
| D-04 | Login con credenciales correctas | Set-Cookie: Cochera.Auth, redirect a `/` | [OK] Pass |
| D-05 | Acceso a `/admin/dashboard` con rol User | Redirige a `/access-denied` | [OK] Pass |
| D-06 | Logout invalida cookie del servidor | Cookie eliminada, redirige a `/login` | [OK] Pass |
| D-07 | Cookie expirada (>8h) | Redirige a login | [OK] Pass |
| D-08 | Fuerza bruta en `/auth/login` (100 intentos) | Sin rate limiting — vulnerable (V-012) | [FALLA] Falla |
| D-09 | CSRF en `/auth/login` (POST desde otro origin) | Sin AntiForgery — vulnerable (V-013) | [FALLA] Falla |

#### SignalR Hub

| # | Prueba | Resultado Esperado | Estado |
|---|--------|-------------------|--------|
| D-10 | Conexión WebSocket sin cookie | Se conecta — clase sin `[Authorize]` (V-009) | [FALLA] Falla |
| D-11 | `UnirseComoAdmin` sin rol Admin | HubException — protegido | [OK] Pass |
| D-12 | `UnirseComoUsuario(2)` siendo usuario_1 | HubException "Acceso denegado" — protegido | [OK] Pass |
| D-13 | `NuevoEvento` sin autenticación | Se ejecuta — método sin `[Authorize]` (V-009) | [FALLA] Falla |
| D-14 | `CambioEstado` sin autenticación | Se ejecuta — método sin `[Authorize]` (V-009) | [FALLA] Falla |

#### MQTT / IoT

| # | Prueba | Resultado Esperado | Estado |
|---|--------|-------------------|--------|
| D-15 | Captura Wireshark en puerto 1883 | Tráfico visible en texto plano (V-005) | [FALLA] Falla |
| D-16 | Inyectar mensaje JSON malformado vía MQTT | Se procesa sin validación (V-003) | [FALLA] Falla |
| D-17 | Conexión MQTT con credenciales del firmware | Se conecta — credenciales `esp32/123456` (V-001) | [FALLA] Falla |

#### Infraestructura

| # | Prueba | Resultado Esperado | Estado |
|---|--------|-------------------|--------|
| D-18 | Verificar headers de seguridad HTTP | Faltan CSP, X-Frame-Options, etc. (V-010) | [FALLA] Falla |
| D-19 | Acceso PostgreSQL con postgres/postgres | Acceso total — superusuario (V-002) | [FALLA] Falla |
| D-20 | Verificar HSTS header | Solo en producción (UseHsts condicional) | [RIESGO]️ Parcial |

---

## 5.4 Pruebas Manuales Específicas

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
// Desde navegador DevTools — sin cookie de autenticación
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/cocherahub")
    .build();

await connection.start();
console.log("Conectado:", connection.connectionId); // Se conecta (V-009)

// Método protegido — debe fallar
try {
    await connection.invoke("UnirseComoAdmin");
} catch (e) {
    console.log("Error esperado:", e); // HubException [OK]
}

// Método sin protección — se ejecuta
await connection.invoke("NuevoEvento", { /* dto falso */ }); // Se ejecuta (V-009) [FALLA]
```

### 5.4.3 Test: Validación de returnUrl

```bash
# Intentar open redirect
curl -X POST https://localhost:7XXX/auth/login \
  -d "username=admin&password=Admin12345&returnUrl=https://evil.com"
# Esperado: Redirige a "/" (validación rechaza URLs absolutas) [OK]

# returnUrl relativa sin /
curl -X POST https://localhost:7XXX/auth/login \
  -d "username=admin&password=Admin12345&returnUrl=evil"
# Esperado: Redirige a "/" (requiere que empiece con /) [OK]
```

### 5.4.4 Test: MQTT Inyección

```bash
# Conectar al broker con credenciales expuestas
mosquitto_pub -h 192.168.100.16 -p 1883 \
  -u esp32 -P 123456 \
  -t cola_sensores \
  -m '{"tipo":"entrada","distancia":-9999,"timestamp":"2099-01-01"}'
# Resultado esperado: El sistema procesa el mensaje sin validar (V-003) [FALLA]
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
      - run: dotnet build --warnaserror
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

## 5.6 Resumen de Resultados de Pruebas

| Categoría | Pass | Fail | Parcial | Total |
|-----------|------|------|---------|-------|
| Autenticación y Autorización | 7 | 2 | 0 | 9 |
| SignalR Hub | 2 | 3 | 0 | 5 |
| MQTT / IoT | 0 | 3 | 0 | 3 |
| Infraestructura | 0 | 2 | 1 | 3 |
| **Total** | **9** | **10** | **1** | **20** |

**Tasa de aprobación:** 9/20 = **45%**

Las 10 pruebas fallidas corresponden directamente a las vulnerabilidades V-001 a V-014 documentadas en este análisis.

---


