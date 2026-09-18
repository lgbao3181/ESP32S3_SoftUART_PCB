# ESP32-S3 Software UART & PCB Design

![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)
![ESP32-S3](https://img.shields.io/badge/MCU-ESP32--S3-blue)
![Software UART](https://img.shields.io/badge/Communication-Software%20UART-green)
![PCB](https://img.shields.io/badge/Hardware-PCB%20Design-orange)
![.NET WinForms](https://img.shields.io/badge/PC%20App-.NET%20WinForms-darkblue)

## Project Overview

A hands-on embedded systems project focused on designing and implementing a **Software UART (bit-banging)** on the ESP32-S3, together with the design of the corresponding **electronic schematic and PCB**.

Instead of using the ESP32-S3's built-in UART peripheral for the PC communication channel, this project implements UART communication directly in software using ordinary GPIO pins. The firmware handles the UART timing, start bit, data bits, stop bit, and byte-level transmission/reception.

The hardware side of the project follows the process from **schematic design to PCB layout**, including component selection, pin assignment, signal routing, and PCB design.

The **US-100 ultrasonic sensor** and **C# WinForms application** are used as practical components for demonstrating and testing the Software UART communication.

---

## Video Demonstrations

https://github.com/user-attachments/assets/TBA_9MDTWb8

*Software UART communication and distance measurement demonstration*

---

## Project Schematic

![Schematic diagram](demo/Schematic.svg)

The schematic contains the ESP32-S3, US-100 sensor, USB-UART interface, power connections, and communication signals.

The system uses two independent UART communication channels:

* **Software UART:** GPIO43/GPIO44 ↔ USB-UART Converter ↔ PC
* **Hardware UART:** GPIO17/GPIO18 ↔ US-100

The Software UART channel is the main focus of the project.

---

## PCB Design

![PCB Design](demo/PCB.svg)

The PCB is designed from the schematic to provide a dedicated hardware platform for the ESP32-S3 and communication interfaces.

The PCB design process includes:

* Component and footprint selection
* Component placement
* Signal routing
* Ground plane
* Design Rule Check (DRC)
* Schematic-to-PCB consistency checking

The PCB is intended to demonstrate the complete workflow from **electrical schematic to physical board layout**.

---

## Hardware Components

| Component          | Quantity | Purpose                                                     |
| ------------------ | -------- | ----------------------------------------------------------- |
| ESP32-S3           | 1        | Main microcontroller running the Software UART              |
| US-100             | 1        | Ultrasonic sensor used for UART communication demonstration |
| USB-UART Converter | 1        | Interface between Software UART and PC                      |
| Custom PCB         | 1        | Hardware platform designed for the project                  |
| PC / Laptop        | 1        | Testing and monitoring Software UART communication          |

---

## Pin Configuration

### Software UART – PC

| ESP32-S3 Pin | Function         | USB-UART Converter | Notes                          |
| ------------ | ---------------- | ------------------ | ------------------------------ |
| **GPIO43**   | Software UART TX | RX                 | ESP32-S3 transmits data to PC  |
| **GPIO44**   | Software UART RX | TX                 | ESP32-S3 receives data from PC |
| **GND**      | Ground           | GND                | **Must be connected!**         |

### Hardware UART – US-100

| ESP32-S3 Pin | Function | US-100 Pin | Notes                                   |
| ------------ | -------- | ---------- | --------------------------------------- |
| **GPIO17**   | UART RX  | TX         | ESP32-S3 receives sensor data           |
| **GPIO18**   | UART TX  | RX         | ESP32-S3 sends measurement command      |
| **GND**      | Ground   | GND        | **Must be connected!**                  |
| **5V / VCC** | Power    | VCC        | Supply according to sensor requirements |

---

## Software UART

### UART Configuration

The Software UART is implemented using GPIO bit-banging with the following configuration:

| Parameter | Value        |
| --------- | ------------ |
| Baud Rate | **9600 bps** |
| Data Bits | **8**        |
| Parity    | **None**     |
| Stop Bits | **1**        |
| Format    | **8N1**      |
| Bit Time  | **≈ 104 µs** |

### UART Frame

```text
Idle   Start      Data Bits                         Stop
 HIGH    LOW    D0 D1 D2 D3 D4 D5 D6 D7             HIGH
  │       │      │  │  │  │  │  │  │  │              │
  └───────┴──────┴──┴──┴──┴──┴──┴──┴──┴──────────────┘
```

### TX Operation

The Software UART transmitter directly controls the GPIO output:

1. Keep the line HIGH during the idle state.
2. Pull the GPIO LOW to generate the start bit.
3. Transmit 8 data bits, LSB first.
4. Pull the GPIO HIGH for the stop bit.
5. Return to the idle state.

### RX Operation

The Software UART receiver monitors the GPIO input:

1. Wait for the falling edge of the start bit.
2. Wait approximately half a bit time.
3. Sample the start bit.
4. Sample each data bit at one-bit intervals.
5. Assemble the received bits into a byte.
6. Verify the stop bit.

Because the UART timing is controlled by software, accurate timing is important for reliable communication.

---

## Communication Protocol

After receiving or generating UART bytes, the firmware uses a simple data frame:

```text
@DATA:CRC&
```

Where:

* `@` — Start of frame
* `DATA` — Payload
* `:` — Separator
* `CRC` — CRC16 value
* `&` — End of frame

CRC16 is used to detect corrupted data during transmission.

```text
DATA
  │
  ▼
CRC16 Calculation
  │
  ▼
@DATA:CRC&
  │
  ▼
Software UART TX
  │
  ▼
Software UART RX
  │
  ▼
CRC Verification
  │
  ├── Valid   → Accept data
  │
  └── Invalid → CRC_FAIL
```

---

## System Architecture

```text
┌──────────────────────┐
│      PC / Laptop     │
│      C# WinForms     │
└──────────┬───────────┘
           │
       USB-UART
           │
           ▼
┌──────────────────────┐
│       ESP32-S3       │
│                      │
│  Software UART       │
│  GPIO43 / GPIO44     │
│                      │
│  Hardware UART       │
│  GPIO17 / GPIO18     │
└──────────┬───────────┘
           │
      Hardware UART
           │
           ▼
┌──────────────────────┐
│        US-100        │
│  Ultrasonic Sensor   │
└──────────────────────┘
```

---

## Getting Started

### Software Prerequisites

| Software                 | Version | Purpose                       |
| ------------------------ | ------- | ----------------------------- |
| PlatformIO / Arduino IDE | Latest  | ESP32-S3 firmware development |
| ESP32 Arduino Core       | Latest  | ESP32-S3 hardware support     |
| KiCad                    | 9.x+    | Schematic and PCB design      |
| C# / .NET                | .NET 6+ | PC monitoring application     |
| Serial Terminal          | Any     | UART testing and debugging    |

### Installation

1. Clone the repository:

```bash
git clone <repository-url>
cd ESP32S3_Distance_SoftUART
```

2. Open the firmware project using **PlatformIO** or **Arduino IDE**.

3. Select the correct **ESP32-S3** board and serial port.

4. Connect the hardware according to the pin configuration above.

5. Build and upload the firmware.

6. Connect the USB-UART converter to the PC.

7. Open the WinForms application or a serial terminal to monitor the Software UART communication.

---

## Testing

The Software UART can be tested using a USB-UART converter and a serial terminal.

The main points verified during testing are:

* Correct UART TX waveform.
* Correct UART RX sampling.
* 9600-bps communication.
* 8N1 frame format.
* Correct data framing.
* CRC16 validation.
* Communication between ESP32-S3 and PC.

---

## Resources

* [ESP32-S3 Datasheet](https://www.espressif.com/sites/default/files/documentation/esp32-s3_datasheet_en.pdf)
* [ESP32-S3 Technical Reference Manual](https://www.espressif.com/sites/default/files/documentation/esp32-s3_technical_reference_manual_en.pdf)
* [Arduino-ESP32 Documentation](https://docs.espressif.com/projects/arduino-esp32/en/latest/)
* [US-100 Datasheet](https://www.mouser.com/datasheet/2/813/US-100-DS-1218130.pdf)
* [KiCad Documentation](https://docs.kicad.org/)
* [Microsoft .NET Documentation](https://learn.microsoft.com/en-us/dotnet/)
* [Windows Forms Documentation](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/)
* [Modbus CRC16](https://www.modbustools.com/modbus_crc16.htm)

---

## Project Status

* **Status**: Complete
* **Version**: v1.0
* **Last Updated**: September 2026

---

## Contact

**Gia Bảo**

📧 Email: *[your-email@example.com](mailto:your-email@example.com)*
🐙 GitHub: *your-github-profile*
