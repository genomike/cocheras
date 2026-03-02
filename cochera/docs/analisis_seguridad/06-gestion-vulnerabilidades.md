# 06 — Gestión de Vulnerabilidades y Parcheo

## 6.1 Ciclo de Vida de Vulnerabilidades

### Proceso Definido

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  NUEVA   │───►│ TRIAGED  │───►│ EN CURSO │───►│ VERIFICAR│───►│ CERRADA  │
│          │    │          │    │          │    │          │    │          │
│ Detectada│    │ CVSS,    │    │ Desarrollo│   │ Testing  │    │ Mitigada │
│ por scan │    │ prioridad│    │ de fix   │    │ SAST/DAST│    │ en prod  │
└──────────┘    └──────────┘    └──────────┘    └──────────┘    └──────────┘
```

### SLAs por Severidad

| Severidad | CVSS | SLA Respuesta | SLA Resolución |
|-----------|------|--------------|----------------|
| 🔴 Crítica | 9.0-10.0 | 4 horas | 48 horas |
| 🟠 Alta | 7.0-8.9 | 24 horas | 7 días |
| 🟡 Media | 4.0-6.9 | 72 horas | 30 días |
| 🟢 Baja | 0.1-3.9 | 1 semana | 90 días |

---

## 6.2 Registro de Vulnerabilidades (Actualizado)

### 6.2.1 Vulnerabilidades Cerradas ✅

| ID | Vulnerabilidad | CVSS | Fecha Detección | Fecha Cierre | Método de Cierre |
|----|---------------|------|----------------|-------------|-----------------|
| V-001 | Sistema sin autenticación | 9.8 | Ene 2025 | **Mar 2026** | ASP.NET Core Identity implementado |
| V-009 | Modelo sin contraseñas | 9.8 | Ene 2025 | **Mar 2026** | IdentityUser con PasswordHasher |
| V-014 | Sin gestión de sesiones | 7.4 | Ene 2025 | **Mar 2026** | Cookie auth con expiración 8h |
| V-018 | Excepciones silenciadas | 5.3 | Ene 2025 | **Mar 2026** | Catch vacíos eliminados de UsuarioActualService |
| V-003 | IDOR acceso sin ownership | 7.5 | Ene 2025 | **Mar 2026** | Parcial — Auth obliga login, pero IDOR persiste intra-rol |

**Tiempo promedio de resolución (vulnerabilidades críticas):** ~14 meses (análisis académico, no producción)

### 6.2.2 Vulnerabilidades Parcialmente Mitigadas ⚠️

| ID | Vulnerabilidad | CVSS Original | CVSS Actual | Notas |
|----|---------------|--------------|------------|-------|
| V-002 | SignalR Hub sin autorización | 9.1 | **5.5** | Métodos clave protegidos, clase sin `[Authorize]` |
| V-008 | Diseño inseguro general | 9.8 | **5.0** | Auth implementado, falta rate limiting, headers, CSRF |

### 6.2.3 Vulnerabilidades Abiertas ❌

| ID | Vulnerabilidad | CVSS | Severidad | Días Abierta | Dentro de SLA |
|----|---------------|------|-----------|-------------|---------------|
| V-004 | Credenciales hardcoded firmware | 9.1 | 🔴 Crítica | 420+ | ❌ No |
| V-007 | Inyección MQTT | 8.1 | 🟠 Alta | 420+ | ❌ No |
| V-011 | Superusuario PostgreSQL | 8.6 | 🟠 Alta | 420+ | ❌ No |
| V-015 | Sin integridad MQTT | 8.1 | 🟠 Alta | 420+ | ❌ No |
| V-005 | MQTT sin TLS | 7.4 | 🟠 Alta | 420+ | ❌ No |
| V-016 | Sin Secure Boot ESP32 | 6.5 | 🟡 Media | 420+ | ❌ No |
| V-012 | Sin headers seguridad | 5.3 | 🟡 Media | 420+ | ❌ No |
| V-010 | AllowedHosts = "*" | 5.3 | 🟡 Media | 420+ | ❌ No |
| V-006 | Sin cifrado en reposo | 5.3 | 🟡 Media | 420+ | ❌ No |
| V-013 | Sin análisis de CVEs | 5.0 | 🟡 Media | 420+ | ❌ No |
| V-017 | Sin logging de seguridad | 7.0 | 🟠 Alta | 420+ | ❌ No |

---

## 6.3 Métricas de Seguridad

### 6.3.1 Dashboard de Estado

```
══════════════════════════════════════════════════════════
      COCHERA INTELIGENTE — SECURITY DASHBOARD
