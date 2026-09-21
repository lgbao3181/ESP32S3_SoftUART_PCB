# Dự án ESP32S3_Distance_SoftUART

![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)
![ESP32-S3](https://img.shields.io/badge/MCU-ESP32--S3-blue)
![SOFT\_UART](https://img.shields.io/badge/Communication-Software%20UART-green)
![PCB](https://img.shields.io/badge/Hardware-PCB%20Design-orange)
![.NET WinForms](https://img.shields.io/badge/PC%20App-.NET%20WinForms-darkblue)

## Giới thiệu

Đây là một dự án Embedded Systems tập trung vào việc **tự xây dựng Software UART (bit-banging) trên ESP32-S3**, kết hợp với quá trình **thiết kế sơ đồ nguyên lý (Schematic) và mạch in PCB**.

Thay vì sử dụng trực tiếp UART peripheral có sẵn của ESP32-S3 cho kênh giao tiếp với PC, dự án tự điều khiển GPIO bằng phần mềm để thực hiện quá trình truyền và nhận dữ liệu UART ở mức bit. Firmware chịu trách nhiệm xử lý timing, Start bit, Data bits và Stop bit của khung UART.

Bên cạnh phần firmware, dự án thực hiện quá trình thiết kế phần cứng từ **Schematic đến PCB**, bao gồm lựa chọn linh kiện, cấu hình chân, bố trí linh kiện, routing và kiểm tra thiết kế.

ESP32-S3 sử dụng **UART phần cứng** để giao tiếp với cảm biến siêu âm US-100, đồng thời sử dụng **Software UART** trên GPIO43/GPIO44 để giao tiếp với PC thông qua bộ chuyển đổi USB–UART.

Dữ liệu khoảng cách được đóng gói theo một định dạng khung tự định nghĩa và sử dụng **CRC16 (Modbus)** để kiểm tra lỗi trong quá trình truyền.

Ứng dụng **C# WinForms** được sử dụng làm giao diện phía PC để giám sát và trao đổi dữ liệu với ESP32-S3.

---

## Video minh họa



https://github.com/user-attachments/assets/1926875d-f966-454c-846d-3aac775d518c



---

## Sơ đồ nguyên lý

![Sơ đồ nguyên lý](demo/Schematic.svg)

Sơ đồ trên thể hiện hai tuyến giao tiếp UART độc lập trên ESP32-S3:

* **UART mềm (Software UART)**: GPIO43/GPIO44 ↔ USB–UART Converter ↔ PC. Đây là tuyến được tự xây dựng bằng phương pháp **bit-banging**.
* **UART phần cứng (Hardware UART)**: GPIO17/GPIO18 ↔ US-100. Tuyến này sử dụng peripheral UART có sẵn của ESP32-S3.

---

## Sơ đồ mạch in (PCB) minh họa

![Sơ đồ PCB minh họa](demo/PCB.svg)

PCB được thiết kế dựa trên sơ đồ nguyên lý nhằm thể hiện quá trình chuyển từ thiết kế mạch điện sang bố trí mạch in.

Thiết kế bao gồm:

* Bố trí module ESP32-S3.
* Đầu nối cho US-100.
* Đầu nối cho USB–UART Converter.
* Routing các đường tín hiệu.
* Kết nối nguồn và GND.
* Kiểm tra kết nối giữa Schematic và PCB.

> Hình ảnh PCB trong thư mục `demo` dùng để minh họa thiết kế và bố trí mạch, không phải file Gerber sản xuất.

---

## Thành phần phần cứng

| Thành phần              | Số lượng | Vai trò                                                   |
| ----------------------- | -------: | --------------------------------------------------------- |
| ESP32-S3                |        1 | MCU trung tâm, chạy firmware và Software UART             |
| Cảm biến siêu âm US-100 |        1 | Đo khoảng cách, giao tiếp với ESP32-S3 qua UART phần cứng |
| Bộ chuyển đổi USB–UART  |        1 | Cầu nối giữa Software UART của ESP32-S3 và PC             |
| PC / Laptop             |        1 | Chạy ứng dụng WinForms để giám sát và giao tiếp           |

---

## Cấu hình chân

### US-100 – ESP32-S3 (UART phần cứng)

| Chân ESP32-S3 | Chức năng | Chân US-100 | Ghi chú                                   |
| ------------- | --------- | ----------- | ----------------------------------------- |
| **GPIO17**    | UART RX   | **TX**      | ESP32-S3 nhận dữ liệu từ US-100           |
| **GPIO18**    | UART TX   | **RX**      | ESP32-S3 gửi lệnh tới US-100              |
| **GND**       | Mass      | **GND**     | **Bắt buộc phải nối chung**               |
| **5V / VCC**  | Nguồn     | **VCC**     | Cấp nguồn theo thông số của module US-100 |

### UART mềm – PC

| Chân ESP32-S3 | Chức năng        | USB–UART Converter | Mục đích                    |
| ------------- | ---------------- | ------------------ | --------------------------- |
| **GPIO43**    | Software UART TX | **RX**             | ESP32-S3 gửi dữ liệu lên PC |
| **GPIO44**    | Software UART RX | **TX**             | ESP32-S3 nhận dữ liệu từ PC |
| **GND**       | Mass             | **GND**            | **Bắt buộc phải nối chung** |

---

## Software UART tự xây dựng — phần trọng tâm của dự án

Do một kênh UART phần cứng được sử dụng để giao tiếp với US-100, dự án tự xây dựng một **Software UART (bit-banging)** trên GPIO43/GPIO44 để tạo thêm một kênh giao tiếp độc lập với PC.

Mục tiêu của phần này là thực hành và hiểu nguyên lý hoạt động của UART ở mức bit, thay vì chỉ sử dụng peripheral UART hoặc thư viện có sẵn.

### 1. Nguyên lý bit-banging

Software UART không sử dụng bộ ngoại vi UART chuyên dụng của ESP32-S3. Thay vào đó, firmware trực tiếp điều khiển mức logic HIGH/LOW trên GPIO theo timing của từng bit.

* **TX (phát)**: GPIO được cấu hình Output và thay đổi mức logic theo từng bit của khung UART.
* **RX (thu)**: GPIO được cấu hình Input và lấy mẫu tín hiệu tại các thời điểm xác định để xác định giá trị của từng bit.

### 2. Thông số khung truyền

* Tốc độ baud: **9600 bps**
* Định dạng: **8N1**

  * 8 bit dữ liệu
  * Không Parity
  * 1 Stop bit
* *Bit time*:

```text
Tbit = 1 / 9600
     ≈ 104 µs
```

*Bit time* là khoảng thời gian của một bit. Việc đảm bảo timing chính xác là yếu tố quan trọng để Software UART có thể truyền và nhận dữ liệu ổn định.

### 3. Quy trình phát (TX) do phần mềm điều khiển

1. Đưa đường truyền về trạng thái Idle HIGH.
2. Kéo GPIO xuống LOW trong một *bit time* để tạo **Start bit**.
3. Lần lượt truyền 8 bit dữ liệu.
4. Mỗi bit được giữ trong một *bit time*.
5. Đưa GPIO lên HIGH để tạo **Stop bit**.
6. Đường truyền trở về trạng thái Idle.

### 4. Quy trình thu (RX) do phần mềm điều khiển

1. Theo dõi GPIO ở trạng thái Idle HIGH.
2. Phát hiện cạnh xuống để xác định **Start bit**.
3. Chờ khoảng nửa *bit time* để lấy mẫu tại giữa Start bit.
4. Tiếp tục lấy mẫu mỗi *bit time* để nhận 8 bit dữ liệu.
5. Ghép các bit đã nhận thành một byte.
6. Kiểm tra **Stop bit**.
7. Nếu Stop bit không hợp lệ, dữ liệu được xem là lỗi khung (**Frame Error**).

### 5. Định dạng gói tin và CRC16

Sau khi Software UART truyền và nhận được các byte dữ liệu, firmware đóng gói dữ liệu khoảng cách theo định dạng:

```text
@DATA:CRC&
```

Trong đó:

* `@` — Ký tự bắt đầu khung.
* `DATA` — Giá trị khoảng cách.
* `:` — Ký tự phân cách.
* `CRC` — Mã kiểm tra **CRC16 (Modbus)**.
* `&` — Ký tự kết thúc khung.

CRC16 được tính trên phần `DATA`.

Bên nhận sẽ tính lại CRC16 và so sánh với giá trị CRC nhận được:

```text
DATA
  │
  ▼
Tính CRC16
  │
  ▼
@DATA:CRC&
  │
  ▼
Software UART
  │
  ▼
Nhận dữ liệu
  │
  ▼
Kiểm tra CRC
  │
  ├── Hợp lệ   → Chấp nhận dữ liệu
  │
  └── Không hợp lệ → CRC_FAIL
```

### 6. So sánh UART phần cứng và Software UART

| Tiêu chí            | UART phần cứng                     | Software UART            |
| ------------------- | ---------------------------------- | ------------------------ |
| Bộ điều khiển       | Peripheral UART tích hợp trong MCU | Phần mềm điều khiển GPIO |
| Timing              | Được phần cứng xử lý               | Phụ thuộc vào firmware   |
| Tải CPU             | Thấp                               | Cao hơn                  |
| Độ phụ thuộc timing | Thấp                               | Cao                      |
| Trong dự án         | Giao tiếp với US-100               | Giao tiếp với PC         |

---

## Kiến trúc hệ thống tổng quan

```text
┌──────────────────────┐
│    Ứng dụng WinForms │
│     (PC / Laptop)    │
└──────────┬───────────┘
           │
           │ Software UART
           │ @DATA:CRC&
           ▼
┌──────────────────────┐
│       ESP32-S3       │
│                      │
│ ┌──────────────────┐ │
│ │  Software UART   │ │
│ │     + CRC16      │ │
│ └────────┬─────────┘ │
│          │           │
│ ┌────────▼─────────┐ │
│ │  UART phần cứng  │ │
│ └────────┬─────────┘ │
└──────────┼───────────┘
           │
           │ UART
           ▼
┌──────────────────────┐
│        US-100        │
│    Cảm biến siêu âm  │
└──────────────────────┘
```

### Quy trình hoạt động

1. **Khởi tạo:** cấu hình UART phần cứng cho US-100 và Software UART trên GPIO43/GPIO44.
2. **Đo khoảng cách:** ESP32-S3 gửi lệnh `0x55` tới US-100 và đọc dữ liệu phản hồi.
3. **Đóng gói dữ liệu:** tính CRC16 (Modbus) và tạo khung `@DATA:CRC&`.
4. **Truyền dữ liệu:** ESP32-S3 gửi khung dữ liệu lên PC thông qua Software UART.
5. **Giao tiếp với PC:** WinForms nhận, phân tích và hiển thị dữ liệu.
6. **Kiểm tra lỗi:** dữ liệu nhận được được kiểm tra CRC trước khi chấp nhận.

---

## Bắt đầu

### Yêu cầu phần mềm

| Phần mềm                 | Phiên bản | Mục đích                                |
| ------------------------ | --------- | --------------------------------------- |
| Arduino IDE / PlatformIO | Mới nhất  | Phát triển và nạp firmware cho ESP32-S3 |
| ESP32 Arduino Core       | Mới nhất  | Hỗ trợ ESP32-S3                         |
| C# / .NET WinForms       | .NET 6+   | Ứng dụng phía PC                        |
| Serial Terminal          | Bất kỳ    | Kiểm thử và gỡ lỗi UART                 |

### Lắp phần cứng

* Nối US-100 với **GPIO17 (RX)** và **GPIO18 (TX)**.
* Nối USB–UART Converter với **GPIO43 (TX)** và **GPIO44 (RX)**.
* Nối chung **GND** giữa ESP32-S3, US-100 và USB–UART Converter.
* Đảm bảo mức điện áp logic của USB–UART Converter tương thích với ESP32-S3.
* Cấp nguồn phù hợp cho US-100.

### Cài đặt

1. Sao chép repository:

```bash
git clone <repository-url>
cd ESP32S3_Distance_SoftUART
```

2. Mở thư mục firmware bằng **Arduino IDE hoặc PlatformIO**.

3. Chọn đúng board **ESP32-S3**.

4. Chọn đúng cổng serial của ESP32-S3.

5. Biên dịch và nạp firmware.

6. Kết nối USB–UART Converter với PC.

7. Mở ứng dụng WinForms hoặc Serial Terminal để kiểm tra dữ liệu.

8. Khởi động hệ thống. ESP32-S3 sẽ đọc dữ liệu từ US-100 và truyền dữ liệu lên PC thông qua Software UART.

### Kiểm thử giao tiếp

Có thể sử dụng Serial Terminal hoặc ứng dụng WinForms để kiểm tra Software UART.

Các nội dung cần kiểm tra:

* Baud rate **9600 bps**.
* Khung UART **8N1**.
* Dữ liệu TX/RX.
* Định dạng `@DATA:CRC&`.
* Kiểm tra CRC16.
* Phát hiện Frame Error.
* Giao tiếp giữa ESP32-S3 và PC.

---

## Tài liệu tham khảo

* [Datasheet ESP32-S3](https://www.espressif.com/sites/default/files/documentation/esp32-s3_datasheet_en.pdf)
* [Tài liệu tham chiếu kỹ thuật ESP32-S3](https://www.espressif.com/sites/default/files/documentation/esp32-s3_technical_reference_manual_en.pdf)
* [Tài liệu Arduino-ESP32](https://docs.espressif.com/projects/arduino-esp32/en/latest/)
* [Datasheet cảm biến siêu âm US-100](https://www.mouser.com/datasheet/2/813/US-100-DS-1218130.pdf)
* [Tài liệu Microsoft .NET](https://learn.microsoft.com/en-us/dotnet/)
* [Tài liệu Windows Forms](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/)
* [CRC16 Modbus](https://www.modbustools.com/modbus_crc16.htm)

---

## Trạng thái dự án

* **Trạng thái:** Hoàn thành
* **Phiên bản:** v1.0
* **Cập nhật lần cuối:** Tháng 9/2026

---

## Liên hệ

**Gia Bảo**

📧 Email: *[your-email@example.com](mailto:your-email@example.com)*
🐙 GitHub: *your-github-profile*
