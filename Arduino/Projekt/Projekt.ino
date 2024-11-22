#include <IBusBM.h>
#include <Servo.h>

// Definicje pinów dla silnika
const int IN1 = 2;   // Pin sterujący kierunkiem IN1
const int IN2 = 4;   // Pin sterujący kierunkiem IN2
const int PWM1 = 5;  // Pin PWM do kontroli prędkości silnika (kierunek 1)
const int PWM2 = 3;  // Pin PWM do kontroli prędkości silnika (kierunek 2)
const int servPin = 9;
const int servPinH = 10;
const int servPinV = 11;

IBusBM ibus;   
Servo servo; 
Servo servoH; 
Servo servoV; 

bool servoHAttached = false;
int angleH = 90;
bool servoVAttached = false;
int angleV = 90;
bool servoAttached = false;
int angle = 90;

void controlCamServo(int servValue, int servPin, int &angle, int camSpeed, Servo &servo, bool &servoAttached);
void controlServo(int servPin, int &angle, Servo &servo, bool &servoAttached, int targetAngle, int threshold = 1);
void resetCamera(int &angleH, int &angleV, Servo &servoH, Servo &servoV);
void resetControl(int &angle, Servo &servo);


void setup() {
  // Ustawienie pinów jako wyjścia
  pinMode(IN1, OUTPUT);
  pinMode(IN2, OUTPUT);
  pinMode(PWM1, OUTPUT);
  pinMode(PWM2, OUTPUT);

  // Ustawienie początkowego kierunku silnika
  digitalWrite(IN1, HIGH);
  digitalWrite(IN2, LOW);

  ibus.begin(Serial);
  resetControl(angle, servo);
  resetCamera(angleH, angleV, servoH, servoV);
}

int speedPWM1 = 0;
int speedPWM2 = 0;

void loop() {
  if (ibus.readChannel(3) < 1000) {
    delay(100);
    return;
  }

  int servH = ibus.readChannel(0);
  int servV = ibus.readChannel(1);
  int value = ibus.readChannel(2);
  int servVal = ibus.readChannel(3);
  int resetCam = ibus.readChannel(4);

  if (resetCam == 2000) {
    resetCamera(angleH, angleV, servoH, servoV);
  }

  int speed = map(value, 1000, 2000, -100, 100);
  int targetAngle = map(servVal, 1000, 2000, 25, 120);
  int camSpeedH = map(servH, 1000, 2000, 20, -20);
  int camSpeedV = map(servV, 1000, 2000, -20, 20);

  if (speed >= 0) {
    digitalWrite(IN1, HIGH);
    digitalWrite(IN2, LOW);
  } else {
    digitalWrite(IN1, LOW);
    digitalWrite(IN2, HIGH);
    speed = -speed;
  }

  int speedPWM1 = speed;
  int speedPWM2 = speedPWM1;

  analogWrite(PWM1, speedPWM1);
  analogWrite(PWM2, speedPWM2);

  controlServo(servPin, angle, servo, servoAttached, targetAngle);

  controlCamServo(servH, servPinH, angleH, camSpeedH, servoH, servoHAttached);

  controlCamServo(servV, servPinV, angleV, camSpeedV, servoV, servoVAttached);

  delay(100);
}

void controlCamServo(int servValue, int servPin, int &angle, int camSpeed, Servo &servo, bool &servoAttached) {
  if (servValue < 1400 || servValue > 1600) {
    if (!servoAttached) {
      servo.attach(servPin);
      servoAttached = true;
    }
    angle += camSpeed;  // Ruch w lewo (zmniejszanie kąta)

    // Ograniczenie kąta
    if (angle < 30) {
      angle = 30;
    } else if (angle > 140) {
      angle = 140;
    }

    servo.write(angle);  // Ustawienie kąta serwa
  } else {
    if (servoAttached) {
      servo.detach();  // Odłącz serwo, gdy nie zmienia się kąt
      servoAttached = false;  // Flaga wskazująca, że serwo zostało odłączone
    }
  }
}

void controlServo(int servPin, int &angle, Servo &servo, bool &servoAttached, int targetAngle, int threshold) {
  // Sprawdzenie, czy zmiana kąta jest wystarczająco duża
  if (abs(targetAngle - angle) > threshold) {  // Jeśli różnica większa niż próg
    if (!servoAttached) {
      servo.attach(servPin);  // Podłącz serwo, jeśli nie jest podłączone
      servoAttached = true;
    }
    
    angle = targetAngle;  // Aktualizacja kąta
    servo.write(angle);   // Ustawienie nowego kąta
  } else {
    if (servoAttached) {
      servo.detach();  // Odłącz serwo, gdy nie zmienia się kąt
      servoAttached = false;
    }
  }
}

void resetCamera(int &angleH, int &angleV, Servo &servoH, Servo &servoV) {
  angleH = 90;
  angleV = 90;

  servoH.attach(servPinH);
  servoV.attach(servPinV);
  servoH.write(angleH);
  servoV.write(angleV);

  delay(50);  // Poczekaj, aby serwa ustawiły się w pozycji
  servoH.detach();
  servoV.detach();
}

void resetControl(int &angle, Servo &servo) {
  angle = 90;

  servo.attach(servPin);
  servo.write(angle);

  delay(50);  // Poczekaj, aby serwa ustawiły się w pozycji
  servo.detach();
}