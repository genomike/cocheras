#include <WiFi.h>
#include <PubSubClient.h>
#include <time.h>

// --- CONFIGURACIÓN DE RED ---
const char* ssid = "AVRIL@2014";       
const char* password = "AVRIL@2014";   
const char* mqtt_server = "192.168.100.16"; 
const int mqtt_port = 1883;
const char* mqtt_user = "esp32";
const char* mqtt_pass = "123456";
const char* topic_sensores = "cola_sensores";

// --- CONFIGURACIÓN NTP ---
const char* ntpServer = "pool.ntp.org";
const long gmtOffset_sec = -21600;
const int daylightOffset_sec = 0;

// --- PINES ---
const int trigEntrada = 13; const int echoEntrada = 12;
const int trigCajon1 = 14;  const int echoCajon1 = 27;
const int trigCajon2 = 26;  const int echoCajon2 = 25;
const int ledVerde = 32;    const int ledRojo = 33;    
const int buzzerPin = 15;  

// --- CONSTANTES ---
// Techo: 20cm, Vehículos: 7.5-8cm -> Distancia con carro: ~12cm
// Umbral ESTRICTO: 13cm (solo detecta autos realmente estacionados)
const int UMBRAL_OCUPADO = 13;
const int DISTANCIA_MIN = 3;    // Ignorar lecturas menores a 3cm (ruido)
const unsigned long DEBOUNCE_MS = 800;  // 800ms para mayor estabilidad
const unsigned long DEBOUNCE_ENTRADA = 200;  // Entrada más sensible (objeto en movimiento)
const unsigned long PARPADEO_INTERVALO = 100;  // Parpadeo rápido
const unsigned long TIMEOUT_PARPADEO = 15000;
const int LECTURAS_CONFIRMAR = 3;  // Número de lecturas consecutivas para confirmar

// --- VARIABLES GLOBALES ---
WiFiClient espClient;
PubSubClient client(espClient);
char jsonBuffer[512];

// Estados de cajones (estables después de debounce)
bool cajon1_ocupado = false;
bool cajon2_ocupado = false;
bool cajon1_ocupado_prev = false;
bool cajon2_ocupado_prev = false;

// Debounce cajones
bool cajon1_lectura = false;
bool cajon2_lectura = false;
unsigned long cajon1_tiempo_cambio = 0;
unsigned long cajon2_tiempo_cambio = 0;

// Estado entrada
bool entrada_detecta = false;
bool entrada_detecta_prev = false;
bool evento_entrada_enviado = false;
bool vehiculo_presente_entrada = false;  // True mientras hay vehículo en entrada
unsigned long entrada_tiempo_inicio = 0;
unsigned long entrada_tiempo_salida = 0;  // Cuando dejó de detectar
const unsigned long TIEMPO_SALIDA_ENTRADA = 2000;  // 2 seg sin detectar = vehículo se fue

// Parpadeo (solo cuando cochera LLENA se libera un cajón)
bool parpadeo_activo = false;
unsigned long parpadeo_inicio = 0;
unsigned long parpadeo_ultimo = 0;
bool parpadeo_estado = false;
bool cochera_estuvo_llena = false;  // Flag para saber si estuvo llena antes de liberar

// --- PROTOTIPOS ---
void setup_wifi();
void configurarNTP();
String obtenerTimestamp();
int medirDistancia(int trig, int echo);
void publicarMensaje(const char* evento, const char* detalle);
void tonoEntrada();
void tonoBienvenida();
void tonoBloqueo();
void tonoLleno();
void tonoLiberado();
void tonoSalidaParpadeo();
int medirDistanciaRapida(int trig, int echo);

