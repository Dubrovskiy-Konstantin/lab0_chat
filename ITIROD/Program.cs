using System;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

#pragma warning disable S1118 // Utility classes should not have public constructors

namespace ITIROD
{
    class Program
    {
        private const string host = "127.0.0.1";
        private static int port;
        private static int currentPort;
        private static Socket udpSocket;
        private static Task reciver;
        private static string currentClientName = null;
        private static List<string> MessageHistory = new();

        static void Main()
        {

            try
            {
                Initialize();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                CloseConnaction();
            }
        }

        private static void Initialize()
        {
            SystemMessage("Enter commands (!cmd):");
            bool work = true;
            while (work)
            {
                var input = Console.ReadLine();
#pragma warning disable CA1416 // Проверка совместимости платформы
                int line = Console.GetCursorPosition().Top - 1;
                Console.MoveBufferArea(0, line, Console.BufferWidth, 1, Console.BufferWidth, line, ' ', Console.ForegroundColor, Console.BackgroundColor);
#pragma warning restore CA1416 // Проверка совместимости платформы
                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                if (input[0] == '!')
                {
                    var cmd = input.Split(' ');
                    switch (cmd[0])
                    {
                        case "!cmd":
                            {
                                SystemMessage("Commands: \n\t!cmd - command list;\n\t!login {name} - log in with name;\n\t!history - shows message history;\n\t!exit - stop program;\n");
                                break;
                            }
                        case "!l":
                        case "!li":
                        case "!login":
                            {
                                string name = null;
                                try
                                { name = cmd[1]; }
                                catch
                                { SystemMessage("Enter name!"); }
                                if (!string.IsNullOrWhiteSpace(name))
                                    LoginUser(name);
                                else
                                    SystemMessage("Enter name!");
                                break;
                            }
                        case "!hist":
                        case "!history":
                            {
                                SystemMessage("Chat histiry:");
                                foreach (var msg in MessageHistory)
                                    Console.WriteLine($"\t{msg}");
                                Console.WriteLine();
                                break;
                            }
                        case "!exit":
                            {
                                work = false;
                                break;
                            }
                        default:
                            {
                                SystemMessage("Invalid command!");
                                break;
                            }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentClientName))
                        SendMessage(input);
                    else
                    {
                        SystemMessage("Login first!");
                    }
                }
            }

            CloseConnaction();
        }
        private static void LoginUser(string name) 
        {
            if (currentClientName != null)
            {
                SystemMessage($"{currentClientName} already taken over the chat!");
                return;
            }
            SystemMessage($"{name} join chat");
            currentClientName = name;
            PortRegistration();
            StartConnection();
        }

        private static void SendMessage(string msg)
        {
            var message = string.Format("{0} [{1}]: {2}", DateTime.Now, currentClientName, msg);
            UserMessage(message);
            MessageHistory.Add(message);
            byte[] data = Encoding.Unicode.GetBytes($"{message}");
            udpSocket?.SendTo(data, new IPEndPoint(IPAddress.Parse(host), currentPort));
        }

        private static void SystemMessage(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        private static void UserMessage(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        private static void CompanionMessage(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        private static void ReciveMessage()
        {
            try
            {
                udpSocket.Bind(new IPEndPoint(IPAddress.Parse(host), port));

                while (true)
                {
                    StringBuilder builder = new StringBuilder();
                    EndPoint endPoint = new IPEndPoint(IPAddress.Parse(host), currentPort);

                    do
                    {
                        var data = new byte[256];
                        var count = udpSocket.ReceiveFrom(data, data.Length, SocketFlags.Partial, ref endPoint);

                        builder.Append(Encoding.Unicode.GetString(data, 0, count));

                    } while (udpSocket.Available > 0);

                    CompanionMessage(builder.ToString());
                    MessageHistory.Add(builder.ToString());
                }
            }
            catch (Exception ex)
            {
                SystemMessage(ex.Message);
            }
            finally
            {
                CloseConnaction();
            }
        }

        private static void PortRegistration()
        {
            int listeningPort, connectionPort;
            while (true)
            {
                SystemMessage("Enter connection port (companions):");
                if (!int.TryParse(Console.ReadLine(), out connectionPort) || connectionPort < 8000 || connectionPort > 9000)
                {
                    SystemMessage("Incorrect input or this port is reserved. (Try 8000-9000)");
                    continue;
                }
                break;
            }
            while (true)
            {
                SystemMessage("Enter listening port (yours):");
                if (!int.TryParse(Console.ReadLine(), out listeningPort) || listeningPort < 8000 || listeningPort > 9000)
                {
                    SystemMessage("Incorrect input or this port is reserved. (Try 8000-9000)");
                    continue;
                }
                break;
            }
            port = listeningPort;
            currentPort = connectionPort;
        }

        private static void StartConnection()
        {
            Socket tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            tcpSocket.Bind(new IPEndPoint(IPAddress.Parse(host), port));

            while (true)
            {
                try
                {
                    tcpSocket.Connect(new IPEndPoint(IPAddress.Parse(host), currentPort));
                    break;
                }
                catch (SocketException)
                {
                    SystemMessage("Waiting for companion...");
                    Thread.Sleep(1000);
                }
            }

            SystemMessage("Connection started.");
            udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            reciver = new Task(ReciveMessage);
            reciver.Start();
        }

        private static void CloseConnaction()
        {
            if (udpSocket != null)
            {
                udpSocket.Shutdown(SocketShutdown.Both);
                udpSocket.Close();
                udpSocket = null;
            }
        }
    }
}
