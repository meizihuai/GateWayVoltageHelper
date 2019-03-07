using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
            gateWayVoltageHelper.OnGateWayStatusInfo += GateWayVoltageHelper_OnGateWayStatusInfo;
            gateWayVoltageHelper.StartWork();
        }

        private void GateWayVoltageHelper_OnGateWayStatusInfo(GateWayVoltageHelper.GateWayStatusInfo gateWayStatusInfo)
        {
            label1.Text = "实时状态：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            label2.Text = "网络："+ (gateWayStatusInfo.net=="in"?"内":"外");
            label3.Text = "电源："+ (gateWayStatusInfo.power == "on" ? "开" : "关");
            label4.Text = "电压：" + gateWayStatusInfo.vlotage;
            label5.Text = "GPS：" + "(" + gateWayStatusInfo.lon + "," + gateWayStatusInfo.lat + ")";
            VlotageWatch(gateWayStatusInfo.vlotage);
        }
        private void VlotageWatch(double v)
        {
            double minV = 10.5;
            double alarmV = 11;
            if (v == 0)
            {
                label6.Text = "电压为0，不执行关机";
              //  return;
            }
            if (v < minV)
            {
                label6.Text = "电压过低，需要关机！";
              if(checkBox1.Checked)  Process.Start("shutdown.exe", " -s -t 0");
            }
            else
            {
                label6.Text = "电压正常";

            }
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
        

        }

        private void GateWayVoltageHelper_OnUdpHeartBeat(string msg)
        {
            richTextBox1.Clear();
            richTextBox1.AppendText("==========" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "=========="+ System.Environment.NewLine);
            richTextBox1.AppendText(msg);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
