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
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CCCV
{
    public partial class MainWindow : Window
    {
        private bool changedByUser;

        private void ClipboardChanged()
        {
            Console.WriteLine("Clipboard changed");
            client = new DiskSdkClient(access_token);
            Get_and_Upload();
        }

        private void Get_and_Upload()
        {
            Thread t = new Thread(new ThreadStart(delegate
            {
                DateTime now = DateTime.Now;
                string content_path = now.ToBinary().ToString();
                object content = null;
                long size = 0;
                string thisFormat = "";
                string[] formats = System.Windows.Clipboard.GetDataObject().GetFormats();
                foreach (string format in formats)
                {
                    content = System.Windows.Clipboard.GetData(format);
                    if (content != null && content.GetType().IsSerializable)
                    {
                        thisFormat = format;
                        break;
                    }
                }
                Console.WriteLine("Serializing content");

                Item content_info = new Item(content, content_path, now, thisFormat, size);

                JsonSerializer serializer = new JsonSerializer();
                using (StreamWriter sw = new StreamWriter(content_path))
                {
                    using (JsonTextWriter writer = new JsonTextWriter(sw))
                    {
                        serializer.Serialize(writer, content_info);
                    }
                }
                size = new FileInfo(content_path).Length;

                Console.WriteLine("Serialized. Size = " + size);

                if (size < settings.CurrentSizeOfData)
                {
                    Console.WriteLine("size<current size");
                    OkayUpload(content_path, now, thisFormat, size);
                }
                else if (settings.How == Settings.HowUpload.AfterClick && size < settings.MaxSizeOfData)
                {
                    notifyIcon.BalloonTipClicked += delegate
                    {
                        Console.WriteLine("Notify clicked");
                        OkayUpload(content_path, now, thisFormat, size);
                    };
                    notifyIcon.ShowBalloonTip(0, "CCCV", "Содержимое буфера обмена изменилось.\n" +
                        "Нажмите, чтобы загрузить", ToolTipIcon.Info);
                }
                else if (settings.ShowBalloon)
                {
                    notifyIcon.BalloonTipClicked += delegate
                    {
                        OkayUpload(content_path, now, thisFormat, size);
                    };
                    notifyIcon.ShowBalloonTip(0, "CCCV", "Похоже, размер объекта больше максимального.\n" +
                        "Нажмите, если всё равно хотите загрузить", ToolTipIcon.Info);
                }
            }));
            t.SetApartmentState(ApartmentState.STA);
            Console.WriteLine("starting thread in get_and_upload");
            t.Start();
        }

        private void OkayUpload(string content_path, DateTime now, string thisFormat, long size)
        {
            ready.WaitOne();
            Console.WriteLine("Uploading content in OkayUpload");
            FileStream fs = File.Open(content_path, FileMode.Open);
            client = new DiskSdkClient(access_token);

            client.UploadFileAsync(Data_Folder_path + content_path, fs,
                this, Completed);
        }


        private void Completed(object o, SdkEventArgs e)
        {
            notify_message = "Содержимое вашего буфера загружается на Диск\n";
            ready.Set();
            notifyIcon.BalloonTipClicked += delegate
            {

            };
            notifyIcon.ShowBalloonTip(0, "CCCV", "Содержимое буфера обмена загружено на сервер", ToolTipIcon.Info);

        }

    }
}
