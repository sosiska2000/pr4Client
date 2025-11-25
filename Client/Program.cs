using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class Program
    {
        public static IPAddress IPAdress;
        public static int Port;
        public static int Id = -1;

        static void Main(string[] args)
        {
            Console.Write("Введите IP адрес сервера: ");
            string sIpAdress = Console.ReadLine();
            Console.Write("Введите порт: ");
            string sPort = Console.ReadLine();

            if (int.TryParse(sPort, out Port) && IPAddress.TryParse(sIpAdress, out IPAdress))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Данные успешно введены. Подключаюсь к сервер.");

                while (true)
                {
                    ConnectServer();
                }
            }
        }

        public static bool CheckCommand(string message)
        {
            bool BCommand = false;
            string[] DataMessage = message.Split(new string[] { " " }, StringSplitOptions.None);

            if (DataMessage.Length > 0)
            {
                string Command = DataMessage[0];

                if (Command == "connect")
                {
                    if (DataMessage.Length != 3)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Использование: connect [login] [password]\nПример: connect User1 P@sswOrd");
                        BCommand = false;
                    }
                    else
                        BCommand = true;
                }
                else if (Command == "cd")
                {
                    BCommand = true;
                }
                else if (Command == "get")
                {
                    if (DataMessage.Length == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Использование: get [NameFile]\nПример: get Test.txt");
                        BCommand = false;
                    }
                    else
                        BCommand = true;
                }
                else if (Command == "set")
                {
                    if (DataMessage.Length == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Использование: set [NameFile]\nПример: set Test.txt");
                        BCommand = false;
                    }
                    else
                        BCommand = true;
                }
            }
            return BCommand;
        }

        public static void ConnectServer()
        {
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IPAdress, Port);
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(endPoint);

                if (socket.Connected)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("Введите команду: ");
                    string message = Console.ReadLine();

                    if (CheckCommand(message))
                    {
                        ViewModelSend viewModelSend = new ViewModelSend(message, Id);

                        if (message.Split(new string[] { " " }, StringSplitOptions.None)[0] == "set")
                        {
                            string[] DataMessage = message.Split(new string[] { " " }, StringSplitOptions.None);
                            string NameFile = "";
                            for (int i = 1; i < DataMessage.Length; i++)
                            {
                                if (NameFile == "")
                                    NameFile += DataMessage[i];
                                else
                                    NameFile += " " + DataMessage[i];
                            }

                            if (File.Exists(NameFile))
                            {
                                FileInfo FileInfo = new FileInfo(NameFile);
                                FileInfoFTP NewFileInfo = new FileInfoFTP(File.ReadAllBytes(NameFile), FileInfo.Name);
                                viewModelSend = new ViewModelSend(JsonConvert.SerializeObject(NewFileInfo), Id);
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Указанный файл не существует");
                                return;
                            }
                        }

                        byte[] messageByte = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelSend));
                        int BytesSend = socket.Send(messageByte);

                        byte[] bytes = new byte[10485760];
                        int BytesRec = socket.Receive(bytes);
                        string messageServer = Encoding.UTF8.GetString(bytes, 0, BytesRec);
                        ViewModelMessage viewModelMessage = JsonConvert.DeserializeObject<ViewModelMessage>(messageServer);

                        if (viewModelMessage.Command == "autorization")
                        {
                            Id = int.Parse(viewModelMessage.Data);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Авторизация успешна!");
                        }
                        else if (viewModelMessage.Command == "message")
                        {
                            Console.WriteLine(viewModelMessage.Data);
                        }
                        else if (viewModelMessage.Command == "cd")
                        {
                            List<string> FoldersFiles = JsonConvert.DeserializeObject<List<string>>(viewModelMessage.Data);
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Содержимое директории:");
                            foreach (string Name in FoldersFiles)
                                Console.WriteLine(Name);
                        }
                        else if (viewModelMessage.Command == "file")
                        {
                            string[] DataMessage = viewModelSend.Message.Split(new string[] { " " }, StringSplitOptions.None);
                            string getFile = "";
                            for (int i = 1; i < DataMessage.Length; i++)
                            {
                                if (getFile == "")
                                    getFile = DataMessage[i];
                                else
                                    getFile += " " + DataMessage[i];
                            }

                            byte[] byteFile = JsonConvert.DeserializeObject<byte[]>(viewModelMessage.Data);
                            File.WriteAllBytes(getFile, byteFile);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Файл {getFile} успешно скачан!");
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Неверная команда");
                    }

                    socket.Close();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Подключение не удалось.");
                }
            }
            catch (Exception exp)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Что-то случилось: " + exp.Message);
            }
        }
    }
}