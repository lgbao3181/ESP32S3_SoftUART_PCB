
#include <Arduino.h>
#include <HardwareSerial.h>
#include "driver/uart.h"
#include "US100.h"
#include "crc16.h"
#include "Soft_UART.h"

// =========================
// CẤU HÌNH
// =========================
#define US100_TX 18
#define US100_RX 17
#define S_TX 43
#define S_RX 44

HardwareSerial US100Serial(1); 


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
      int pos = rx.indexOf(':');
      if (pos != -1) {
        String dataPart = rx.substring(0, pos);
        String crcPart  = rx.substring(pos + 1);

        uint16_t crc_recv = (uint16_t) strtol(crcPart.c_str(), NULL, 16);
        uint16_t crc_calc = CalculateCRC((uint8_t*)dataPart.c_str(), dataPart.length());

        if (crc_calc == crc_recv) {
          // CRC OK → gửi phản hồi lại bằng CRC mới
          SoftUART_SendWithCRC(dataPart);
        } else {
          // CRC FAIL
          SoftUART_SendWithCRC("CRC_FAIL");
        }
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

