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
using System.IO;
using System.Threading;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using Disk.SDK;
using Disk.SDK.Provider;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CCCV
{
    public partial class MainWindow : Window
    {
        [DllImport("User32.dll")]
        protected static extern int SetClipboardViewer(int hWndNewViewer);
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        private const int WM_CHANGECBCHAIN = 0x30D;
        private const int WM_DRAWCLIPBOARD = 0x308;

        private const int TIMER_INTERVAL = 7500;

        private IntPtr nextClipboardViewer;
        private HwndSource hwndSource;

        private SettingsPage settingsPage;
        private System.Timers.Timer timer;
        private System.Timers.Timer fucking_clipboard_timer;

        private bool canProcess = true;
        private bool canUpdateFromTimer = true;

        public void CreateClipboardListener()
        {
            hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            nextClipboardViewer = (IntPtr)SetClipboardViewer((int)hwndSource.Handle);
            hwndSource.AddHook(WndProc);

            Dispatcher.BeginInvoke(new System.Threading.ThreadStart(delegate
            {
                Console.WriteLine("Creating listener");
                settingsPage = new SettingsPage(settings);
                settingsPage.RB1.Checked += RB1_Checked;
                settingsPage.RB2.Checked += RB2_Checked;
                settingsPage.RB3.Checked += RB3_Checked;
                settingsPage.SizeOfData.TextChanged += SizeOfData_TextChanged;
                settingsPage.IfSizeBigger.Checked += IfSizeBigger_Checked;
                settingsPage.IfSizeBigger.Unchecked += IfSizeBigger_Unchecked;
                settingsPage.Update_Content.Click += Update_Content_Click;

                Console.WriteLine("Preprocessing completed");

                if (Preprocessing_completed != null)
                    Preprocessing_completed(this, new EventArgs());
            }));
        }

        public void Update_Content_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Update click");
            UpdateClipbordContent();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_DRAWCLIPBOARD:
                    if (canProcess)
                        ClipboardChanged();
                    break;
                case WM_CHANGECBCHAIN:
                    if (wParam == nextClipboardViewer)
                    {
                        nextClipboardViewer = lParam;
                    }
                    else
                    {
                        SendMessage(nextClipboardViewer, WM_CHANGECBCHAIN, wParam, lParam);
                    }
                    break;
                default:
                    break;
            }

            return IntPtr.Zero;
        }

        void StartTimer()
        {
            canUpdateFromTimer = true;
            timer.Start();
        }

        void StopTimer()
        {
            canUpdateFromTimer = false;
            timer.Stop();
        }

        void MainWindow_Preprocessing_completed(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Threading.ThreadStart(delegate
            {
                settingsPage = settingsPage ?? new SettingsPage(settings);
                NavigateToSettings();
                addLogOutItem();
                timer = new System.Timers.Timer(TIMER_INTERVAL);
                timer.AutoReset = true;
                timer.Elapsed += Timer_Elapsed;
                StartTimer();
            }));
        }

        void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (canUpdateFromTimer)
            {
                UpdateClipbordContent();
            }
        }

        private void UpdateClipbordContent()
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                StopTimer();
            }));

            client = new DiskSdkClient(access_token);
            client.GetListCompleted += Client_GetListCompleted_To_Update;
            client.GetListAsync(Disk_Data_Folder_path);
        }

        void Client_GetListCompleted_To_Update(object sender, GenericSdkEventArgs<IEnumerable<DiskItemInfo>> e)
        {
            Console.WriteLine("Get list completed");
            if (e.Result != null && e.Result.Count() > elements_in_DataFolder.Count())
            {
                DiskItemInfo in_elements = Item.LastInList(elements_in_DataFolder);
                DiskItemInfo in_Result = Item.LastInList(e.Result);
                Console.WriteLine("Elements in data folder size: " + elements_in_DataFolder.Count());
                Console.WriteLine("Result size: " + e.Result.Count());
                if (!in_elements.Equals(in_Result))
                {
                    Console.WriteLine("New item: " + in_Result.DisplayName);
                    Process_info(in_Result.DisplayName, in_Result.ContentLength);
                }
            }
        }

        void Process_info(string name, int size)
        {
            if (size < settings.CurrentSizeOfData)
            {
                UpdateContent_in_Clipboard(name, size);
            }
            else if (size < settings.MaxSizeOfData && settings.How == Settings.HowUpload.AfterClick)
            {
                notifyIcon.BalloonTipClicked += delegate
                {
                    UpdateContent_in_Clipboard(name, size);
                };
                notifyIcon.ShowBalloonTip(0, "CCCV", "Содержимое буфера обмена изменилось на другом компьютере.\n" +
                    "Нажмите, чтобы загрузить", ToolTipIcon.Info);
            }
            else if (settings.ShowBalloon)
            {
                notifyIcon.BalloonTipClicked += delegate
                {
                    UpdateContent_in_Clipboard(name, size);
                };
                notifyIcon.ShowBalloonTip(0, "CCCV", "Содержимое буфера обмена изменилось на другом компьютере.\n" +
                    "Похоже, её размер больше максимально возможного"
                    + "Нажмите, чтобы загрузить", ToolTipIcon.Info);
            }

        }

        private void UpdateContent_in_Clipboard(string name, int size)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Item item = null;
                client = new DiskSdkClient(access_token);
                FileStream fs;
                string downloaded_data = local_data_path + name;
                try
                {
                    fs = File.Open(downloaded_data, FileMode.Create);
                }
                catch (Exception e)
                {
                    notifyIcon.ShowBalloonTip(0, "CCCV", "Не удалось создать файл:" + downloaded_data + "\n" + e.Message, ToolTipIcon.Error);
                    return;
                }

                InitProgress(size);


                /*
                 * здесь мы загружаем информацию о содержимом буфера обмена
                 */
                client.DownloadFileAsync(Disk_Data_Folder_path + name, fs,
                     progress,
                     delegate
                     {
                         try
                         {
                             fs.Close();
                         }
                         catch
                         {

                         }

                         Dispatcher.BeginInvoke(new ThreadStart(delegate
                         {
                             progress.DecrementCountOfTasks(size);
                         }));

                         JsonSerializer serializer = new JsonSerializer();

                         try
                         {
                             using (StreamReader sr = new StreamReader(fs = File.Open(downloaded_data, FileMode.Open)))
                             {
                                 using (JsonReader reader = new JsonTextReader(sr))
                                 {
                                     item = serializer.Deserialize<Item>(reader);
                                     if (item.Type == Item.ContentType.FileList)
                                     {
                                         item.Data = JsonConvert.DeserializeObject<CCCV_FileList>(item.Data.ToString());
                                     }
                                 }
                             }
                         }
                         catch (Exception e)
                         {
                             Dispatcher.BeginInvoke(new ThreadStart(delegate
                             {
                                 notifyIcon.ShowBalloonTip(0, "CCCV", "Не удалось открыть файл:" + downloaded_data + "\n" + e.Message, ToolTipIcon.Error);
                             }));
                             return;
                         }
                         finally
                         {
                             try
                             {
                                 fs.Close();
                             }
                             catch
                             {

                             }
                         }
                         if (item.Data != null)
                         {
                             Thread t = new Thread(ThreadStart =>
                                {
                                    switch (item.Type)
                                    {
                                        case Item.ContentType.Text:
                                            SetData(item);
                                            break;
                                        case Item.ContentType.FileList:
                                            Process_to_download_FL(name, item);
                                            break;
                                        default:
                                            break;
                                    }

                                });
                             t.SetApartmentState(ApartmentState.STA);
                             t.Start();
                             t.Join();

                         }
                     });
            }));
        }

        private void SetData(Item item)
        {
            canProcess = false;
            fucking_clipboard_timer = new System.Timers.Timer(1000);
            fucking_clipboard_timer.Elapsed += delegate { canProcess = true; };
            fucking_clipboard_timer.Enabled = true;
            fucking_clipboard_timer.Start();
            switch (item.Type)
            {
                case Item.ContentType.Text:
                    System.Windows.Clipboard.SetText((string)item.Data);
                    break;
                case Item.ContentType.FileList:
                    UpdateClipbordContent_withFiles(item.Data as CCCV_FileList);
                    break;
                default:
                    break;
            }
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                notifyIcon.ShowBalloonTip(0, "CCCV", "Содержимое буфера обмена изменено.", ToolTipIcon.Info);
            }));

            client.GetListCompleted += delegate (object sender, GenericSdkEventArgs<IEnumerable<DiskItemInfo>> e)
            {
                this.elements_in_DataFolder = e.Result;
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    StartTimer();
                    
                }));
                canUpdateFromTimer = true;
            };
            client.GetListAsync(Disk_Data_Folder_path);
        }

        private void ClipboardChanged()
        {
            Console.WriteLine("Clipboard changed");
            client = new DiskSdkClient(access_token);
            new Thread(ThreadStart => { Get_and_Upload(); }).Start();
        }

        private void Get_and_Upload()
        {
            Thread t = new Thread(new ThreadStart(delegate
            {
                DateTime now = DateTime.Now;
                string content_name = now.ToBinary().ToString();
                object content = null;
                Item.ContentType thisType = Item.ContentType.Text;

                if (System.Windows.Clipboard.ContainsText())
                {
                    thisType = Item.ContentType.Text;
                    content = System.Windows.Clipboard.GetText();
                }
                else if (System.Windows.Clipboard.ContainsFileDropList())
                {
                    thisType = Item.ContentType.FileList;
                    content = new CCCV_FileList(System.Windows.Clipboard.GetFileDropList(), Disk_Files_Folder_path + content_name + "/");
                }
                else
                {
                    return;
                }

                Console.WriteLine("Serializing content");

                Item content_info = new Item(content, thisType);

                JsonSerializer serializer = new JsonSerializer();
                string serialized_data = local_data_path + content_name;
                try
                {
                    using (StreamWriter sw = new StreamWriter(serialized_data))
                    {
                        using (JsonTextWriter writer = new JsonTextWriter(sw))
                        {
                            serializer.Serialize(writer, content_info);
                        }
                    }
                }
                catch (IOException e)
                {
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        notifyIcon.ShowBalloonTip(0, "CCCV", "Не удалось создать файл:" + serialized_data + "\n" + e.Message, ToolTipIcon.Error);
                    }));
                    return;
                }
                long info_size = new FileInfo(serialized_data).Length;
                long fl_size = (thisType == Item.ContentType.FileList ? ((CCCV_FileList)content).Size : 0);

                Console.WriteLine("Serialized info. Size = " + info_size);

                if (info_size + fl_size < settings.CurrentSizeOfData)
                {
                    Console.WriteLine("size<current size");
                    OkayUpload(content_info, serialized_data, Disk_Data_Folder_path + content_name, info_size, fl_size);
                }
                else if (settings.How == Settings.HowUpload.AfterClick && info_size + fl_size < settings.MaxSizeOfData)
                {
                    notifyIcon.BalloonTipClicked += delegate
                    {
                        Console.WriteLine("Notify clicked");
                        OkayUpload(content_info, serialized_data, Disk_Data_Folder_path + content_name, info_size, fl_size);
                    };
                    notifyIcon.ShowBalloonTip(0, "CCCV", "Содержимое буфера обмена изменилось.\n" +
                        "Нажмите, чтобы загрузить", ToolTipIcon.Info);
                }
                else if (settings.ShowBalloon)
                {
                    notifyIcon.BalloonTipClicked += delegate
                    {
                        OkayUpload(content_info, serialized_data, Disk_Data_Folder_path + content_name, info_size, fl_size);
                    };
                    notifyIcon.ShowBalloonTip(0, "CCCV", "Похоже, размер объекта больше максимального.\n" +
                        "Нажмите, если всё равно хотите загрузить", ToolTipIcon.Info);
                }
            }));

            t.SetApartmentState(ApartmentState.STA);
            Console.WriteLine("starting thread in get_and_upload");
            t.Start();
        }

        private void OkayUpload(Item item, string local_path, string disk_path, long info_size, long fl_size)
        {
            Console.WriteLine("Uploading content in OkayUpload");

            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                if (item.Type == Item.ContentType.FileList)
                {
                    StartUploadFiles(item.Data as CCCV_FileList, local_path, disk_path, info_size, fl_size);
                }
                else
                {
                    UploadInfo(local_path, disk_path, info_size);
                }
            }));
        }

        private void UploadInfo(string local_path, string disk_path, long size)
        {
            canUpdateFromTimer = false;

            InitProgress(size);

            FileStream fs;
            try
            {
                fs = File.Open(local_path, FileMode.Open);
            }
            catch (Exception e)
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    progress.DecrementCountOfTasks(size);
                    notifyIcon.ShowBalloonTip(0, "CCCV", "Не удалось открыть файл:" + local_path + "\n" + e.Message, ToolTipIcon.Error);
                }));
                return;
            }

            Console.WriteLine("After catch");
            client = new DiskSdkClient(access_token);


            client.UploadFileAsync(disk_path, fs,
                progress, delegate
            {
                try
                {
                    fs.Close();
                }
                catch
                {

                }
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    notifyIcon.BalloonTipClicked += delegate
                    {

                    };
                    progress.DecrementCountOfTasks(size);
                    notifyIcon.ShowBalloonTip(0, "CCCV", "Содержимое буфера обмена загружено на сервер", ToolTipIcon.Info);
                }));

            });
        }

        public void StartTracking()
        {
            client.GetListCompleted += delegate (object sender, GenericSdkEventArgs<IEnumerable<DiskItemInfo>> e)
            {
                this.elements_in_DataFolder = e.Result;
                canUpdateFromTimer = true;
            };
            client.GetListAsync(Disk_Data_Folder_path);
        }
    }
}