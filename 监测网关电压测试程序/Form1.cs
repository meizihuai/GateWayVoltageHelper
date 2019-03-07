using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace 监测网关电压测试程序
{
    public partial class Form1 : Form
    {
        private GateWayVoltageHelper gateWayVoltageHelper;
        private readonly string title="监测网关电压测试程序V1.0.0";
        public Form1()
        {
            InitializeComponent();
        }
     

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = title;
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += Form1_FormClosing;
            Run();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            System.Environment.Exit(0);
        }
        private void Run()
        {
            gateWayVoltageHelper = new GateWayVoltageHelper("10.253.12.105",3206);
            gateWayVoltageHelper.OnUdpHeartBeat += GateWayVoltageHelper_OnUdpHeartBeat;
            gateWayVoltageHelper.OnUdpVoltageInfo += GateWayVoltageHelper_OnUdpVoltageInfo;
            gateWayVoltageHelper.OnLog += GateWayVoltageHelper_OnLog;
            gateWayVoltageHelper.StartWork();
        }

        private void GateWayVoltageHelper_OnLog(string msg)
        {
            listBox1.Items.Add(msg);
            listBox1.SelectedIndex = listBox1.Items.Count - 1;
            if (listBox1.Items.Count >= 1000)
            {
                listBox1.Items.Clear();
            }
        }

        private void GateWayVoltageHelper_OnUdpVoltageInfo(string msg)
        {
            label1.Text = "实时电压：" + msg + "  刷新时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void GateWayVoltageHelper_OnUdpHeartBeat(string msg)
        {
            richTextBox1.Clear();
            richTextBox1.AppendText("==========" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "=========="+ System.Environment.NewLine);
            richTextBox1.AppendText(msg);
        }
    }
}
