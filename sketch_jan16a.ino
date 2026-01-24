#include <WiFi.h>
#include <PubSubClient.h>

// ==========================================
// 1. CONFIGURACIÓN DE RED (¡Revisa esto!)
// ==========================================
const char* ssid = "AVRIL@2014";       // Tu nombre de WiFi
const char* password = "AVRIL@2014";   // Tu contraseña
const char* mqtt_server = "192.168.100.16"; // IP de tu compu con RabbitMQ
const int mqtt_port = 1883;
const char* mqtt_user = "esp32";
const char* mqtt_pass = "123456";

// Topics MQTT (Donde publicaremos los mensajes)
const char* topic_espacios = "cochera/estado/espacios";
const char* topic_alerta = "cochera/estado/alerta";

// ==========================================
// 2. MAPA DE PINES (Tus conexiones exactas)
// ==========================================
// Sensor Entrada
const int trigEntrada = 13;
const int echoEntrada = 12;

// Sensor Cajón 1
const int trigCajon1 = 14;
const int echoCajon1 = 27;

// Sensor Cajón 2
const int trigCajon2 = 26;
const int echoCajon2 = 25;

// Luces y Sonido
const int ledVerde = 32;   // Luz de "Pase"
const int ledRojo = 33;    // Luz de "Lleno"
const int buzzerPin = 15;  // Alarma

// ==========================================
// 3. VARIABLES GLOBALES
// ==========================================
WiFiClient espClient;
PubSubClient client(espClient);

// Estado de los cajones
bool ocupadoC1 = false;
bool ocupadoC2 = false;
bool cocheraLlena = false;

// Variables para tiempos (sin usar delay largos)
unsigned long ultimoParpadeo = 0;
bool estadoLed = false;
unsigned long tiempoDeteccionEntrada = 0;
bool autoEnPuerta = false;

void setup() {
  Serial.begin(115200);
  
  // Configurar Pines como Salida (Output) o Entrada (Input)
  pinMode(trigEntrada, OUTPUT); pinMode(echoEntrada, INPUT);
  pinMode(trigCajon1, OUTPUT);  pinMode(echoCajon1, INPUT);
  pinMode(trigCajon2, OUTPUT);  pinMode(echoCajon2, INPUT);
  
  pinMode(ledVerde, OUTPUT);
  pinMode(ledRojo, OUTPUT);
  pinMode(buzzerPin, OUTPUT);

  // Prueba de vida: Prender todo 1 segundo al arrancar
  digitalWrite(ledVerde, HIGH);
  digitalWrite(ledRojo, HIGH);
  digitalWrite(buzzerPin, HIGH);
  delay(500);
  digitalWrite(ledVerde, LOW);
  digitalWrite(ledRojo, LOW);
  digitalWrite(buzzerPin, LOW);

  // Iniciar conexiones
  setup_wifi();
  client.setServer(mqtt_server, mqtt_port);
}

void setup_wifi() {
  delay(10);
  Serial.println();
  Serial.print("Conectando a WiFi: ");
  Serial.println(ssid);

  WiFi.begin(ssid, password);

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }

  Serial.println("");
  Serial.println("WiFi Conectado!");
  Serial.print("IP del ESP32: ");
  Serial.println(WiFi.localIP());
}

void reconnect() {
  while (!client.connected()) {
    Serial.print("Intentando conectar a RabbitMQ...");
    if (client.connect("ESP32_Cliente", mqtt_user, mqtt_pass)) {
      Serial.println("¡Conectado!");
    } else {
      Serial.print("Fallo, rc=");
      Serial.print(client.state());
      Serial.println(" intentando en 5 segundos");
      delay(5000);
    }
  }
}

// Función auxiliar para leer distancia en cm
int obtenerDistancia(int trig, int echo) {
  digitalWrite(trig, LOW);
  delayMicroseconds(2);
  digitalWrite(trig, HIGH);
  delayMicroseconds(10);
  digitalWrite(trig, LOW);
  
  long duration = pulseIn(echo, HIGH, 30000); // Timeout corto para no trabar
  if (duration == 0) return 999; // Si no hay rebote, asumimos lejos
  return duration * 0.034 / 2;
}

void loop() {
  if (!client.connected()) {
    reconnect();
  }
  client.loop();

  // --- A. LEER SENSORES ---
  int distEntrada = obtenerDistancia(trigEntrada, echoEntrada);
  int distC1 = obtenerDistancia(trigCajon1, echoCajon1);
  int distC2 = obtenerDistancia(trigCajon2, echoCajon2);

  // --- B. PROCESAR ESTADO CAJONES (Umbral 15cm) ---
  bool c1_actual = (distC1 < 15);
  bool c2_actual = (distC2 < 15);

  // Publicar solo si cambia el estado para no saturar
  if (c1_actual != ocupadoC1) {
    ocupadoC1 = c1_actual;
    String msg = c1_actual ? "OCUPADO" : "LIBRE";
    String json = "{\"cajon\": 1, \"estado\": \"" + msg + "\"}";
    client.publish(topic_espacios, json.c_str());
    Serial.println("Cajon 1: " + msg);
  }

  if (c2_actual != ocupadoC2) {
    ocupadoC2 = c2_actual;
    String msg = c2_actual ? "OCUPADO" : "LIBRE";
    String json = "{\"cajon\": 2, \"estado\": \"" + msg + "\"}";
    client.publish(topic_espacios, json.c_str());
    Serial.println("Cajon 2: " + msg);
  }

  // --- C. LOGICA DE SEMÁFORO Y ALARMA ---
  cocheraLlena = (ocupadoC1 && ocupadoC2);

  // Lógica de Entrada
  if (distEntrada < 20) { 
    // ¡HAY UN AUTO EN LA PUERTA!
    
    if (cocheraLlena) {
      // CASO 1: LLENO -> ALERTA MÁXIMA
      // Parpadeo Rojo Rápido
      if (millis() - ultimoParpadeo > 100) { // 100ms = muy rápido
        estadoLed = !estadoLed;
        digitalWrite(ledRojo, estadoLed);
        digitalWrite(buzzerPin, estadoLed); // El buzzer suena al ritmo del led
        ultimoParpadeo = millis();
      }
      digitalWrite(ledVerde, LOW);
      
    } else {
      // CASO 2: HAY LUGAR -> BIENVENIDA
      // Parpadeo Verde Lento
      if (millis() - ultimoParpadeo > 500) { 
        estadoLed = !estadoLed;
        digitalWrite(ledVerde, estadoLed);
        ultimoParpadeo = millis();
      }
      digitalWrite(ledRojo, LOW);
      
      // Un solo beep corto de bienvenida
      if (!autoEnPuerta) { 
        digitalWrite(buzzerPin, HIGH);
        delay(200); // Pequeño delay imperceptible
        digitalWrite(buzzerPin, LOW);
      }
    }
    autoEnPuerta = true;

  } else {
    // --- PUERTA LIBRE (Modo espera) ---
    autoEnPuerta = false;
    digitalWrite(buzzerPin, LOW); // Silencio total

    if (cocheraLlena) {
      digitalWrite(ledRojo, HIGH);  // Rojo fijo (No pase)
      digitalWrite(ledVerde, LOW);
    } else {
      digitalWrite(ledRojo, LOW);
      digitalWrite(ledVerde, HIGH); // Verde fijo (Pase)
    }
  }

  delay(50); // Pequeña pausa para estabilidad
}