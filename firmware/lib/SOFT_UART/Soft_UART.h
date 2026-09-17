#ifndef SOFT_UART_H
#define SOFT_UART_H

#include <Arduino.h>

// Cấu hình hằng số (Macro)
#ifndef S_TX
  #define S_TX 43 
#endif

#ifndef S_RX
  #define S_RX 44
#endif

#define SOFT_BAUD 9600
#define TIMING_ADJ 1.02
#define BIT_TIME ((1000000UL / SOFT_BAUD) * TIMING_ADJ)

// Khai báo các hàm Soft UART
void SoftUART_SendByte(uint8_t b);
void SoftUART_SendString(const String &s);
void SoftUART_SendWithCRC(String dataPart);
int SoftUART_ReadByte();
bool SoftUART_ReadFrame(String &frame);


#endif // SOFT_UART_H