using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp2
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        // === Değişken Tanımları ===
        private ComboBox comboPort, comboBaud, comboDataBits, comboStopBits, comboParity;
        private Button btnOpen, btnClose;
        private NumericUpDown numDiscount, numPaidAmount, numCurrency, numTotalPrice, numProgramNo;
        private TextBox txtRemainingTime, txtPollCounter, txtEventTracker;
        private PictureBox lampCtrlBit0, lampCtrlBit1, lampPayBit0, lampPayBit1, lampPayBit2;
        private GroupBox groupPrices, groupModifiers;
        private NumericUpDown numExtraRinse, numExtraSoap, numExtraPrewash;
        private NumericUpDown[] numProgramPrices;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // === COM PORT CONTROL ===
            GroupBox groupCom = new GroupBox();
            groupCom.Text = "Com Port Control";
            groupCom.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            groupCom.Location = new Point(20, 20);
            groupCom.Size = new Size(240, 200);

            Label lblPort = new Label { Text = "COM PORT", Location = new Point(10, 25) };
            comboPort = new ComboBox { Location = new Point(110, 22), Size = new Size(100, 23) };

            Label lblBaud = new Label { Text = "BAUD RATE", Location = new Point(10, 55) };
            comboBaud = new ComboBox { Location = new Point(110, 52), Size = new Size(100, 23) };
            comboBaud.Items.AddRange(new object[] { "9600", "19200", "38400", "57600", "115200" });

            Label lblData = new Label { Text = "DATA BITS", Location = new Point(10, 85) };
            comboDataBits = new ComboBox { Location = new Point(110, 82), Size = new Size(100, 23) };
            comboDataBits.Items.AddRange(new object[] { "7", "8" });

            Label lblStop = new Label { Text = "STOP BITS", Location = new Point(10, 115) };
            comboStopBits = new ComboBox { Location = new Point(110, 112), Size = new Size(100, 23) };
            comboStopBits.Items.AddRange(new object[] { "One", "Two" });

            Label lblParity = new Label { Text = "PARITY BITS", Location = new Point(10, 145) };
            comboParity = new ComboBox { Location = new Point(110, 142), Size = new Size(100, 23) };
            comboParity.Items.AddRange(new object[] { "None", "Even", "Odd" });

            btnOpen = new Button { Text = "OPEN", Location = new Point(20, 170), Size = new Size(90, 25) };
            btnOpen.Click += btnOpen_Click;
            btnClose = new Button { Text = "CLOSE", Location = new Point(120, 170), Size = new Size(90, 25) };
            btnClose.Click += btnClose_Click;

            groupCom.Controls.AddRange(new Control[] {
                lblPort, comboPort, lblBaud, comboBaud,
                lblData, comboDataBits, lblStop, comboStopBits,
                lblParity, comboParity, btnOpen, btnClose
            });

            // === SYSTEM STATUS ===
            GroupBox groupSystem = new GroupBox();
            groupSystem.Text = "System Status";
            groupSystem.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            groupSystem.Location = new Point(280, 20);
            groupSystem.Size = new Size(580, 180);

            Label lblCtrl = new Label { Text = "Controller", Location = new Point(20, 30) };
            lampCtrlBit0 = new PictureBox { BackColor = Color.Gray, Size = new Size(20, 20), Location = new Point(440, 30), BorderStyle = BorderStyle.FixedSingle };
            lampCtrlBit1 = new PictureBox { BackColor = Color.Gray, Size = new Size(20, 20), Location = new Point(440, 55), BorderStyle = BorderStyle.FixedSingle };
            groupSystem.Controls.AddRange(new Control[]
            {
                lblCtrl,
                new Label { Text = "Bit0: Program Price Update Finished", Location = new Point(200, 30), Size = new Size(230, 20) },
                new Label { Text = "Bit1: Overpayment Enabled", Location = new Point(200, 55), Size = new Size(230, 20) },
                lampCtrlBit0, lampCtrlBit1
            });

            Label lblPay = new Label { Text = "Payment Status", Location = new Point(20, 90) };
            lampPayBit0 = new PictureBox { BackColor = Color.Gray, Size = new Size(20, 20), Location = new Point(440, 90), BorderStyle = BorderStyle.FixedSingle };
            lampPayBit1 = new PictureBox { BackColor = Color.Gray, Size = new Size(20, 20), Location = new Point(440, 115), BorderStyle = BorderStyle.FixedSingle };
            lampPayBit2 = new PictureBox { BackColor = Color.Gray, Size = new Size(20, 20), Location = new Point(440, 140), BorderStyle = BorderStyle.FixedSingle };
            groupSystem.Controls.AddRange(new Control[]
            {
                lblPay,
                new Label { Text = "Bit0: Program Price Update Available", Location = new Point(200, 90), Size = new Size(230, 20) },
                new Label { Text = "Bit1: Discount Enabled", Location = new Point(200, 115), Size = new Size(230, 20) },
                new Label { Text = "Bit2: System Update / Ready", Location = new Point(200, 140), Size = new Size(230, 20) },
                lampPayBit0, lampPayBit1, lampPayBit2
            });

            // === PAYMENT INFO ===
            GroupBox groupPayment = new GroupBox();
            groupPayment.Text = "Payment Info";
            groupPayment.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            groupPayment.Location = new Point(280, 220);
            groupPayment.Size = new Size(580, 220);

            Label lblDiscount = new Label { Text = "Discount:", Location = new Point(20, 30) };
            numDiscount = new NumericUpDown { Location = new Point(120, 28), Maximum = 9999, Width = 100 };

            Label lblPaid = new Label { Text = "Paid Amount:", Location = new Point(250, 30) };
            numPaidAmount = new NumericUpDown { Location = new Point(350, 28), Maximum = 9999, Width = 100 };

            Label lblRemain = new Label { Text = "Remaining Time:", Location = new Point(20, 70) };
            txtRemainingTime = new TextBox { Location = new Point(120, 68), Width = 100, ReadOnly = true };

            Label lblCurrency = new Label { Text = "Currency:", Location = new Point(250, 70) };
            numCurrency = new NumericUpDown { Location = new Point(350, 68), Maximum = 9999, Width = 100 };

            Label lblPoll = new Label { Text = "Poll Counter:", Location = new Point(20, 110) };
            txtPollCounter = new TextBox { Location = new Point(120, 108), Width = 100, ReadOnly = true };

            Label lblTotal = new Label { Text = "Total Price:", Location = new Point(250, 110) };
            numTotalPrice = new NumericUpDown { Location = new Point(350, 108), Maximum = 999999, Width = 100, ReadOnly = true };

            Label lblProgram = new Label { Text = "Program No:", Location = new Point(20, 150) };
            numProgramNo = new NumericUpDown { Location = new Point(120, 148), Minimum = 0, Maximum = 20, Width = 100, ReadOnly = true };

            Label lblEvent = new Label { Text = "Event Tracker:", Location = new Point(250, 150) };
            txtEventTracker = new TextBox { Location = new Point(350, 148), Width = 200, ReadOnly = true };

            groupPayment.Controls.AddRange(new Control[]
            {
                lblDiscount, numDiscount,
                lblPaid, numPaidAmount,
                lblRemain, txtRemainingTime,
                lblCurrency, numCurrency,
                lblPoll, txtPollCounter,
                lblTotal, numTotalPrice,
                lblProgram, numProgramNo,
                lblEvent, txtEventTracker
            });

            // === EXTRA MODIFIERS ===
            groupModifiers = new GroupBox();
            groupModifiers.Text = "Extra Modifiers";
            groupModifiers.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            groupModifiers.Location = new Point(20, 240);
            groupModifiers.Size = new Size(240, 160);

            Label lblRinse = new Label { Text = "Extra Rinse Modifier:", Location = new Point(10, 30), AutoSize = true };
            numExtraRinse = new NumericUpDown { Location = new Point(160, 26), Width = 80, Maximum = 999999, DecimalPlaces = 2 };

            Label lblSoap = new Label { Text = "Extra Soap Modifier:", Location = new Point(10, 65), AutoSize = true };
            numExtraSoap = new NumericUpDown { Location = new Point(160, 61), Width = 80, Maximum = 999999, DecimalPlaces = 2 };

            Label lblPrewash = new Label { Text = "Extra Prewash Modifier:", Location = new Point(10, 100), AutoSize = true };
            numExtraPrewash = new NumericUpDown { Location = new Point(160, 96), Width = 80, Maximum = 999999, DecimalPlaces = 2 };

            groupModifiers.Controls.AddRange(new Control[]
            {
                lblRinse, numExtraRinse,
                lblSoap, numExtraSoap,
                lblPrewash, numExtraPrewash
            });

            // === PROGRAM PRICES ===
            groupPrices = new GroupBox();
            groupPrices.Text = "Program Prices";
            groupPrices.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            groupPrices.Location = new Point(280, 460);
            groupPrices.Size = new Size(580, 450);

            numProgramPrices = new NumericUpDown[20];
            int startXLabel = 20, startXNum = 150, startY = 30, spacingY = 28, columnOffset = 270;

            for (int i = 0; i < 20; i++)
            {
                int col = i >= 10 ? 1 : 0;
                int row = i % 10;

                Label lbl = new Label
                {
                    Text = $"Program Price {i + 1}:",
                    Location = new Point(startXLabel + (col * columnOffset), startY + row * spacingY + 5),
                    AutoSize = true
                };

                NumericUpDown num = new NumericUpDown
                {
                    Name = $"numProgramPrice{i + 1}",
                    Location = new Point(startXNum + (col * columnOffset), startY + row * spacingY),
                    Width = 80,
                    Maximum = 999999,
                    DecimalPlaces = 2
                };

                numProgramPrices[i] = num;
                groupPrices.Controls.Add(lbl);
                groupPrices.Controls.Add(num);
            }

            // === FORM ===
            ClientSize = new Size(900, 950);
            Controls.AddRange(new Control[] { groupCom, groupSystem, groupPayment, groupModifiers, groupPrices });
            Text = "C# COM PORT SERIAL";
        }
    }
}
