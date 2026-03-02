# 05 - Sugerencia de Pruebas de Seguridad (SAST y DAST)

## 5.1 Introducción a las Pruebas de Seguridad

Las pruebas de seguridad se dividen en dos categorías principales:

| Tipo | Descripción | Cuándo |
|------|-------------|--------|
| **SAST** (Static Application Security Testing) | Análisis del código fuente sin ejecutar la aplicación | Durante desarrollo y CI/CD |
| **DAST** (Dynamic Application Security Testing) | Análisis de la aplicación en ejecución | En entornos de staging/QA |

Adicionalmente, se incluyen:
- **SCA** (Software Composition Analysis) – Análisis de dependencias de terceros
- **IAST** (Interactive Application Security Testing) – Combinación de SAST y DAST

---

## 5.2 SAST – Análisis Estático de Código

### 5.2.1 Herramienta: dotnet-security-guard (Roslyn Analyzer)

**Descripción:** Analizador de seguridad que se integra directamente en el compilador de .NET via Roslyn. Detecta patrones de código inseguro durante la compilación.

**Instalación:**
```bash
# Agregar a cada proyecto .csproj
dotnet add src/Cochera.Web/Cochera.Web.csproj package SecurityCodeScan.VS2019 --version 5.6.7
dotnet add src/Cochera.Application/Cochera.Application.csproj package SecurityCodeScan.VS2019 --version 5.6.7
dotnet add src/Cochera.Infrastructure/Cochera.Infrastructure.csproj package SecurityCodeScan.VS2019 --version 5.6.7
dotnet add src/Cochera.Worker/Cochera.Worker.csproj package SecurityCodeScan.VS2019 --version 5.6.7
```

**Reglas relevantes para este proyecto:**

| ID | Nombre | Aplica a |
|----|--------|----------|
| SCS0001 | Command injection | Ejecución de comandos del SO |
| SCS0002 | SQL Injection | Consultas SQL raw |
| SCS0005 | Weak random | Generación de tokens |
| SCS0016 | CSRF | Formularios sin antiforgery token |
| SCS0018 | Path traversal | Acceso a archivos |
| SCS0028 | Deserialización insegura | JsonSerializer sin restricciones |
| SCS0029 | XSS | Renderización de HTML sin encoding |
| SCS0034 | Password en texto plano | Hardcoded passwords |

**Hallazgos esperados en Cochera Inteligente:**
- `SCS0034` en `MqttSettings.cs` (valores default de contraseña)
- `SCS0028` en `MqttConsumerService.cs` (deserialización de payload MQTT)
- Advertencias sobre `catch` vacíos en `UsuarioActualService.cs`

**Ejecución:**
```bash
cd src
dotnet build Cochera.sln /p:TreatWarningsAsErrors=false 2>&1 | findstr "SCS"
```

---

### 5.2.2 Herramienta: SonarQube / SonarCloud

**Descripción:** Plataforma de análisis de calidad y seguridad de código. Detecta vulnerabilidades, code smells, bugs y deuda técnica.

**Configuración con Docker:**
```bash
# Levantar SonarQube
docker run -d --name sonarqube -p 9000:9000 sonarqube:community

# Instalar scanner global
dotnet tool install --global dotnet-sonarscanner
```

**Ejecución del análisis:**
```bash
cd cochera

# Iniciar análisis
dotnet sonarscanner begin \
  /k:"cochera-inteligente" \
  /d:sonar.host.url="http://localhost:9000" \
  /d:sonar.token="<TOKEN>" \
  /d:sonar.cs.analyzer.projectOutPaths="**/bin/**" \
  /d:sonar.exclusions="**/Migrations/**,**/wwwroot/**,**/obj/**"

# Compilar
dotnet build src/Cochera.sln

# Finalizar y enviar
dotnet sonarscanner end /d:sonar.token="<TOKEN>"
```

**Reglas de seguridad a habilitar (Security Hotspots):**

