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
        static double receive(UdpClient listener ,IPEndPoint groupEP)
        {
            double angle = 0;
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
            return angle;
        }
        static  int Change(double x, double y)
        {
            if (x - y < -250) return 1;
            else if (x - y > 250) return 2;
            else return 0;
        }
        static int State_Transition(double lastx, double angle, int possible_states, int state)
        {
            if (Change(lastx, angle) == 1)
            {
                if (state != 0) return -1;
                else return 0;
            }
            else if (Change(lastx, angle) == 2)
            {
                if (state != possible_states+1) return 1;
                else return 0;
            }
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
            Console.WriteLine("Choose range(in degrees, e.g. 900):");
            double range = Convert.ToInt32(Console.ReadLine());
            Console.Clear();

            Console.WriteLine("Should work          ");
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine("                                      ");
            UdpClient listener = new UdpClient(servPort);
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, servPort);
            int state = 0;
            int possible_states = 0;
            possible_states = 2*(int)Math.Ceiling(((float)range / 2.0) / 360.0);
            double realAngle=0,normalized, lastx = 0;
            double angle = receive(listener, groupEP);
            if (angle < 180) state = 1 + (possible_states / 2);
            else state = possible_states / 2;
            lastx = angle;
            double range_limit_usable = (range / 2) / 360;
            double range_four_helper = ((range / 2) - 360) / 360;
            double range_four_proportion = ((range / 2) - 360) / range;
            double range_six_helper = ((range / 2) - 2*360) / 360;
            double range_six_proportion = ((range / 2) - 2*360) / range;
            double full_rotation_proportion = 360/range;
            try
            {
                while (true)
                {
                    angle = receive(listener, groupEP);
                    state += State_Transition(lastx, angle, possible_states, state);
                    normalized = angle / 360;
                    if (possible_states == 2)
                    {
                        if (state == 1)
                        {
                            realAngle = 0.5 - (0.5 * ((1 - normalized) / range_limit_usable));
                            if (realAngle <= 0) realAngle = 0;
                            else if (realAngle > 0.5) realAngle = 0.5;
                        }
                        else if (state == 2)
                        {
                            realAngle = 0.5 + 0.5 * (normalized / range_limit_usable);
                            if (realAngle <= 0.5) realAngle = 0.5;
                            else if (realAngle > 1) realAngle = 1;
                        }
                    }
                    if (possible_states==4)
                    {
                        if (state == 1)
                        {
                            realAngle = range_four_proportion - (range_four_proportion * ((1 - normalized) / range_four_helper));
                            if (realAngle <= 0) realAngle = 0;
                            else if (realAngle > range_four_proportion) realAngle = range_four_proportion;
                        }
                        else if (state == 2)
                        {
                            realAngle = 0.5 - (0.5 - range_four_proportion) * (1 - normalized);
                            if (realAngle <= range_four_proportion) realAngle = range_four_proportion;
                            else if (realAngle > 0.5) realAngle = 0.5;
                        }
                        else if (state == 3)
                        {
                            realAngle = 0.5 + (0.5 - range_four_proportion) * normalized;
                            if (realAngle <= 0.5) realAngle = 0.5;
                            else if (realAngle > 0.5 + (1 - range_four_proportion)) realAngle = 0.5 + (1 - range_four_proportion);
                        }
                        else if (state == 4)
                        {
                            realAngle = (1 - range_four_proportion) + range_four_proportion * (normalized / range_four_helper);
                            if (realAngle <= (1 - range_four_proportion)) realAngle = (1 - range_four_proportion);
                            else if (realAngle > 1) realAngle = 1;
                        }
                    }
                    if (possible_states==6)
                    {
                        if (state == 1)
                        {
                            realAngle = range_six_proportion - (range_six_proportion * ((1 - normalized) / range_six_helper));
                            if (realAngle <= 0) realAngle = 0;
                            else if (realAngle > range_six_proportion) realAngle = range_six_proportion;
                        }
                        else if (state == 2)
                        {
                            double temp = range_six_proportion + full_rotation_proportion;
                            realAngle = temp - full_rotation_proportion * (1 - normalized);
                            if (realAngle <= range_six_proportion) realAngle = range_six_proportion;
                            else if (realAngle > temp) realAngle = temp;
                        }
                        else if (state == 3)
                        {
                            realAngle = 0.5 - full_rotation_proportion * (1 - normalized);
                            if (realAngle <= 0.5 - full_rotation_proportion) realAngle = 0.5 - full_rotation_proportion;
                            else if (realAngle > 0.5) realAngle = 0.5;
                        }
                        else if (state == 4)
                        {
                            realAngle = 0.5 + full_rotation_proportion * normalized;
                            if (realAngle <= 0.5) realAngle = 0.5;
                            else if (realAngle > 0.5 + full_rotation_proportion) realAngle = 0.5 + full_rotation_proportion;
                        }
                        else if (state == 5)
                        {
                            double temp = 0.5 + full_rotation_proportion;
                            realAngle = temp + full_rotation_proportion * normalized;
                            if (realAngle <= temp) realAngle = temp;
                            else if (realAngle > 1 - range_six_proportion) realAngle = 1 - range_six_proportion;
                        }
                        else if (state == 6)
                        {
                            realAngle = (1 - range_six_proportion) + range_six_proportion * (normalized / range_six_helper);
                            if (realAngle <= (1 - range_six_proportion)) realAngle = (1 - range_six_proportion);
                            else if (realAngle > 1) realAngle = 1;
                        }
                    }
                    lastx = angle;
                    controller.SetAxisValue(Xbox360Axis.LeftThumbX, Convert.ToInt16((realAngle - 0.5) * 65534));
                   
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    ClearCurrentConsoleLine();
                    Console.WriteLine("Current angle: " + Math.Truncate((realAngle - 0.5) * range));
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