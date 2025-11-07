using System;
using System.Drawing;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;
using Modbus.Data;
using Modbus.Device;

namespace WinFormsApp2
{
    public partial class Form1 : Form
    {
        private Thread slaveThread;
        private ModbusSerialSlave slave;
        private DataStore store;
        private bool running = false;
        private SerialPort port;
        private bool updatingUI = false;
        private readonly object storeLock = new object();

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.FormClosing += OnFormClosing;
        }

        // === Bit0 (Program Price Update Available) Setleme Fonksiyonu ===
        private void WriteProgramPriceUpdateFlag()
        {
            if (store == null) return;
            lock (storeLock)
            {
                ushort status = store.HoldingRegisters[12];
                status |= 0x0001; // Bit0 set
                store.HoldingRegisters[12] = status;
            }
            ForceSlaveRefresh();
            // 🔥 UI hemen güncellensin
            BeginInvoke(new Action(UpdateUIFromRegisters));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboPort.Items.Clear();
            comboPort.Items.AddRange(SerialPort.GetPortNames());
            if (comboPort.Items.Count > 0)
                comboPort.SelectedIndex = 0;

            comboBaud.SelectedItem ??= "9600";
            comboDataBits.SelectedItem ??= "8";
            comboStopBits.SelectedItem ??= "One";
            comboParity.SelectedItem ??= "None";

            // tek word yazılanlar
            numDiscount.ValueChanged += (s, ev) => WriteAndNotify(15, (ushort)numDiscount.Value);
            numPaidAmount.ValueChanged += (s, ev) =>
            {
                if (updatingUI) return;

                float paidFloat = ToPaidAmountFloat(numPaidAmount.Value);
                WriteAndNotifyFloat(13, paidFloat);
            };
            numCurrency.ValueChanged += (s, ev) => WriteAndNotify(21, (ushort)numCurrency.Value);

            // program fiyatları
            for (int i = 0; i < numProgramPrices.Length; i++)
            {
                int idx = i;
                numProgramPrices[i].ValueChanged += (s, ev) =>
                {
                    if (updatingUI) return;

                    // store hazırsa hemen setle, değilse açılışta işaretle
                    if (store != null)
                        WriteProgramPriceUpdateFlag();
                    else
                        numProgramPrices[idx].Tag = true;
                };
            }

            // extra modifiers
            numExtraRinse.ValueChanged += (s, ev) =>
            {
                if (!updatingUI && store != null)
                    WriteAndNotify32(63, (uint)numExtraRinse.Value);
            };
            numExtraSoap.ValueChanged += (s, ev) =>
            {
                if (!updatingUI && store != null)
                    WriteAndNotify32(65, (uint)numExtraSoap.Value);
            };
            numExtraPrewash.ValueChanged += (s, ev) =>
            {
                if (!updatingUI && store != null)
                    WriteAndNotify32(67, (uint)numExtraPrewash.Value);
            };
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            if (running)
            {
                MessageBox.Show("Zaten bağlı.", "Uyarı");
                return;
            }

            try
            {
                string portName = comboPort.SelectedItem?.ToString() ?? "COM1";
                int baudRate = int.Parse(comboBaud.SelectedItem.ToString());
                int dataBits = int.Parse(comboDataBits.SelectedItem.ToString());
                StopBits stopBits = (StopBits)Enum.Parse(typeof(StopBits), comboStopBits.SelectedItem.ToString());
                Parity parity = (Parity)Enum.Parse(typeof(Parity), comboParity.SelectedItem.ToString());

                StartModbusSlave(portName, parity, dataBits, stopBits, baudRate);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bağlantı başlatılamadı: {ex.Message}", "Hata");
            }
        }

