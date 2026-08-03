#line 1 "D:\\Peronal_Data\\Subject\\Learning\\DAKTNC\\TH\\bc\\TH_DAKTNC\\TH_DAKTNC.ino"
#include <Arduino.h>
#include <HardwareSerial.h>
#include "driver/uart.h"

// =========================
// CẤU HÌNH
// =========================
#define US100_TX 18
#define US100_RX 17
#define S_TX 43
#define S_RX 44

HardwareSerial US100Serial(1); 

#define SOFT_BAUD 9600
#define TIMING_ADJ 1.02
#define BIT_TIME ((1000000UL / SOFT_BAUD) * TIMING_ADJ)
String test;
// =========================
// HÀM TÍNH CRC16 (MODBUS) - QUAN TRỌNG
// =========================

uint16_t CalculateCRC(uint8_t *data, int len);

void SoftUART_SendByte(uint8_t b);

void SoftUART_SendString(const String &s);

void SoftUART_SendWithCRC(String dataPart);

int SoftUART_ReadByte();

bool SoftUART_ReadFrame(String &frame);

int US100_ReadDistance();

void setup();

void loop();

uint16_t CalculateCRC(uint8_t *data, int len) {
  uint16_t crc = 0xFFFF;
  for (int i = 0; i < len; i++) {
    crc ^= (uint16_t)data[i];
    for (int j = 0; j < 8; j++) {
      if ((crc & 1) != 0)
        crc = (crc >> 1) ^ 0xA001;
      else
        crc >>= 1;
    }
  }
  return crc;
}

// =========================
// SOFT UART GỬI CƠ BẢN
// =========================
inline void SoftUART_SendByte(uint8_t b) {
  uint32_t bit_us = BIT_TIME;
  noInterrupts(); 
  digitalWrite(S_TX, LOW);  // Start bit
  delayMicroseconds(bit_us);

  for (uint8_t i = 0; i < 8; i++) {
    digitalWrite(S_TX, (b >> i) & 1);
    delayMicroseconds(bit_us);
  }

  digitalWrite(S_TX, HIGH); // Stop bit
  delayMicroseconds(bit_us);
  interrupts(); 
}

void SoftUART_SendString(const String &s) {
  for (size_t i = 0; i < s.length(); i++) {
    if (digitalRead(S_RX) == LOW) {
      // SoftUART_SendByte('&');
      // return ;
    }
    SoftUART_SendByte(s[i]);
  }
}


// =========================
// GỬI DỮ LIỆU KÈM CRC (MỚI)
// =========================
void SoftUART_SendWithCRC(String dataPart) {
    // 1. Tính CRC cho phần dữ liệu (Ví dụ: "2443")
    uint16_t crc = CalculateCRC((uint8_t*)dataPart.c_str(), dataPart.length());
    
    // 2. Chuyển CRC sang chuỗi Hex 4 ký tự (Ví dụ: "A1B2")
    char crcStr[5];
    sprintf(crcStr, "%04X", crc);
    
    // 3. Đóng gói: @DATA:CRC&
    String packet = "@" + dataPart + ":" + String(crcStr) + "&";
    
    // 4. Gửi đi
    SoftUART_SendString(packet);
}

// =========================
// SOFT UART NHẬN
// =========================
int SoftUART_ReadByte() {
  if (digitalRead(S_RX) == HIGH) return -1; 
  unsigned long start = micros();
  delayMicroseconds(BIT_TIME / 2);
  uint8_t val = 0;
  for (uint8_t i = 0; i < 8; i++) {
    while ((uint32_t)(micros() - start) < (uint32_t)(BIT_TIME * (i + 1.5)));
    val |= (digitalRead(S_RX) << i);
  }
  while ((uint32_t)(micros() - start) < (uint32_t)(BIT_TIME * 9.5));
  return val;
}

bool SoftUART_ReadFrame(String &frame) {
  if (digitalRead(S_RX) == LOW) {
      // đã ẩn
  }
  return false;
}

// =========================
// HARD UART - US100
// =========================
int US100_ReadDistance() {
  US100Serial.write(0x55);
  delay(100);
  if (US100Serial.available() >= 2) {
    byte highB = US100Serial.read();
    byte lowB = US100Serial.read();
    int dist = (highB << 8) + lowB;
    if (dist > 1 && dist < 10000) return dist;
  }
  return -1;
}

// =========================
// SETUP & LOOP
// =========================
void setup() {
  // Serial.begin(9600); // Debug nếu cần
  uart_driver_delete(UART_NUM_0);

  pinMode(US100_TX, OUTPUT);
  pinMode(US100_RX, INPUT_PULLUP);
  pinMode(S_TX, OUTPUT);
  pinMode(S_RX, INPUT_PULLUP);
  digitalWrite(S_TX, HIGH);

  US100Serial.begin(9600, SERIAL_8N1, US100_RX, US100_TX);
}



void loop() {
  static unsigned long lastUS100 = 0;
  static int distance = -1;
  String rx;

  // 1. Kiểm tra ngay dữ liệu test từ WinForm
if (digitalRead(S_RX) == LOW) {
    if (SoftUART_ReadFrame(rx)) {

      // Bỏ @ và &
      rx.replace("@", "");
      rx.replace("&", "");

      // Tìm dấu :

      
      // đã ẩn
      } else {
        SoftUART_SendWithCRC("NO_COLON");
      }
    }
    return;
  }

  // 2. Gửi khoảng cách theo chu kỳ, chỉ khi cờ cho phép
  if (millis() - lastUS100 >= 800) {
    lastUS100 = millis();
    distance = US100_ReadDistance();
    if (digitalRead(S_RX) == LOW){return;}
    if (distance != -1)
      SoftUART_SendWithCRC(String(distance));
    else
      SoftUART_SendWithCRC("ERROR");
  }
}

