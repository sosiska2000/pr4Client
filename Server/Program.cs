using Common;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Server.Data;
using Server.Models;
using Server.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    class Program
    {
        public static IPAddress IPAdress;
        public static int Port;
        private static IUserService _userService;
        private static AppDbContext _context;

        static void Main(string[] args)
        {
            // Инициализация БД
            _context = new AppDbContext();
            _context.Database.EnsureCreated();
            _userService = new UserService(_context);

            // Получаем путь к папке проекта Server
            string projectPath = Directory.GetCurrentDirectory();
            string ftpUserPath = Path.Combine(projectPath, "FTPUser");

            // Создаем папку FTPUser в папке проекта
            if (!Directory.Exists(ftpUserPath))
            {
                Directory.CreateDirectory(ftpUserPath);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Создана папка FTPUser: {ftpUserPath}");
            }

            // Создаем тестовые файлы
            CreateTestFiles(ftpUserPath);

            // Добавляем тестового пользователя, если нет пользователей
            if (!_context.Users.Any())
            {
                _context.Users.Add(new User
                {
                    Login = "skomor1n",
                    PasswordHash = "Asdfgl23",
                    BaseDirectory = ftpUserPath,
                    temp_src = ftpUserPath
                });
                _context.SaveChanges();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Создан тестовый пользователь: skomor1n/Asdfgl23");
                Console.WriteLine($"Директория пользователя: {ftpUserPath}");
            }

            Console.Write("Введите IP адрес сервера: ");
            string sIpAdress = Console.ReadLine();
            Console.Write("Введите порт: ");
            string sPort = Console.ReadLine();

            if (int.TryParse(sPort, out Port) && IPAddress.TryParse(sIpAdress, out IPAdress))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Сервер запущен. База данных инициализирована.");
                Console.WriteLine($"Папка FTPUser: {ftpUserPath}");
                StartServer();
            }
            Console.Read();
        }

        // Метод для создания тестовых файлов
        private static void CreateTestFiles(string basePath)
        {
            try
            {
                var testFiles = new[]
                {
                    "test.txt",
                    "document.pdf",
                    "image.jpg"
                };

                foreach (var file in testFiles)
                {
                    var fullPath = Path.Combine(basePath, file);
                    if (!File.Exists(fullPath))
                    {
                        File.WriteAllText(fullPath, $"This is test file: {file}\nCreated: {DateTime.Now}");
                        Console.WriteLine($"Создан файл: {file}");
                    }
                }

                // Создаем тестовую папку
                var testFolder = Path.Combine(basePath, "TestFolder");
                if (!Directory.Exists(testFolder))
                {
                    Directory.CreateDirectory(testFolder);
                    File.WriteAllText(Path.Combine(testFolder, "file_in_folder.txt"), "File inside folder");
                    Console.WriteLine($"Создана папка: TestFolder");
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Тестовые файлы созданы в: {basePath}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка создания тестовых файлов: {ex.Message}");
                Console.ResetColor();
            }
        }

        public static List<string> GetDirectory(string src)
        {
            List<string> FoldersFiles = new List<string>();
            if (Directory.Exists(src))
            {
                string[] dirs = Directory.GetDirectories(src);
                foreach (string dir in dirs)
                {
                    string NameDirectory = Path.GetFileName(dir) + "/";
                    FoldersFiles.Add(NameDirectory);
                }

                string[] files = Directory.GetFiles(src);
                foreach (string file in files)
                {
                    string NameFile = Path.GetFileName(file);
                    FoldersFiles.Add(NameFile);
                }
            }
            return FoldersFiles;
        }

        public static void StartServer()
        {
            IPEndPoint endPoint = new IPEndPoint(IPAdress, Port);
            Socket slistener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            slistener.Bind(endPoint);
            slistener.Listen(10);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Сервер запущен и ожидает подключений.");

            while (true)
            {
                try
                {
                    Socket Handler = slistener.Accept();
                    string Data = null;
                    byte[] Bytes = new byte[10485760];
                    int BytesRec = Handler.Receive(Bytes);
                    Data += Encoding.UTF8.GetString(Bytes, 0, BytesRec);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("Сообщение от пользователя: " + Data + "\n");

                    string Reply = "";
                    ViewModelSend ViewModelSend = JsonConvert.DeserializeObject<ViewModelSend>(Data);

                    if (ViewModelSend != null)
                    {
                        ViewModelMessage viewModelMessage;
                        string[] DataCommand = ViewModelSend.Message.Split(new string[] { " " }, StringSplitOptions.None);

                        if (DataCommand[0] == "connect")
                        {
                            string[] DataMessage = ViewModelSend.Message.Split(new string[] { " " }, StringSplitOptions.None);
                            var user = _userService.Authenticate(DataMessage[1], DataMessage[2]);

                            if (user != null)
                            {
                                _userService.LogCommand(user.Id, "connect", ViewModelSend.Message, $"{DataMessage[1]}, {DataMessage[2]}", true);
                                viewModelMessage = new ViewModelMessage("autorization", user.Id.ToString());
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"Пользователь {user.Login} авторизован успешно (ID: {user.Id})");
                            }
                            else
                            {
                                _userService.LogCommand(-1, "connect", ViewModelSend.Message, $"{DataMessage[1]}, {DataMessage[2]}", false);
                                viewModelMessage = new ViewModelMessage("message", "Не правильный логин и пароль пользователя.");
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Неудачная попытка авторизации: {DataMessage[1]}");
                            }
                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);
                        }
                        else if (DataCommand[0] == "cd")
                        {
                            if (ViewModelSend.Id != -1)
                            {
                                var user = _context.Users.Find(ViewModelSend.Id);
                                if (user == null)
                                {
                                    viewModelMessage = new ViewModelMessage("message", "Пользователь не найден");
                                    Reply = JsonConvert.SerializeObject(viewModelMessage);
                                    byte[] Message = Encoding.UTF8.GetBytes(Reply);
                                    Handler.Send(Message);
                                    continue;
                                }

                                string[] DataMessage = ViewModelSend.Message.Split(new string[] { " " }, StringSplitOptions.None);
                                List<string> FoldersFiles = new List<string>();
                                string currentPath = user.temp_src;

                                if (DataMessage.Length == 1)
                                {
                                    // Возврат к корневой директории
                                    user.temp_src = user.BaseDirectory;
                                    currentPath = user.temp_src;
                                    FoldersFiles = GetDirectory(user.BaseDirectory);
                                    _userService.LogCommand(user.Id, "cd", ViewModelSend.Message, "root", true);
                                }
                                else
                                {
                                    string cdfolder = "";
                                    for (int i = 1; i < DataMessage.Length; i++)
                                    {
                                        if (cdfolder == "")
                                            cdfolder += DataMessage[i];
                                        else
                                            cdfolder += " " + DataMessage[i];
                                    }

                                    // Обработка команды cd ..
                                    if (cdfolder == "..")
                                    {
                                        DirectoryInfo parentDir = Directory.GetParent(user.temp_src);
                                        if (parentDir != null && parentDir.FullName.StartsWith(user.BaseDirectory))
                                        {
                                            user.temp_src = parentDir.FullName;
                                            currentPath = user.temp_src;
                                            FoldersFiles = GetDirectory(user.temp_src);
                                        }
                                        else
                                        {
                                            // Если выше корневой директории пользователя
                                            user.temp_src = user.BaseDirectory;
                                            currentPath = user.temp_src;
                                            FoldersFiles = GetDirectory(user.BaseDirectory);
                                        }
                                    }
                                    else
                                    {
                                        // Переход в указанную директорию
                                        string newPath = Path.Combine(user.temp_src, cdfolder);

                                        // Проверяем существует ли директория и находится ли она в разрешенной области
                                        if (Directory.Exists(newPath) && newPath.StartsWith(user.BaseDirectory))
                                        {
                                            user.temp_src = newPath;
                                            currentPath = user.temp_src;
                                            FoldersFiles = GetDirectory(newPath);
                                        }
                                        else
                                        {
                                            viewModelMessage = new ViewModelMessage("message", "Директория не существует или доступ запрещен: " + newPath);
                                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                                            byte[] Message = Encoding.UTF8.GetBytes(Reply);
                                            Handler.Send(Message);
                                            _userService.LogCommand(user.Id, "cd", ViewModelSend.Message, cdfolder, false);
                                            continue;
                                        }
                                    }

                                    _userService.LogCommand(user.Id, "cd", ViewModelSend.Message, cdfolder, true);
                                }

                                // Сохраняем изменения пути
                                _context.SaveChanges();

                                // Отправляем текущий путь вместе со списком файлов
                                var responseData = new
                                {
                                    CurrentPath = currentPath,
                                    Files = FoldersFiles
                                };

                                if (FoldersFiles.Count == 0)
                                    viewModelMessage = new ViewModelMessage("message", "Директория пуста.");
                                else
                                    viewModelMessage = new ViewModelMessage("cd", JsonConvert.SerializeObject(responseData));
                            }
                            else
                            {
                                viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться");
                            }
                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);
                        }
                        // ... остальные команды (get, set) остаются без изменений
                    }

                    Handler.Close();
                }
                catch (Exception exp)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Ошибка: " + exp.Message);
                }
            }
        }
    }
}