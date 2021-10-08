using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
namespace DroidWheelServer
{
    class Program
    {
        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
        static  int Change(double x, double y)
        {
            if (x - y < -250) return 1;
            else if (x - y > 250) return 2;
            else return 0;
        }
        static void Main(string[] args)
        {
            string localIP;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }
            Console.WriteLine("Type this in the IP field in phone app:");
            Console.WriteLine(localIP);
            ViGEmClient client = new ViGEmClient();
            IXbox360Controller controller = client.CreateXbox360Controller();
            controller.Connect();
            Console.WriteLine("Choose port(e.g. 8888):");
            int servPort = Convert.ToInt32(Console.ReadLine());
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.WriteLine("                                      ");
            Console.SetCursorPosition(0, Console.CursorTop - 2);
            Console.WriteLine("                                      ");
            Console.SetCursorPosition(0, Console.CursorTop - 3);
            Console.WriteLine("                                      ");

            Console.WriteLine("Should work          ");
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine("                                      ");
            UdpClient listener = new UdpClient(servPort);
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, servPort);
            int state = 0;
            double realAngle=0,normalized, lastx = 0;
            try
            {
                while (true)
                {
                    double angle=0;
                    byte[] bytes = listener.Receive(ref groupEP);
                    Char[] chars;
                    Decoder d = Encoding.UTF8.GetDecoder();
                    int charCount = d.GetCharCount(bytes, 0, bytes.Length);
                    chars = new Char[charCount];
                    int charsDecodedCount = d.GetChars(bytes, 0, bytes.Length, chars, 0);
                    foreach (Char c in chars)
                    {
                        angle = angle * 10 + (c - '0');
                    }
                    Console.SetCursorPosition(0, Console.CursorTop-1);
                    Console.WriteLine(angle+"    ");
                    
                    if (state==0)
                    {
                        if (angle < 180) state = 3;
                        else state = 2;
                        lastx = angle;
                    }
                    else
                    {
                        if (Change(lastx, angle) == 1)
                        {
                            if (state != 1) state -= 1;
                        }
                        else if (Change(lastx, angle) == 2)
                        {
                            if (state != 4) state += 1;
                        }
                        normalized = angle / 360;
                        if (state == 2) realAngle = 0.1 + 0.4 * normalized;
                        else if (state == 1)
                        {
                            if (normalized >= 0.75) realAngle = ((normalized - 0.75) / 0.25) * 0.1;
                            else realAngle = 0;
                        }
                        else if (state == 3) realAngle = 0.5 + 0.4 * normalized;
                        else if (state == 4)
                        {
                            if (normalized <= 0.25) realAngle = 0.9 + ((normalized) / 0.25) * 0.1;
                            else realAngle = 0.999;
                        }
                        lastx = angle;
                        controller.SetAxisValue(Xbox360Axis.LeftThumbX, Convert.ToInt16((realAngle-0.5)*65534));
                    }
                    
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                listener.Close();
            }
        }
    }
}