        private void StartModbusSlave(string portName, Parity parity, int dataBits, StopBits stopBits, int baudRate)
        {
            try
            {
                port = new SerialPort(portName)
                {
                    BaudRate = baudRate,
                    Parity = parity,
                    DataBits = dataBits,
                    StopBits = stopBits
                };
                port.Open();

                store = DataStoreFactory.CreateDefaultDataStore();

                // başlangıç register’ları
                store.HoldingRegisters[1] = 0;
                store.HoldingRegisters[7] = 0;
                store.HoldingRegisters[9] = 0;
                store.HoldingRegisters[11] = 0;
                store.HoldingRegisters[12] = 0;
                store.HoldingRegisters[13] = 0;
                store.HoldingRegisters[14] = 0;
                store.HoldingRegisters[15] = 0;
                store.HoldingRegisters[17] = 0;
                store.HoldingRegisters[19] = 0;
                store.HoldingRegisters[21] = 0;

                // program fiyatlarını başlat
                for (int i = 0; i < 20; i++)
                {
                    int reg = 23 + (i * 2);
                    WriteU32(reg, (uint)numProgramPrices[i].Value);
                }

                // modifiers
                WriteU32(63, (uint)numExtraRinse.Value);
                WriteU32(65, (uint)numExtraSoap.Value);
                WriteU32(67, (uint)numExtraPrewash.Value);

                slave = ModbusSerialSlave.CreateRtu(1, port);
                slave.DataStore = store;

                // gelen yazma komutlarını dinle
                slave.ModbusSlaveRequestReceived += (sender, args) =>
                {
                    if (args.Message.FunctionCode == 6 || args.Message.FunctionCode == 16)
                        BeginInvoke(new Action(UpdateUIFromRegisters));
                };

                slave.DataStore.DataStoreWrittenTo += (s, e) =>
                {
                    BeginInvoke(new Action(UpdateUIFromRegisters));
                };

                running = true;
                slaveThread = new Thread(() =>
                {
                    while (running)
                    {
                        try
                        {
                            slave.Listen();
                            Thread.Sleep(5);
                        }
                        catch { }
                    }
                })
                { IsBackground = true };
                slaveThread.Start();

                // Eğer bağlantıdan önce program fiyatı değiştiyse şimdi Bit0 setle
                for (int i = 0; i < numProgramPrices.Length; i++)
                {
                    if (numProgramPrices[i].Tag is bool changed && changed)
                    {
                        WriteProgramPriceUpdateFlag();
                        numProgramPrices[i].Tag = false;
                    }
                }

                MessageBox.Show($"Bağlantı açıldı: {portName}", "Bilgi");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bağlantı hatası: {ex.Message}", "Hata");
            }
        }

        private void WriteAndNotify(int reg, ushort value)
        {
            if (store == null) return;
            lock (storeLock)
            {
                store.HoldingRegisters[reg] = value;
            }
            ForceSlaveRefresh();
        }

        private void WriteAndNotify32(int reg, uint value)
        {
            if (store == null) return;
            lock (storeLock)
            {
                if (reg + 1 < store.HoldingRegisters.Count)
                {
                    ushort low = (ushort)(value & 0xFFFF);
                    ushort high = (ushort)(value >> 16);
                    store.HoldingRegisters[reg] = low;
                    Thread.Sleep(5);
                    store.HoldingRegisters[reg + 1] = high;
                }
            }
            ForceSlaveRefresh();
        }

        private void WriteAndNotifyFloat(int reg, float value)
        {
            uint raw = unchecked((uint)BitConverter.SingleToInt32Bits(value));
            WriteAndNotify32(reg, raw);
        }

        private void ForceSlaveRefresh()
        {
            if (slave == null || store == null) return;
            try
            {
                ushort tmp = store.HoldingRegisters[2];
                store.HoldingRegisters[2] = (ushort)(tmp ^ 0x0001);
                Thread.Sleep(5);
                store.HoldingRegisters[2] = tmp;
            }
            catch { }
        }

