using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace FTPClient
{
    public partial class MainWindow : Window
    {
        private FTPClientService _clientService;
        private ObservableCollection<FileItem> _files;
        private int _currentUserId = -1;

        public MainWindow()
        {
            InitializeComponent();
            _files = new ObservableCollection<FileItem>();
            listFiles.ItemsSource = _files;
            _clientService = new FTPClientService();
            _clientService.OnFileListReceived += UpdateFileList;
            _clientService.OnStatusChanged += UpdateStatus;
            _clientService.OnDirectoryChanged += UpdateDirectoryView;
        }

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string serverIP = txtIP.Text;
                int port = int.Parse(txtPort.Text);
                string login = txtLogin.Text;
                string password = txtPassword.Password;

                UpdateStatus("Подключение...");
                progressBar.IsIndeterminate = true;

                bool connected = await _clientService.ConnectAsync(serverIP, port, login, password);

                if (connected)
                {
                    _currentUserId = _clientService.UserId;
                    btnConnect.IsEnabled = false;
                    btnDisconnect.IsEnabled = true;
                    txtUserInfo.Text = $"Пользователь: {login} (ID: {_currentUserId})";
                    UpdateStatus("Подключено успешно");

                    // Загружаем корневую директорию
                    await _clientService.ChangeDirectoryAsync("");
                }
                else
                {
                    UpdateStatus("Ошибка подключения");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"Ошибка: {ex.Message}");
            }
            finally
            {
                progressBar.IsIndeterminate = false;
            }
        }

        private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            _clientService.Disconnect();
            btnConnect.IsEnabled = true;
            btnDisconnect.IsEnabled = false;
            treeDirectories.Items.Clear();
            _files.Clear();
            txtUserInfo.Text = "Пользователь: Не авторизован";
            UpdateStatus("Отключено");
        }

        private void UpdateDirectoryView(string currentPath, DirectoryItem rootDirectory)
        {
            Dispatcher.Invoke(() =>
            {
                // Отображаем относительный путь (убираем полный путь к проекту)
                string displayPath = currentPath;
                if (!string.IsNullOrEmpty(currentPath))
                {
                    // Показываем только путь относительно корневой папки пользователя
                    try
                    {
                        var userRoot = Path.GetDirectoryName(currentPath);
                        if (!string.IsNullOrEmpty(userRoot))
                        {
                            displayPath = currentPath.Replace(userRoot, "").TrimStart('\\');
                            if (string.IsNullOrEmpty(displayPath))
                                displayPath = "/";
                        }
                    }
                    catch
                    {
                        displayPath = currentPath;
                    }
                }

                txtCurrentPath.Text = $"Текущий путь: {displayPath}";
                treeDirectories.Items.Clear();
                if (rootDirectory != null)
                {
                    treeDirectories.Items.Add(rootDirectory);
                }
            });
        }

        private void UpdateFileList(ObservableCollection<FileItem> files)
        {
            Dispatcher.Invoke(() =>
            {
                _files.Clear();
                foreach (var file in files)
                {
                    _files.Add(file);
                }
            });
        }

        private void UpdateStatus(string status)
        {
            Dispatcher.Invoke(() =>
            {
                txtStatus.Text = status;
            });
        }

        private async void TreeDirectories_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var item = treeDirectories.SelectedItem as DirectoryItem;
            if (item != null)
            {
                await _clientService.ChangeDirectoryAsync(item.FullPath);
            }
        }

        private async void ListFiles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            await DownloadSelectedFile();
        }

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            await DownloadSelectedFile();
        }

        private async Task DownloadSelectedFile()
        {
            var selectedItem = listFiles.SelectedItem as FileItem;
            if (selectedItem != null && !selectedItem.IsDirectory)
            {
                var saveDialog = new SaveFileDialog
                {
                    FileName = selectedItem.Name,
                    Filter = "Все файлы (*.*)|*.*"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    try
                    {
                        progressBar.IsIndeterminate = true;
                        UpdateStatus($"Скачивание {selectedItem.Name}...");
                        await _clientService.DownloadFileAsync(selectedItem.Name, saveDialog.FileName);
                        UpdateStatus($"Файл {selectedItem.Name} успешно скачан!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка скачивания: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        UpdateStatus($"Ошибка скачивания: {ex.Message}");
                    }
                    finally
                    {
                        progressBar.IsIndeterminate = false;
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите файл для скачивания", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void BtnUpload_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Все файлы (*.*)|*.*",
                Multiselect = false
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    progressBar.IsIndeterminate = true;
                    UpdateStatus($"Загрузка {openDialog.FileName}...");
                    await _clientService.UploadFileAsync(openDialog.FileName);
                    UpdateStatus($"Файл {Path.GetFileName(openDialog.FileName)} успешно загружен!");

                    // Обновляем список файлов
                    await _clientService.ChangeDirectoryAsync("");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus($"Ошибка загрузки: {ex.Message}");
                }
                finally
                {
                    progressBar.IsIndeterminate = false;
                }
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await _clientService.ChangeDirectoryAsync("");
        }

        private void TreeDirectories_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Можно добавить логику при изменении выбора в дереве
        }
    }
}