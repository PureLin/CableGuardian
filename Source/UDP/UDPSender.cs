using System;
using System.Net;
using System.Net.Sockets;
using Valve.VR;

namespace CableGuardian
{
    class UDPSender
    {
        /// <summary>
        /// 用于UDP发送的网络服务类
        /// </summary>
        private static UdpClient udpcSend = null;

        private static IPEndPoint localIpep = null;
        private static IPEndPoint remoteIpep = null;
        private static double[] values = new double[6];
        private static double positionZoom = 1.5;

        static UDPSender()
        {
            localIpep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999); // 本机IP和监听端口号
            remoteIpep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3055); // 发送到的IP地址和端口号
            udpcSend = new UdpClient();
        }


        /// <summary>
        /// 发送信息
        /// </summary>
        /// <param name="obj"></param>
        public static void SendYaw(double yaw, double pitch, double roll)
        {
            try
            {
                values[3] = (yaw / Math.PI * 180) * -1;
                values[4] = (pitch / Math.PI * 180);
                values[5] = (roll / Math.PI * 180) * 1.5;
                //Console.WriteLine($"{values[3]},{values[4]},{values[5]}");
                byte[] sendbytes = GetBytesAlt(values);
                udpcSend.Send(sendbytes, sendbytes.Length, remoteIpep);
            }
            catch { }
        }

        static byte[] GetBytesAlt(double[] values)
        {
            var result = new byte[values.Length * sizeof(double)];
            Buffer.BlockCopy(values, 0, result, 0, result.Length);
            return result;
        }

        internal static void Send(HmdRotation_t rotation, HmdPosition_t position)
        {
            try
            {
                values[0] = position.x / positionZoom * -1;
                values[1] = position.y / positionZoom * -1;
                values[2] = position.z / positionZoom - 0.9;
                values[3] = (rotation.yaw / Math.PI * 180) * -1;
                values[4] = (rotation.pitch / Math.PI * 180);
                values[5] = (rotation.roll / Math.PI * 180) * 1.5;
                Console.WriteLine($"{values[0]},{values[1]},{values[2]}");
                byte[] sendbytes = GetBytesAlt(values);
                udpcSend.Send(sendbytes, sendbytes.Length, remoteIpep);
            }
            catch { }
        }
    }
}
