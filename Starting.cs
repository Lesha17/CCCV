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
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            CreateNotifyIcon();
            this.Closing += MainWindow_Closing;
            this.Initialized += MainWindow_Initialized;
            Preprocessing_completed += MainWindow_Preprocessing_completed;
            changedByUser = false;
            ready = new AutoResetEvent(true);
            notify_message = "Ничего не происходит";
            settings = new Settings();
            Authorize();
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

        void MainWindow_Initialized(object sender, EventArgs e)
        {
            //bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            
        }
    }
}