void setup() {
  Serial.begin(115200);
  
  pinMode(trigEntrada, OUTPUT); pinMode(echoEntrada, INPUT);
  pinMode(trigCajon1, OUTPUT);  pinMode(echoCajon1, INPUT);
  pinMode(trigCajon2, OUTPUT);  pinMode(echoCajon2, INPUT);
  pinMode(ledVerde, OUTPUT);    pinMode(ledRojo, OUTPUT);
  pinMode(buzzerPin, OUTPUT);

  digitalWrite(ledVerde, HIGH);
  digitalWrite(ledRojo, LOW);

  setup_wifi();
  configurarNTP();
  
  client.setServer(mqtt_server, mqtt_port);
  client.setBufferSize(512);
  
  // Tono de inicio
  tone(buzzerPin, 1000, 150);
  delay(200);
  tone(buzzerPin, 1500, 150);
  delay(200);
  noTone(buzzerPin);
  
  // === LECTURA INICIAL DE SENSORES ===
  // Hacer varias lecturas para estabilizar
  delay(500);
  for (int i = 0; i < 5; i++) {
    medirDistancia(trigCajon1, echoCajon1);
    medirDistancia(trigCajon2, echoCajon2);
    medirDistancia(trigEntrada, echoEntrada);
    delay(100);
  }
  
  // Leer estado inicial real
  int dist_c1 = medirDistancia(trigCajon1, echoCajon1);
  int dist_c2 = medirDistancia(trigCajon2, echoCajon2);
  
  // Inicializar estados basados en lectura real
  cajon1_ocupado = (dist_c1 < UMBRAL_OCUPADO && dist_c1 > DISTANCIA_MIN);
  cajon2_ocupado = (dist_c2 < UMBRAL_OCUPADO && dist_c2 > DISTANCIA_MIN);
  cajon1_ocupado_prev = cajon1_ocupado;
  cajon2_ocupado_prev = cajon2_ocupado;
  cajon1_lectura = cajon1_ocupado;
  cajon2_lectura = cajon2_ocupado;
  cajon1_tiempo_cambio = millis();
  cajon2_tiempo_cambio = millis();
  
  Serial.print("Distancia Cajon1: "); Serial.print(dist_c1); Serial.println(" cm");
  Serial.print("Distancia Cajon2: "); Serial.print(dist_c2); Serial.println(" cm");
  Serial.print("Estado inicial C1: "); Serial.println(cajon1_ocupado ? "OCUPADO" : "LIBRE");
  Serial.print("Estado inicial C2: "); Serial.println(cajon2_ocupado ? "OCUPADO" : "LIBRE");
  
  Serial.println("=== SISTEMA INICIADO ===");
  
  if (client.connect("ESP32_Parking", mqtt_user, mqtt_pass)) {
    publicarMensaje("SISTEMA_INICIADO", "ESP32 conectado");
  }
}

void setup_wifi() {
  Serial.print("Conectando WiFi");
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) { 
    delay(500); 
    Serial.print("."); 
  }
  Serial.println(" OK");
  Serial.print("IP: ");
  Serial.println(WiFi.localIP());
}

void configurarNTP() {
  configTime(gmtOffset_sec, daylightOffset_sec, ntpServer);
  struct tm timeinfo;
  for (int i = 0; i < 10 && !getLocalTime(&timeinfo); i++) {
    delay(500);
  }
}

String obtenerTimestamp() {
  struct tm timeinfo;
  if (!getLocalTime(&timeinfo)) {
    return String(millis());
  }
  char buffer[25];
  strftime(buffer, sizeof(buffer), "%Y-%m-%dT%H:%M:%S", &timeinfo);
  return String(buffer);
}

int medirDistanciaUnica(int trig, int echo) {
  digitalWrite(trig, LOW);
  delayMicroseconds(2);
  digitalWrite(trig, HIGH);
  delayMicroseconds(10);
  digitalWrite(trig, LOW);
  
  long dur = pulseIn(echo, HIGH, 30000);
  if (dur <= 0) return 999;
  return dur * 0.034 / 2;
}

// Mide distancia con filtrado (promedio de varias lecturas, descarta outliers)
int medirDistancia(int trig, int echo) {
  int lecturas[5];
  
  // Tomar 5 lecturas
  for (int i = 0; i < 5; i++) {
    lecturas[i] = medirDistanciaUnica(trig, echo);
    delayMicroseconds(500);
  }
  
  // Ordenar (bubble sort simple)
  for (int i = 0; i < 4; i++) {
    for (int j = i + 1; j < 5; j++) {
      if (lecturas[i] > lecturas[j]) {
        int temp = lecturas[i];
        lecturas[i] = lecturas[j];
        lecturas[j] = temp;
      }
    }
  }
  
  // Devolver la mediana (valor del medio, ignora extremos)
  return lecturas[2];
}

