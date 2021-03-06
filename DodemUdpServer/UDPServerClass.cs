﻿using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;//在C#中使用ArrayList必须引用Collections类  

namespace DodemUdpServer
{
    class UDPServerClass
    {
        public delegate void MessageHandler(string Message);//定义委托事件  
        public event MessageHandler MessageArrived;

        public delegate void ByteMessageHandler(byte[] Message);//定义委托事件  
        public event ByteMessageHandler ByteMessageArrived;
        public event ByteMessageHandler MessageDeviceStatus;

        public delegate void DelegateDealByte(byte[] Messages);

        public delegate void DeviceMessageHandler(string Message);//定义委托事件  
        public event DeviceMessageHandler MessageDeviceOnline;
        public event DeviceMessageHandler MessageDeviceOffline;
        
        public class DevicePicture
        {
            string DeviceIMEI;
            int TotalPackNum;
            string PictureName;
            string PictureTime;
            string DeviceChannel;
            string DevicePreform;//预制位
            List<byte[]> picData = new List<byte[]>();
            bool CompleteTransData;

            public void Init(int num)
            {
                DeviceIMEI = "";
                TotalPackNum =0;
                PictureName = "";
                PictureTime = DateTime.Now.ToString("yyyymmddhhmmss");
                DeviceChannel = "1";
                DevicePreform = "0";
                CompleteTransData = false;
                picData.Clear();
                for(int i=0; i<num; i++)
                {
                    byte[] tempdata = new byte[1];
                    tempdata[0] = 0x1A;
                    picData.Add(tempdata);
                }
            }

            public string GetDeviceIMEI()
            {
                return DeviceIMEI;
            }
            public byte[] CheckMissPackages()
            {
                List<byte> byteSource = new List<byte>();
                for(int i=0; i<picData.Count; i++)
                {
                    if (picData[i].Length == 1 && picData[i][0] == 0x1D)
                        byteSource.Add((byte)(i+1));//需要增加1 数据包是从1开始计算的
                }
                byte[] packByte = byteSource.ToArray();
                return packByte;
            }
            public bool GetCompleteTransData()
            {
                return CompleteTransData;
            }
            public int GetPackageNum()
            {
                return TotalPackNum;
            }
            public string GetPictureName()
            {
                return PictureName;
            }
            public string GetPictureTime()
            {
                return PictureTime;
            }
            public string GetDeviceChannel()
            {
                return DeviceChannel;
            }

            public string GetDevicePreform()
            {
                return DevicePreform;
            }

            public void SetPackageNum(int str)
            {
                TotalPackNum = str;
            }
            public void SetPictureName(string str)
            {
                PictureName = str;
            }
            public void SetPictureTime(string str)
            {
                PictureTime = str;
            }
            public void SetDeviceChannel(string str)
            {
                DeviceChannel = str;
            }

            public void SetDevicePreform(string str)
            {
                DevicePreform = str;
            }

            public void SetCompleteTransData(bool str)
            {
                CompleteTransData = str;
            }

            public void SetDeviceIMEI(string str)
            {
                DeviceIMEI = str;
            }

            public void AddDataToList(byte[] str, int packagesNum)
            {
                picData[packagesNum] = str;
            }
        }

        /*
        Status状态
        0 掉线
        1 开机
        2 校时
        3 保活
             
        */
        public class ElecDevice{
            string DeviceName;
            string DeviceIMEI;
            DateTime LastTime;
            IPEndPoint mIPEndPoint;
            byte[] LastMessage;
            string DSVersion;
            int status;

            public void Init()
            {
                DeviceName = "";
                DeviceIMEI = "";
                LastTime = DateTime.Now;
                mIPEndPoint = new IPEndPoint(0, 666);
                LastMessage = new byte[]{0};
                status = 0;
                DSVersion = "1.0";
            }
            public string GetVersion()
            {
                return DSVersion;
            }
            public DateTime GetLastTime()
            {
                return LastTime;
            }

            public int GetStatus()
            {
                return status;
            }
            public string GetDeviceName()
            {
                return DeviceName;
            }

            public string GetDeviceIMEI()
            {
                return DeviceIMEI;
            }

            public IPEndPoint GetDeviceIPEndPoint()
            {
                return mIPEndPoint;
            }

            public byte[] GetDeviceLastMessage()
            {
                return LastMessage;
            }
            public void SetDeviceName(string str)
            {
                DeviceName = str;
            }

            public void SetDeviceIMEI(string str)
            {
                DeviceIMEI = str;
            }

            public void SetLastTime(DateTime str)
            {
                LastTime = str;
            }

            public void SetIPEndPoint(IPEndPoint str)
            {
                mIPEndPoint = str;
            }

            public void SetLastMessage(byte[] str)
            {
                LastMessage = str;
            }

            public void SetStatus(int str)
            {
                status = str;
            }

            public void SetVersion(string str)
            {
                DSVersion = str;
            }
        }
        List<ElecDevice> mDevice;
        List<DevicePicture> mDevicePicture;
        public UDPServerClass()
        {
            //获取本机可用IP地址  
            mDevice = new List<ElecDevice>();
            mDevicePicture = new List<DevicePicture>();
            thread_Flag = true;
            IPAddress[] ips = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            foreach (IPAddress ipa in ips)
            {
                if (ipa.AddressFamily == AddressFamily.InterNetwork)
                {
                    MyIPAddress = ipa;//获取本地IP地址
                    break;
                }
            }
            Note_StringBuilder = new StringBuilder();
            PortName = 8080;
        }

        public UdpClient ReceiveUdpClient;

        /// <summary>  
        /// 侦听端口名称  
        /// </summary>  
        public int PortName;

        /// <summary>  
        /// 本地地址  
        /// </summary>  
        public IPEndPoint LocalIPEndPoint;

        /// <summary>  
        /// 日志记录  
        /// </summary>  
        public StringBuilder Note_StringBuilder;
        /// <summary>  
        /// 本地IP地址  
        /// </summary>  
        public IPAddress MyIPAddress;
        /// <summary>
        /// 终止标志
        /// </summary>
        public bool thread_Flag;

