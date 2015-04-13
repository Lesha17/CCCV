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
using System.Threading;
using Disk.SDK;
using Disk.SDK.Provider;

namespace CCCV
{
    /// <summary>
    /// Interaction logic for Progress.xaml
    /// </summary>
    public partial class Progress : Page, IProgress
    {
        private MainWindow mw;
        private long all;
        private long current;
        private int index;
        private long done;
        private bool working = true;
        Dictionary<long, long> processes = new Dictionary<long, long>();

        public Progress(MainWindow mw, long all)
        {
            InitializeComponent();
            this.All = all;
            this.Current = 0;
            this.mw = mw;
            mw.NavigateTo(this);
        }

        public long All
        {
            get { return all; }
            set
            {
                this.working = true;
                this.all = value;
                string toShow = "";

                if (all / (1024 * 1024 * 1024) > 0)
                {
                    toShow = ((int)(10 * all / (double)(1024 * 1024 * 1024))) / 10.0 + " ГБ";
                }
                else if (all / (1024 * 1024) > 0)
                {
                    toShow = ((int)(10 * all / (double)(1024 * 1024))) / 10.0 + " МБ";
                }
                else if (all / (1024) > 0)
                {
                    toShow = ((int)(10 * all / (double)(1024))) / 10.0 + " КБ";
                }
                else
                {
                    toShow = all + " Байт";
                }
                All_TB.Text = toShow;
            }
        }

        public long Current
        {
            get { return this.current; }
            set
            {
                this.current = done + value;
                string toShow = "";

                if (current / (1024 * 1024 * 1024) > 0)
                {
                    toShow = ((int)(10 * current / (double)(1024 * 1024 * 1024))) / 10.0 + " ГБ";
                }
                else if (current / (1024 * 1024) > 0)
                {
                    toShow = ((int)(10 * current / (double)(1024 * 1024))) / 10.0 + " МБ";
                }
                else if (current / (1024) > 0)
                {
                    toShow = ((int)(10 * current / (double)(1024))) / 10.0 + " КБ";
                }
                else
                {
                    toShow = current + " Байт";
                }
                Processed_TB.Text = toShow;
                PrBar.Value = (current / (double)all) * 100;
            }
        }

        public int Index
        {
            get { return this.index; }
            set { this.index = value; }
        }

        public long Done
        {
            get { return this.done; }
            set { this.done = value; }
        }

        public bool Working
        {
            get { return this.working; }
        }

        public void UpdateProgress(ulong current, ulong all)
        {
            if (processes.ContainsKey((long)all))
            {
                if (current == all)
                {
                    Done += (long)all;
                    processes.Remove((long)all);
                }
                else
                {
                    processes[(long)all] = (long)current;
                }
            }
            else
            {
                processes.Add((long)all, (long)current);
            }
            long summ = 0;
            foreach (KeyValuePair<long, long> p in processes)
            {
                summ += p.Value;
            }
            Dispatcher.BeginInvoke(new ThreadStart(delegate { this.Current = summ; }));
        }

        public void Completed(bool b)
        {
            if (b || Current >= All)
            {
                working = false;
                mw.NavigateToSettings();
            }
        }
    }
}
