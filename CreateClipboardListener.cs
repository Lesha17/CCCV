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
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace CCCV
{
    public partial class MainWindow : Window, IProgress
    {
        [DllImport("User32.dll")]
        protected static extern int SetClipboardViewer(int hWndNewViewer);
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        const int WM_CHANGECBCHAIN = 0x30D;
        const int WM_DRAWCLIPBOARD = 0x308;

        private IntPtr nextClipboardViewer;
        private HwndSource hwndSource;

        public void CreateClipboardListener()
        {
            
            
            hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            nextClipboardViewer = (IntPtr)SetClipboardViewer((int)hwndSource.Handle);
            hwndSource.AddHook(WndProc);
            
            Preprocessing_completed(this, new EventArgs());
        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_DRAWCLIPBOARD:
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
    }
}