#include <IBusBM.h>
#include <Servo.h>

const int IN1 = 2;   
const int IN2 = 4;   
const int PWM1 = 5;  
const int PWM2 = 3;  
const int servPin = 9;
const int servPinH = 10;
const int servPinV = 11;

IBusBM ibus;  
ibus.begin(Serial); 
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
  pinMode(IN1, OUTPUT);
  pinMode(IN2, OUTPUT);
  pinMode(PWM1, OUTPUT);
  pinMode(PWM2, OUTPUT);

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

  analogWrite(PWM1, speed);
  analogWrite(PWM2, speed);

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
    angle += camSpeed;

    if (angle < 30) {
      angle = 30;
    } else if (angle > 150) {
      angle = 150;
    }

    servo.write(angle);
  } else {
    if (servoAttached) {
      servo.detach();
      servoAttached = false;
    }
  }
}

void controlServo(int servPin, int &angle, Servo &servo, bool &servoAttached, int targetAngle, int threshold) {
  if (abs(targetAngle - angle) > threshold) {
    if (!servoAttached) {
      servo.attach(servPin);
      servoAttached = true;
    }
    
    angle = targetAngle;
    servo.write(angle);
  } else {
    if (servoAttached) {
      servo.detach();
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