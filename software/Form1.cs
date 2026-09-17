using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace GiaoDien
{
    public partial class Form1 : Form
    {
        string ReceiveData = String.Empty;
        string TranmitData = String.Empty;
        string ReceiveData2 = String.Empty;
        string TranmitData2 = String.Empty;
        int dung = 0;
        bool testOrRead = false;
        Stopwatch sw = new Stopwatch();

        public Form1()
        {
            InitializeComponent();
        }

        // =========================================================
        // 1. HÀM TÍNH CRC16 (MODBUS) - KHỚP VỚI FIRMWARE
        // =========================================================
        private ushort CalculateCRC(byte[] data)
        {
            ushort crc = 0xFFFF;
            for (int i = 0; i < data.Length; i++)
            {
                crc ^= (ushort)data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) != 0)
                        crc = (ushort)((crc >> 1) ^ 0xA001);
                    else
                        crc >>= 1;
                }
            }
            return crc;
        }
        // =========================================================

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            serialPort1.PortName = comboBox1.Text;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            serialPort1.BaudRate = 2400;

            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                comboBox1.Items.Add(port);
            }

            int[] bauds = { 2400, 4800, 9600, 19200, 38400 };
            foreach (int baud in bauds)
            {
                comboBox2.Items.Add(baud.ToString());
            }

            serialPort2.BaudRate = 2400;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (comboBox1.Text == "" | comboBox2.Text == "")
            {
                MessageBox.Show("Select COM Port and Baud. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                try
                {
                    if (serialPort1.IsOpen)
                    {
                        MessageBox.Show("COM Port is connected and ready for use ", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        serialPort1.Open();
                        textBox1.BackColor = Color.Lime;
                        textBox1.Text = "Connecting...";
                        comboBox1.Enabled = false;
                        comboBox2.Enabled = false;
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("COM Port is not found. Please check your COM or Cable. ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (comboBox1.Text == "")
            {
                MessageBox.Show("COM Port is Disconnected. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                try
                {
                    if (serialPort1.IsOpen)
                    {
                        serialPort1.Close();
                        textBox1.BackColor = Color.Red;
                        textBox1.Text = "Disconnected";
                        comboBox1.Enabled = true;
                        comboBox2.Enabled = true;
                    }
                    else
                    {
                        MessageBox.Show("COM Port is Disconnected ", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("COM Port is not found. Please check your COM or Cable. ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // =========================================================
        // 2. SỬA HÀM NHẬN DỮ LIỆU ĐỂ KIỂM TRA CRC
        // =========================================================
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                sw.Reset();
                sw.Start();
                // Đọc gói tin (Dạng mong đợi: @DATA:CRC&)
                string rawPacket = serialPort1.ReadTo("&");
                sw.Stop();

                // Xóa ký tự bắt đầu
                rawPacket = rawPacket.Replace("@", "");

                // Tách dữ liệu và CRC dựa vào dấu hai chấm ':'
                int separatorIndex = rawPacket.LastIndexOf(':');

                if (separatorIndex > 0)
                {
                    string dataPart = rawPacket.Substring(0, separatorIndex); // Phần Dữ liệu
                    string crcPart = rawPacket.Substring(separatorIndex + 1); // Phần CRC nhận được

                    // Tính lại CRC để kiểm tra
                    ushort calculatedCrc = CalculateCRC(Encoding.ASCII.GetBytes(dataPart));
                    string calculatedCrcHex = calculatedCrc.ToString("X4"); // Chuyển sang Hex 4 ký tự

                    if (crcPart == calculatedCrcHex)
                    {
                        // CRC ĐÚNG: Gán dữ liệu sạch vào ReceiveData để xử lý tiếp
                        ReceiveData = dataPart;
                        this.Invoke(testOrRead ? new EventHandler(DoUpDate2) : new EventHandler(DoUpDate1));
                    }
                    else
                    {
                        // CRC SAI: Báo lỗi (hoặc bỏ qua)
                        this.Invoke(new Action(() =>
                        {
                            // Nếu đang test tự động thì báo lỗi ngay
                            if (testOrRead)
                            {
                                MessageBox.Show("Lỗi Checksum CRC! Nhận: " + crcPart + " != Tính: " + calculatedCrcHex, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                testOrRead = false;
                            }
                            else
                            {
                                textBox4.Text = "CRC Error";
                            }
                        }));
                    }
                }
                else
                {
                    // Trường hợp không có CRC 
                    MessageBox.Show("Lỗi: Du lieu nay ko co crc");
                }
            }
            catch (Exception)
            {
                // Xử lý lỗi nếu cần
            }
        }

        private void DoUpDate1(object sender, EventArgs e)
        {
            textBox4.Text = ReceiveData;
        }

        private void DoUpDate2(object sender, EventArgs e)
        {
            dung = 0;
            double percent = 0;
            //textBox8.Text = ReceiveData;

            // So sánh dữ liệu nhận và gửi
            //if (string.Equals(ReceiveData, TranmitData, StringComparison.OrdinalIgnoreCase))
            //{
            //dung++;
            //}
            if (ReceiveData.Length == TranmitData.Length)
            {
                for (int i = 0; i < TranmitData.Length; i++)
                {
                    if (ReceiveData[i] != TranmitData[i])
                    {
                        textBox8.Text = "Loi: " + "nhan:" + ReceiveData[i] + ",goc:" + TranmitData[i] + dung;
                    }
                    else
                    {
                        dung++;
                        //textBox8.Text = "bang"; // Tạm ẩn để đỡ rối
                    }
                }
                textBox9.Text = TranmitData;
                textBox8.Text = ReceiveData;
                percent = ((double)dung / TranmitData.Length) * 100;
            }
            else if (ReceiveData.Length < TranmitData.Length)
            {
                for (int i = 0; i < ReceiveData.Length; i++)
                {
                    string sub = ReceiveData.Substring(i);
                    string sub1 = ReceiveData.Substring(0, ReceiveData.Length - i);

                    if (TranmitData.Contains(sub) || TranmitData.Contains(sub1))
                    {
                        textBox9.Text = TranmitData;
                        textBox8.Text = ReceiveData;

                        string subFinal = TranmitData.Contains(sub) ? sub : sub1;

                        percent = ((double)subFinal.Length / TranmitData.Length) * 100.0;
                        textBox9.Text = TranmitData;
                        textBox8.Text = ReceiveData;
                        break;
                    }
                }
            }
            else if (ReceiveData.Length > TranmitData.Length)
            {
                for (int i = 0; i < TranmitData.Length; i++)
                {
                    string sub = TranmitData.Substring(i);
                    string sub1 = TranmitData.Substring(0, TranmitData.Length - i);

                    if (ReceiveData.Contains(sub) || ReceiveData.Contains(sub1))
                    {
                        textBox9.Text = "SUB " + sub;
                        textBox8.Text = "SUB1" + sub1;
                        string subFinal = ReceiveData.Contains(sub) ? sub : sub1;


                        percent = ((double)subFinal.Length / ReceiveData.Length) * 100.0;

                        textBox9.Text = TranmitData;
                        textBox8.Text = ReceiveData;
                        break;
                    }
                }
            }

            progressBar1.Value = (int)percent;
            textBox7.Text = percent.ToString("0.00") + "%"; // hiển thị 2 chữ số sau dấu thập phân

            MessageBox.Show(
                  "Gui: " + TranmitData + "\n"
              + "Nhan: " + ReceiveData + "\n"
              + "so ky tu Gui: " + TranmitData.Length + "\n"
              + "so ky tu Nhan: " + ReceiveData.Length + "\n"
              + "Dung: " + dung + "\n"
              + "Ty le: " + percent.ToString("0.00") + "%" + "\n"
              + "Thoi Gian: " + sw.Elapsed.TotalMilliseconds.ToString("0.00") + " ms" + "\n"
              + "Toc do: " + (ReceiveData.Length > 0 ? (sw.Elapsed.TotalMilliseconds / ReceiveData.Length).ToString("0.00") : "0") + " ms/kt",
                "Ket qua Test"
            );

            testOrRead = false;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (comboBox1.Text == "")
            {
                MessageBox.Show("Select COM Port. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                try
                {
                    if (serialPort1.IsOpen)
                    {
                        TranmitData = "@l2_of"; // Dữ liệu gốc
                        // Thêm CRC
                        ushort crc = CalculateCRC(Encoding.ASCII.GetBytes(TranmitData.Replace("@", "")));
                        string packet = TranmitData + ":" + crc.ToString("X4") + "&";

                        serialPort1.Write(packet);
                        testOrRead = true;
                    }
                    else
                    {
                        MessageBox.Show("COM Port is Disconnected ", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("COM Port is not found. Please check your COM or Cable. ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void groupBox5_Enter(object sender, EventArgs e) { }

        private void label7_Click(object sender, EventArgs e) { }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            serialPort1.BaudRate = Convert.ToInt32(comboBox2.Text);
        }

        private void groupBox1_Enter(object sender, EventArgs e) { }

        // =========================================================
        // 3. NÚT GỬI TAY (BUTTON 7) CÓ CRC
        // =========================================================
        private void button7_Click(object sender, EventArgs e)
        {
            if (comboBox1.Text == "")
            {
                MessageBox.Show("Select COM Port. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                try
                {
                    if (serialPort1.IsOpen)
                    {
                        TranmitData = textBox5.Text;

                        // Tính toán CRC cho dữ liệu nhập vào
                        ushort crc = CalculateCRC(Encoding.ASCII.GetBytes(TranmitData));

                        // Đóng gói theo định dạng: @DATA:CRC&
                        string packet = "@" + TranmitData + ":" + crc.ToString("X4") + "&";

                        serialPort1.Write(packet);
                        testOrRead = true;
                    }
                    else
                    {
                        MessageBox.Show("COM Port is Disconnected ", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("COM Port is not found. Please check your COM or Cable. ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            DialogResult answer = MessageBox.Show("Do you want to exit the program?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer == DialogResult.Yes)
            {
                if (serialPort1.IsOpen)
                {
                    serialPort1.Close(); // Đóng cổng Serial nếu đang mở
                }
                Application.Exit(); // Thoát chương trình
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                button7.Enabled = true;
                textBox5.ReadOnly = false;
            }
            else
            {
                button7.Enabled = false;
                textBox5.ReadOnly = true;
            }
        }

        private void textBox4_TextChanged(object sender, EventArgs e) { }
        private void label6_Click(object sender, EventArgs e) { }
        private void groupBox4_Enter(object sender, EventArgs e) { }
        private void label13_Click(object sender, EventArgs e) { }
        private void label12_Click(object sender, EventArgs e) { }
        private void textBox5_TextChanged(object sender, EventArgs e) { }
        private void textBox6_TextChanged(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }

        // =========================================================
        // 4. NÚT TEST TỰ ĐỘNG (BUTTON 3) CÓ CRC
        // =========================================================
        private void button3_Click(object sender, EventArgs e)
        {
            if (comboBox1.Text == "")
            {
                MessageBox.Show("Select COM Port. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                try
                {
                    if (serialPort1.IsOpen)
                    {
                        TranmitData =
                            "DATA00001_DATA00002_DATA00003_DATA00004_DATA00005_DATA00006_DATA00007_DATA00008_DATA00009_DATA00010_" +
                            "DATA00011_DATA00012_DATA00013_DATA00014_DATA00015_DATA00016_DATA00017_DATA00018_DATA00019_DATA00020_" +
                            "DATA00021_DATA00022_DATA00023_DATA00024_DATA00025_DATA00026_DATA00027_DATA00028_DATA00029_DATA00030_" +
                            "DATA00031_DATA00032_DATA00033_DATA00034_DATA00035_DATA00036_DATA00037_DATA00038_DATA00039_DATA00040_" +
                            "DATA00041_DATA00042_DATA00043_DATA00044_DATA00045_DATA00046_DATA00047_DATA00048_DATA00049_DATA00050_" +
                            "DATA00051_DATA00052_DATA00053_DATA00054_DATA00055_DATA00056_DATA00057_DATA00058_DATA00059_DATA00060_" +
                            "DATA00061_DATA00062_DATA00063_DATA00064_DATA00065_DATA00066_DATA00067_DATA00068_DATA00069_DATA00070_" +
                            "DATA00071_DATA00072_DATA00073_DATA00074_DATA00075_DATA00076_DATA00077_DATA00078_DATA00079_DATA00080_" +
                            "DATA00081_DATA00082_DATA00083_DATA00084_DATA00085_DATA00086_DATA00087_DATA00088_DATA00089_DATA00090_" +
                            "DATA00091_DATA00092_DATA00093_DATA00094_DATA00095_DATA00096_DATA00097_DATA00098_DATA00099_DATA00100_";

                        // Tính CRC cho chuỗi dài
                        ushort crc = CalculateCRC(Encoding.ASCII.GetBytes(TranmitData));

                        // Đóng gói
                        string packet = "@" + TranmitData + ":" + crc.ToString("X4") + "&";

                        serialPort1.Write(packet);
                        testOrRead = true;
                    }
                    else
                    {
                        MessageBox.Show("COM Port is Disconnected ", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("COM Port is not found. Please check your COM or Cable. ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
// 