        Thread myThread;
        /// <summary>
        /// 开始监听
        /// </summary>
        public void Thread_Listen()
        {
            //创建一个线程接收远程主机发来的信息  
            myThread = new Thread(ReceiveData);
            myThread.IsBackground = true;
            thread_Flag = true;
            myThread.Start();
        }
        /// <summary>  
        /// 接收数据  
        /// </summary>  
        private void ReceiveData()
        {
            IPEndPoint local = new IPEndPoint(MyIPAddress, PortName);
            ReceiveUdpClient = new UdpClient(local);
            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
            Console.WriteLine("IP: {0}---PORT:{1}", MyIPAddress.ToString(), PortName);
            MessageArrived(string.Format("{0}启动{1}:{2}", DateTime.Now.ToString(), MyIPAddress.ToString(), PortName));
            while (thread_Flag)
            {
                try
                {
                    //关闭udpClient 时此句会产生异常  
                    byte[] receiveBytes = ReceiveUdpClient.Receive(ref remote);
                    string receiveMessage = "设备指令:";
                    receiveMessage += byteTOstring(receiveBytes);
                    //string receiveMessage = Encoding.Default.GetString(receiveBytes, 0, receiveBytes.Length);
                    //receiveMessage = ASCIIEncoding.ASCII.GetString(receiveBytes, 0, receiveBytes.Length);  
                    MessageArrived(string.Format("{0}来自{1}:{2}", DateTime.Now.ToString(), remote, receiveMessage));
                    Console.WriteLine("线程接收到的数据:{0}", receiveMessage);
                    ByteMessageArrived(receiveBytes);//传输byte模式
                    EventDealByte(receiveBytes,remote);
//                     try  
//                     {  
//                         Byte[] sendBytes = Encoding.ASCII.GetBytes("Is anybody there?");  
//                         ReceiveUdpClient.Send(sendBytes, sendBytes.Length, local);  
//                     }  
//                     catch (Exception e)  
//                     {
//                         MessageArrived(string.Format("{0}返回失败{1}:{2}", DateTime.Now.ToString(), remote, receiveMessage));
//                     }  
                    //break;  

                }
                catch
                {
                    break;
                }
            }
            //ReceiveUdpClient.Close();
        }

        /// <summary>  
        /// 添加日志信息到Note_StringBuilder  
        /// </summary>  
        public void AddMessage_Note_StringBuilder()
        {

        }

        /// <summary>  
        /// 终止线程  
        /// </summary>  
        public void CloseUdpThread()
        {
            MessageArrived(string.Format("{0}停止{1}:{2}", DateTime.Now.ToString(), MyIPAddress.ToString(), PortName));
            thread_Flag = false;
            myThread.Abort();
            ReceiveUdpClient.Close();
        }

