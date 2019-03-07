using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace 监测网关电压测试程序
{
    class GateWayVoltageHelper
    {
        public int localPort, remotePort;
        public string remoteIp;
        private IPEndPoint remoteIpEndPoint,localIpEndPoint;
        private Socket socket;
        private Thread udpServerThread;
        private Thread udpClientThread;
        private int sleepTimes;
        public delegate void DlgOnUdpMsg(string msg);
        public event DlgOnUdpMsg OnUdpHeartBeat;
        public event DlgOnUdpMsg OnUdpVoltageInfo;
        public event DlgOnUdpMsg OnLog;
        public GateWayVoltageHelper(string remoteIp,int remotePort,int localPort= 4516)
        {
            this.localPort = localPort;
            this.remoteIp = remoteIp;
            this.remotePort = remotePort;
            remoteIpEndPoint = new IPEndPoint(IPAddress.Parse(remoteIp), remotePort);
            localIpEndPoint = new IPEndPoint(IPAddress.Any, localPort);
        }
        private void Log(string str)
        {
            str = DateTime.Now.ToString("[HH:mm:ss] ") + str;
            Console.WriteLine(str);
            OnLog(str);
        }

        public void StartWork(int sleepSecond=5)
        {
            StopWork();
            sleepTimes = sleepSecond * 1000;
            udpServerThread = new Thread(StartUdpServer);
            udpServerThread.IsBackground = true;
            udpServerThread.Start();
            //  udpClientThread = new Thread(StartUdpClient);
            //  udpClientThread.IsBackground = true;

            //  udpClientThread.Start();
        }
        public void StopWork()
        {
            try
            {
                if (udpClientThread != null)
                {
                    udpClientThread.Abort();
                }
            }catch(Exception e)
            {

            }
            try
            {
                if (udpServerThread != null)
                {
                    udpServerThread.Abort();
                }
            }
            catch (Exception e)
            {

            }
        }
        private void StartUdpServer()
        {
            while (true)
            {
                try
                {
                    using (UdpClient udpClient = new UdpClient(localPort))
                    {
                        TssMsg tm = Msg2TssMsg(0, "task", "getheartbeat", "", null);
                        byte[] buffer = Tssmsg2byte(tm);
                        if (buffer == null)
                        {
                            Log("<Send> buffer is null");
                        }
                        Log("<Send>" + buffer.Length);
                        udpClient.Send(buffer, buffer.Length, remoteIpEndPoint);
                        IPEndPoint rp = new IPEndPoint(IPAddress.Any, 0);
                        buffer = null;
                        buffer = udpClient.Receive(ref rp);
                        string thisRemoteIp = rp.Address.ToString();
                        if (thisRemoteIp == remoteIp)
                        {
                            tm = Byte2tssmsg(buffer);
                            Log(tm.datatype + "," + tm.functype+","+tm.canshuqu);
                            if(tm.datatype=="Data" && tm.functype == "heartbeat")
                            {
                                string msg = tm.canshuqu;
                                string str = msg.Replace("<", "").Replace(">", "");
                                string headFunc = str.Split(':')[0];
                                if (headFunc == "heartbeat")
                                {
                                    OnUdpHeartBeat(tm.canshuqu);
                                    string[] paras= str.Split(';');
                                    foreach(string kv in paras)
                                    {
                                        string key = kv.Split('=')[0];
                                        string value = kv.Split('=')[1];
                                        if (key == "voltage") OnUdpVoltageInfo(value);
                                    }
                                }  
                                
                            }                     
                        }

                    }
                  
                }
                catch (Exception e)
                {
                    Log("<Send Err>" + e.ToString());
                }
                Thread.Sleep(sleepTimes);
            }
        }
        private void StartUdpClient()
        {
            while (true)
            {
                try
                {
                    using (UdpClient udpClient = new UdpClient(localPort))
                    {
                        TssMsg tm = Msg2TssMsg(0, "task", "getheartbeat", "", null);
                        byte[] buffer = Tssmsg2byte(tm);
                        Log("<Send>" + buffer.Length);
                        udpClient.Send(buffer, buffer.Length, remoteIpEndPoint);
                    }
                    Thread.Sleep(sleepTimes);
                }
                catch (Exception e)
                {
                    Log("<Send Err>" + e.ToString());
                }
            }
               
        }
       
        private struct TssMsg
        {
            public string flag;
            public string crc;
            public int lenofmsg;
            public byte ctrl;
            public byte xieyibanbenhao;
            public string datatype;
            public string functype;
            public int baowenxuhao;
            public short baotouchangdu;
            public short canshuquchangdu;
            public int shujuquchangdu;
            public string shibiao;
            public string deviceID;
            public string source;
            public string destination;
            public string canshuqu;
            public byte[] shujuqu;
        }
        private byte[] tobyte(string str)
        {
            if (str == null)
            {
                return new byte[] { 0 };
            }
            if (str == "")
            {
                return new byte[] { 0 };
            }
            return Encoding.Default.GetBytes(str);
        }
        private TssMsg Msg2TssMsg(byte ctrl, string datatype, string functype, string canshu, byte[] shuju)
        {
            TssMsg tm = new TssMsg();
            tm.deviceID = "0";
            tm.flag = "$RADIOINF";
            tm.ctrl = ctrl;
            tm.datatype = datatype;
            tm.functype = functype;
            tm.canshuquchangdu = 0;
            tm.shujuquchangdu = 0;
            int numofmsg = 102;
            if (canshu != "")
            {
                tm.canshuqu = canshu;
                byte[] b = Encoding.Default.GetBytes(tm.canshuqu);
                tm.canshuquchangdu =(Int16) b.Length;
                numofmsg = numofmsg + b.Count();
            }
            if (shuju != null)
            {
                tm.shujuqu = new byte[shuju.Length];
                Array.Copy(shuju, tm.shujuqu, shuju.Count());
                tm.shujuquchangdu = shuju.Count();
                numofmsg = numofmsg + shuju.Count();
            }
            tm.lenofmsg = numofmsg;
            return tm;
        }
        private byte[] Tssmsg2byte(TssMsg tm)
        {
            byte[] by = new byte[102];
            Array.Copy(tobyte(tm.flag), by, tobyte(tm.flag).Length);
            Array.Copy(BitConverter.GetBytes(tm.lenofmsg + 1), 0, by, 10, 4);
            Array.Copy(new byte[] { tm.ctrl }, 0, by, 14, 1);
            Array.Copy(new byte[] { tm.xieyibanbenhao }, 0, by, 15, 1);
            Array.Copy(tobyte(tm.datatype), 0, by, 16, tobyte(tm.datatype).Length);
            Array.Copy(tobyte(tm.functype), 0, by, 32, tobyte(tm.functype).Length);
            Array.Copy(BitConverter.GetBytes(tm.baowenxuhao), 0, by, 48, 4);
            Array.Copy(BitConverter.GetBytes(tm.baotouchangdu), 0, by, 52, 2);
            Array.Copy(BitConverter.GetBytes(tm.canshuquchangdu), 0, by, 54, 2);
            Array.Copy(BitConverter.GetBytes(tm.shujuquchangdu), 0, by, 56, 4);
            // 时标
            DateTime de = DateTime.Now;
            int yy = int.Parse(de.ToString("yy"));
            int MM = int.Parse(de.ToString("MM"));
            int dd = int.Parse(de.ToString("dd"));
            int HH = int.Parse(de.ToString("HH"));
            int m = int.Parse(de.ToString("mm"));
            int ss = int.Parse(de.ToString("ss"));
            Array.Copy(new byte[] { (byte)yy }, 0, by, 60, 1);
            Array.Copy(new byte[] { (byte)MM }, 0, by, 61, 1);
            Array.Copy(new byte[] { (byte)dd }, 0, by, 62, 1);
            Array.Copy(new byte[] { (byte)HH }, 0, by, 63, 1);
            Array.Copy(new byte[] { (byte)m }, 0, by, 64, 1);
            Array.Copy(new byte[] { (byte)ss }, 0, by, 65, 1);
            short ms = 0;
            Array.Copy(BitConverter.GetBytes(ms), 0, by, 66, 2);
            Array.Copy(BitConverter.GetBytes(ms), 0, by, 68, 2);
            Array.Copy(BitConverter.GetBytes(ms), 0, by, 70, 2);
            Array.Copy(tobyte(tm.deviceID), 0, by, 72, tobyte(tm.deviceID).Length);
            if(tm.source!=null)  Array.Copy(tobyte(tm.source), 0, by, 82, tobyte(tm.source).Length);
            if (tm.destination != null) Array.Copy(tobyte(tm.destination), 0, by, 92, tobyte(tm.destination).Length);
            // CRC
            byte crcInt = getHeadcrc(by);
            Array.Copy(new byte[] { crcInt }, 0, by, 9, 1);
            byte[] bu = null; 
            if (tm.canshuquchangdu > 0)
                bu = tobyte(tm.canshuqu);
            byte[] bk = null ;
            if (tm.shujuquchangdu > 0)
                bk = tm.shujuqu;
            int k1 = 0;
            int k2 = 0;
            if (bu!=null)
                k1 = bu.Count();
            if (bk != null)
                k2 = bk.Count();
            byte[] bbb = new byte[101 + k1 + k2 + 1];
            Array.Copy(by, 0, bbb, 0, 102);
            if (bu != null)
                Array.Copy(bu, 0, bbb, 102, k1);
            if (bk != null)
                Array.Copy(bk, 0, bbb, 102 + k1, k2);
            int t = getcrc(bbb);
            byte[] bkk;
            byte[] bt = new byte[1];
            bt[0] = (byte)t;
            bkk = bbb.Concat(bt).ToArray();
            return bkk;
        }
        private byte getcrc(byte[] by)
        {
            byte @int = 0;
            for (var i = 0; i <= by.Length - 1; i++)
            {
                int y = @int ^ by[i];
                @int = crcTable[y];
            }
            return @int;
        }
        private byte getHeadcrc(byte[] by)
        {
            byte @int = 0;
            for (var i = 10; i <= 101; i++)
            {
                int y = @int ^ by[i];
                @int = crcTable[y];
            }
            return @int;
        }
        private byte[] crcTable = new byte[] { 0x21, 0x7F, 0x9D, 0xC3, 0x40, 0x1E, 0xF, 0xA2, 0xE3, 0xBD, 0x5F, 0x1, 0x82, 0xD, 0x3E, 0x60, 0xB, 0xE2, 0x0, 0x5E, 0xDD, 0x83, 0x61, 0x3F, 0x7E, 0x20, 0xC2, 0x9, 0x1F, 0x41, 0xA3, 0xFD, 0x2, 0x5, 0xBE, 0xE0, 0x63, 0x3D, 0xDF, 0x81, 0xC0, 0x9E, 0x7, 0x22, 0xA1, 0xFF, 0x1D, 0x43, 0x9F, 0xC1, 0x23, 0x7D, 0xFE, 0xA0, 0x42, 0x1, 0x5D, 0x3, 0xE1, 0xBF, 0x3, 0x62, 0x80, 0xDE, 0x67, 0x39, 0xDB, 0x85, 0x6, 0x58, 0xBA, 0xE4, 0xA5, 0xFB, 0x19, 0x47, 0xC4, 0x9A, 0x78, 0x26, 0xFA, 0xA4, 0x46, 0x18, 0x9B, 0xC5, 0x27, 0x79, 0x38, 0x66, 0x84, 0xDA, 0x59, 0x7, 0xE5, 0xBB, 0x44, 0x1A, 0xF8, 0xA6, 0x25, 0x7B, 0x99, 0xC7, 0x86, 0xD8, 0x3A, 0x64, 0xE7, 0xB9, 0x5B, 0x5, 0xD9, 0x87, 0x65, 0x3B, 0xB8, 0xE6, 0x4, 0x5A, 0x1B, 0x45, 0xA7, 0xF9, 0x7A, 0x24, 0xC6, 0x98, 0xAD, 0xF3, 0x11, 0x4F, 0xC, 0x92, 0x70, 0x2E, 0x6F, 0x31, 0xD3, 0x8D, 0xE, 0x50, 0xB2, 0xE, 0x30, 0x6E, 0x8, 0xD2, 0x51, 0xF, 0xED, 0xB3, 0xF2, 0xA, 0x4E, 0x10, 0x93, 0xCD, 0x2F, 0x71, 0x8E, 0xD0, 0x32, 0x6, 0xEF, 0xB1, 0x53, 0xD, 0x4, 0x12, 0xF0, 0xAE, 0x2D, 0x73, 0x91, 0xCF, 0x13, 0x4D, 0xAF, 0xF1, 0x72, 0x2, 0xCE, 0x90, 0xD1, 0x8F, 0x6D, 0x33, 0xB0, 0xEE, 0xC, 0x52, 0xEB, 0xB5, 0x57, 0x9, 0x8A, 0xD4, 0x36, 0x68, 0x29, 0x77, 0x95, 0xCB, 0x48, 0x16, 0xF4, 0xAA, 0x76, 0x28, 0xCA, 0x94, 0x17, 0x49, 0xAB, 0xF5, 0xB4, 0xEA, 0x8, 0x56, 0xD5, 0x8B, 0x69, 0x37, 0xC8, 0x96, 0x74, 0x2A, 0xA9, 0xF7, 0x15, 0x4B, 0xA, 0x54, 0xB6, 0xE8, 0x6B, 0x35, 0xD7, 0x89, 0x55, 0xB, 0xE9, 0xB7, 0x34, 0x6A, 0x88, 0xD6, 0x97, 0xC9, 0x2B, 0x75, 0xF6, 0xA8, 0x4A, 0x14 };
        private TssMsg Byte2tssmsg(byte[] by)
        {
            TssMsg tm = new TssMsg();
            try
            {        
                tm.flag = Encoding.Default.GetString(by, 0, 9).TrimEnd('\0');
                tm.crc = by[9].ToString();
                tm.lenofmsg = BitConverter.ToInt32(by, 10);
                tm.ctrl = by[14];
                tm.xieyibanbenhao = by[15];
                tm.datatype= Encoding.Default.GetString(by, 16, 16).TrimEnd('\0');
                tm.functype = Encoding.Default.GetString(by, 32, 16).TrimEnd('\0');
                tm.baowenxuhao = BitConverter.ToInt32(by, 48);
                tm.baotouchangdu = BitConverter.ToInt16(by, 52);
                tm.canshuquchangdu = BitConverter.ToInt16(by, 54);
                tm.shujuquchangdu = BitConverter.ToInt32(by, 56);
                try
                {
                    string shijian = "";
                    shijian = "20" + by[60] + "-" + by[61] + "-" + by[62] + " " + by[63] + ":" + by[64] + ":" + by[65];
                    tm.shibiao = DateTime.Parse(shijian).ToString("yyyy-MM-dd HH:mm:ss");
                }
                catch (Exception ex)
                {
                }
                tm.deviceID = Encoding.Default.GetString(by, 72, 10).TrimEnd('\0');
                tm.source = Encoding.Default.GetString(by, 82, 10).TrimEnd('\0');
                tm.destination = Encoding.Default.GetString(by, 82, 10).TrimEnd('\0');
                if (tm.canshuquchangdu > 0)
                    tm.canshuqu = Encoding.Default.GetString(by, 102, tm.canshuquchangdu).TrimEnd('\0');
                if (tm.shujuquchangdu > 0)
                {
                    tm.shujuqu = new byte[tm.shujuquchangdu];
                    Array.Copy(by, 102 + tm.canshuquchangdu, tm.shujuqu, 0, tm.shujuquchangdu);
                }
                return tm;
            }
            catch (Exception ex)
            {
            }
            return tm;
        }

    }
}