| Regla | Descripción | Archivo esperado |
|-------|-------------|-----------------|
| S2068 | Credentials should not be hard-coded | appsettings.json, MqttSettings.cs |
| S4790 | Make sure using a hash without salt is safe | N/A (no hay hashing) |
| S5332 | Using clear-text protocols is security-sensitive | MqttConsumerService.cs |
| S5344 | Passwords should not be stored in plain-text | appsettings.json |
| S5542 | Encryption algorithms should be used with secure mode | N/A |
| S2077 | SQL queries should not be vulnerable to injection | Repositories |
| S3330 | Cookies should be HttpOnly | Program.cs |
| S5122 | Setting loose CORS policy is security-sensitive | Program.cs |
| S2092 | Cookies should be secure | Program.cs |
| S5131 | XSS: Methods should sanitize data | EventoSensorService.cs |

**Archivo de configuración recomendado (sonar-project.properties):**
```properties
sonar.projectKey=cochera-inteligente
sonar.projectName=Cochera Inteligente
sonar.sources=src/
sonar.exclusions=**/Migrations/**,**/obj/**,**/bin/**,**/wwwroot/**
sonar.cs.opencover.reportsPaths=**/coverage.opencover.xml
sonar.qualitygate.wait=true
sonar.issue.ignore.multicriteria=e1
sonar.issue.ignore.multicriteria.e1.ruleKey=csharpsquid:S1135
sonar.issue.ignore.multicriteria.e1.resourceKey=**/*
```

---

### 5.2.3 Herramienta: Semgrep

**Descripción:** Herramienta de SAST ligera y rápida que soporta reglas personalizadas. Ideal para CI/CD.

**Instalación:**
```bash
pip install semgrep
# o
brew install semgrep
```

**Ejecución con reglas de seguridad para .NET:**
```bash
cd cochera

# Usar reglas de seguridad de la comunidad
semgrep --config "p/csharp" --config "p/security-audit" --config "p/owasp-top-ten" src/

# Regla personalizada para detectar catch vacíos
semgrep --config custom-rules/ src/
```

**Regla personalizada para Cochera (YAML):**
```yaml
# custom-rules/empty-catch.yml
rules:
  - id: empty-catch-block
    patterns:
      - pattern: |
          try { ... }
          catch { }
    message: "Bloque catch vacío detectado. Los errores deben registrarse."
    languages: [csharp]
    severity: WARNING
    metadata:
      cwe: ["CWE-390"]
      owasp: ["A09:2021"]

  - id: hardcoded-password-default
    patterns:
      - pattern: |
          public string Password { get; set; } = "...";
    message: "Contraseña hardcoded como valor por defecto en propiedad"
    languages: [csharp]
    severity: ERROR
    metadata:
      cwe: ["CWE-798"]
      owasp: ["A02:2021"]

  - id: signalr-hub-without-authorize
    patterns:
      - pattern: |
          public class $HUB : Hub { ... }
      - pattern-not: |
          [Authorize]
          public class $HUB : Hub { ... }
    message: "Hub de SignalR sin atributo [Authorize]"
    languages: [csharp]
    severity: ERROR
    metadata:
      cwe: ["CWE-862"]
      owasp: ["A01:2021"]
```

---

### 5.2.4 Herramienta: .NET CLI Security Audit

**Descripción:** Comando integrado en el CLI de .NET para verificar paquetes con vulnerabilidades conocidas (CVEs).

**Ejecución:**
```bash
cd cochera

# Verificar vulnerabilidades en dependencias
dotnet list src/Cochera.Web/Cochera.Web.csproj package --vulnerable --include-transitive
dotnet list src/Cochera.Worker/Cochera.Worker.csproj package --vulnerable --include-transitive
dotnet list src/Cochera.Infrastructure/Cochera.Infrastructure.csproj package --vulnerable --include-transitive

# Verificar paquetes desactualizados
dotnet list src/Cochera.sln package --outdated
```

