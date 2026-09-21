#include <Arduino.h>
#include "US100.h"

int US100_ReadDistance()
{
    // Xóa dữ liệu cũ còn trong buffer
    while (US100Serial.available())
    {
        US100Serial.read();
    }

    // Gửi lệnh đo khoảng cách
    US100Serial.write(0x55);

    // Chờ tối đa 100 ms để nhận đủ 2 byte
    unsigned long startTime = millis();

    while (US100Serial.available() < 2)
    {
        if (millis() - startTime >= 100)
        {
            return -1;  // Timeout
        }
    }

    // Đọc 2 byte
    uint8_t highB = US100Serial.read();
    uint8_t lowB  = US100Serial.read();

    // Ghép 2 byte thành khoảng cách
    int distance = ((uint16_t)highB << 8) | lowB;

    // Kiểm tra khoảng cách hợp lệ
    if (distance >= 2 && distance <= 9999)
    {
        return distance;   // mm
    }

    return -1;
}
