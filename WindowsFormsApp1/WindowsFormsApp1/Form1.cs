using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Globalization;
using System.Diagnostics;

namespace WindowsFormsApp1
{
    public partial class BallBeam : Form
    {
        // Variables

        private SerialPort port;
        //25 milliseconds Interrupt timer
        private System.Windows.Forms.Timer t;

        //Serial input string
        private string data;
        //First column Serial input string
        private string data1 = "0";
        //second column Serial input string
        private string data2;
        //Send 'p' to serial port and stop/start motor
        private Boolean stop = false;
        //plot control signal in graphic
        private Boolean show_ctrl = false;
        private Boolean setpoint_change = false;
        //PID parameters
        private string kp;
        private string setpoint;
        private string ki;
        private string kd;
        private string ts;

        //Variables for setling time and overshoot calculation
        Stopwatch sw = Stopwatch.StartNew();
        private int start_time;
        private Boolean get_setling = false;
        private Boolean setling_in = false;
        private Boolean get_overshoot = false;
        private Boolean reference_tracking = false;
        private int start_pos;
        private int data_max;
        private int data_min;
        private int derivative = 0;
        private int last_derivative = 0;
        private int last_data1 = 0;

        Boolean i = false;
        double a = 0;
        int amplitude = 10;
        int frequency = 100;
        int temp = 0;
        double position = 0;

        public BallBeam()
        {
            InitializeComponent();
        }





        //Form Startup Method
        private void Form1_Load(object sender, EventArgs e)
        {
            setup_gui();

        }
               


        //Main Execution Thread
        //Interrupt Method - Graphic is plot here
        private void timer_Tick(object sender, EventArgs e)
        {
            //temp equals execution tick
            //Update tick
            temp = temp + 1;

            if (reference_tracking)           
                auto_tracking();          
            
            update_chart();

            //Compute error
            textBox6.Text = (((double)Int32.Parse(setpoint) * 30 / 1024 - position)).ToString("#.##");

            //////////// Animation //////////
            // textBox5.Text = trackBar4.Value.ToString();
            draw_animation();            
            
            //Change panel color when getting transient response paramemters
            if (get_setling)
                panel3.BackColor = Color.Yellow;
            else
                panel3.BackColor = Color.Lime;

            //Compute Transient
            transient_analysis();

        }

        




        //Connect button
        private void button1_Click(object sender, EventArgs e)
        {
            setup_serial();
            setup_timer();
            setup_chart();
        }


        
        //Refresh button
        private void button2_Click(object sender, EventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();   
            comboBox1.Items.Clear();
            // Display each port name to the console.
            foreach (string port in ports)
            {
                comboBox1.Items.Add(port);
            }

        }




        //Serial data received event
        void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (i == false)
            {
                temp = 0;
                i = true;
            }
            data = port.ReadLine();
            string[] s = data.Split(' ');

            last_data1 = Int32.Parse(data1);
            data1 = s[0];
            if (s.Length > 1) data2 = s[1];
        }

        //Manual set button
        private void button3_Click(object sender, EventArgs e)
        {
            set_data();
        }



