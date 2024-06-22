#include <SerialCommand.h>

SerialCommand sCmd;

byte motor_speed = 255;
unsigned long currentMillis, previousMillis;
bool isStop;
//Pair of motor 1
//int ENA = D0;
int IN1 = D0;
int IN2 = D1;

//int ENB = D0;ssssssssssssssssssss
int IN3 = D2;
int IN4 = D3;
//Pair of motors 2
//int ENC = D5;
int IN5 = D4;
int IN6 = D5;

//int END = D0;
int IN7 = D6;
int IN8 = D7;

void setup() {
    isStop = true;
    previousMillis = 0;
    //Setting motors pin
    //pinMode(ENA, OUTPUT);
    //pinMode(ENB, OUTPUT);
    pinMode(IN1, OUTPUT);
    pinMode(IN2, OUTPUT);
    pinMode(IN3, OUTPUT);
    pinMode(IN4, OUTPUT);
    pinMode(IN5, OUTPUT);
    pinMode(IN6, OUTPUT);
    pinMode(IN7, OUTPUT);
    pinMode(IN8, OUTPUT);

    digitalWrite(IN1, LOW);
    digitalWrite(IN2, LOW);
    digitalWrite(IN3, LOW);
    digitalWrite(IN4, LOW);
    digitalWrite(IN5, LOW);
    digitalWrite(IN6, LOW);
    digitalWrite(IN7, LOW);
    digitalWrite(IN8, LOW);

    Serial.begin(115200);
    while (!Serial) {

    }

    Serial.println();
    Serial.println("&Ready*");
    sCmd.addCommand("forward", forward);
    sCmd.addCommand("backward", backward);
    sCmd.addCommand("turn-left", turn_left);
    sCmd.addCommand("turn-right", turn_right);
    sCmd.addCommand("speed", set_speed);
    sCmd.addCommand("interval", interval);
    sCmd.addCommand("checkName", checkName);
    sCmd.setDefaultHandler(unrecognized);
}

void loop() {
    currentMillis = millis();
    if (!isStop) {
        if ((currentMillis - previousMillis) >= 300) {
            previousMillis = currentMillis;
            isStop = true;
            stop();
        }
    }
    sCmd.readSerial();
}

void forward() {
    if (isStop) {
        isStop = false;

        digitalWrite(IN1, LOW);
        digitalWrite(IN2, HIGH);

        digitalWrite(IN3, LOW);
        digitalWrite(IN4, HIGH);

        digitalWrite(IN5, LOW);
        digitalWrite(IN6, HIGH);

        digitalWrite(IN7, LOW);
        digitalWrite(IN8, HIGH);

        //analogWrite(ENA, motor_speed);
        //analogWrite(ENB, motor_speed);
    }
}

void backward() {
    if (isStop) {
        isStop = false;

        digitalWrite(IN1, HIGH);
        digitalWrite(IN2, LOW);

        digitalWrite(IN3, HIGH);
        digitalWrite(IN4, LOW);

        digitalWrite(IN5, HIGH);
        digitalWrite(IN6, LOW);

        digitalWrite(IN7, HIGH);
        digitalWrite(IN8, LOW);

        //analogWrite(ENA, motor_speed);
        //analogWrite(ENB, motor_speed);
    }
}

void turn_left() {
    if (isStop) {
        isStop = false;

        digitalWrite(IN1, HIGH);
        digitalWrite(IN2, LOW);

        digitalWrite(IN3, HIGH);
        digitalWrite(IN4, LOW);

        digitalWrite(IN5, LOW);
        digitalWrite(IN6, HIGH);

        digitalWrite(IN7, LOW);
        digitalWrite(IN8, HIGH);

        //analogWrite(ENA, motor_speed);
        //analogWrite(ENB, motor_speed);
    }
}

void turn_right() {
    isStop = false;

    digitalWrite(IN1, LOW);
    digitalWrite(IN2, HIGH);

    digitalWrite(IN3, LOW);
    digitalWrite(IN4, HIGH);

    digitalWrite(IN5, HIGH);
    digitalWrite(IN6, LOW);

    digitalWrite(IN7, HIGH);
    digitalWrite(IN8, LOW);

    //analogWrite(ENA, motor_speed);
    //analogWrite(ENB, motor_speed);
}

void stop() {
    digitalWrite(IN1, LOW);
    digitalWrite(IN2, LOW);
    digitalWrite(IN3, LOW);
    digitalWrite(IN4, LOW);
    digitalWrite(IN5, LOW);
    digitalWrite(IN6, LOW);
    digitalWrite(IN7, LOW);
    digitalWrite(IN8, LOW);
}

void set_speed() {
    char *arg;

    Serial.println("Setting speed");
    arg = sCmd.next();
    if (arg != NULL) {
        motor_speed = atoi(arg);
        Serial.print("Motor speed is set to: ");
        Serial.println(motor_speed);
    } else {
        Serial.println("No speed argument");
    }
}

void interval() {
    char *arg;
    int interval;
    int i;

    Serial.println("Setting interval");
    arg = sCmd.next();
    if (arg != NULL) {
        interval = atoi(arg);
        Serial.print("Interval is set to: ");
        Serial.println(interval);
        i = 0;
        while (i < (interval * 10)) {
            i++;
            forward();
        }
    } else {
        Serial.println("No interval argument");
    }
}

// This gets set as the default handler, and gets called when no other command matches.
void unrecognized(const char *command) {
    Serial.println("What?");
}

void checkName() {
    Serial.println("&motor-controller*");
}