══════════════════════════════════════════════════════════

  Vulnerabilidades Totales:     18
  ┌─────────┬─────────┬─────────┬─────────┐
  │ ✅ CERR  │ ⚠️ PARC  │ ❌ ABRT  │ CVSS AVG│
  │   5+2    │    2    │    9    │   6.9   │
  └─────────┴─────────┴─────────┴─────────┘

  Hallazgos de Código:          23
  ┌─────────┬─────────┐
  │ ✅ CORR  │ ❌ PEND  │
  │   10    │   13    │
  └─────────┴─────────┘

  Mejoras Propuestas:           18
  ┌─────────┬─────────┐
  │ ✅ IMPL  │ ❌ PEND  │
  │    4    │   14    │
  └─────────┴─────────┘

  OWASP Top 10 Cobertura:
  A01 ████████░░ 80%  A02 ░░░░░░░░░░  0%
  A03 ░░░░░░░░░░  0%  A04 ████████░░ 80%
  A05 ░░░░░░░░░░  0%  A06 ░░░░░░░░░░  0%
  A07 ██████████ 100%  A08 N/A
  A09 ████░░░░░░ 40%  A10 N/A
══════════════════════════════════════════════════════════
```

### 6.3.2 Tendencia de Riesgo

```
CVSS Promedio del Sistema:

10 ┤
 9 ┤ ████  (9.8 — Ene 2025: sin auth)
 8 ┤ ████
 7 ┤ ████
 6 ┤ ████  ────────────  ████ (6.9 — Mar 2026: con Identity)
 5 ┤ ████                ████
 4 ┤ ████                ████
 3 ┤ ████                ████
 2 ┤ ████                ████
 1 ┤ ████                ████
 0 ┼────────────────────────────
   Ene 2025           Mar 2026
```

---

## 6.4 Gestión de Dependencias

### 6.4.1 Paquetes NuGet Monitoreados

| Paquete | Versión Actual | Última Estable | CVEs Conocidos |
|---------|---------------|---------------|---------------|
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.x | Verificar | Ninguno conocido |
| MQTTnet | 4.3.3 | 4.3.7+ | Verificar |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.x | Verificar | Ninguno conocido |
| Radzen.Blazor | 5.x | Verificar | Ninguno conocido |
| Microsoft.EntityFrameworkCore | 8.0.x | Verificar | Ninguno conocido |

### 6.4.2 Configuración Dependabot Propuesta

```yaml
# .github/dependabot.yml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/cochera/src"
    schedule:
      interval: "weekly"
    reviewers:
      - "security-team"
    labels:
      - "dependencies"
      - "security"
    open-pull-requests-limit: 10
```

---

## 6.5 Plan de Respuesta a Incidentes

### 6.5.1 Clasificación de Incidentes

| Nivel | Descripción | Ejemplo | Acción |
|-------|------------|---------|--------|
| SEV-1 | Brecha activa | Acceso no autorizado a datos | Respuesta inmediata, desconectar servicio |
| SEV-2 | Vulnerabilidad explotable | Bypass de autenticación | Parche de emergencia <48h |
| SEV-3 | Exposición de datos | Credenciales en log | Rotación de credenciales |
| SEV-4 | Vulnerabilidad teórica | CVE en dependencia no explotable | Actualización planificada |

### 6.5.2 Contactos de Respuesta

| Rol | Responsabilidad |
|-----|----------------|
| Desarrollador principal | Triaje técnico, desarrollo de parche |
| Responsable de seguridad | Evaluación de impacto, comunicación |
| DBA | Respuesta a incidentes de base de datos |
| Administrador de red | Respuesta a incidentes de red/MQTT |

---

## 6.6 Checklist de Parcheo Post-Remediación

### Fase 2 — Próximos Parches Prioritarios

- [ ] **Agregar `[Authorize]` a nivel de clase en `CocheraHub`** (~30 min, V-002)
- [ ] **Cambiar `LockoutEnabled = true`** en usuarios seed (~15 min, V-008)
- [ ] **Agregar rate limiting** en `/auth/login` (~2h, V-008)
- [ ] **Mover connection string a User Secrets** (~1h, V-011)
- [ ] **Configurar AllowedHosts** (~15 min, V-010)
- [ ] **Agregar headers de seguridad** (~2h, V-012)
- [ ] **Eliminar credenciales de prueba** de Login.razor (~5 min, O-002)

---

*Anterior: [05 — Pruebas de Seguridad](05-pruebas-sast-dast.md)*
*Siguiente: [07 — Conclusiones](07-conclusiones.md)*