**Resultados esperados (ejemplo):**

```
Project 'Cochera.Web' has the following vulnerable packages:
   [net8.0]:
   Top-level Package                          Requested   Resolved   Severity   Advisory URL
   > MQTTnet                                  4.3.3.952   4.3.3.952  Moderate   https://github.com/advisories/...
```

---

## 5.3 SCA – Análisis de Composición de Software

### 5.3.1 Herramienta: OWASP Dependency-Check

**Descripción:** Detecta dependencias con vulnerabilidades conocidas usando la base de datos NVD (National Vulnerability Database).

**Instalación y ejecución:**
```bash
# Descargar
# https://owasp.org/www-project-dependency-check/

# Ejecutar contra los proyectos .NET
dependency-check --project "Cochera" --scan src/ --format HTML --out reports/

# O con Docker
docker run --rm -v "$(pwd)/src:/src" -v "$(pwd)/reports:/reports" \
  owasp/dependency-check:latest \
  --project "Cochera" --scan /src --format HTML --out /reports
```

**Configuración para CI/CD (GitHub Actions):**
```yaml
# .github/workflows/security.yml
name: Security Scan
on: [push, pull_request]

jobs:
  dependency-check:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: OWASP Dependency Check
        uses: dependency-check/Dependency-Check_Action@main
        with:
          project: 'Cochera Inteligente'
          path: 'cochera/src'
          format: 'HTML'
      - name: Upload report
        uses: actions/upload-artifact@v4
        with:
          name: dependency-check-report
          path: reports/
```

### 5.3.2 Herramienta: Snyk

**Descripción:** Plataforma SaaS para análisis de vulnerabilidades en dependencias, con base de datos propia más completa que NVD.

```bash
# Instalar
npm install -g snyk

# Autenticar
snyk auth

# Escanear proyecto .NET
snyk test --file=src/Cochera.Web/Cochera.Web.csproj --all-projects
snyk test --file=src/Cochera.Worker/Cochera.Worker.csproj

# Monitorear continuamente
snyk monitor --file=src/Cochera.Web/Cochera.Web.csproj
```

---

## 5.4 DAST – Análisis Dinámico de Seguridad

### 5.4.1 Herramienta: OWASP ZAP (Zed Attack Proxy)

**Descripción:** Proxy de seguridad para pruebas de penetración automatizadas. Identifica vulnerabilidades en aplicaciones web en ejecución.

