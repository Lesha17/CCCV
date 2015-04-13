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

namespace CCCV
{
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon;
        private string access_token;
        private DiskSdkClient client;
        private EventWaitHandle ready;
        private string notify_message;

        private LogIn login_page;
        private Processing_page processing_page;

        private const string LogOut = "Log out";

        private string local_data_path;
        private string local_files_path;

        private Progress progress;

        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void addLogOutItem()
        {
            System.Windows.Forms.ContextMenu menu = notifyIcon.ContextMenu;
            foreach (System.Windows.Forms.MenuItem item in menu.MenuItems)
                if (item.Text.Equals(LogOut))
                    return;
            var item2 = new System.Windows.Forms.MenuItem();
            item2.Text = LogOut;
            item2.Click += item2_Click;
            menu.MenuItems.Add(1, item2);
        }

        private void removeOutItem()
        {
            foreach (System.Windows.Forms.MenuItem item in notifyIcon.ContextMenu.MenuItems)
            {
                if (item != null && item.Text.Equals(LogOut))
                    notifyIcon.ContextMenu.MenuItems.Remove(item);
            }
        }

        void item2_Click(object sender, EventArgs e)
        {
            ReLogIn();
        }

        void ReLogIn()
        {
            removeOutItem();
            access_token = "";
            settings.Token = access_token;
            settings.save();
            System.Diagnostics.Process.Start("https://passport.yandex.ru/passport?mode=logout&yu=2087562651427364264");
            check_token_from_settings();
        }

        void item3_Click(object sender, EventArgs e)
        {
            if (settings != null)
                settings.save();
            System.Windows.Application.Current.Shutdown();
        }

        void item1_Click(object sender, EventArgs e)
        {
            Show();
        }

        void notifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Show();
        }

        public void NavigateToSettings()
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate { MainFrame.Navigate(settingsPage); }));
        }

        public void NavigateTo(object content)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate { MainFrame.Navigate(content); }));
        }
    }
}
