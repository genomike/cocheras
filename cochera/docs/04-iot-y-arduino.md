# IoT y Firmware ESP32 - Sistema de Cochera Inteligente

## 1. Visión General del Hardware

El sistema IoT se basa en un microcontrolador **ESP32** que gestiona sensores ultrasónicos, LEDs indicadores y un buzzer para controlar el acceso y monitoreo de una cochera de 2 cajones.

> **Diagrama PlantUML:** Ver [diagramas/estados-esp32.puml](../diagramas/estados-esp32.puml) y [diagramas/secuencia-procesamiento-mqtt.puml](../diagramas/secuencia-procesamiento-mqtt.puml)

---

## 2. Componentes de Hardware

### 2.1 Lista de Componentes

| Componente | Cantidad | Descripción |
|-----------|----------|-------------|
| ESP32 DevKit V1 | 1 | Microcontrolador con WiFi integrado |
| HC-SR04 | 3 | Sensores ultrasónicos de distancia |
| LED Verde | 1 | Indicador de disponibilidad |
| LED Rojo | 1 | Indicador de cochera llena |
| Buzzer Activo | 1 | Señales sonoras |
| Resistencias 220Ω | 2 | Para LEDs |
| Protoboard | 1 | Para prototipado |
| Cables Dupont | ~20 | Conexiones |

### 2.2 Asignación de Pines GPIO

| Componente | Pin TRIG | Pin ECHO | GPIO |
|-----------|----------|----------|------|
| Sensor Entrada | GPIO 13 | GPIO 12 | - |
| Sensor Cajón 1 | GPIO 14 | GPIO 27 | - |
| Sensor Cajón 2 | GPIO 26 | GPIO 25 | - |
| LED Verde | - | - | GPIO 32 |
| LED Rojo | - | - | GPIO 33 |
| Buzzer | - | - | GPIO 15 |

### 2.3 Diagrama de Conexiones

```
ESP32 DevKit V1
┌──────────────────────┐
│                      │
│  GPIO 13 ──────────── TRIG (Sensor Entrada)
│  GPIO 12 ──────────── ECHO (Sensor Entrada)
│                      │
│  GPIO 14 ──────────── TRIG (Sensor Cajón 1)
│  GPIO 27 ──────────── ECHO (Sensor Cajón 1)
│                      │
│  GPIO 26 ──────────── TRIG (Sensor Cajón 2)
│  GPIO 25 ──────────── ECHO (Sensor Cajón 2)
│                      │
│  GPIO 32 ──── 220Ω ── LED Verde (ánodo)
│  GPIO 33 ──── 220Ω ── LED Rojo (ánodo)
│  GPIO 15 ──────────── Buzzer (+)
│                      │
│  GND ────────────────── GND común
│  3.3V ───────────────── VCC sensores
│  5V ─────────────────── VCC HC-SR04 (si necesario)
│                      │
└──────────────────────┘
```

---

## 3. Configuración de Red

### 3.1 WiFi

```cpp
const char* ssid = "AVRIL@2014";
const char* password = "Abr11@2014";
```

### 3.2 MQTT (Broker RabbitMQ)

```cpp
const char* mqtt_server = "192.168.100.16";
const int mqtt_port = 1883;
const char* mqtt_user = "esp32";
const char* mqtt_password = "123456";
const char* mqtt_topic = "cola_sensores";
```

### 3.3 NTP (Sincronización de Tiempo)

```cpp
const char* ntpServer = "pool.ntp.org";
const long gmtOffset_sec = -21600;  // UTC-6 (Centro de México)
const int daylightOffset_sec = 0;
```

---

## 4. Constantes del Sistema

| Constante | Valor | Descripción |
|-----------|-------|-------------|
| `UMBRAL_OCUPADO` | 16 cm | Distancia para considerar cajón ocupado |
| `DISTANCIA_MIN` | 3 cm | Distancia mínima válida (evita falsos positivos) |
| `DEBOUNCE_MS` | 800 ms | Debounce para sensores de cajón |
| `DEBOUNCE_ENTRADA_MS` | 200 ms | Debounce para sensor de entrada |
| `PARPADEO_TIMEOUT` | 60000 ms | Timeout de secuencia de salida (60 segundos) |
| `INTERVALO_BLINK` | 300 ms | Intervalo de parpadeo de LEDs |

---

## 5. Algoritmo de Medición de Distancia

### 5.1 Función `medirDistancia()`

```cpp
float medirDistancia(int trigPin, int echoPin) {
    // 1. Enviar pulso ultrasónico
    digitalWrite(trigPin, LOW);
    delayMicroseconds(2);
    digitalWrite(trigPin, HIGH);
    delayMicroseconds(10);
    digitalWrite(trigPin, LOW);
    
    // 2. Medir tiempo de eco (timeout: 30000 µs ≈ 5.1m)
    long duracion = pulseIn(echoPin, HIGH, 30000);
    
    // 3. Calcular distancia: velocidad del sonido / 2
    float distancia = duracion * 0.034 / 2.0;
    
    // 4. Si timeout (duración=0), retornar 999 cm
    return (duracion == 0) ? 999.0 : distancia;
}
```