**Prerequisitos:**
1. La aplicación Cochera.Web debe estar ejecutándose (`dotnet run`)
2. OWASP ZAP debe estar instalado (descarga: https://www.zaproxy.org/)

**Configuración del escaneo:**

```bash
# 1. Levantar la aplicación
cd src/Cochera.Web
dotnet run --urls="http://localhost:5000"

# 2. Ejecutar ZAP en modo headless (Docker)
docker run -t -v "$(pwd)/reports:/zap/wrk" zaproxy/zap-stable \
  zap-full-scan.py \
  -t http://host.docker.internal:5000 \
  -r zap-report.html \
  -J zap-report.json \
  -c zap-config.conf \
  --hook=/zap/auth_hook.py
```

**Configuración de ZAP para Blazor Server:**

Blazor Server usa WebSockets y SignalR, lo que requiere configuración especial:

```
# zap-config.conf
10015	IGNORE	(Incomplete or No Cache-control)
10037	IGNORE	(Server Leaks Information via "X-Powered-By")
10096	IGNORE	(Timestamp Disclosure)
```

**Pruebas manuales con ZAP (lista de verificación):**

| # | Prueba | Método | URL/Endpoint | Resultado esperado |
|---|--------|--------|-------------|-------------------|
| 1 | Acceso sin autenticación | GET | `/admin/dashboard` | 🔴 Acceso concedido (vulnerabilidad) |
| 2 | SignalR sin auth | WebSocket | `/cocherahub` | 🔴 Conexión aceptada (vulnerabilidad) |
| 3 | Forced browsing | GET | `/admin/tarifas` | 🔴 Acceso concedido |
| 4 | Headers de seguridad | GET | `/` | 🔴 Faltan CSP, X-Frame-Options |
| 5 | HTTPS enforcement | HTTP | `http://localhost:5000` | 🟡 Redirección a HTTPS |
| 6 | Cookie flags | - | Inspeccionar cookies | 🔴 Posibles flags faltantes |
| 7 | Error handling | GET | `/ruta-inexistente` | 🟡 Verificar que no muestre stack trace |
| 8 | Enumeración de usuarios | - | Observar UI | 🔴 Lista completa visible |

### 5.4.2 Herramienta: Burp Suite Community

**Descripción:** Suite de pruebas de seguridad web con proxy interceptor. La edición Community es gratuita.

**Configuración para Cochera:**

1. **Configurar proxy del navegador** en `127.0.0.1:8080` (puerto de Burp)
2. **Navegar la aplicación** para que Burp capture el sitemap
3. **Análisis pasivo** automático de headers y respuestas
4. **Intruder** para pruebas de fuerza bruta (si se implementa login)

**Pruebas específicas con Burp:**

**a) Prueba de IDOR en sesiones:**
```
# Request interceptado:
GET /api/sesion/1 HTTP/1.1
Host: localhost:5000

# Modificar ID secuencialmente:
GET /api/sesion/2 HTTP/1.1
GET /api/sesion/3 HTTP/1.1
# Si retorna datos de otros usuarios → IDOR confirmado
```

**b) Prueba de SignalR WebSocket hijacking:**
```
# Capturar la conexión WebSocket a /cocherahub
# Verificar si se puede enviar invoke("UnirseComoAdmin") sin token
```

---

### 5.4.3 Pruebas de Seguridad para MQTT

**Herramienta:** mosquitto-clients + scripts personalizados

```bash
# Prueba 1: Conectar al broker con credenciales conocidas
mosquitto_sub -h 192.168.100.16 -p 1883 -u esp32 -P 123456 -t "#" -v
# Si funciona → credenciales por defecto confirmadas

# Prueba 2: Verificar credenciales por defecto de RabbitMQ
mosquitto_sub -h 192.168.100.16 -p 1883 -u guest -P guest -t "#" -v
# Si funciona → credenciales guest activas

# Prueba 3: Inyección de mensajes MQTT
mosquitto_pub -h 192.168.100.16 -p 1883 -u esp32 -P 123456 \
  -t cola_sensores \
  -m '{"evento":"CAJON_OCUPADO","detalle":"INYECTADO","timestamp":"2026-01-01","cajon1":"OCUPADO","cajon2":"OCUPADO","libres":0,"ocupados":2,"lleno":true}'
# Si el dashboard refleja el cambio → inyección exitosa

# Prueba 4: Intentar suscribirse a todos los topics
mosquitto_sub -h 192.168.100.16 -p 1883 -u esp32 -P 123456 -t "#" -v
# Si lista topics → control de acceso insuficiente

# Prueba 5: Fuerza bruta de credenciales
# Usar diccionario contra el broker
ncrack mqtt://192.168.100.16:1883 --user esp32 -P /usr/share/wordlists/common-passwords.txt
```

---

### 5.4.4 Pruebas de Red (Network Security)

**Herramienta:** nmap + wireshark

```bash
# Escaneo de puertos del servidor
nmap -sV -sC -p- 192.168.100.16

# Puertos esperados abiertos:
# 5000/tcp  - Cochera.Web (HTTP)
# 5432/tcp  - PostgreSQL
# 1883/tcp  - MQTT (sin TLS)
# 5672/tcp  - AMQP (RabbitMQ)
# 15672/tcp - RabbitMQ Management

# Verificar si se puede acceder a RabbitMQ Management
curl -u guest:guest http://192.168.100.16:15672/api/overview

# Captura de tráfico MQTT (con Wireshark)
# Filtro: mqtt
# Verificar que credenciales viajan en texto plano en el paquete CONNECT
```

