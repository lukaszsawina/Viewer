#include <IBusBM.h>
#include <Servo.h>

// Definicje pinów dla silnika
const int IN1 = 2;   // Pin sterujący kierunkiem IN1
const int IN2 = 4;   // Pin sterujący kierunkiem IN2
const int PWM1 = 5;  // Pin PWM do kontroli prędkości silnika (kierunek 1)
const int PWM2 = 3;  // Pin PWM do kontroli prędkości silnika (kierunek 2)
const int servPin = 9;
// const int maxSpeedPWM1 = 76; // Maksymalna wartość PWM1 odpowiadająca 30%
// const int maxSpeedPWM2 = maxSpeedPWM1 * 0.75; // Maksymalna wartość PWM2 odpowiadająca 24%

IBusBM ibus;   
Servo servo; 

void setup() {
  // Ustawienie pinów jako wyjścia
  pinMode(IN1, OUTPUT);
  pinMode(IN2, OUTPUT);
  pinMode(PWM1, OUTPUT);
  pinMode(PWM2, OUTPUT);

  // Ustawienie początkowego kierunku silnika
  digitalWrite(IN1, HIGH);
  digitalWrite(IN2, LOW);

  // Inicjalizacja komunikacji szeregowej
  Serial.begin(115200);
  ibus.begin(Serial);
  servo.attach(servPin);
}

int speedPWM1 = 0;
int speedPWM2 = 0;

void loop() {
  int value = ibus.readChannel(2);
  int servVal = ibus.readChannel(3);

  int speed = map(value, 1000, 2000, -100, 100);
  int angle = map(servVal, 1000, 2000, 25, 120);

  Serial.print("Speed: ");
  Serial.print(speed);
  Serial.print(" angle: ");
  Serial.print(angle);
  Serial.print(" servVal ");
  Serial.println(servVal);

  // Zmiana kierunku w zależności od znaku prędkości
    if (speed >= 0) {
      digitalWrite(IN1, HIGH);
      digitalWrite(IN2, LOW);
    } else {
      digitalWrite(IN1, LOW);
      digitalWrite(IN2, HIGH);
      speed = -speed;  // Konwersja ujemnej wartości na dodatnią
    }

    // Obliczenie wartości PWM dla PWM1 (30% to 76 w skali 0-255)
    int speedPWM1 = speed;
    // Obliczenie wartości PWM dla PWM2 (20% mniejsza niż PWM1)
    int speedPWM2 = speedPWM1;

    // Ustawienie prędkości silnika
    analogWrite(PWM1, speedPWM1);
    analogWrite(PWM2, speedPWM2);

    servo.write(angle);

  // Opóźnienie, aby zmiana była widoczna
  delay(100); // Można dostosować czas opóźnienia według potrzeb
}
