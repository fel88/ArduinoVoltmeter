using System.Globalization;
using System.IO.Ports;
using System.Text;
using System.Xml.Linq;

namespace ArduinoVoltmeter
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            pictureBox1.Paint += PictureBox1_Paint;
            pictureBox1.Resize += PictureBox1_Resize;
        }

        private void PictureBox1_Resize(object? sender, EventArgs e)
        {
            pictureBox1.Invalidate();
        }

        private void PictureBox1_Paint(object? sender, PaintEventArgs e)
        {
            DrawPlot(e.Graphics);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox1_DropDown(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            foreach (var item in SerialPort.GetPortNames())
            {
                comboBox1.Items.Add(item);
            }
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort spL = (SerialPort)sender;
            byte[] buf = new byte[spL.BytesToRead];

            spL.Read(buf, 0, buf.Length);
            var str = Encoding.UTF8.GetString(buf);

            var ar = str.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToArray();
            foreach (var item in ar)
            {
                if (item.Contains("Voltage ="))
                {
                    var spl = item.Trim().Split(new char[] { ' ', '=', 'V' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
                    var v = float.Parse(spl[1], CultureInfo.InvariantCulture);
                    lastVoltage = v;
                    label1.Text = v + "V";

                }
            }
        }

        public List<VoltageItem> Voltages = new List<VoltageItem>();
        public class VoltageItem
        {
            public DateTime Timestamp;
            public float Value;
        }
        SerialPort port;
        private void button1_Click(object sender, EventArgs e)
        {
            port = new SerialPort(comboBox1.Text, 115200);
            port.Open();
            port.DataReceived += Port_DataReceived;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Voltages.Clear();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() != DialogResult.OK)
                return;


            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\"?>");
            sb.AppendLine("<root>");
            foreach (var item in Voltages)
            {
                sb.AppendLine($"<item v=\"{item.Value}\" timestamp=\"{item.Timestamp}\"/>");
            }
            sb.AppendLine("</root>");

            File.WriteAllText(sfd.FileName, sb.ToString());
        }

        private void button4_Click(object sender, EventArgs e)
        {
            port.Close();
        }


        private void DrawPlot(Graphics gr)
        {
            var pos = pictureBox1.PointToClient(Cursor.Position);
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            gr.Clear(Color.White);
            if (Voltages.Count < 2)
                return;

            var maxTime = Voltages.Max(z => z.Timestamp);
            var minTime = Voltages.Min(z => z.Timestamp);
            var diffTime = maxTime - minTime;
            var timePos = minTime + diffTime * (pos.X / (float)pictureBox1.Width);
            int? cursorIdx = null;
            for (int i = 1; i < Voltages.Count; i++)
            {
                if (Voltages[i - 1].Timestamp < timePos && Voltages[i].Timestamp > timePos)
                {
                    cursorIdx = i;
                    break;
                }
            }
            toolStripStatusLabel4.Text = $"{timePos}";
            if (cursorIdx != null)
            {
                toolStripStatusLabel4.Text += $" V: {Voltages[cursorIdx.Value].Value}";
            }
            var maxV = Voltages.Max(z => z.Value);
            var minV = Voltages.Min(z => z.Value);
            var diffV = maxV - minV;
            if (diffV < float.Epsilon)
            {
                diffV = 1;
            }
            var kx = pictureBox1.Width / diffTime.TotalSeconds;
            var ky = pictureBox1.Height / diffV;
            float gap = 5;

            for (int i = 1; i < Voltages.Count; i++)
            {
                var x0 = (float)((Voltages[i - 1].Timestamp - minTime).TotalSeconds * kx);
                var x1 = (float)((Voltages[i].Timestamp - minTime).TotalSeconds * kx);
                var y0 = pictureBox1.Height - 1 - (float)((Voltages[i - 1].Value - minV) * ky);
                var y1 = pictureBox1.Height - 1 - (float)((Voltages[i].Value - minV) * ky);
                gr.DrawLine(Pens.Black, x0, y0, x1, y1);
                //    gr.DrawEllipse(Pens.Black, x0 - gap, y0 - gap, gap * 2, gap * 2);
            }
            gr.DrawLine(Pens.Red, pos.X, 0, pos.X, pictureBox1.Height - 1);
            gr.DrawString($"{(timePos - minTime).TotalSeconds}sec  V: {(cursorIdx == null ? "(?)" : Voltages[cursorIdx.Value].Value)}", SystemFonts.DefaultFont, Brushes.Black, pos.X + 50, pos.Y - 50);
            if (cursorIdx != null)
            {
                var x0 = (float)((Voltages[cursorIdx.Value].Timestamp - minTime).TotalSeconds * kx);
                var y0 = pictureBox1.Height - 1 - (float)((Voltages[cursorIdx.Value].Value - minV) * ky);
                gr.DrawEllipse(Pens.Red, x0 - gap, y0 - gap, gap * 2, gap * 2);
            }

        }

        float ParseFloat(string str)
        {
            return float.Parse(str.Replace(",", "."), CultureInfo.InvariantCulture);
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            timer1.Interval = (int)numericUpDown1.Value;
        }

        float lastVoltage;
        private void timer1_Tick(object sender, EventArgs e)
        {
            Voltages.Add(new VoltageItem() { Timestamp = DateTime.Now, Value = lastVoltage });
            toolStripStatusLabel3.Text = Voltages.Count + " counts";
            //pictureBox1.Invalidate();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            timer1.Enabled = checkBox1.Checked;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            int? startIdx = null;
            int? endIdx = null;
            bool startSearch = false;
            for (int i = 0; i < Voltages.Count; i++)
            {
                if (startIdx == null && !startSearch && Voltages[i].Value > (float)numericUpDown2.Value)
                {
                    startSearch = true;
                }
                if (startSearch && Voltages[i].Value < (float)numericUpDown2.Value && startIdx == null)
                {
                    startIdx = i;
                }
                if (Voltages[i].Value < (float)numericUpDown3.Value && startIdx != null && endIdx == null)
                    endIdx = i;
            }
            if (startIdx == null || endIdx == null)
            {
                MessageBox.Show("interval not found");
                return;
            }
            var v0 = Voltages[startIdx.Value];
            var v1 = Voltages[endIdx.Value];

            MessageBox.Show($"Decay from {v0.Value}->{v1.Value}: discharge time {(v1.Timestamp - v0.Timestamp)}");
        }

        internal void LoadFile()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            var doc = XDocument.Load(ofd.FileName);
            Text = "Arduino Voltmeter: " + Path.GetFileNameWithoutExtension(ofd.FileName);
            Voltages.Clear();
            foreach (var item in doc.Descendants("item"))
            {
                Voltages.Add(new VoltageItem() { Timestamp = DateTime.Parse(item.Attribute("timestamp").Value), Value = ParseFloat(item.Attribute("v").Value) });
            }
            toolStripStatusLabel1.Text = $"voltage interval: {Voltages.Min(z => z.Value)}-{Voltages.Max(z => z.Value)}";
            //pictureBox1.Invalidate();
            toolStripStatusLabel3.Text = Voltages.Count + " counts";
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            pictureBox1.Invalidate();
        }
    }
}