        // === UI Güncellemesi ===
        private void UpdateUIFromRegisters()
        {
            if (store == null) return;
            updatingUI = true;

            try
            {
                ushort ctrl = store.HoldingRegisters[1];

                // 🟢 Master “Program Price Updated” (Bit0) set ederse -> Payment Status Bit0 clear
                bool programPriceUpdated = (ctrl & 0x0001) != 0;
                if (programPriceUpdated)
                {
                    lock (storeLock)
                    {
                        // 🔹 Önce Payment Status Bit0'ı sıfırla
                        ushort paymentStatus = store.HoldingRegisters[12];
                        paymentStatus = (ushort)(paymentStatus & ~0x0001);
                        store.HoldingRegisters[12] = paymentStatus;

                        // 🔹 Ardından Controller Status Bit0'ı sıfırla
                        ushort controllerStatus = store.HoldingRegisters[1];
                        controllerStatus = (ushort)(controllerStatus & ~0x0001);
                        store.HoldingRegisters[1] = controllerStatus;
                    }

                    ForceSlaveRefresh();
                    BeginInvoke(new Action(UpdateUIFromRegisters));
                }


                // === Status Lampalar ===
                lampCtrlBit0.BackColor = (ctrl & 0x0001) != 0 ? Color.Lime : Color.Gray;
                lampCtrlBit1.BackColor = (ctrl & 0x0002) != 0 ? Color.Lime : Color.Gray;

                // Payment Status sadece 16 bit (low word) olarak okunacak
                ushort payLow = (ushort)(store.HoldingRegisters[12] & 0x00FF); // sadece low word
                lampPayBit0.BackColor = ((payLow & 0x0001) != 0) ? Color.Lime : Color.Gray;
                lampPayBit1.BackColor = ((payLow & 0x0002) != 0) ? Color.Lime : Color.Gray;
                lampPayBit2.BackColor = ((payLow & 0x0004) != 0) ? Color.Lime : Color.Gray;


                // === UI Değerleri ===
                txtRemainingTime.Text = store.HoldingRegisters[7].ToString();
                txtPollCounter.Text = store.HoldingRegisters[3].ToString();
                numProgramNo.Value = ClampToRange(store.HoldingRegisters[6], numProgramNo.Minimum, numProgramNo.Maximum);

                // === Total Price Hesaplama ===
                ushort programNo = store.HoldingRegisters[6];
                if (programNo > 0 && programNo <= numProgramPrices.Length)
                {
                    uint price = (uint)numProgramPrices[programNo - 1].Value;
                    lock (storeLock)
                    {
                        store.HoldingRegisters[4] = (ushort)(price & 0xFFFF);
                        store.HoldingRegisters[5] = (ushort)(price >> 16);
                    }
                    numTotalPrice.Value = ClampToRange(price, numTotalPrice.Minimum, numTotalPrice.Maximum);
                }
                else
                {
                    lock (storeLock)
                    {
                        store.HoldingRegisters[4] = 0;
                        store.HoldingRegisters[5] = 0;
                    }
                    numTotalPrice.Value = 0;
                }
                ForceSlaveRefresh();

                // === Discount İşleme ===
                ushort payStatus = store.HoldingRegisters[12];
                bool discountEnabled = (payStatus & 0x0002) != 0; // Bit1 aktif mi?
                if (discountEnabled)
                {
                    uint discountValue = (uint)((store.HoldingRegisters[16] << 16) | store.HoldingRegisters[15]);
                    uint totalValue = (uint)((store.HoldingRegisters[5] << 16) | store.HoldingRegisters[4]);

                    if (discountValue > 0 && discountValue < totalValue)
                    {
                        uint discounted = totalValue - discountValue;

                        // register’a yaz
                        lock (storeLock)
                        {
                            store.HoldingRegisters[4] = (ushort)(discounted & 0xFFFF);
                            store.HoldingRegisters[5] = (ushort)(discounted >> 16);
                        }

                        numTotalPrice.BeginInvoke(new Action(() =>
                        {
                            numTotalPrice.Value = ClampToRange(discounted, numTotalPrice.Minimum, numTotalPrice.Maximum);
                        }));

                        ForceSlaveRefresh();
                    }
                }


                // Total Price (32 bit)
                uint totalPrice = (uint)((store.HoldingRegisters[5] << 16) | store.HoldingRegisters[4]);
                numTotalPrice.Value = ClampToRange(totalPrice, numTotalPrice.Minimum, numTotalPrice.Maximum);

                // Event Tracker
                ushort evt = store.HoldingRegisters[2];
                txtEventTracker.Text = evt switch
                {
                    10 => "Idle",
                    20 => "Program Selection",
                    21 => "Extra Selection",
                    30 => "Payment",
                    40 => "Starting",
                    50 => "Cycling",
                    60 => "Cycle Finished",
                    999 => "Machine Unavailable",
                    _ => evt.ToString()
                };

                uint paidRaw = (uint)((store.HoldingRegisters[14] << 16) | store.HoldingRegisters[13]);
                float paidFloat = BitConverter.Int32BitsToSingle(unchecked((int)paidRaw));
                float sanitizedPaid = (!float.IsNaN(paidFloat) && !float.IsInfinity(paidFloat)) ? paidFloat : 0f;
                decimal paidValue = decimal.Round((decimal)sanitizedPaid, 2, MidpointRounding.AwayFromZero);
                uint discount = (uint)((store.HoldingRegisters[16] << 16) | store.HoldingRegisters[15]);
                numPaidAmount.Value = ClampToRange(paidValue, numPaidAmount.Minimum, numPaidAmount.Maximum);
                numDiscount.Value = ClampToRange(discount, numDiscount.Minimum, numDiscount.Maximum);
                numCurrency.Value = ClampToRange(store.HoldingRegisters[21], numCurrency.Minimum, numCurrency.Maximum);

                for (int i = 0; i < 20; i++)
                {
                    int reg = 23 + (i * 2);
                    uint val = (uint)((store.HoldingRegisters[reg + 1] << 16) | store.HoldingRegisters[reg]);

                    // Eğer store’da 0 yazıyorsa UI’deki değeri koru (resetleme!)
                    if (val > 0)
                        numProgramPrices[i].Value = ClampToRange(val, numProgramPrices[i].Minimum, numProgramPrices[i].Maximum);
                }


                uint rinse = (uint)((store.HoldingRegisters[64] << 16) | store.HoldingRegisters[63]);
                uint soap = (uint)((store.HoldingRegisters[66] << 16) | store.HoldingRegisters[65]);
                uint prewash = (uint)((store.HoldingRegisters[68] << 16) | store.HoldingRegisters[67]);

                numExtraRinse.Value = ClampToRange(rinse, numExtraRinse.Minimum, numExtraRinse.Maximum);
                numExtraSoap.Value = ClampToRange(soap, numExtraSoap.Minimum, numExtraSoap.Maximum);
                numExtraPrewash.Value = ClampToRange(prewash, numExtraPrewash.Minimum, numExtraPrewash.Maximum);
            }
            finally
            {
                updatingUI = false;
            }
        }

        private float ToPaidAmountFloat(decimal value)
        {
            decimal rounded = decimal.Round(value, 2, MidpointRounding.AwayFromZero);
            return (float)rounded;
        }

        private decimal ClampToRange(decimal value, decimal min, decimal max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            try
            {
                running = false;
                slave?.Transport?.Dispose();
                slaveThread?.Join(200);
                if (port != null && port.IsOpen)
                    port.Close();
                MessageBox.Show("Bağlantı kapatıldı.", "Bilgi");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bağlantı kapatılamadı: {ex.Message}", "Hata");
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            running = false;
            slave?.Transport?.Dispose();
            slaveThread?.Join(200);
            if (port != null && port.IsOpen)
                port.Close();
        }

        private void WriteU32(int reg, uint value)
        {
            if (store == null) return;
            if (reg + 1 < store.HoldingRegisters.Count)
            {
                store.HoldingRegisters[reg] = (ushort)(value & 0xFFFF);
                store.HoldingRegisters[reg + 1] = (ushort)(value >> 16);
            }
        }
    }
}