        private void trackBar5_Scroll(object sender, EventArgs e)
        {
            textBox1.Text = (trackBar5.Value * 30 / 1024).ToString();
            //  set_data();

        }



        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            textBox2.Text = (trackBar1.Value * 0.005).ToString("F");

        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            textBox4.Text = (trackBar2.Value * 0.05).ToString("F");

        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            textBox3.Text = (trackBar3.Value * 0.01).ToString("F");

        }

        private void trackBar4_Scroll(object sender, EventArgs e)
        {


        }


        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {

            int n;
            bool isNumeric = int.TryParse(textBox1.Text.ToString(), out n);


            if (e.KeyCode == Keys.Enter)
            {
                if (textBox1.Text == string.Empty)
                {
                    textBox1.Text = (trackBar5.Value).ToString();
                }
                else if (!isNumeric)
                {
                    MessageBox.Show("NAN");
                    textBox1.Text = (trackBar5.Value).ToString();
                }

                else if (Int32.Parse(textBox1.Text.ToString()) > 30 || Int32.Parse(textBox1.Text.ToString()) < 0)
                {
                    MessageBox.Show("NOTVALID");
                    textBox1.Text = (trackBar5.Value).ToString();
                }
                else
                {
                    trackBar5.Value = Int32.Parse(textBox1.Text.ToString()) * 1024 / 30;
                    set_data();
                    setpoint_change = true;
                }

            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            float n;
            bool isNumeric = float.TryParse(textBox2.Text.ToString(), out n);


            if (e.KeyCode == Keys.Enter)
            {

                //                System.Diagnostics.Debug.Write(double.Parse(textBox2.Text.ToString()));
                if (textBox2.Text == string.Empty)
                {
                    textBox2.Text = (trackBar1.Value * 0.005).ToString();
                }
                else if (!isNumeric)
                {
                    MessageBox.Show("NAN");
                    textBox2.Text = (trackBar1.Value * 0.005).ToString();
                }

                else if (float.Parse(textBox2.Text.ToString()) > 0.5 || float.Parse(textBox2.Text.ToString()) < 0)
                {
                    MessageBox.Show("NOTVALIdsaD2");

                    textBox2.Text = (trackBar1.Value * 0.005).ToString();
                }
                else
                {
                    trackBar1.Value = (int)(float.Parse(textBox2.Text.ToString()) * 200);
                    set_data();
                }

            }
        }


        private void textBox4_KeyDown(object sender, KeyEventArgs e)
        {
            float n;
            bool isNumeric = float.TryParse(textBox4.Text.ToString(), out n);


            if (e.KeyCode == Keys.Enter)
            {
                if (textBox4.Text == string.Empty)
                {
                    textBox4.Text = (trackBar1.Value * 0.05).ToString();
                }
                else if (!isNumeric)
                {
                    MessageBox.Show("NAN");
                    textBox4.Text = (trackBar1.Value * 0.05).ToString();
                }

                else if (float.Parse(textBox4.Text.ToString()) > 5 || float.Parse(textBox4.Text.ToString()) < 0)
                {
                    MessageBox.Show("NOTVALID2");
                    textBox4.Text = (trackBar2.Value * 0.05).ToString();
                }
                else
                {
                    trackBar2.Value = (int)float.Parse(textBox4.Text.ToString()) * 20;
                    set_data();
                }

            }
        }


        private void textBox3_KeyDown(object sender, KeyEventArgs e)
        {
            float n;
            bool isNumeric = float.TryParse(textBox3.Text.ToString(), out n);

            if (e.KeyCode == Keys.Enter)
            {
                if (textBox3.Text == string.Empty)
                {
                    textBox3.Text = (trackBar3.Value * 0.01).ToString();
                }
                else if (!isNumeric)
                {
                    MessageBox.Show("NAN");
                    textBox3.Text = (trackBar3.Value * 0.01).ToString();
                }

                else if (float.Parse(textBox3.Text.ToString()) > 1 || float.Parse(textBox3.Text.ToString()) < 0)
                {
                    MessageBox.Show("NOTVALID2");
                    textBox3.Text = (trackBar3.Value * 0.01).ToString();
                }
                else
                {
                    trackBar3.Value = (int)(float.Parse(textBox3.Text.ToString()) * 100);
                    set_data();
                }

            }
        }


        private void textBox5_KeyDown(object sender, KeyEventArgs e)
        {
            //System.Diagnostics.Debug.Write("event 1");
            int n;
            bool isNumeric = int.TryParse(textBox5.Text.ToString(), out n);


            if (e.KeyCode == Keys.Enter)
            {
                if (textBox5.Text == string.Empty)
                {
                    textBox5.Text = (trackBar4.Value).ToString();
                }
                else if (!isNumeric)
                {
                    MessageBox.Show("NAN");
                    textBox5.Text = (trackBar4.Value).ToString();
                }

                else if (Int32.Parse(textBox5.Text.ToString()) > 100 || Int32.Parse(textBox5.Text.ToString()) < 0)
                {
                    MessageBox.Show("NOTVALID");
                    textBox5.Text = (trackBar4.Value).ToString();
                }
                else
                {
                    trackBar4.Value = Int32.Parse(textBox5.Text.ToString());
                    set_data();
                }

            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            temp = 0;

            this.chart1.Series["Position"].Points.Clear();
            this.chart1.Series["Control"].Points.Clear();
            this.chart1.Series["REF"].Points.Clear();

        }

        private void button4_Click(object sender, EventArgs e)
        {
            port.Write("p");

            stop = !stop;

            if (stop)
                panel1.BackColor = Color.Red;
            else
                panel1.BackColor = Color.Lime;

        }

        private void button7_Click(object sender, EventArgs e)
        {
            port.Close();
            t.Stop();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }






        //////////////////Important Methods///////////////////





        //Create timer method
        private void setup_timer()
        {
            t = new System.Windows.Forms.Timer();
            t.Interval = 25; // specify interval time as you want
            t.Tick += new EventHandler(timer_Tick);
            t.Start();
        }



        //Add data points to chart
        private void update_chart()
        {
            position = (double) (Int32.Parse(data1)) * 30 / 1024;
            
            if (position >= 0 && position< 31)
            {
                chart1.Series["Position"].Points.AddXY(temp, position);
            }
            chart1.Series["REF"].Points.AddXY(temp, (double) Int32.Parse(setpoint) * 30 / 1024);

            if (show_ctrl)
                chart1.Series["Control"].Points.AddXY(temp, data2);

            if (temp > 500)
            {
                chart1.ChartAreas[0].AxisX.Minimum = temp - 500;
            }
            else
            {
                chart1.ChartAreas[0].AxisX.Minimum = 0;

            }

        }


    //Setup chart initialization parameters
    private void setup_chart ()
        {
            chart1.Series ["Position"].BorderWidth = 3;
            chart1.Series ["REF"].BorderWidth = 2;
            chart1.Series ["REF"].Color =Color.Red;
        }





        //GUI Initialization parameters function
        private void setup_gui()
        {
            //Use '.' instead of ','
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            comboBox2.Items.Add("230400");
            comboBox2.SelectedIndex = 0;
            string[] ports = SerialPort.GetPortNames();
            comboBox1.Items.Clear();
            // Display each port name to the console.
            foreach (string port in ports)
                {
                    comboBox1.Items.Add(port);
                }
            //comboBox1.SelectedIndex = 1;
            comboBox3.Items.Add("Square");
            comboBox3.Items.Add("Sine");
            comboBox3.SelectedIndex = 0;
            chart1.ChartAreas[0].AxisY.Title = "Position (cm)";
            chart1.ChartAreas[0].AxisX.Title = "Sample";

            setpoint = (Int32.Parse(textBox1.Text.ToString()) * 1024 / 30).ToString();
        }





        //Setup auto reference trackin
        private void auto_tracking()
        {

            if (comboBox3.SelectedIndex == 0)
            {
                a = 10 + amplitude * ((temp / frequency) % 2);
            }
            else
            {
                a = (double)(13 + amplitude * (Math.Sin(temp * frequency * 3.14 / 180)));
            }
            setpoint = ((int)(a * 1024 / 30)).ToString();
            //textBox1.Text = (a).ToString("#.#");
            // Thread.Sleep(5000);
            set_data();
        }


        //Animation Function
        private void draw_animation()
        {
            panel2.Refresh();
            Graphics g = panel2.CreateGraphics();

            Pen p = new Pen(Color.Red);
            SolidBrush sb = new SolidBrush(Color.Red);
            // g.DrawRectangle(p, 200+(float)position, 50, 100, 5);
            //   g.FillRectangle(sb, 200+ (float)position, 50, 100, 5);


            //FIRST POINT CONSTANT
            Point point1 = new Point(50, 50);
            double ang;
            double pos;

            pos = position;
            // ang = (float.Parse(trackBar4.Value.ToString()) - 50) * 3.14 / 180;
            ang = double.Parse(data2) * 3.14 / 180 * -0.25;
            //END POINT DEPENDS ON ANGLE
            Point point2 = new Point((int)(50 + 100 * Math.Cos(ang)), (int)(50 + 100 * Math.Sin(ang)));

            Point point3 = new Point((int)(50 + 100 * Math.Cos(ang) + 10 * Math.Sin(ang)), (int)(50 + 100 * Math.Sin(ang) - 10 * Math.Cos(ang)));

            Point point4 = new Point((int)(50 + 10 * Math.Sin(ang)), (int)(50 - 10 * Math.Cos(ang)));

            Point[] points = { point1, point2, point3, point4 };
            g.FillPolygon(sb, points);


            SolidBrush sb2 = new SolidBrush(Color.Blue);
            g.DrawEllipse(p, (float)(50 + 5 * (24 - pos) * (Math.Cos(ang))), (float)(30 + 5 * (24 - pos) * (Math.Sin(ang))), 10, 10);
            g.FillEllipse(sb2, (float)(50 + 5 * (24 - pos) * (Math.Cos(ang))), (float)(30 + 5 * (24 - pos) * (Math.Sin(ang))), 10, 10);
            //////////////////////////////////////////////////////
            //////////////////////////////////////////////////
        }



        //Setup Serial communication port
        private void setup_serial()
        {
            int BaudRate;
            string serial_port;
            serial_port = comboBox1.SelectedItem.ToString();
            BaudRate = Int32.Parse(comboBox2.SelectedItem.ToString());
            port = new SerialPort(serial_port, BaudRate, Parity.None, 8, StopBits.One);
            port.DataReceived += port_DataReceived;
            port.Open();
            port.DiscardInBuffer();
            //Wait a while - Garbage startup serial data
            Thread.Sleep(150);
            //Flush Garbage
            port.DiscardInBuffer();
        }
        



        //Set serial parameters data
        private void set_data()
        {
            if(!reference_tracking)
            setpoint = (Int32.Parse(textBox1.Text.ToString()) * 1024 / 30).ToString();
            kp = textBox2.Text.ToString();
            kd = textBox4.Text.ToString();
            ki = textBox3.Text.ToString();
            ts = textBox5.Text.ToString();
            port.Write(setpoint + " " + kp + " " + kd + " " + ki + " " + ts + " ");

            //System.Diagnostics.Debug.Write(setpoint + " " + kp + " " + kd + " " + ki + " " + ts + " " +"\n");

            Thread.Sleep(1);
        }




        //Transient analysis
        private void transient_analysis()
        {

            /////////////////////////////////    STEP RESPONSE//////////////////////////////////

            if (setpoint_change)
            {
                setpoint_change = !setpoint_change;
                start_time = temp;
                start_pos = Int32.Parse(data1);
                sw.Reset();
                sw.Start();

                System.Diagnostics.Debug.Write("temp set");
                get_setling = true;
                get_overshoot = true;
                data_min = Int32.Parse(data1) - 10;
                data_max = Int32.Parse(data1) + 10;
                setling_in = false;
            }

            // First identify the point where the funcion  gets in the setling region 1cm or 2% etc
            // Then identify next change of direction
            // If both are satisfied, stop.
            if (get_setling)
            {
                last_derivative = derivative;
                derivative = Int32.Parse(data1) - last_data1;

                // System.Diagnostics.Debug.Write("GETING SETLING");
                if ((Int32.Parse(data1) > data_max))
                    data_max = Int32.Parse(data1);

                if ((Int32.Parse(data1) < data_min))
                    data_min = Int32.Parse(data1);
                //Check for zero derivative
                if (Math.Sign(derivative) != Math.Sign(last_derivative))
                {

                    if (setling_in)
                    {


                        sw.Stop();
                        textBox8.Text = ((sw.Elapsed.TotalSeconds)).ToString("#.##");
                        System.Diagnostics.Debug.Write("Time taken: {0}ms " + sw.Elapsed.TotalMilliseconds);
                        sw.Reset();

                        Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
                        //sw.Reset();
                        get_setling = false;

                        if (get_overshoot)
                        {
                            textBox7.Text = "n/a";
                        }

                    }
                }
                if ((Math.Sign(derivative) != Math.Sign(last_derivative)) && Math.Abs(Int32.Parse(data1) - start_pos) > Math.Abs(Int32.Parse(setpoint) - start_pos))
                {
                    if (get_overshoot)
                    {
                        get_overshoot = false;
                        System.Diagnostics.Debug.Write("getin mp");

                        System.Diagnostics.Debug.Write(data1);
                        System.Diagnostics.Debug.Write(setpoint);
                        System.Diagnostics.Debug.Write(start_pos);
                        //Thread.Sleep(10000);
                        textBox7.Text = ((double)100 * (Int32.Parse(data1) - Int32.Parse(setpoint)) / (Int32.Parse(setpoint) - start_pos)).ToString("#.#") + "%";
                        // System.Diagnostics.Debug.Write("Time taken: {0}ms " + sw.Elapsed.TotalMilliseconds);


                    }
                }

                if (Math.Abs((Int32.Parse(setpoint) - Int32.Parse(data1))) < 20)
                {
                    // System.Diagnostics.Debug.Write(setpoint);
                    // System.Diagnostics.Debug.Write(data1);
                    //Enter the setling region!
                    setling_in = true;

                }
                else
                {
                    //Exit the setling region!
                    setling_in = false;

                }
            }
        }
        





        //GUI Events

        private void trackBar5_MouseUp(object sender, MouseEventArgs e)
        {
            set_data();
        }

        private void trackBar1_MouseUp(object sender, MouseEventArgs e)
        {
            set_data();
        }
                    

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            show_ctrl = !show_ctrl;

            if(!show_ctrl)
                chart1.Series["Control"].Points.Clear();
        }
             

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            reference_tracking = !reference_tracking;
        }

        private void trackBar6_Scroll(object sender, EventArgs e)
        {
            frequency = trackBar6.Value;
        }

        private void trackBar7_Scroll(object sender, EventArgs e)
        {
            amplitude = trackBar7.Value;
        }
    }
}
