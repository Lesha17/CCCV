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
using System.Windows.Threading;
using Disk.SDK;
using Disk.SDK.Provider;

namespace CCCV
{
    /// <summary>
    /// Interaction logic for Progress.xaml
    /// </summary>
    public partial class Progress : Page, IProgress
    {
        MainWindow mw;
        private long all;
        private long current;
        private int index;
        private long done;
        private long count_of_tasks;
        Dictionary<long, long> processes = new Dictionary<long, long>();

        public Progress(MainWindow mw, long all)
        {
            InitializeComponent();
            this.All = all;
            this.Current = 0;
            this.count_of_tasks = 0;
            this.mw = mw;
        }

        public long All
        {
            get { return all; }
            set
            {
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
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    All_TB.Text = toShow;
                }));
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
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    Processed_TB.Text = toShow;
                    PrBar.Value = (current / (double)all) * 100;
                }));
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
            get { return this.count_of_tasks > 0; }
        }

        public long CountOfTasks
        {
            get { return count_of_tasks; }
        }

        public void IncrementCountOfTasks()
        {
            this.count_of_tasks++;
        }

        public void DecrementCountOfTasks(long size)
        {
            this.count_of_tasks--;
            if (this.count_of_tasks <= 0)
            {
                this.count_of_tasks = 0;
                this.current = 0;
                this.all = 0;
                this.done = 0;
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    mw.NavigateToSettings();
                }));

            }
        }

        public void UpdateProgress(ulong current, ulong all)
        {
            lock (processes)
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
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
                }));
            }
        }
    }

    partial class MainWindow
    {
        //Must call from STA
        private void InitProgress(long size)
        {
            if (progress == null ? true : !progress.Working)
            {
                progress = new Progress(this, size);
            }
            else
            {
                progress.All += size;
            }
            progress.IncrementCountOfTasks();
            MainFrame.Navigate(progress);
        }
    }
}