void publicarMensaje(const char* evento, const char* detalle) {
  if (!client.connected()) return;
  
  String ts = obtenerTimestamp();
  int libres = (cajon1_ocupado ? 0 : 1) + (cajon2_ocupado ? 0 : 1);
  int ocupados = 2 - libres;
  
  snprintf(jsonBuffer, sizeof(jsonBuffer),
    "{\"evento\":\"%s\",\"detalle\":\"%s\",\"timestamp\":\"%s\",\"cajon1\":\"%s\",\"cajon2\":\"%s\",\"libres\":%d,\"ocupados\":%d,\"lleno\":%s}",
    evento, detalle, ts.c_str(),
    cajon1_ocupado ? "OCUPADO" : "LIBRE",
    cajon2_ocupado ? "OCUPADO" : "LIBRE",
    libres, ocupados,
    (cajon1_ocupado && cajon2_ocupado) ? "true" : "false"
  );
  
  client.publish(topic_sensores, jsonBuffer);
  Serial.println(jsonBuffer);
}

// Medición rápida para entrada (solo 2 lecturas, objeto en movimiento)
int medirDistanciaRapida(int trig, int echo) {
  int suma = 0;
  int validas = 0;
  
  for (int i = 0; i < 2; i++) {
    digitalWrite(trig, LOW);
    delayMicroseconds(2);
    digitalWrite(trig, HIGH);
    delayMicroseconds(10);
    digitalWrite(trig, LOW);
    
    long dur = pulseIn(echo, HIGH, 25000);
    if (dur > 0) {
      suma += dur * 0.034 / 2;
      validas++;
    }
    delayMicroseconds(200);
  }
  
  if (validas == 0) return 999;
  return suma / validas;
}

// === TONOS ===

void tonoEntrada() {
  // Solo un beep corto de notificación
  tone(buzzerPin, 1500, 100);
  delay(120);
  noTone(buzzerPin);
}

void tonoBienvenida() {
  // Melodía alegre de bienvenida (LED verde = hay espacio)
  tone(buzzerPin, 1047, 100);  // Do
  delay(120);
  tone(buzzerPin, 1319, 100);  // Mi
  delay(120);
  tone(buzzerPin, 1568, 100);  // Sol
  delay(120);
  tone(buzzerPin, 2093, 200);  // Do alto
  noTone(buzzerPin);
}

void tonoBloqueo() {
  // Melodía de bloqueo/rechazo (LED rojo = cochera llena)
  tone(buzzerPin, 400, 200);
  delay(250);
  tone(buzzerPin, 300, 200);
  delay(250);
  tone(buzzerPin, 200, 400);  // Tono muy grave final
  noTone(buzzerPin);
}

void tonoLleno() {
  // Tono grave: cochera se llenó
  tone(buzzerPin, 400, 200);
  delay(250);
  tone(buzzerPin, 300, 300);
  noTone(buzzerPin);
}

void tonoLiberado() {
  // Tono positivo: se liberó un cajón
  tone(buzzerPin, 800, 100);
  delay(120);
  tone(buzzerPin, 1000, 100);
  delay(120);
  tone(buzzerPin, 1200, 150);
  noTone(buzzerPin);
}

void tonoSalidaParpadeo() {
  // Melodía de despedida cuando termina parpadeo
  tone(buzzerPin, 1568, 120);
  delay(150);
  tone(buzzerPin, 1319, 120);
  delay(150);
  tone(buzzerPin, 1047, 120);
  delay(150);
  tone(buzzerPin, 784, 250);
  delay(300);
  tone(buzzerPin, 1047, 100);
  delay(120);
  tone(buzzerPin, 1319, 180);
  noTone(buzzerPin);
}

