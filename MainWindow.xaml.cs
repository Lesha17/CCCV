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
        private DiskSdkClient client;
        private EventWaitHandle ready;
        private string notify_message;

        private const string LogOut = "Log out";

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
            login_page = new LogIn(true);
            MainFrame.NavigationService.Navigate(login_page);
            //login_page.Browser.Navigate("https://passport.yandex.ru/passport?mode=logout&yu=1285273511392102260&retpath=http%3A%2F%2Fwww.yandex.ru%2F");
        }

        void ReLogIn(object sender, NavigationEventArgs e)
        {
            removeOutItem();
            changedByUser = false;
            Authorize();
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
    }
}
