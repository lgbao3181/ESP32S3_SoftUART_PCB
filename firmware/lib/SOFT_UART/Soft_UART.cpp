#include <Arduino.h>
#include "crc16.h"
#include "Soft_UART.h"



/// =========================
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
    int c = SoftUART_ReadByte();
    if (c == '@') { 
      static char buf[2048];
      uint16_t idx = 0;
      buf[idx++] = '@';
      unsigned long t = millis();
      while (millis() - t < 5000 && idx < sizeof(buf) - 1) {
        c = SoftUART_ReadByte();
        if (c == -1) continue;
        buf[idx++] = (char)c;
        if (c == '&') { 
          buf[idx] = '\0';
          frame = String(buf);
          return true;
        }
      }
      buf[idx] = '\0';
      frame = String(buf);
      return false;
    }
  }
  return false;
}