void loop() {
  // === MQTT ===
  if (!client.connected()) {
    if (client.connect("ESP32_Parking", mqtt_user, mqtt_pass)) {
      Serial.println("MQTT reconectado");
    }
  }
  client.loop();

  // === LEER SENSORES ===
  // Entrada: medición rápida (objeto en movimiento)
  int dist_entrada = medirDistanciaRapida(trigEntrada, echoEntrada);
  // Cajones: medición con filtro (objetos estáticos)
  int dist_c1 = medirDistancia(trigCajon1, echoCajon1);
  int dist_c2 = medirDistancia(trigCajon2, echoCajon2);

  // Umbral más amplio para entrada (15cm) porque el auto está pasando
  bool hay_objeto_entrada = (dist_entrada < 15 && dist_entrada > DISTANCIA_MIN);
  bool hay_objeto_c1 = (dist_c1 < UMBRAL_OCUPADO && dist_c1 > DISTANCIA_MIN);
  bool hay_objeto_c2 = (dist_c2 < UMBRAL_OCUPADO && dist_c2 > DISTANCIA_MIN);

  // === DEBOUNCE CAJÓN 1 ===
  if (hay_objeto_c1 != cajon1_lectura) {
    cajon1_lectura = hay_objeto_c1;
    cajon1_tiempo_cambio = millis();
  }
  if (millis() - cajon1_tiempo_cambio > DEBOUNCE_MS) {
    if (cajon1_ocupado != cajon1_lectura) {
      cajon1_ocupado_prev = cajon1_ocupado;  // Solo actualizar prev cuando hay cambio real
      cajon1_ocupado = cajon1_lectura;
    }
  }

  // === DEBOUNCE CAJÓN 2 ===
  if (hay_objeto_c2 != cajon2_lectura) {
    cajon2_lectura = hay_objeto_c2;
    cajon2_tiempo_cambio = millis();
  }
  if (millis() - cajon2_tiempo_cambio > DEBOUNCE_MS) {
    if (cajon2_ocupado != cajon2_lectura) {
      cajon2_ocupado_prev = cajon2_ocupado;  // Solo actualizar prev cuando hay cambio real
      cajon2_ocupado = cajon2_lectura;
    }
  }

  // === ESTADOS ===
  bool cochera_llena = cajon1_ocupado && cajon2_ocupado;

  // === EVENTO: MOVIMIENTO EN ENTRADA ===
  entrada_detecta_prev = entrada_detecta;
  entrada_detecta = hay_objeto_entrada;

  // Detectó inicio de movimiento (y no había vehículo presente)
  if (entrada_detecta && !entrada_detecta_prev) {
    entrada_tiempo_inicio = millis();
  }

  // Dejó de detectar - iniciar contador de salida
  if (!entrada_detecta && entrada_detecta_prev) {
    entrada_tiempo_salida = millis();
  }

  // Si no detecta por TIEMPO_SALIDA_ENTRADA, el vehículo se fue
  if (!entrada_detecta && vehiculo_presente_entrada && 
      (millis() - entrada_tiempo_salida > TIEMPO_SALIDA_ENTRADA)) {
    vehiculo_presente_entrada = false;
    Serial.println(">>> Vehículo salió de la entrada");
  }

  // Publicar evento SOLO si:
  // 1. Hay detección estable (pasó debounce)
  // 2. NO había vehículo presente antes
  if (entrada_detecta && !vehiculo_presente_entrada && 
      (millis() - entrada_tiempo_inicio > DEBOUNCE_ENTRADA)) {
    
    vehiculo_presente_entrada = true;  // Marcar que hay vehículo
    
    // Si estamos en parpadeo, terminarlo primero
    if (parpadeo_activo) {
      parpadeo_activo = false;
      cochera_estuvo_llena = false;
      publicarMensaje("VEHICULO_SALIO", "Vehiculo confirmo salida en entrada");
      tonoSalidaParpadeo();
      Serial.println(">>> VEHICULO SALIO (fin parpadeo)");
    } 
    // Si cochera está LLENA: tono de bloqueo
    else if (cochera_llena) {
      publicarMensaje("MOVIMIENTO_ENTRADA_BLOQUEADO", "Vehiculo en entrada pero cochera LLENA");
      tonoBloqueo();
      Serial.println(">>> ENTRADA BLOQUEADA - COCHERA LLENA");
    }
    // Si hay espacio (LED verde): tono de bienvenida
    else {
      publicarMensaje("MOVIMIENTO_ENTRADA", "Vehiculo detectado en entrada - BIENVENIDO");
      tonoBienvenida();
      Serial.println(">>> BIENVENIDO - HAY ESPACIO");
    }
  }

  // === RASTREAR SI COCHERA ESTÁ LLENA ===
  if (cochera_llena) {
    cochera_estuvo_llena = true;
  }

  // === EVENTO: CAJÓN 1 CAMBIÓ ===
  if (cajon1_ocupado != cajon1_ocupado_prev) {
    if (cajon1_ocupado) {
      publicarMensaje("CAJON_OCUPADO", "Cajon 1 ocupado");
      Serial.println(">>> CAJON 1: OCUPADO");
    } else {
      publicarMensaje("CAJON_LIBERADO", "Cajon 1 liberado");
      Serial.println(">>> CAJON 1: LIBRE");
      tonoLiberado();
      
      // Activar parpadeo SOLO si:
      // 1. La cochera ESTUVO llena antes
      // 2. Ahora hay al menos un cajón libre (ya lo sabemos porque este se liberó)
      if (cochera_estuvo_llena && !cochera_llena) {
        parpadeo_activo = true;
        parpadeo_inicio = millis();
        parpadeo_ultimo = millis();
        parpadeo_estado = false;
        publicarMensaje("PARPADEO_INICIADO", "Cajon 1 liberado - esperando salida de vehiculo");
        Serial.println(">>> PARPADEO ACTIVADO (cajón 1 liberado)");
      }
    }
    cajon1_ocupado_prev = cajon1_ocupado;  // Resetear prev después de procesar
  }

  // === EVENTO: CAJÓN 2 CAMBIÓ ===
  if (cajon2_ocupado != cajon2_ocupado_prev) {
    if (cajon2_ocupado) {
      publicarMensaje("CAJON_OCUPADO", "Cajon 2 ocupado");
      Serial.println(">>> CAJON 2: OCUPADO");
    } else {
      publicarMensaje("CAJON_LIBERADO", "Cajon 2 liberado");
      Serial.println(">>> CAJON 2: LIBRE");
      tonoLiberado();
      
      // Activar parpadeo SOLO si:
      // 1. La cochera ESTUVO llena antes
      // 2. Ahora hay al menos un cajón libre
      if (cochera_estuvo_llena && !cochera_llena) {
        parpadeo_activo = true;
        parpadeo_inicio = millis();
        parpadeo_ultimo = millis();
        parpadeo_estado = false;
        publicarMensaje("PARPADEO_INICIADO", "Cajon 2 liberado - esperando salida de vehiculo");
        Serial.println(">>> PARPADEO ACTIVADO (cajón 2 liberado)");
      }
    }
    cajon2_ocupado_prev = cajon2_ocupado;  // Resetear prev después de procesar
  }

  // === EVENTO: COCHERA SE LLENÓ ===
  if (cochera_llena && !cochera_llena_prev) {
    publicarMensaje("COCHERA_LLENA", "Todos los cajones ocupados");
    tonoLleno();
    Serial.println(">>> COCHERA LLENA");
  }

  // === TIMEOUT PARPADEO ===
  if (parpadeo_activo && (millis() - parpadeo_inicio > TIMEOUT_PARPADEO)) {
    parpadeo_activo = false;
    cochera_estuvo_llena = false;
    publicarMensaje("PARPADEO_TIMEOUT", "Tiempo de espera agotado");
    Serial.println(">>> PARPADEO TIMEOUT");
  }

  // === CONTROL DE LEDS ===
  if (parpadeo_activo) {
    // Parpadeo alternado verde-rojo
    if (millis() - parpadeo_ultimo > PARPADEO_INTERVALO) {
      parpadeo_estado = !parpadeo_estado;
      if (parpadeo_estado) {
        digitalWrite(ledRojo, HIGH);
        digitalWrite(ledVerde, LOW);
      } else {
        digitalWrite(ledRojo, LOW);
        digitalWrite(ledVerde, HIGH);
      }
      parpadeo_ultimo = millis();
    }
  } else {
    // Estado normal según ocupación
    if (cochera_llena) {
      digitalWrite(ledRojo, HIGH);
      digitalWrite(ledVerde, LOW);
    } else {
      digitalWrite(ledRojo, LOW);
      digitalWrite(ledVerde, HIGH);
    }
  }

  delay(50);
}