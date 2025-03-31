using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Helpers
{
    internal class UpdateTimeHelper
    {
        //设置系统时间的API函数
        [DllImport("kernel32.dll")]
        private static extern bool SetLocalTime(ref SYSTEMTIME time);

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEMTIME
        {
            public ushort year;
            public ushort month;
            public ushort dayOfWeek;
            public ushort day;
            public ushort hour;
            public ushort minute;
            public ushort second;
            public ushort milliseconds;
        }

        /// <summary>  
        /// 获取标准北京时间，读取http://www.beijing-time.org/time.asp  
        /// </summary>  
        /// <returns>返回网络时间</returns>  
        public static DateTime GetBeijingTime()
        {
            // NTP服务器地址 "ntp.ntsc.ac.cn"
            string ntpServer = "ntp.ntsc.ac.cn";

            // 创建UDP套接字
            UdpClient udpClient = new UdpClient(ntpServer, 123);

            // 发送NTP请求包
            byte[] ntpData = new byte[48];
            ntpData[0] = 0x1B;
            udpClient.Send(ntpData, ntpData.Length);

            // 接收NTP响应包
            IPEndPoint remoteEP = null;
            byte[] responseData = udpClient.Receive(ref remoteEP);

            // 关闭UDP套接字
            udpClient.Close();

            // 解析NTP响应包中的时间戳
            ulong intPart = (ulong)responseData[40] << 24 | (ulong)responseData[41] << 16 | (ulong)responseData[42] << 8 | responseData[43];
            ulong fractPart = (ulong)responseData[44] << 24 | (ulong)responseData[45] << 16 | (ulong)responseData[46] << 8 | responseData[47];
            ulong milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

            // 计算北京时间
            DateTime ntpTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds((long)milliseconds);
            DateTime beijingTime = TimeZoneInfo.ConvertTimeFromUtc(ntpTime, TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"));

            return beijingTime;
        }

        /// <summary>
        /// 设置系统时间
        /// </summary>
        /// <param name="dt">需要设置的时间</param>
        /// <returns>返回系统时间设置状态，true为成功，false为失败</returns>
        public static bool SetDate()
        {
            SYSTEMTIME st = new SYSTEMTIME();
            DateTime dt = GetBeijingTime();
            st.year = Convert.ToUInt16(dt.Year);
            st.month = Convert.ToUInt16(dt.Month);
            st.dayOfWeek = Convert.ToUInt16(dt.DayOfWeek);
            st.day = Convert.ToUInt16(dt.Day);
            st.hour = Convert.ToUInt16(dt.Hour);
            st.minute = Convert.ToUInt16(dt.Minute);
            st.second = Convert.ToUInt16(dt.Second);
            st.milliseconds = Convert.ToUInt16(dt.Millisecond);
            bool rt = false;
            try
            {
                rt = SetLocalTime(ref st);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return rt;
        }

        public static bool SetDate(DateTime dt)
        {
            SYSTEMTIME st = new SYSTEMTIME();
            st.year = Convert.ToUInt16(dt.Year);
            st.month = Convert.ToUInt16(dt.Month);
            st.dayOfWeek = Convert.ToUInt16(dt.DayOfWeek);
            st.day = Convert.ToUInt16(dt.Day);
            st.hour = Convert.ToUInt16(dt.Hour);
            st.minute = Convert.ToUInt16(dt.Minute);
            st.second = Convert.ToUInt16(dt.Second);
            st.milliseconds = Convert.ToUInt16(dt.Millisecond);
            bool rt = false;
            try
            {
                rt = SetLocalTime(ref st);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return rt;
        }

    }
}