### 5.2 Lógica de Detección

```
SI distancia >= DISTANCIA_MIN (3cm) Y distancia <= UMBRAL_OCUPADO (16cm):
    → Objeto detectado (cajón ocupado / vehículo en entrada)
SINO:
    → Sin objeto (cajón libre / entrada despejada)
```

---

## 6. Lógica de Sensores de Cajón

### 6.1 Máquina de Estados por Cajón

Cada cajón tiene un estado independiente con debounce:

```
Variables por cajón:
- estadoActual: bool (true = ocupado)
- ultimoCambio: unsigned long (timestamp ms)

Cada iteración del loop():
1. Medir distancia del sensor
2. Determinar estado detectado (ocupado/libre)
3. Si estado detectado ≠ estadoActual:
   a. Verificar debounce (millis() - ultimoCambio > DEBOUNCE_MS)
   b. Si pasó debounce:
      - Actualizar estadoActual
      - Registrar ultimoCambio
      - Si ahora OCUPADO: enviar evento "CAJON_OCUPADO"
      - Si ahora LIBRE: enviar evento "CAJON_LIBERADO"
```

### 6.2 Contadores Globales

```cpp
int cajonesOcupados = (estadoCajon1 ? 1 : 0) + (estadoCajon2 ? 1 : 0);
int cajonesLibres = 2 - cajonesOcupados;
bool cocheraLlena = (cajonesOcupados >= 2);
```

---

## 7. Lógica del Sensor de Entrada

### 7.1 Detección de Vehículo en Entrada

```
Cada iteración:
1. Medir distancia del sensor de entrada
2. Detectar si hay vehículo (distancia en rango válido)
3. Si vehículo detectado Y no había antes (flanco de subida):
   a. Verificar debounce de entrada (200ms)
   b. Si cochera NO llena:
      - Enviar "MOVIMIENTO_ENTRADA"
      - Tono de bienvenida (buzzer)
   c. Si cochera LLENA:
      - Enviar "MOVIMIENTO_ENTRADA_BLOQUEADO"
      - Tono de cochera llena (buzzer)
4. Si vehículo detectado Y el estado es "parpadeo" (saliendo):
   - Enviar "VEHICULO_SALIO"
   - Tono de salida (buzzer)
   - Detener parpadeo
```

---

## 8. Sistema de LEDs

### 8.1 Estados de LED

| Estado Cochera | LED Verde | LED Rojo | Descripción |
|---------------|-----------|----------|-------------|
| Cajones disponibles | ON | OFF | Cochera con espacio |
| Cochera llena | OFF | ON | Sin cajones disponibles |
| Vehículo saliendo | BLINK | BLINK | Parpadeo alternado (300ms) |

### 8.2 Parpadeo (Secuencia de Salida)

Cuando se libera un cajón estando la cochera llena:

```
1. Se activa el modo "parpadeo"
2. LEDs verde y rojo alternan cada 300ms
3. El parpadeo continúa hasta:
   a. Se detecta vehículo en sensor de entrada (salida confirmada)
      → Evento "VEHICULO_SALIO"
   b. Se cumple el timeout de 60 segundos
      → Evento "PARPADEO_TIMEOUT"
4. Al terminar, se restaura el estado normal de LEDs
```

---

## 9. Sistema de Buzzer

### 9.1 Tonos Definidos

| Tono | Frecuencia | Duración | Trigger |
|------|-----------|----------|---------|
| Bienvenida | 1000 Hz → 1500 Hz | 100ms + 100ms + 200ms | Vehículo entra (hay espacio) |
| Bloqueado | 200 Hz × 3 | 300ms × 3 | Vehículo entra (cochera llena) |
| Cochera Llena | 500 Hz × 2 | 150ms × 2 | Se llena el último cajón |
| Liberado | 1500 Hz → 1000 Hz | 100ms + 100ms + 200ms | Se libera un cajón |
| Salida | 800 Hz → 1200 Hz → 1500 Hz | 100ms + 100ms + 100ms + 300ms | Vehículo sale confirmado |

---

## 10. Comunicación MQTT

### 10.1 Formato del Mensaje JSON

```json
{
  "evento": "CAJON_OCUPADO",
  "detalle": "Vehiculo detectado en cajon 1",
  "timestamp": "2025-01-16 14:30:00",
  "estado": {
    "cajon1": "OCUPADO",
    "cajon2": "LIBRE",
    "cajones_libres": 1,
    "cajones_ocupados": 1,
    "cochera_llena": false
  }
}
```

### 10.2 Implementación de Publicación

