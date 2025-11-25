using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Common;
using Newtonsoft.Json;

namespace FTPClient
{
    public class FTPClientService
    {
        private Socket _mainSocket;
        private IPEndPoint _serverEndPoint;
        private int _userId = -1;
        private string _currentDirectory = "";

        public int UserId => _userId;

        public event Action<string, DirectoryItem> OnDirectoryChanged;
        public event Action<ObservableCollection<FileItem>> OnFileListReceived;
        public event Action<string> OnStatusChanged;

        public async Task<bool> ConnectAsync(string serverIP, int port, string login, string password)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), port);
                    _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    _mainSocket.Connect(_serverEndPoint);

                    if (_mainSocket.Connected)
                    {
                        // Авторизация
                        string command = $"connect {login} {password}";
                        var viewModelSend = new ViewModelSend(command, -1);

                        byte[] messageByte = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelSend));
                        _mainSocket.Send(messageByte);

                        byte[] buffer = new byte[1048576];
                        int bytesRec = _mainSocket.Receive(buffer);
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRec);
                        var viewModelMessage = JsonConvert.DeserializeObject<ViewModelMessage>(response);

                        if (viewModelMessage.Command == "autorization")
                        {
                            _userId = int.Parse(viewModelMessage.Data);
                            return true;
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    OnStatusChanged?.Invoke($"Ошибка подключения: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task ChangeDirectoryAsync(string path)
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var tempSocket = CreateNewConnection())
                    {
                        if (tempSocket != null && tempSocket.Connected)
                        {
                            string command = string.IsNullOrEmpty(path) ? "cd" : $"cd {path}";
                            var viewModelSend = new ViewModelSend(command, _userId);
                            byte[] messageByte = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelSend));
                            tempSocket.Send(messageByte);

                            byte[] buffer = new byte[1048576];
                            int bytesRec = tempSocket.Receive(buffer);
                            string response = Encoding.UTF8.GetString(buffer, 0, bytesRec);
                            ProcessResponse(response, command, false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnStatusChanged?.Invoke($"Ошибка смены директории: {ex.Message}");
                }
            });
        }

        public async Task DownloadFileAsync(string remoteFileName, string localFilePath)
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var downloadSocket = CreateNewConnection())
                    {
                        if (downloadSocket != null && downloadSocket.Connected)
                        {
                            string command = $"get {remoteFileName}";
                            var viewModelSend = new ViewModelSend(command, _userId);
                            byte[] messageByte = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelSend));
                            downloadSocket.Send(messageByte);

                            // Получаем ответ с файлом
                            byte[] buffer = new byte[10485760]; // 10MB
                            int bytesRec = downloadSocket.Receive(buffer);
                            string response = Encoding.UTF8.GetString(buffer, 0, bytesRec);
                            var viewModelMessage = JsonConvert.DeserializeObject<ViewModelMessage>(response);

                            if (viewModelMessage.Command == "file")
                            {
                                byte[] fileData = JsonConvert.DeserializeObject<byte[]>(viewModelMessage.Data);
                                File.WriteAllBytes(localFilePath, fileData);
                                OnStatusChanged?.Invoke($"Файл {remoteFileName} успешно скачан!");
                            }
                            else if (viewModelMessage.Command == "message")
                            {
                                throw new Exception(viewModelMessage.Data);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnStatusChanged?.Invoke($"Ошибка скачивания: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task UploadFileAsync(string localFilePath)
        {
            await Task.Run(() =>
            {
                try
                {
                    string fileName = Path.GetFileName(localFilePath);

                    if (File.Exists(localFilePath))
                    {
                        FileInfo fileInfo = new FileInfo(localFilePath);
                        FileInfoFTP fileInfoFTP = new FileInfoFTP(File.ReadAllBytes(localFilePath), fileInfo.Name);

                        using (var uploadSocket = CreateNewConnection())
                        {
                            if (uploadSocket != null && uploadSocket.Connected)
                            {
                                // Отправляем команду set с данными файла
                                var viewModelSend = new ViewModelSend(JsonConvert.SerializeObject(fileInfoFTP), _userId);
                                byte[] messageByte = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelSend));
                                uploadSocket.Send(messageByte);

                                // Получаем ответ от сервера
                                byte[] buffer = new byte[1048576];
                                int bytesRec = uploadSocket.Receive(buffer);
                                string response = Encoding.UTF8.GetString(buffer, 0, bytesRec);
                                var viewModelMessage = JsonConvert.DeserializeObject<ViewModelMessage>(response);

                                if (viewModelMessage.Command == "message")
                                {
                                    OnStatusChanged?.Invoke(viewModelMessage.Data);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnStatusChanged?.Invoke($"Ошибка загрузки: {ex.Message}");
                    throw;
                }
            });
        }

        // ДОБАВЛЕННЫЙ МЕТОД - создает новое подключение к серверу
        private Socket CreateNewConnection()
        {
            try
            {
                if (_serverEndPoint == null)
                {
                    throw new InvalidOperationException("Сервер не подключен");
                }

                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(_serverEndPoint);
                return socket;
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke($"Ошибка создания подключения: {ex.Message}");
                return null;
            }
        }

        private void SendCommand(string command, bool isFileDownload = false)
        {
            using (var tempSocket = CreateNewConnection())
            {
                if (tempSocket != null && tempSocket.Connected)
                {
                    var viewModelSend = new ViewModelSend(command, _userId);
                    byte[] messageByte = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelSend));
                    tempSocket.Send(messageByte);

                    byte[] buffer = new byte[10485760]; // 10MB buffer
                    int bytesRec = tempSocket.Receive(buffer);
                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRec);
                    ProcessResponse(response, command, isFileDownload);
                }
            }
        }

        private void ProcessResponse(string response, string command, bool isFileDownload)
        {
            var viewModelMessage = JsonConvert.DeserializeObject<ViewModelMessage>(response);

            switch (viewModelMessage.Command)
            {
                case "cd":
                    // Новый формат ответа с текущим путем
                    var responseData = JsonConvert.DeserializeObject<dynamic>(viewModelMessage.Data);
                    string currentPath = responseData.CurrentPath;
                    var foldersFiles = JsonConvert.DeserializeObject<List<string>>(responseData.Files.ToString());

                    UpdateFileAndDirectoryViews(foldersFiles, currentPath);
                    break;
                case "message":
                    OnStatusChanged?.Invoke(viewModelMessage.Data);
                    break;
                case "file":
                    if (isFileDownload)
                    {
                        OnStatusChanged?.Invoke("Файл успешно скачан");
                    }
                    break;
            }
        }

        private void UpdateFileAndDirectoryViews(List<string> items, string currentPath)
        {
            var files = new ObservableCollection<FileItem>();
            var rootDirectory = new DirectoryItem { Name = "Корневая папка", FullPath = "" };

            foreach (var item in items)
            {
                bool isDirectory = item.EndsWith("/");
                string name = isDirectory ? item.Substring(0, item.Length - 1) : item;

                files.Add(new FileItem
                {
                    Name = name,
                    Type = isDirectory ? "Папка" : "Файл",
                    Size = isDirectory ? "" : "N/A",
                    IsDirectory = isDirectory,
                    FullPath = name
                });

                if (isDirectory)
                {
                    rootDirectory.SubDirectories.Add(new DirectoryItem
                    {
                        Name = name,
                        FullPath = name
                    });
                }
            }

            OnFileListReceived?.Invoke(files);
            OnDirectoryChanged?.Invoke(currentPath, rootDirectory);
        }

        public void Disconnect()
        {
            _mainSocket?.Close();
            _userId = -1;
        }
    }
}