---

## 5.5 Plan de Pruebas de Seguridad Integrado

### 5.5.1 Pipeline CI/CD con Pruebas de Seguridad

```yaml
# .github/workflows/security-pipeline.yml
name: Security Pipeline

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]
  schedule:
    - cron: '0 6 * * 1'  # Cada lunes a las 6:00 AM

jobs:
  # FASE 1: SAST
  sast:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore
        run: dotnet restore cochera/src/Cochera.sln
      
      - name: Build with Security Analyzers
        run: dotnet build cochera/src/Cochera.sln --no-restore /p:TreatWarningsAsErrors=false
      
      - name: Semgrep SAST
        uses: returntocorp/semgrep-action@v1
        with:
          config: >-
            p/csharp
            p/security-audit
            p/owasp-top-ten

  # FASE 2: SCA
  sca:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Check vulnerable packages
        run: |
          dotnet list cochera/src/Cochera.Web/Cochera.Web.csproj package --vulnerable --include-transitive
          dotnet list cochera/src/Cochera.Worker/Cochera.Worker.csproj package --vulnerable --include-transitive
      
      - name: OWASP Dependency Check
        uses: dependency-check/Dependency-Check_Action@main
        with:
          project: 'Cochera'
          path: 'cochera/src'
          format: 'HTML'
          args: '--failOnCVSS 7'

  # FASE 3: DAST (solo en schedule o manual)
  dast:
    if: github.event_name == 'schedule' || github.event_name == 'workflow_dispatch'
    needs: [sast, sca]
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_PASSWORD: test_password
          POSTGRES_DB: Cochera
        ports:
          - 5432:5432
    steps:
      - uses: actions/checkout@v4
      
      - name: Start application
        run: |
          cd cochera/src/Cochera.Web
          dotnet run --urls="http://0.0.0.0:5000" &
          sleep 15
      
      - name: OWASP ZAP Scan
        uses: zaproxy/action-full-scan@v0.10.0
        with:
          target: 'http://localhost:5000'
          rules_file_name: '.zap/rules.tsv'
          artifact_name: zap-report
```

### 5.5.2 Frecuencia Recomendada de Pruebas

| Tipo | Herramienta | Frecuencia | Trigger |
|------|-------------|------------|---------|
| SAST | SecurityCodeScan | Cada build | Push a cualquier rama |
| SAST | Semgrep | Cada PR | Pull Request |
| SAST | SonarQube | Diario | Nightly build |
| SCA | dotnet list --vulnerable | Cada build | Push a main |
| SCA | OWASP Dependency-Check | Semanal | Programado |
| SCA | Snyk | Continuo | Monitoreo automático |
| DAST | OWASP ZAP | Semanal | Programado |
| DAST | Burp Suite | Mensual | Manual (pentesting) |
| Network | nmap + wireshark | Trimestral | Manual |
| MQTT | mosquitto-clients | Después de cambios en IoT | Manual |

---

## 5.6 Métricas de Seguridad a Monitorear

| Métrica | Objetivo | Herramienta |
|---------|----------|-------------|
| Vulnerabilidades SAST críticas | 0 | SonarQube |
| Vulnerabilidades SAST altas | < 5 | SonarQube |
| Dependencias vulnerables (CVSS > 7) | 0 | Dependency-Check |
| Coverage de código en tests de seguridad | > 70% | dotnet test + coverlet |
| Tiempo medio de remediación (críticas) | < 7 días | Jira/Azure DevOps |
| Tiempo medio de remediación (altas) | < 30 días | Jira/Azure DevOps |
| Security Hotspots revisados | 100% | SonarQube |
| False positive rate | < 20% | Manual tracking |
