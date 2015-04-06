using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
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
using System.IO;
using System.Threading;
using System.Web;
using System.Net;
//using System.Web.Helpers;
using Newtonsoft.Json.Linq;

namespace CCCV
{
    public partial class MainWindow : Window, IProgress
    {
        private static string clientID = "b9f638c2745c426e87bab5d4edd60d9b";
        private static string appPassword = "42809060e9b140ada29febd0f1316320";
        private static string returnURI = "myapp://token";
        private const string Data_Folder_path = "/CCCV/Data/";

        private LogIn login_page;
        private string access_token;

        //Здесь авторизация
        public void Authorize()
        {
            access_token = settings.Token;
            check_token();
        }

        private void get_code()
        {
            Console.WriteLine("Getting code");
            login_page = new LogIn();
            login_page.LogIn_OK.Click += LogIn_OK_Click;
            MainFrame.Navigate(login_page);
            System.Diagnostics.Process.Start("https://oauth.yandex.ru/authorize?response_type=code&client_id="
                + "b9f638c2745c426e87bab5d4edd60d9b");
        }

        void LogIn_OK_Click(object sender, RoutedEventArgs e)
        {
            string code = login_page.LogIn_code.Text;
            Thread t = new Thread(new ThreadStart(delegate { get_token(code); }));
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

        private void check_token()
        {
            if (access_token.Equals("") || settings.TokenWillLive - DateTime.Now.Ticks < 1000l * 60l * 60l * 12l)
            {
                get_code();
                return;
            }
            client = new DiskSdkClient(access_token);
            client.GetListCompleted += client_GetListCompleted_0;
            client.GetListAsync();
        }

        //Отсюда начинается шняга после авторизации

        public event EventHandler Preprocessing_completed;

        //
        void client_GetListCompleted_0(object sender, GenericSdkEventArgs<IEnumerable<DiskItemInfo>> e)
        {
            client.GetListCompleted -= client_GetListCompleted_0;
            if (e.Result == null)
            {
                get_code();
            }
            client = new DiskSdkClient(access_token);
            if (!e.Result.Any(p => p.FullPath.Equals(Data_Folder_path)))
            {
                client.MakeFolderCompleted += client_MakeFolderCompleted;
                client.MakeDirectoryAsync(Data_Folder_path);
            }
            else
            {
                client.GetListCompleted += client_GetListCompleted;
                client.GetListAsync(Data_Folder_path);
            }
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

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Console.WriteLine("Responce status code: " + response.StatusCode);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using(StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string json_data = reader.ReadLine();
                    reader.Close();
                    var json = JObject.Parse(json_data);
                    access_token = json["access_token"].Value<string>();
                    Console.WriteLine("Token: " + access_token);
                    long expires_in = json["expires_in"].Value<long>();
                    settings.TokenWillLive = DateTime.Now.Ticks + expires_in * 1000l ;
                    settings.Token = access_token;
                    settings.save();
                    client = new DiskSdkClient(access_token);
                    check_token();
                }
            }
            else
            {
                //
                return;
            }
        }

        void client_GetListCompleted(object sender, GenericSdkEventArgs<IEnumerable<DiskItemInfo>> e)
        {
            elements_in_DataFolder = e.Result;
            Console.WriteLine("Get list completed");
            Dispatcher.BeginInvoke(new ThreadStart(delegate { CreateClipboardListener(); }));
        }

        void client_MakeFolderCompleted(object sender, SdkEventArgs e)
        {
            client.GetListCompleted += client_GetListCompleted;
            client.GetListAsync(Data_Folder_path);
        }
        public void UpdateProgress(ulong current, ulong total)
        {
            string plus = 100 * current / total + "%; ";
            if (total / (1024 * 1024) > 0)
                plus += (int)(100 * total / (1024 * 1024 * 1.0)) * 0.01 + " MБ";
            else if (total / 1024 > 0)
                plus += (int)(100 * total / (1024 * 1.0)) * 0.01 + " КБ";
            else
                plus += total + " байт";
            notifyIcon.Text = notify_message + plus;

        }
    }
}