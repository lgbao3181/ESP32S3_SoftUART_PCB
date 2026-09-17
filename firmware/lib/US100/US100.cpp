#include<stdio.h>
#include<stdint.h>
#include <Arduino.h>
#include "US100.h"


int US100_ReadDistance()
{
    US100Serial.write(0x55);

    delay(100);

    if (US100Serial.available() >= 2)
    {
        uint8_t highB = US100Serial.read();
        uint8_t lowB  = US100Serial.read();

        int dist = ((int)highB << 8) | lowB;

        if (dist > 1 && dist < 10000)
        {
            return dist;
        }
    }

    return -1;
}

// da chuan