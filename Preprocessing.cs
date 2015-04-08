using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using Disk.SDK;
using Disk.SDK.Provider;
using System.Threading;
using System.IO;
using System.Web;
using System.Net;
using Newtonsoft.Json.Linq;

namespace CCCV
{
    public partial class MainWindow
    {
        public event EventHandler Preprocessing_completed;
        private void Init()
        {
            this.Closing += MainWindow_Closing;
            Preprocessing_completed += MainWindow_Preprocessing_completed;
            changedByUser = false;
            ready = new AutoResetEvent(true);
            notify_message = "Ничего не происходит";
            settings = new Settings();
            CreateNotifyIcon();

            string tmp = System.IO.Path.GetTempPath();
            Directory.CreateDirectory(local_data_path = tmp + Local_Data_Folder_path);
            Directory.CreateDirectory(local_files_path = tmp + Local_Files_Folder_path);

            Console.WriteLine("Data path: " + local_data_path);
            Console.WriteLine("Files path: " + local_files_path);

            check_token_from_settings();
        }

        private void check_token_from_settings()
        {
            access_token = settings.Token;
            if (access_token == TOKEN_UNDEFINED || settings.TokenWillLive - DateTime.Now.Ticks < 1000l * 60l * 60l * 12l)
            {
                get_code();
            }
            else
            {
                check_token();
            }
        }

        private void get_code()
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Console.WriteLine("Getting code");
                login_page = new LogIn();
                login_page.LogIn_OK.Click += LogIn_OK_Click;
                MainFrame.Navigate(login_page);
                new Thread(ThreadStart =>
                {
                    System.Diagnostics.Process.Start("https://oauth.yandex.ru/authorize?response_type=code&client_id="
                        + "b9f638c2745c426e87bab5d4edd60d9b");
                }).Start();
            }));
        }

        void LogIn_OK_Click(object sender, RoutedEventArgs e)
        {
            processing_page = new Processing_page();
            processing_page.Status.Text = "Получение токена...";
            MainFrame.Navigate(processing_page);

            string code = login_page.LogIn_code.Text;
            new Thread(ThreadStart => { get_token(code); }).Start();
        }

        private void get_token(string code)
        {
            Console.WriteLine("Getting token");
            string to_post = "grant_type=authorization_code"
            + "&code=" + code
            + "&client_id=" + clientID
            + "&client_secret=" + appPassword;

            byte[] array = System.Text.Encoding.ASCII.GetBytes(to_post);
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("https://oauth.yandex.ru/token");
            request.Method = "POST";
            request.Host = "oauth.yandex.ru";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = array.Length;
            using (Stream req_stream = request.GetRequestStream())
            {
                req_stream.Write(array, 0, array.Length);
                req_stream.Close();
            }

            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (Exception)
            {

            }
            Console.WriteLine("Responce status code: " + response.StatusCode);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string json_data = reader.ReadLine();
                    reader.Close();
                    var json = JObject.Parse(json_data);
                    access_token = json["access_token"].Value<string>();
                    Console.WriteLine("Token: " + access_token);
                    long expires_in = json["expires_in"].Value<long>();
                    settings.TokenWillLive = DateTime.Now.Ticks + expires_in * 1000l;
                    settings.Token = access_token;
                    settings.save();
                }

                check_token();
            }
            else
            {
                //
                return;
            }
        }

        private void check_token()
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                processing_page = processing_page ?? new Processing_page();
                processing_page.Status.Text = "Проверка токена...";
                MainFrame.Navigate(processing_page);
            }));
            client = new DiskSdkClient(access_token);
            client.GetListCompleted += Check_Data_Folder;
            client.GetListAsync();
        }

        void Check_Data_Folder(object sender, GenericSdkEventArgs<IEnumerable<DiskItemInfo>> e)
        {
            client.GetListCompleted -= Check_Data_Folder;
            if (e.Result == null)
            {
                get_code();
                return;
            }
            client = new DiskSdkClient(access_token);
            if (!e.Result.Any(p => p.FullPath.Equals(Disk_Data_Folder_path)))
            {
                client.MakeFolderCompleted += client_MakeDataFolderCompleted;
                client.MakeDirectoryAsync(Disk_Data_Folder_path);
            }
            else
            {
                client.GetListCompleted += client_GetListCompleted;
                client.GetListAsync(Disk_Data_Folder_path);
            }

            processing_page = processing_page ?? new Processing_page();
            processing_page.Status.Text = "Запуск программы...";
            MainFrame.Navigate(processing_page);
        }

        void client_MakeDataFolderCompleted(object sender, SdkEventArgs e)
        {
            client.GetListCompleted += client_GetListCompleted;
            client.GetListAsync(Disk_Data_Folder_path);
        }

        void client_GetListCompleted(object sender, GenericSdkEventArgs<IEnumerable<DiskItemInfo>> e)
        {
            Console.WriteLine("Get list completed");
            elements_in_DataFolder = e.Result;
            if (!e.Result.Any(p => p.FullPath.Equals(Disk_Files_Folder_path)))
            {
                client = new DiskSdkClient(access_token);
                client.MakeFolderCompleted += client_MakeFilesFolderCompleted;
                client.MakeDirectoryAsync(Disk_Files_Folder_path);
            }
            else
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate { CreateClipboardListener(); }));
            }
        }

        void client_MakeFilesFolderCompleted(object sender, SdkEventArgs e)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate { CreateClipboardListener(); }));
        }

        private void CreateNotifyIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon("ico.ico");
            notifyIcon.MouseDoubleClick += notifyIcon_MouseDoubleClick;
            System.Windows.Forms.ContextMenu menu = new System.Windows.Forms.ContextMenu();
            notifyIcon.ContextMenu = menu;
            var item1 = new System.Windows.Forms.MenuItem();
            item1.Text = "Show";
            item1.Click += item1_Click;
            var item3 = new System.Windows.Forms.MenuItem();
            item3.Text = "Close";
            item3.Click += item3_Click;

            menu.MenuItems.Add(item1);
            menu.MenuItems.Add(item3);
            notifyIcon.Visible = true;
        }
    }
}
