#include <WiFi.h>
#include <PubSubClient.h>

// --- CONFIGURACIÓN DE RED ---
const char* ssid = "AVRIL@2014";       
const char* password = "AVRIL@2014";   
const char* mqtt_server = "192.168.100.16"; 
const int mqtt_port = 1883;
const char* mqtt_user = "esp32";
const char* mqtt_pass = "123456";
const char* topic_espacios = "cochera/estado/espacios";

// --- PINES ---
const int trigEntrada = 13; const int echoEntrada = 12;
const int trigCajon1 = 14;  const int echoCajon1 = 27;
const int trigCajon2 = 26;  const int echoCajon2 = 25;
const int ledVerde = 32;    const int ledRojo = 33;    
const int buzzerPin = 15;  

// --- CONSTANTES DE DISTANCIA (Caja de Zapatos) ---
const int UMBRAL_CARRO = 12; // Menos de 12cm = OCUPADO (Carrito de 10cm detectado)

// --- VARIABLES ---
WiFiClient espClient;
PubSubClient client(espClient);
bool ocupadoC1 = false;
bool ocupadoC2 = false;
unsigned long ultimoParpadeo = 0;
bool estadoLed = false;
bool autoEnPuerta = false;

void setup() {
  Serial.begin(115200);
  pinMode(trigEntrada, OUTPUT); pinMode(echoEntrada, INPUT);
  pinMode(trigCajon1, OUTPUT);  pinMode(echoCajon1, INPUT);
  pinMode(trigCajon2, OUTPUT);  pinMode(echoCajon2, INPUT);
  pinMode(ledVerde, OUTPUT);    pinMode(ledRojo, OUTPUT);
  pinMode(buzzerPin, OUTPUT);

  setup_wifi();
  client.setServer(mqtt_server, mqtt_port);
  
  // Beep de inicio (Tono medio)
  tone(buzzerPin, 1000, 200); 
}

void setup_wifi() {
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) { delay(500); Serial.print("."); }
  Serial.println("\nWiFi Conectado");
}

int obtenerDistancia(int trig, int echo) {
  digitalWrite(trig, LOW); delayMicroseconds(2);
  digitalWrite(trig, HIGH); delayMicroseconds(10);
  digitalWrite(trig, LOW);
  long duration = pulseIn(echo, HIGH, 25000); 
  if (duration <= 0) return 999;
  return duration * 0.034 / 2;
}

void loop() {
  if (!client.connected()) {
    if (client.connect("ESP32_Caja", mqtt_user, mqtt_pass)) {
       Serial.println("MQTT Reconectado");
    }
  }
  client.loop();

  int dEntrada = obtenerDistancia(trigEntrada, echoEntrada);
  int dC1 = obtenerDistancia(trigCajon1, echoCajon1);
  int dC2 = obtenerDistancia(trigCajon2, echoCajon2);

  bool c1_actual = (dC1 < UMBRAL_CARRO && dC1 > 1);
  bool c2_actual = (dC2 < UMBRAL_CARRO && dC2 > 1);
  bool puerta_actual = (dEntrada < UMBRAL_CARRO && dEntrada > 1);

  // Lógica MQTT Cajones
  if (c1_actual != ocupadoC1) {
    ocupadoC1 = c1_actual;
    client.publish(topic_espacios, ocupadoC1 ? "{\"id\":1,\"status\":\"FULL\"}" : "{\"id\":1,\"status\":\"EMPTY\"}");
  }
  if (c2_actual != ocupadoC2) {
    ocupadoC2 = c2_actual;
    client.publish(topic_espacios, ocupadoC2 ? "{\"id\":2,\"status\":\"FULL\"}" : "{\"id\":2,\"status\":\"EMPTY\"}");
  }

  bool cocheraLlena = (ocupadoC1 && ocupadoC2);

  // --- LÓGICA DE ALERTAS Y SONIDOS ---
  if (puerta_actual) {
    if (cocheraLlena) {
      // CASO: LLENO (Alerta Grave)
      if (millis() - ultimoParpadeo > 200) {
        estadoLed = !estadoLed;
        digitalWrite(ledRojo, estadoLed);
        digitalWrite(ledVerde, LOW);
        if (estadoLed) tone(buzzerPin, 250); // Tono grave (Frecuencia baja)
        else noTone(buzzerPin);
        ultimoParpadeo = millis();
      }
    } else {
      // CASO: DISPONIBLE (Bienvenida Aguda)
      if (millis() - ultimoParpadeo > 500) {
        estadoLed = !estadoLed;
        digitalWrite(ledVerde, estadoLed);
        digitalWrite(ledRojo, LOW);
        ultimoParpadeo = millis();
      }
      if (!autoEnPuerta) {
        tone(buzzerPin, 2000, 150); // Tono agudo y corto (tipo "Ding")
        autoEnPuerta = true;
      }
    }
  } else {
    // REPOSO
    autoEnPuerta = false;
    noTone(buzzerPin); // Asegura que el buzzer se apague
    if (cocheraLlena) {
      digitalWrite(ledRojo, HIGH);
      digitalWrite(ledVerde, LOW);
    } else {
      digitalWrite(ledRojo, LOW);
      digitalWrite(ledVerde, HIGH);
    }
  }
  delay(50);
}