# ESP32-S3 Software UART & PCB Design

![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)
![ESP32-S3](https://img.shields.io/badge/MCU-ESP32--S3-blue)
![Software UART](https://img.shields.io/badge/Communication-Software%20UART-green)
![PCB](https://img.shields.io/badge/Hardware-PCB%20Design-orange)

## Giới thiệu

Đây là dự án thực hành **Embedded Systems**, tập trung vào việc **tự xây dựng Software UART (bit-banging) trên ESP32-S3**, kết hợp với quá trình **thiết kế sơ đồ nguyên lý (Schematic) và mạch in PCB** cho hệ thống.

Thay vì sử dụng trực tiếp UART peripheral có sẵn của ESP32-S3, dự án tự điều khiển GPIO bằng phần mềm để thực hiện quá trình truyền và nhận dữ liệu UART ở mức bit.

Bên cạnh phần firmware, dự án thực hiện đầy đủ quy trình thiết kế phần cứng:

* Thiết kế **Schematic**.
* Lựa chọn và kết nối các linh kiện.
* Thiết kế **PCB layout**.
* Thiết kế đường kết nối giữa ESP32-S3, cảm biến và USB-UART.
* Kiểm tra kết nối và nguyên tắc bố trí mạch.

Hệ thống đo khoảng cách bằng cảm biến **US-100** và ứng dụng **C# WinForms** được sử dụng như một **ứng dụng minh họa cho Software UART**, không phải trọng tâm chính của dự án.

---

## Mục tiêu dự án

### Firmware

* Hiểu nguyên lý hoạt động của giao tiếp UART ở mức bit.
* Tự xây dựng **Software UART TX/RX** bằng GPIO.
* Điều khiển timing của từng bit bằng phần mềm.
* Xử lý Start bit, Data bits và Stop bit.
* Xây dựng giao thức truyền dữ liệu riêng.
* Sử dụng CRC16 để phát hiện lỗi dữ liệu.

### Hardware

* Thiết kế schematic cho hệ thống ESP32-S3.
* Thiết kế PCB dựa trên schematic.
* Bố trí linh kiện và routing các đường tín hiệu.
* Thiết kế đầu nối cho cảm biến và USB-UART.
* Kiểm tra tính nhất quán giữa Schematic và PCB.

---

## Kiến trúc hệ thống

```text
                    ┌─────────────────────┐
                    │       PC / Laptop   │
                    │     C# WinForms     │
                    └──────────┬──────────┘
                               │
                         USB - UART
                               │
                    ┌──────────▼──────────┐
                    │     ESP32-S3        │
                    │                     │
                    │  Software UART      │
                    │  GPIO43 / GPIO44    │
                    │                     │
                    │       +             │
                    │                     │
                    │  Hardware UART      │
                    │  GPIO17 / GPIO18    │
                    └──────────┬──────────┘
                               │
                         Hardware UART
                               │
                    ┌──────────▼──────────┐
                    │       US-100        │
                    │  Ultrasonic Sensor  │
                    └─────────────────────┘
```

Trong hệ thống có hai kênh UART:

| Giao tiếp         | GPIO ESP32-S3   | Phương thức       | Mục đích                                       |
| ----------------- | --------------- | ----------------- | ---------------------------------------------- |
| PC ↔ ESP32-S3     | GPIO43 / GPIO44 | **Software UART** | Kênh giao tiếp chính để kiểm thử Software UART |
| US-100 ↔ ESP32-S3 | GPIO17 / GPIO18 | Hardware UART     | Đọc dữ liệu từ cảm biến                        |

---

# Software UART

## 1. Nguyên lý

Software UART không sử dụng UART peripheral của ESP32-S3.

Thay vào đó, firmware trực tiếp điều khiển GPIO và tạo ra tín hiệu UART bằng phần mềm.

### TX

GPIO được cấu hình Output và thay đổi mức logic theo từng bit:

```text
Idle    Start       Data bits                 Stop
 HIGH     LOW    D0 D1 D2 D3 D4 D5 D6 D7      HIGH
  │        │      │  │  │  │  │  │  │  │       │
  └────────┴──────┴──┴──┴──┴──┴──┴──┴──┴───────┘
```

Firmware phải đảm bảo mỗi bit được giữ trong đúng khoảng thời gian.

### RX

GPIO được cấu hình Input.

Firmware phát hiện cạnh xuống của Start bit, sau đó lấy mẫu tín hiệu tại giữa mỗi khoảng bit để xác định giá trị dữ liệu.

```text
        Start       D0       D1       D2       ...      D7      Stop
          ↓
──────────┐
          └───────┐
                  └──────── ...
             ↑
          Sample
```

---

## 2. UART Configuration

Software UART sử dụng cấu hình:

| Parameter | Value        |
| --------- | ------------ |
| Baud rate | **9600 bps** |
| Data bits | **8**        |
| Parity    | **None**     |
| Stop bits | **1**        |
| Format    | **8N1**      |
| Bit time  | ≈ **104 µs** |

Bit time được tính:

```text
Tbit = 1 / Baudrate

Tbit = 1 / 9600
     ≈ 104 µs
```

Timing là một trong những vấn đề quan trọng nhất của Software UART. Sai lệch timing có thể khiến bên nhận lấy mẫu sai vị trí của bit.

---

# Software UART TX

Quy trình truyền một byte:

1. Đường truyền ở trạng thái Idle HIGH.
2. Kéo GPIO xuống LOW để tạo Start bit.
3. Gửi lần lượt 8 Data bits theo thứ tự LSB trước.
4. Đưa GPIO lên HIGH để tạo Stop bit.
5. Trở về trạng thái Idle.

Ví dụ truyền một byte:

```text
Idle   Start      Data bits                         Stop
 HIGH    LOW    b0 b1 b2 b3 b4 b5 b6 b7             HIGH
   ────────┐   ┌──┐   ┌──────┐   ┌──────┐
           └───┘  └───┘      └───┘      └──────────
```

---

# Software UART RX

Quy trình nhận:

1. Chờ GPIO ở trạng thái Idle HIGH.
2. Phát hiện cạnh xuống.
3. Chờ khoảng `0.5 × Tbit` để kiểm tra Start bit.
4. Lấy mẫu 8 Data bits với khoảng cách `Tbit`.
5. Ghép các bit thành một byte.
6. Kiểm tra Stop bit.
7. Nếu Stop bit không hợp lệ → Frame Error.

---

# Protocol & CRC16

Sau khi Software UART truyền được các byte, firmware sử dụng một protocol đơn giản để đóng gói dữ liệu.

```text
@DATA:CRC&
```

Trong đó:

```text
@       → Start of frame
DATA    → Payload
:       → Separator
CRC     → CRC16
&       → End of frame
```

CRC sử dụng thuật toán **CRC16 Modbus**.

Quá trình xử lý:

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

CRC được sử dụng để phát hiện dữ liệu bị lỗi trong quá trình truyền.

---

# Hardware Design

Một phần quan trọng của dự án là chuyển hệ thống từ kết nối bằng dây rời sang thiết kế mạch điện tử hoàn chỉnh.

Quy trình thiết kế:

```text
Requirements
     │
     ▼
Component Selection
     │
     ▼
Schematic Design
     │
     ▼
ERC Check
     │
     ▼
PCB Layout
     │
     ▼
Routing
     │
     ▼
DRC Check
     │
     ▼
PCB
```

## Schematic

Schematic mô tả toàn bộ kết nối điện giữa:

* ESP32-S3.
* US-100.
* USB-UART interface.
* Các đầu nối.
* Nguồn và GND.
* Các đường Software UART TX/RX.

![Schematic](demo/Schematic.svg)

---

## PCB Design

PCB được thiết kế dựa trên schematic nhằm tạo thành một board mạch có cấu trúc rõ ràng thay vì sử dụng kết nối dây rời.

![PCB](demo/PCB.svg)

Các công việc thực hiện:

* Assign footprint.
* Component placement.
* Routing.
* Ground plane.
* Kiểm tra clearance.
* Kiểm tra DRC.
* Kiểm tra kết nối giữa schematic và PCB.

> PCB trong repository được sử dụng để thể hiện quá trình thiết kế phần cứng của dự án.

---

# Pin Configuration

## Software UART

| ESP32-S3 | Function         | Connected to |
| -------- | ---------------- | ------------ |
| GPIO43   | Software UART TX | USB-UART RX  |
| GPIO44   | Software UART RX | USB-UART TX  |
| GND      | Ground           | USB-UART GND |

## Hardware UART – US-100

| ESP32-S3 | Function | US-100 |
| -------- | -------- | ------ |
| GPIO17   | UART RX  | TX     |
| GPIO18   | UART TX  | RX     |
| GND      | Ground   | GND    |
| 5V / VCC | Power    | VCC    |

---

# Hardware Components

| Component          | Quantity | Function                      |
| ------------------ | -------: | ----------------------------- |
| ESP32-S3           |        1 | Main MCU                      |
| US-100             |        1 | Distance sensor / UART device |
| USB-UART Converter |        1 | PC communication interface    |
| PCB                |        1 | Custom hardware platform      |
| PC / Laptop        |        1 | Test and monitor system       |

---

# Application Demo

US-100 được sử dụng để tạo dữ liệu thực tế cho quá trình kiểm thử Software UART.

Luồng hoạt động:

```text
US-100
  │
  │ Hardware UART
  ▼
ESP32-S3
  │
  │ Generate packet
  │ @DATA:CRC&
  ▼
Software UART
  │
  ▼
USB-UART
  │
  ▼
PC
  │
  ▼
C# WinForms
```

Ứng dụng WinForms có nhiệm vụ hiển thị và trao đổi dữ liệu với ESP32-S3.

Phần này chủ yếu phục vụ **demo và kiểm thử Software UART**.

---

# Project Structure

```text
ESP32S3_Distance_SoftUART/
│
├── demo/
│   ├── 3D.png
│   ├── demo.mp4
│   ├── PCB.svg
│   └── Schematic.svg
│
├── firmware/
│   ├── include/
│   ├── src/
│   └── ...
│
├── pc/
│   └── WinForms/
│
└── README.md
```

---

# Development Environment

| Tool                     | Purpose                |
| ------------------------ | ---------------------- |
| PlatformIO / Arduino IDE | ESP32-S3 firmware      |
| ESP32 Arduino Core       | MCU framework          |
| KiCad                    | Schematic & PCB design |
| C# / .NET WinForms       | PC application         |
| Serial Terminal          | UART testing           |

---

# What I Learned

Thông qua dự án, các nội dung chính được thực hành gồm:

### Embedded Firmware

* GPIO manipulation.
* UART protocol.
* Software UART / Bit-banging.
* UART timing.
* Serial communication.
* CRC16.
* Data framing.
* Error detection.

### Hardware Design

* Schematic design.
* Component selection.
* Footprint assignment.
* PCB layout.
* Signal routing.
* Ground plane.
* ERC / DRC.
* Hardware–firmware integration.

### System Integration

* ESP32-S3 ↔ sensor.
* ESP32-S3 ↔ USB-UART.
* MCU ↔ PC communication.
* Firmware ↔ hardware.
* Testing and debugging.

---

# Demo

[![Watch the video](https://img.youtube.com/vi/TBA_9MDTWb8/hqdefault.jpg)](https://youtube.com/shorts/TBA_9MDTWb8)

---

# References

* [ESP32-S3 Datasheet](https://www.espressif.com/sites/default/files/documentation/esp32-s3_datasheet_en.pdf)
* [ESP32-S3 Technical Reference Manual](https://www.espressif.com/sites/default/files/documentation/esp32-s3_technical_reference_manual_en.pdf)
* [Arduino-ESP32 Documentation](https://docs.espressif.com/projects/arduino-esp32/en/latest/)
* [US-100 Datasheet](https://www.mouser.com/datasheet/2/813/US-100-DS-1218130.pdf)
* [Microsoft .NET Documentation](https://learn.microsoft.com/en-us/dotnet/)
* [Windows Forms Documentation](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/)
* [Modbus CRC16](https://www.modbustools.com/modbus_crc16.htm)

---

# Project Status

**Status:** Completed
**Version:** v1.0
**Last updated:** September 2026

---

## Author

**Gia Bảo**

Embedded Systems / Computer Engineering
