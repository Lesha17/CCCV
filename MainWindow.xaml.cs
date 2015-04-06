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
    public partial class MainWindow : Window, IProgress
    {
        private NotifyIcon notifyIcon;
        private string access_token;
        private DiskSdkClient client;
        private EventWaitHandle ready;
        private string notify_message;

        private LogIn login_page;
        private Processing_page processing_page;

        private const string LogOut = "Log out";

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
                if(item!=null&& item.Text.Equals(LogOut))
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
            changedByUser = false;
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
