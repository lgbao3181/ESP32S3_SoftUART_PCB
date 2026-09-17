#include<stdio.h>
#include <stdint.h>

#include "crc16.h"

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

// đã chuẩn