```cpp
void publicarEvento(const char* evento, const char* detalle) {
    StaticJsonDocument<512> doc;
    doc["evento"] = evento;
    doc["detalle"] = detalle;
    doc["timestamp"] = obtenerTimestamp();  // NTP sincronizado
    
    JsonObject estado = doc.createNestedObject("estado");
    estado["cajon1"] = estadoCajon1 ? "OCUPADO" : "LIBRE";
    estado["cajon2"] = estadoCajon2 ? "OCUPADO" : "LIBRE";
    estado["cajones_libres"] = cajonesLibres;
    estado["cajones_ocupados"] = cajonesOcupados;
    estado["cochera_llena"] = cocheraLlena;
    
    char buffer[512];
    serializeJson(doc, buffer);
    client.publish(mqtt_topic, buffer);
}
```

### 10.3 Reconexión MQTT

```
En cada iteración del loop():
1. Verificar conexión WiFi
2. Si desconectado de MQTT:
   a. Intentar reconectar con credenciales
   b. Si falla, esperar 5 segundos
   c. Reintentar en la siguiente iteración
```

### 10.4 Tipos de Eventos Publicados

| Evento | Cuándo se Publica |
|--------|-------------------|
| `SISTEMA_INICIADO` | Al arrancar el ESP32 (una vez) |
| `CAJON_OCUPADO` | Sensor de cajón detecta vehículo |
| `CAJON_LIBERADO` | Sensor de cajón deja de detectar |
| `COCHERA_LLENA` | Segundo cajón se ocupa |
| `MOVIMIENTO_ENTRADA` | Vehículo en entrada, hay espacio |
| `MOVIMIENTO_ENTRADA_BLOQUEADO` | Vehículo en entrada, cochera llena |
| `PARPADEO_INICIADO` | Cajón liberado estando llena |
| `PARPADEO_TIMEOUT` | 60s sin salida confirmada |
| `VEHICULO_SALIO` | Vehículo detectado saliendo |

---

## 11. Librerías Utilizadas

| Librería | Versión | Uso |
|----------|---------|-----|
| WiFi.h | Incluida ESP32 | Conexión WiFi |
| PubSubClient.h | 2.8+ | Cliente MQTT |
| ArduinoJson.h | 6.x | Serialización JSON |
| time.h | Estándar C | Sincronización NTP |

### 11.1 Instalación de Librerías

En el **Arduino IDE** o **PlatformIO**, instalar:
```
PubSubClient by Nick O'Leary
ArduinoJson by Benoit Blanchon
```

Para soporte ESP32:
1. **Arduino IDE** → Preferencias → URLs adicionales:
   ```
   https://dl.espressif.com/dl/package_esp32_index.json
   ```
2. Gestor de tarjetas → Buscar "ESP32" → Instalar "esp32 by Espressif Systems"

---

## 12. Flujo del `loop()` Principal

```
loop() {
    1. Verificar conexión MQTT (reconectar si necesario)
    2. client.loop()  // Procesar mensajes MQTT pendientes
    
    3. Leer sensor Cajón 1:
       - Medir distancia
       - Aplicar debounce
       - Si cambió: publicar evento + actualizar contadores
    
    4. Leer sensor Cajón 2:
       - Medir distancia
       - Aplicar debounce
       - Si cambió: publicar evento + actualizar contadores
    
    5. Verificar si cochera se llenó:
       - Si se llenó ahora: publicar "COCHERA_LLENA"
    
    6. Verificar si se liberó cajón estando llena:
       - Si se liberó: iniciar parpadeo
    
    7. Si en modo parpadeo:
       - Alternar LEDs cada 300ms
       - Verificar timeout (60s)
    
    8. Leer sensor Entrada:
       - Medir distancia
       - Si vehículo detectado (flanco):
         * Si parpadeo activo: publicar "VEHICULO_SALIO"
         * Si cochera con espacio: publicar "MOVIMIENTO_ENTRADA"
         * Si cochera llena: publicar "MOVIMIENTO_ENTRADA_BLOQUEADO"
    
    9. Actualizar LEDs según estado
}
```

---

## 13. Consideraciones de Diseño

### 13.1 Debounce Diferenciado

- **Cajones (800ms):** Mayor debounce para evitar fluctuaciones cuando un vehículo se estaciona/sale lentamente
- **Entrada (200ms):** Menor debounce para capturar vehículos que pasan rápidamente

### 13.2 Rango de Detección

- **Mínimo (3cm):** Evita lecturas erróneas cuando un objeto está demasiado cerca del sensor
- **Máximo (16cm):** Calibrado para la distancia típica entre el sensor y el techo de un vehículo en un cajón

### 13.3 Secuencia de Salida (Parpadeo)

El sistema no tiene barrera física, por lo que usa una heurística:
1. Cuando se libera un cajón estando llena → se espera que un vehículo salga
2. Se activa el parpadeo como señal visual
3. Si el sensor de entrada detecta movimiento → se confirma la salida
4. Si pasan 60 segundos → se asume que el vehículo no salió (timeout)

### 13.4 Tolerancia a Fallos

- **WiFi desconectado:** El ESP32 sigue operando localmente (LEDs y buzzer funcionan)
- **MQTT desconectado:** Reintentos automáticos cada 5 segundos
- **Sensor sin eco (timeout):** Retorna 999cm, se trata como "sin objeto"
- **NTP no disponible:** Usa epoch 0, no afecta funcionalidad crítica