        /// <summary>
        /// 验证IP地址是否有效
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        private bool IsCorrentIP(string ip)
        {
            string pattrn = @"(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])";
            if (System.Text.RegularExpressions.Regex.IsMatch(ip, pattrn))
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        void UDPServer_ByteNessageArrived(byte[] Message)
        {
            //DelegateDealByte ProtocolDealByte = new DelegateDealByte(EventDealByte);
            //ProtocolDealByte(Message);
        }

        /// <summary>
        /// 处理数据，根据数据进行响应
        /// </summary>
        /// <param name="Messages"></param>
        /// <param name="refIPEndPoint"></param>
        public void EventDealByte(byte[] Messages,IPEndPoint refIPEndPoint)
        {
            //Console.WriteLine("处理收到的数据Byte:{0}", Encoding.Default.GetString(Messages, 0, Messages.Length));
            if(CheckMessage(Messages))
            {
                //string Device_Imei = getDeviceImei(Messages);
                //CheckDeviceInMysql(Device_Imei);
                //FlashDeviceStatus(Device_Imei);
                //CheckDeviceList(Device_Imei, refIPEndPoint, Messages);
                //在检查数据正确后，检查数据格式
                int sendlenth;
                int status;
                switch (Messages[7])
                {
                    case 0x00:
                        MessageArrived(string.Format("开机联络信息"));
                        CheckDeviceList(Messages, refIPEndPoint);
                        sendlenth = ReceiveUdpClient.Send(Messages, Messages.Length, refIPEndPoint);
                        if(sendlenth==Messages.Length)
                        {
                            MessageArrived(string.Format("{0}应答{1}:{2}",DateTime.Now.ToString(),refIPEndPoint, "应答指令:"+byteTOstring(Messages)));
                        }
                        SetMessageDeviceStatus(Messages, 1);
                        MessageDeviceOnline(getDeviceImei(Messages));//把设备上线信息发给Form
                        break;
                    case 0x01:
                        MessageArrived(string.Format("校时"));
                        status = GetMessageDeviceStatus(Messages);
                        if(status<2)
                        {
                            sendlenth = ReceiveUdpClient.Send(CreatCheckTimeOrder(Messages), CreatCheckTimeOrder(Messages).Length, refIPEndPoint);

                            if (sendlenth == Messages.Length)
                            {
                                MessageArrived(string.Format("{0}应答{1}:{2}", DateTime.Now.ToString(), refIPEndPoint, "应答指令:" + byteTOstring(Messages)));
                            }
                        }
                       
                        SetMessageDeviceStatus(Messages, 3);
                        break;
                    case 0x02:
                        MessageArrived(string.Format("设置终端密码"));
                        break;
                    case 0x03:
                        MessageArrived(string.Format("主站下发参数配置"));
                        break;
                    case 0x05:
                        MessageArrived(string.Format("终端心跳信息"));
                        status = GetMessageDeviceStatus(Messages);
                        if (status == 2)
                        {
                            sendlenth = ReceiveUdpClient.Send(CreatCheckTimeOrder(Messages), CreatCheckTimeOrder(Messages).Length, refIPEndPoint);
                            if (sendlenth == Messages.Length)
                            {
                                MessageArrived(string.Format("{0}应答{1}:{2}", DateTime.Now.ToString(), refIPEndPoint, "应答指令:" + byteTOstring(Messages)));
                            }
                            SetMessageDeviceStatus(Messages, 3);
                        }
                        else if(status == 3)
                        {
                            sendlenth = ReceiveUdpClient.Send(Messages, Messages.Length, refIPEndPoint);
                            if (sendlenth == Messages.Length)
                            {
                                MessageArrived(string.Format("{0}应答{1}:{2}", DateTime.Now.ToString(), refIPEndPoint, "应答指令:" + byteTOstring(Messages)));
                            }
                            SetMessageDeviceStatus(Messages, 3);
                        }
                        break;
                    case 0x06:
                        MessageArrived(string.Format("更改主站IP地址、端口号和卡号"));
                        break;
                    case 0x07:
                        MessageArrived(string.Format("查询主站IP地址、端口号和卡号"));
                        break;
                    case 0x08:
                        MessageArrived(string.Format("终端复位"));
                        break;
                    case 0x09:
                        MessageArrived(string.Format("短信唤醒"));
                        break;
                    case 0x0A:
                        MessageArrived(string.Format("查询终端配置参数"));
                        break;
                    case 0x0B:
                        MessageArrived(string.Format("终端功能配置"));
                        break;
                    case 0x0C:
                        MessageArrived(string.Format("终端休眠"));
                        break;
                    case 0x0D:
                        MessageArrived(string.Format("查询终端设备时间"));
                        break;
                    case 0x21:
                        MessageArrived(string.Format("主站请求终端数据"));
                        break;
                    case 0x22:
                        MessageArrived(string.Format("上传导地线拉力及倾角数据"));
                        break;
                    case 0x25:
                        MessageArrived(string.Format("上传气象数据"));
                        break;
                    case 0x26:
                        MessageArrived(string.Format("上传导线温度、导线电流数据"));
                        break;
                    case 0x27:
                        MessageArrived(string.Format("上传杆塔振动数据"));
                        break;
                    case 0x29:
                        MessageArrived(string.Format("上传舞动振幅频率数据"));
                        break;
                    case 0x2A:
                        MessageArrived(string.Format("上传杆塔倾斜数据"));
                        break;
                    case 0x2B:
                        MessageArrived(string.Format("上传导线微风振动数据"));
                        break;
                    case 0x2C:
                        MessageArrived(string.Format("上传综合防盗数据"));
                        break;
                    case 0x2D:
                        MessageArrived(string.Format("上传山火报警数据"));
                        break;
                    case 0x2E:
                        MessageArrived(string.Format("上传大风舞动报警数据"));
                        break;
                    case 0x30:
                        MessageArrived(string.Format("上传设备故障信息"));
                        break;
                    case 0x31:
                        MessageArrived(string.Format("主站请求微风振动动态数据"));
                        break;
                    case 0x32:
                        MessageArrived(string.Format("上传微风振动动态数据"));
                        break;
                    case 0x33:
                        MessageArrived(string.Format("微风振动动态数据上传结束标记"));
                        break;
                    case 0x34:
                        MessageArrived(string.Format("下发微风振动动态数据补报"));
                        break;
                    case 0x35:
                        MessageArrived(string.Format("主站请求舞动动态数据"));
                        break;
                    case 0x36:
                        MessageArrived(string.Format("舞动动态数据上传"));
                        break;
                    case 0x38:
                        MessageArrived(string.Format("下发舞动动态数据补包"));
                        break;
                    case 0x39:
                        MessageArrived(string.Format("主站请求拉力及偏角动态数据"));
                        break;
                    case 0x3A:
                        MessageArrived(string.Format("上传拉力及偏角动态数据"));
                        break;
                    case 0x3B:
                        MessageArrived(string.Format("拉力及偏角动态数据上传结束标记"));
                        break;
                    case 0x3C:
                        MessageArrived(string.Format("下发拉力及偏角动态数据补包"));
                        break;
                    case 0x3D:
                        MessageArrived(string.Format("下发拉力及偏角动态数据补包"));
                        break;
                    case 0x41:
                        MessageArrived(string.Format("上传污秽数据"));
                        break;
                    case 0x42:
                        MessageArrived(string.Format("上传导线弧垂数据"));
                        break;
                    case 0x43:
                        MessageArrived(string.Format("上传电缆温度数据"));
                        break;
                    case 0x44:
                        MessageArrived(string.Format("上传电缆护层接地电流数据"));
                        break;
                    case 0x45:
                        MessageArrived(string.Format("上传故障定位数据"));
                        break;
                    case 0x46:
                        MessageArrived(string.Format("上传电缆局放数据"));
                        break;
                    case 0x60:
                        MessageArrived(string.Format("主站设置故障测距终端参数"));
                        break;
                    case 0x6A:
                        MessageArrived(string.Format("主站查询故障测距终端参数"));
                        break;
                    case 0x61:
                        MessageArrived(string.Format("上传故障测距终端工况数据"));
                        break;
                    case 0x62:
                        MessageArrived(string.Format("上传工频故障波形数据"));
                        break;
                    case 0x64:
                        MessageArrived(string.Format("终端向主站请求上传故障行波波形数据"));
                        break;
                    case 0x65:
                        MessageArrived(string.Format("上传故障行波波形数据"));
                        break;
                    case 0x66:
                        MessageArrived(string.Format("故障行波波形数据上传结束标志"));
                        break;
                    case 0x67:
                        MessageArrived(string.Format("主站向终端发送故障行波波形数据补包"));
                        break;
                    case 0x71:
                        MessageArrived(string.Format("主站查询终端文件列表"));
                        break;
                    case 0x72:
                        MessageArrived(string.Format("主站请求终端上传文件"));
                        break;
                    case 0x73:
                        MessageArrived(string.Format("终端请求上传文件"));
                        break;
                    case 0x74:
                        MessageArrived(string.Format("上传文件"));
                        break;
                    case 0x75:
                        MessageArrived(string.Format("文件上传结束标记"));
                        break;
                    case 0x76:
                        MessageArrived(string.Format("下发文件补包数据"));
                        break;
                    case 0x81:
                        MessageArrived(string.Format("图像采集参数配置"));
                        break;
                    case 0x82:
                        MessageArrived(string.Format("拍照时间表设置"));
                        break;
                    case 0x83:
                        MessageArrived(string.Format("设备应答主站请求拍摄照片"));
                        break;
                    case 0x84:
                        MessageArrived(string.Format("采集终端请求上传照片"));
                        /*数据需要原始返回给设备说明准备好了*/
                        sendlenth = ReceiveUdpClient.Send(Messages, Messages.Length, refIPEndPoint);
                        if (sendlenth == Messages.Length)
                        {
                            MessageArrived(string.Format("{0}应答{1}:{2}", DateTime.Now.ToString(), refIPEndPoint, "应答指令:" + byteTOstring(Messages)));
                        }
                        /*开始接收图片*/
                        /*解析总包数*/
                        DealNewPictureTrans(Messages);
                        break;
                    case 0x85:
                        MessageArrived(string.Format("上传图像数据"));
                        //把数据包组装到内存数组
                        AddPackDataToListData(Messages);
                        break;
                    case 0x86:
                        MessageArrived(string.Format("图像数据上传结束标记"));
                        /*结束后统计数据包，补发数据*/
                        CheakListData(Messages);
                        break;
                    case 0x87:
                        MessageArrived(string.Format("下发补包数据"));
                        break;
                    case 0x89:
                        MessageArrived(string.Format("启动摄像视频传输"));
                        break;
                    case 0x8A:
                        MessageArrived(string.Format("终止摄像视频传输"));
                        break;
                    case 0x8B:
                        MessageArrived(string.Format("查询拍照时间表"));
                        break;
                }
                Console.WriteLine("命令格式正确");
            }
            else
            {
                MessageArrived(string.Format("{0}来自{1}:{2}{3}", DateTime.Now.ToString(), refIPEndPoint, "设备指令:"+byteTOstring(Messages), "命令格式错误"));
            }
        }
        /// <summary>
        /// 检查内存中图片结构体正常否
        /// </summary>
        /// <param name="Messages"></param>
        /// <returns></returns>
        public int DealNewPictureTrans(byte[] Messages)
        {
            int status = 0;
            string Device_IMEI = getDeviceImei(Messages);
            
            for (int i = 0; i < mDevicePicture.Count; i++)
            {
                if (mDevicePicture[i].GetDeviceIMEI() == Device_IMEI)
                {       
                    mDevicePicture[i].Init(getPictureTotalPackageNum(Messages));
                    return i;
                }
            }
            DevicePicture OnePicture = new DevicePicture(); ;
            OnePicture.Init(getPictureTotalPackageNum(Messages));
            OnePicture.SetDeviceIMEI(Device_IMEI);
            mDevicePicture.Add(OnePicture);
            return status;
        }
        /// <summary>
        /// 获取当前图片总包数
        /// </summary>
        /// <param name="Messages"></param>
        /// <returns></returns>
        public int getPictureTotalPackageNum(byte[] Messages)
        {
            byte HPackages = Messages[18];
            byte LPackages = Messages[19];
            int TotalNum = HPackages + LPackages;
            return (TotalNum);
        }
        /// <summary>
        /// 把数据包添加至内存中
        /// </summary>
        /// <param name="Messages"></param>
        /// <returns></returns>
        public int AddPackDataToListData(byte[] Messages)
        {
            int status = 0;
            string Device_IMEI = getDeviceImei(Messages);

            for (int i = 0; i < mDevicePicture.Count; i++)
            {
                if (mDevicePicture[i].GetDeviceIMEI() == Device_IMEI)
                {
                    int nowPackageNum = Messages[12] + Messages[13] - 1;//减去1 因为内存是从0开始的
                    int nowPackageLenth = Messages[8]*256 + Messages[9];
                    byte[] addBuff = Messages.Skip(13).Take(nowPackageLenth).ToArray();
                    mDevicePicture[i].AddDataToList(addBuff,nowPackageNum);
                    return i;
                }
            }
            return status;
        }

        public int CheakListData(byte[] Messages)
        {
            int status = 0;
            string Device_IMEI = getDeviceImei(Messages);

            for (int i = 0; i < mDevicePicture.Count; i++)
            {
                if (mDevicePicture[i].GetDeviceIMEI() == Device_IMEI)
                {
                   
                    byte[] MissPackages = mDevicePicture[i].CheckMissPackages();
                    if(MissPackages.Length==0)
                    {
                        byte[] orderByte = new byte[MissPackages.Length + 3];
                        orderByte[0] = 0x01;
                        orderByte[1] = 0x00;
                        orderByte[2] = (byte)MissPackages.Length;
                        for (int j = 0; j < MissPackages.Length; j++)
                        {
                            orderByte[j + 3] = MissPackages[j];
                        }
                        SendMessage_GetMissPicturePackages(Device_IMEI, MissPackages);
                    }
                    else
                    {
                        Console.WriteLine("组装数据");
                    }
                    
                    return i;
                }
            }
            return status;
        }
        /// <summary>
        /// byte[]转化为string
        /// </summary>
        /// <param name="Message"></param>
        /// <returns></returns>
        public string byteTOstring(byte[] Message)
        {
            string receiveMessage = "";
            foreach (byte mByte in Message)
            {
                receiveMessage += " " + mByte.ToString("X2");
            }
            return receiveMessage;
        }

        /// <summary>
        /// string转化为byte[]
        /// </summary>
        /// <param name="Message"></param>
        /// <returns></returns>
        public byte[] stringTObyte(string Message)
        {
            //             List<byte> byteSource = new List<byte>();
            //             byteSource.Add(0x16);
            //             byte[] TimeOrder = byteSource.ToArray();
            //             return TimeOrder;
            byte[] byteArray = System.Text.Encoding.Default.GetBytes(Message);
            return byteArray;

        }
        /// <summary>
        /// 检查数据是否合法
        /// </summary>
        /// <param name="Messages"></param>
        /// <returns></returns>
        public bool CheckMessage(byte[] Messages)
        {

            
            int Slenth = Messages.Length;//实际长度
            if (Slenth < 8)
                return false;
            int Ylenth =Messages[8]*255 + Messages[9] + 12;//预计长度
            if (Slenth != Ylenth)
                return false;
            else
            {
                int CheckByte = 0;
                for (int i = 1; i<Messages.Length-2; i++ )
                {
                    CheckByte += Messages[i];
                }
                CheckByte = 255 - CheckByte % 256;
                if(CheckByte==Messages[Messages.Length-2])
                {
                    return true;
                }
                else
                    return false;

            }
        }
        /// <summary>
        /// 从数据帧中获取设备编号
        /// </summary>
        /// <param name="Messages"></param>
        /// <returns></returns>
        public string getDeviceImei(byte[] Messages)
        {
            string Device_IMEI="";
            for(int i=1; i<7; i++)
            {
                Device_IMEI += Convert.ToChar(Messages[i]);
            }
            return Device_IMEI;
        }

        /// <summary>
        /// 在数据库中查找设备，更新设备最后数据时间
        /// </summary>
        /// <param name="Device_IMEI"></param>
        public void CheckDeviceInMysql(byte[] Message)
        {
            //string strSQL = "select case when COUNT(*)>0 then '存在' when count(*)=0 then '不存在' end from device where IMEI =" + "'" + Device_IMEI + "'";
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");//
            string strSQL = string.Format("UPDATE device SET TIME='{0}' where IMEI='{1}'",time, getDeviceImei(Message));
            MySqlDataReader reader = MySqlHelper.ExecuteReader(MySqlHelper.Conn, CommandType.Text, strSQL, null);
            if (reader.HasRows)
            {
                // reader中有数据  
                while(reader.Read())
                {
                    if (reader.GetString(0) == "成功")
                    {
                        Console.WriteLine("yes");
                    }
                    
                }
                
            }
            else
            {
                //reader中没有数据
                Console.WriteLine("no");
            }
            
        }

        /// <summary>
        /// 检查设备数据是否已经在内存中、如果没有就把此设备增加进内存
        /// 使用地点 开机指令 心跳
        /// </summary>
        /// <param name="Device_IMEI"></param>
        /// <param name="refIPEndPoint"></param>
        /// <param name="Messages"></param>
        public void CheckDeviceList(byte[] Messages,IPEndPoint refIPEndPoint)
        {
            string Device_IMEI = getDeviceImei(Messages);
            string dsVersion = Messages[10].ToString("X2") + "." + Messages[11].ToString("X2");
            for (int i = 0; i < mDevice.Count; i++)
            {
                 if(mDevice[i].GetDeviceIMEI() == Device_IMEI)
                 {
                    mDevice[i].SetIPEndPoint(refIPEndPoint);
                    mDevice[i].SetLastMessage(Messages);
                    mDevice[i].SetLastTime(DateTime.Now);
                   
                    mDevice[i].SetVersion(dsVersion);
                    return;
                 }
            }
            ElecDevice OneDevice = new ElecDevice(); ;
            OneDevice.Init();
            OneDevice.SetDeviceIMEI(Device_IMEI);
            OneDevice.SetIPEndPoint(refIPEndPoint);
            OneDevice.SetLastMessage(Messages);
            OneDevice.SetStatus(0);
            OneDevice.SetVersion(dsVersion);
            mDevice.Add(OneDevice);
           
        }
        /// <summary>
        /// 服务器生成校时数据
        /// </summary>
        /// <param name="Message"></param>
        /// <returns></returns>
        public byte[] CreatCheckTimeOrder(byte[] Message)
        {
            List<byte> byteSource = new List<byte>();
           
            byteSource.Add(0x68);
            byteSource.Add(Message[1]);
            byteSource.Add(Message[2]);
            byteSource.Add(Message[3]);
            byteSource.Add(Message[4]);
            byteSource.Add(Message[5]);
            byteSource.Add(Message[6]);
            byteSource.Add(0x01);
            byteSource.Add(0x00);
            byteSource.Add(0x06);
            //byte[] nowTime = BitConverter.GetBytes(DateTime.Now.);
            DateTime now = DateTime.Now;
            int tempdate = now.Year - 2000;
            byteSource.Add((byte)(tempdate));
            tempdate = now.Month;
            byteSource.Add((byte)(tempdate));
            tempdate = now.Day;
            byteSource.Add((byte)(tempdate));
            tempdate = now.Hour;
            byteSource.Add((byte)(tempdate));
            tempdate = now.Minute;
            byteSource.Add((byte)(tempdate));
            tempdate = now.Second;
            byteSource.Add((byte)(tempdate));

            int CheckByte = 0;
            for (int i = 1; i < byteSource.Count; i++)
            {
                CheckByte += byteSource[i];
            }
            CheckByte = 255 - CheckByte % 256;

            byteSource.Add((byte)(CheckByte));
            byteSource.Add(0x16);
            byte[] TimeOrder = byteSource.ToArray();
            return TimeOrder;
        }
       
        /// <summary>
        /// 查找当前设备状态 返回0说明内存中没有或者掉线
        /// </summary>
        /// <param name="Message"></param>
        /// <returns></returns>
        public int GetMessageDeviceStatus(byte[] Message)
        {
            int status = 0;
            for (int i = 0; i < mDevice.Count; i++)
            {
                if (mDevice[i].GetDeviceIMEI() == getDeviceImei(Message))
                {
                    status = mDevice[i].GetStatus();
                    return status;
                }
            }
            return status;
        }
        /// <summary>
        /// 设置设备状态，如果成功返回设备在内存中的编号，如果失败返回0
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="res"></param>
        public int SetMessageDeviceStatus(byte[] Message, int res)
        {
            int status = 0;
            for (int i = 0; i < mDevice.Count; i++)
            {
                if (mDevice[i].GetDeviceIMEI() == getDeviceImei(Message))
                {
                    mDevice[i].SetStatus(res);
                    mDevice[i].SetLastMessage(Message);
                    
                    status = i;
                    return status;
                }
            }
            return status;
        }
        /// <summary>
        /// 发送设置密码指令
        /// </summary>
        /// <param name="SelectDeviceName"></param>
        /// <param name="strPassWord"></param>
        /// <returns></returns>
        public int SendMessage_SetDevicePassWord(string SelectDeviceName, byte[] strPassWord)
        {
            int status = 0;
            for (int i = 0; i < mDevice.Count; i++)
            {
                if (mDevice[i].GetDeviceIMEI() == SelectDeviceName && mDevice[i].GetStatus()>0)
                {
                    //mDevice[i].SetStatus(res);
                    byte[] Messages = CreateSetDevicePassWordOrder(stringTObyte(SelectDeviceName),strPassWord);
                    int sendlenth = ReceiveUdpClient.Send(Messages, Messages.Length, mDevice[i].GetDeviceIPEndPoint());
                    if (sendlenth == Messages.Length)
                    {
                        MessageArrived(string.Format("{0}应答{1}:{2}", DateTime.Now.ToString(), mDevice[i].GetDeviceIPEndPoint(), "应答指令:" + byteTOstring(Messages)));
                    }
                    status = i;
                    return status;
                }
            }
            return status;

        }

        /// <summary>
        /// 创建设置设备密码数据包
        /// </summary>
        /// <param name="DeviceIMEI"></param>
        /// <param name="strPassWord"></param>
        /// <returns></returns>
        public byte[] CreateSetDevicePassWordOrder(byte[] DeviceIMEI, byte[] strPassWord)
        {
            List<byte> byteSource = new List<byte>();

            byteSource.Add(0x68);
            byteSource.Add(DeviceIMEI[0]);
            byteSource.Add(DeviceIMEI[1]);
            byteSource.Add(DeviceIMEI[2]);
            byteSource.Add(DeviceIMEI[3]);
            byteSource.Add(DeviceIMEI[4]);
            byteSource.Add(DeviceIMEI[5]);
            byteSource.Add(0x02);
            byteSource.Add(0x00);
            byteSource.Add(0x08);
            //byte[] nowTime = BitConverter.GetBytes(DateTime.Now.);
            byteSource.Add(strPassWord[0]);
            byteSource.Add(strPassWord[1]);
            byteSource.Add(strPassWord[2]);
            byteSource.Add(strPassWord[3]);
            byteSource.Add(strPassWord[4]);
            byteSource.Add(strPassWord[5]);
            byteSource.Add(strPassWord[6]);
            byteSource.Add(strPassWord[7]);

            int CheckByte = 0;
            for (int i = 1; i < byteSource.Count; i++)
            {
                CheckByte += byteSource[i];
            }
            CheckByte = 255 - CheckByte % 256;

            byteSource.Add((byte)(CheckByte));
            byteSource.Add(0x16);
            byte[] PassWordOrder = byteSource.ToArray();
            return PassWordOrder;
        }

        /// <summary>
        /// 获得设备状态
        /// </summary>
        /// <param name="SelectDeviceName"></param>
        /// <returns></returns>
        public int GetDeviceStatus(string SelectDeviceName)
        {
            int status = 0;
            for (int i = 0; i < mDevice.Count; i++)
            {
                if (mDevice[i].GetDeviceIMEI() == SelectDeviceName && mDevice[i].GetStatus() == 3)
                {
                    MessageDeviceStatus(mDevice[i].GetDeviceLastMessage());
                    status = i;
                    return status;
                }
            }
            return status;
           
        }

        /// <summary>
        /// 发送设备设置参数
        /// </summary>
        /// <param name="SelectDeviceName"></param>
        /// <param name="strParameter"></param>
        /// <returns></returns>
        public int SendMessage_SetDeviceStationParameter(string SelectDeviceName, byte[] strParameter)
        {
            int status = 0;
            for (int i = 0; i < mDevice.Count; i++)
            {
                if (mDevice[i].GetDeviceIMEI() == SelectDeviceName && mDevice[i].GetStatus() > 0)
                {
                    //mDevice[i].SetStatus(res);
                    byte[] DataLenth = new byte[2];
                    DataLenth[0] = 0x00;
                    DataLenth[1] = 0x1C;
                    byte[] Messages = CreateSetDeviceByOrder(stringTObyte(SelectDeviceName), strParameter, 0x06, DataLenth);
                    //byte[] Messages = CreateSetDeviceParameter(stringTObyte(SelectDeviceName), strParameter);
                    int sendlenth = ReceiveUdpClient.Send(Messages, Messages.Length, mDevice[i].GetDeviceIPEndPoint());
                    if (sendlenth == Messages.Length)
                    {
                        MessageArrived(string.Format("{0}应答{1}:{2}", DateTime.Now.ToString(), mDevice[i].GetDeviceIPEndPoint(), "应答指令:" + byteTOstring(Messages)));
                    }
                    status = i;
                    return status;
                }
            }
            return status;
        }

        /// <summary>
        /// 创建设置设备参数指令
        /// </summary>
        /// <param name="DeviceIMEI"></param>
        /// <param name="strParameter"></param>
        /// <returns></returns>
        public byte[] CreateSetDeviceParameter(byte[] DeviceIMEI, byte[] strParameter)
        {
            List<byte> byteSource = new List<byte>();

            byteSource.Add(0x68);
            byteSource.Add(DeviceIMEI[0]);
            byteSource.Add(DeviceIMEI[1]);
            byteSource.Add(DeviceIMEI[2]);
            byteSource.Add(DeviceIMEI[3]);
            byteSource.Add(DeviceIMEI[4]);
            byteSource.Add(DeviceIMEI[5]);
            byteSource.Add(0x06);
            byteSource.Add(0x00);
            byteSource.Add(0x1C);
            //byte[] nowTime = BitConverter.GetBytes(DateTime.Now.);
            byteSource.Add(strParameter[0]);
            byteSource.Add(strParameter[1]);
            byteSource.Add(strParameter[2]);
            byteSource.Add(strParameter[3]);
            byteSource.Add(strParameter[4]);
            byteSource.Add(strParameter[5]);
            byteSource.Add(strParameter[6]);
            byteSource.Add(strParameter[7]);

            int CheckByte = 0;
            for (int i = 1; i < byteSource.Count; i++)
            {
                CheckByte += byteSource[i];
            }
            CheckByte = 255 - CheckByte % 256;

            byteSource.Add((byte)(CheckByte));
            byteSource.Add(0x16);
            byte[] PassWordOrder = byteSource.ToArray();
            return PassWordOrder;
        }

        /// <summary>
        /// 创建设备指令
        /// </summary>
        /// <param name="DeviceIMEI"></param>
        /// <param name="strParameter"></param>
        /// <param name="Order"></param>
        /// <param name="DataLenth"></param>
        /// <returns></returns>
        public byte[] CreateSetDeviceByOrder(byte[] DeviceIMEI, byte[] strParameter,byte Order, byte[]DataLenth)
        {
            List<byte> byteSource = new List<byte>();
            int i = 0;
            byteSource.Add(0x68);
            byteSource.Add(DeviceIMEI[0]);
            byteSource.Add(DeviceIMEI[1]);
            byteSource.Add(DeviceIMEI[2]);
            byteSource.Add(DeviceIMEI[3]);
            byteSource.Add(DeviceIMEI[4]);
            byteSource.Add(DeviceIMEI[5]);
            byteSource.Add(Order);
            byteSource.Add(DataLenth[0]);
            byteSource.Add(DataLenth[1]);
            if(strParameter!=null)
            {
                for (i = 0; i < strParameter.Length; i++)
                {
                    byteSource.Add(strParameter[i]);
                }
            }

            int CheckByte = 0;
            for (i = 1; i < byteSource.Count; i++)
            {
                CheckByte += byteSource[i];
            }
            CheckByte = 255 - CheckByte % 256;

            byteSource.Add((byte)(CheckByte));
            byteSource.Add(0x16);
            byte[] PassWordOrder = byteSource.ToArray();
            return PassWordOrder;
        }
        /// <summary>
        /// 发送设置重置指令
        /// </summary>
        /// <param name="SelectDeviceName"></param>
        /// <param name="strParameter"></param>
        /// <returns></returns>
        public int SendMessage_SetDeviceReset(string SelectDeviceName, byte[] strParameter)
        {
            int status = 0;
            for (int i = 0; i < mDevice.Count; i++)
            {
                if (mDevice[i].GetDeviceIMEI() == SelectDeviceName && mDevice[i].GetStatus() > 0)
                {
                    //mDevice[i].SetStatus(res);
                    byte[] DataLenth = new byte[2];
                    DataLenth[0] = 0x00;
                    DataLenth[1] = 0x04;
                    byte[] Messages = CreateSetDeviceByOrder(stringTObyte(SelectDeviceName), strParameter, 0x08, DataLenth);
                    //byte[] Messages = CreateSetDeviceParameter(stringTObyte(SelectDeviceName), strParameter);
                    int sendlenth = ReceiveUdpClient.Send(Messages, Messages.Length, mDevice[i].GetDeviceIPEndPoint());
                    if (sendlenth == Messages.Length)
                    {
                        MessageArrived(string.Format("{0}应答{1}:{2}", DateTime.Now.ToString(), mDevice[i].GetDeviceIPEndPoint(), "应答指令:" + byteTOstring(Messages)));
                    }
                    status = i;
                    return status;
                }
            }
            return status;
        }

        /// <summary>
        /// 唤醒设备
        /// </summary>
        /// <param name="SelectDeviceName"></param>
        /// <param name="strParameter"></param>
        /// <returns></returns>
        public int SendMessage_SetDeviceWeekUp(string SelectDeviceName, byte[] strParameter)
        {
            int status = 0;
            for (int i = 0; i < mDevice.Count; i++)
            {
                if (mDevice[i].GetDeviceIMEI() == SelectDeviceName && mDevice[i].GetStatus() > 0)
                {
                    //mDevice[i].SetStatus(res);
                    byte[] DataLenth = new byte[2];
                    DataLenth[0] = 0x00;
                    DataLenth[1] = 0x04;
                    byte[] Messages = CreateSetDeviceByOrder(stringTObyte(SelectDeviceName), strParameter, 0x09, DataLenth);
                    //byte[] Messages = CreateSetDeviceParameter(stringTObyte(SelectDeviceName), strParameter);
                    int sendlenth = ReceiveUdpClient.Send(Messages, Messages.Length, mDevice[i].GetDeviceIPEndPoint());
                    if (sendlenth == Messages.Length)
                    {
                        MessageArrived(string.Format("{0}应答{1}:{2}", DateTime.Now.ToString(), mDevice[i].GetDeviceIPEndPoint(), "应答指令:" + byteTOstring(Messages)));
                    }
                    status = i;
                    return status;
                }
            }
            return status;
        }

        /// <summary>
        /// 查询设备配置
        /// </summary>
        /// <param name="SelectDeviceName"></param>
        /// <returns></returns>
        public int SendMessage_GetDeviceInfo(string SelectDeviceName)
        {
            int status = 0;
            for (int i = 0; i < mDevice.Count; i++)
            {
                if (mDevice[i].GetDeviceIMEI() == SelectDeviceName && mDevice[i].GetStatus() > 0)
                {
                    //mDevice[i].SetStatus(res);
                    byte[] DataLenth = new byte[2];
                    DataLenth[0] = 0x00;
                    DataLenth[1] = 0x00;
                    byte[] Messages = CreateSetDeviceByOrder(stringTObyte(SelectDeviceName), null, 0x0A, DataLenth);
                    //byte[] Messages = CreateSetDeviceParameter(stringTObyte(SelectDeviceName), strParameter);
                    int sendlenth = ReceiveUdpClient.Send(Messages, Messages.Length, mDevice[i].GetDeviceIPEndPoint());
                    if (sendlenth == Messages.Length)
                    {
                        MessageArrived(string.Format("{0}应答{1}:{2}", DateTime.Now.ToString(), mDevice[i].GetDeviceIPEndPoint(), "应答指令:" + byteTOstring(Messages)));
                    }
                    status = i;
                    return status;
                }
            }
            return status;
        }

        /// <summary>
        /// 设置设备功能
        /// </summary>
        /// <param name="SelectDeviceName"></param>
        /// <param name="strParameter"></param>
        /// <returns></returns>
        public int SendMessage_SetDeviceFunction(string SelectDeviceName, byte[] strParameter)
        {
            int status = 0;
            for (int i = 0; i < mDevice.Count; i++)
            {
                if (mDevice[i].GetDeviceIMEI() == SelectDeviceName && mDevice[i].GetStatus() > 0)
                {
                    //mDevice[i].SetStatus(res);
                    byte[] DataLenth = new byte[2];
                    DataLenth[0] = 0x00;
                    DataLenth[1] = 0x05;
                    byte[] Messages = CreateSetDeviceByOrder(stringTObyte(SelectDeviceName), strParameter, 0x0B, DataLenth);
                    //byte[] Messages = CreateSetDeviceParameter(stringTObyte(SelectDeviceName), strParameter);
                    int sendlenth = ReceiveUdpClient.Send(Messages, Messages.Length, mDevice[i].GetDeviceIPEndPoint());
                    if (sendlenth == Messages.Length)
                    {
                        MessageArrived(string.Format("{0}应答{1}:{2}", DateTime.Now.ToString(), mDevice[i].GetDeviceIPEndPoint(), "应答指令:" + byteTOstring(Messages)));
                    }
                    status = i;
                    return status;
                }
            }
            return status;
        }

        /// <summary>
        /// 查询设备时间
        /// </summary>
        /// <param name="SelectDeviceName"></param>
        /// <returns></returns>
        public int SendMessage_GetDeviceTime(string SelectDeviceName)
        {
            int status = 0;
            for (int i = 0; i < mDevice.Count; i++)
            {
                if (mDevice[i].GetDeviceIMEI() == SelectDeviceName && mDevice[i].GetStatus() > 0)
                {
                    //mDevice[i].SetStatus(res);
                    byte[] DataLenth = new byte[2];
                    DataLenth[0] = 0x00;
                    DataLenth[1] = 0x00;
                    byte[] Messages = CreateSetDeviceByOrder(stringTObyte(SelectDeviceName), null, 0x0D, DataLenth);
                    //byte[] Messages = CreateSetDeviceParameter(stringTObyte(SelectDeviceName), strParameter);
                    int sendlenth = ReceiveUdpClient.Send(Messages, Messages.Length, mDevice[i].GetDeviceIPEndPoint());
                    if (sendlenth == Messages.Length)
                    {
                        MessageArrived(string.Format("{0}应答{1}:{2}", DateTime.Now.ToString(), mDevice[i].GetDeviceIPEndPoint(), "应答指令:" + byteTOstring(Messages)));
                    }
                    status = i;
                    return status;
                }
            }
            return status;
        }

        /// <summary>
        /// 发送历史数据指令
        /// </summary>
        /// <param name="SelectDeviceName"></param>
        /// <returns></returns>
        public int SendMessage_GetDeviceHistoryData(string SelectDeviceName)
        {
            int status = 0;
            for (int i = 0; i < mDevice.Count; i++)
            {
                if (mDevice[i].GetDeviceIMEI() == SelectDeviceName && mDevice[i].GetStatus() > 0)
                {
                    //mDevice[i].SetStatus(res);
                    byte[] DataLenth = new byte[2];
                    DataLenth[0] = 0x00;
                    DataLenth[1] = 0x02;
                    byte[] Messages = CreateSetDeviceByOrder(stringTObyte(SelectDeviceName), null, 0x21, DataLenth);
                    //byte[] Messages = CreateSetDeviceParameter(stringTObyte(SelectDeviceName), strParameter);
                    int sendlenth = ReceiveUdpClient.Send(Messages, Messages.Length, mDevice[i].GetDeviceIPEndPoint());
                    if (sendlenth == Messages.Length)
                    {
                        MessageArrived(string.Format("{0}应答{1}:{2}", DateTime.Now.ToString(), mDevice[i].GetDeviceIPEndPoint(), "应答指令:" + byteTOstring(Messages)));
                    }
                    status = i;
                    return status;
                }
            }
            return status;
        }

        /// <summary>
        /// 发送当前最新数据指令
        /// </summary>
        /// <param name="SelectDeviceName"></param>
        /// <param name="strParameter"></param>
        /// <returns></returns>
        public int SendMessage_GetDeviceNowData(string SelectDeviceName, byte[] strParameter)
        {
            int status = 0;
            for (int i = 0; i < mDevice.Count; i++)
            {
                if (mDevice[i].GetDeviceIMEI() == SelectDeviceName && mDevice[i].GetStatus() > 0)
                {
                    //mDevice[i].SetStatus(res);
                    byte[] DataLenth = new byte[2];
                    DataLenth[0] = 0x00;
                    DataLenth[1] = 0x02;
                    byte[] Messages = CreateSetDeviceByOrder(stringTObyte(SelectDeviceName), strParameter, 0x21, DataLenth);
                    //byte[] Messages = CreateSetDeviceParameter(stringTObyte(SelectDeviceName), strParameter);
                    int sendlenth = ReceiveUdpClient.Send(Messages, Messages.Length, mDevice[i].GetDeviceIPEndPoint());
                    if (sendlenth == Messages.Length)
                    {
                        MessageArrived(string.Format("{0}应答{1}:{2}", DateTime.Now.ToString(), mDevice[i].GetDeviceIPEndPoint(), "应答指令:" + byteTOstring(Messages)));
                    }
                    status = i;
                    return status;
                }
            }
            return status;
        }

        /// <summary>
        /// 发送请求照片指令
        /// </summary>
        /// <param name="SelectDeviceName"></param>
        /// <param name="strParameter"></param>
        /// <returns></returns>
        public int SendMessage_GetDeviceNowPicture(string SelectDeviceName, byte[] strParameter)
        {
            int status = 0;
            for (int i = 0; i < mDevice.Count; i++)
            {
                if (mDevice[i].GetDeviceIMEI() == SelectDeviceName && mDevice[i].GetStatus() > 0)
                {
                    //mDevice[i].SetStatus(res);
                    byte[] DataLenth = new byte[2];
                    DataLenth[0] = 0x00;
                    DataLenth[1] = 0x02;
                    byte[] Messages = CreateSetDeviceByOrder(stringTObyte(SelectDeviceName), strParameter, 0x83, DataLenth);
                    //byte[] Messages = CreateSetDeviceParameter(stringTObyte(SelectDeviceName), strParameter);
                    int sendlenth = ReceiveUdpClient.Send(Messages, Messages.Length, mDevice[i].GetDeviceIPEndPoint());
                    if (sendlenth == Messages.Length)
                    {
                        MessageArrived(string.Format("{0}应答{1}:{2}", DateTime.Now.ToString(), mDevice[i].GetDeviceIPEndPoint(), "应答指令:" + byteTOstring(Messages)));
                    }
                    status = i;
                    return status;
                }
            }
            return status;
        }

        /// <summary>
        /// 下发图片补包指令
        /// </summary>
        /// <param name="SelectDeviceName"></param>
        /// <param name="strParameter"></param>
        /// <returns></returns>
        public int SendMessage_GetMissPicturePackages(string DeviceIMEI, byte[] strParameter)
        {
            int status = 0;
            for (int i = 0; i < mDevice.Count; i++)
            {
                if (mDevice[i].GetDeviceIMEI() == DeviceIMEI && mDevice[i].GetStatus() > 0)
                {
                    //mDevice[i].SetStatus(res);
                    byte[] DataLenth = new byte[2];
                    DataLenth[0] = (byte)(strParameter.Length/255);
                    DataLenth[1] = (byte)(strParameter.Length % 255);
                    byte[] Messages = CreateSetDeviceByOrder(stringTObyte(DeviceIMEI), strParameter, 0x87, DataLenth);
                    //byte[] Messages = CreateSetDeviceParameter(stringTObyte(SelectDeviceName), strParameter);
                    int sendlenth = ReceiveUdpClient.Send(Messages, Messages.Length, mDevice[i].GetDeviceIPEndPoint());
                    if (sendlenth == Messages.Length)
                    {
                        MessageArrived(string.Format("{0}应答{1}:{2}", DateTime.Now.ToString(), mDevice[i].GetDeviceIPEndPoint(), "应答指令:" + byteTOstring(Messages)));
                    }
                    status = i;
                    return status;
                }
            }
            return status;
        }

    }
}