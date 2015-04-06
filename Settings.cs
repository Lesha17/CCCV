using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace CCCV
{
    [Serializable]
    public class Settings
    {
        public enum HowUpload { Allways, Auto, AfterClick };
        private const long SIZE_OF_DATA_DEFAULT = 5242880;
        private const long MAX_FILE_SIZE = 1073741824;

        private HowUpload how;
        private long sizeOfData;
        private bool showBalloon;
        private string token;
        private long tokenWillLive;

        public Settings()
        {
            Properties.Settings.Default.Reload();
            this.how = (HowUpload) Properties.Settings.Default.HowUpload;
            this.sizeOfData = Properties.Settings.Default.SizeOfData;
            this.showBalloon = Properties.Settings.Default.ShowBaloon;
            this.token = Properties.Settings.Default.token;
            this.tokenWillLive = Properties.Settings.Default.TokenWillLiveTo;
        }

        public Settings(HowUpload how, long sizeOfData, bool show)
        {
            this.how = how;
            this.sizeOfData = sizeOfData;
            this.showBalloon = show;
        }

        public Settings(HowUpload how, bool show)
        {
            this.how = how;
            this.sizeOfData = SIZE_OF_DATA_DEFAULT;
            this.showBalloon = show;
        }

        public Settings(HowUpload how, long sizeOfData)
        {
            this.how = how;
            this.sizeOfData = sizeOfData;
            this.showBalloon = false;
        }

        public Settings(HowUpload how)
        {
            this.how = how;
            switch (how)
            {
                case HowUpload.Auto:
                    sizeOfData = SIZE_OF_DATA_DEFAULT;
                    break;
                case HowUpload.Allways:
                    sizeOfData = MAX_FILE_SIZE;
                    break;
                case HowUpload.AfterClick:
                    sizeOfData = MAX_FILE_SIZE;
                    break;
                default:
                    break;
            }
        }
        public HowUpload How
        {
            get { return how; }
            set { how = value; }
        }

        public long SizeOfData
        {
            get { return sizeOfData; }
            set { sizeOfData = value; }
        }
        public bool ShowBalloon
        {
            get { return showBalloon; }
            set { showBalloon = value; }
        }
        public long CurrentSizeOfData
        {
            get
            {
                switch (how)
                {
                    case HowUpload.Allways:
                        return MAX_FILE_SIZE;
                    case HowUpload.AfterClick:
                        return 0;
                    case HowUpload.Auto:
                        return sizeOfData;
                    default:
                        return 0;
                }
            }
        }
        public long MaxSizeOfData
        {
            get { return MAX_FILE_SIZE; }
        }

        public String Token
        {
            get { return token; }
            set { this.token = value; }
        }

        public long TokenWillLive
        {
            get { return this.tokenWillLive; }
            set { this.tokenWillLive = value; }
        }

        public void save()
        {
            Properties.Settings.Default.HowUpload = (int)how;
            Properties.Settings.Default.SizeOfData = sizeOfData;
            Properties.Settings.Default.ShowBaloon = showBalloon;
            Properties.Settings.Default.token = token;
            Properties.Settings.Default.TokenWillLiveTo = tokenWillLive;
            Properties.Settings.Default.Save();
        }
    }
    public partial class MainWindow : Window
    {
        private Settings settings;
        private const Settings.HowUpload HOW_UPLOAD_DEFAULT = Settings.HowUpload.AfterClick;
        private const string SETTINGS = "settings.cbd";

        void SizeOfData_TextChanged(object sender, TextChangedEventArgs e)
        {
            string s = settingsPage.SizeOfData.Text;
            int result;
            if (int.TryParse(s, out result))
            {
                settings.SizeOfData = result * 1024 * 1024;
            }
            else
            {
                settingsPage.SizeOfData.Text = (settings.SizeOfData / (1024 * 1024)).ToString();
            }
        }

        void RB3_Checked(object sender, RoutedEventArgs e)
        {
            if (settings == null)
                settings = new Settings(Settings.HowUpload.Allways);
            else
                settings.How = Settings.HowUpload.Allways;
            settingsPage.SizeOfData.Visibility = System.Windows.Visibility.Hidden;
        }

        void RB2_Checked(object sender, RoutedEventArgs e)
        {
            if (settings == null)
                settings = new Settings(Settings.HowUpload.Auto);
            else
                settings.How = Settings.HowUpload.Auto;
            settingsPage.SizeOfData.Visibility = System.Windows.Visibility.Visible;
        }

        void RB1_Checked(object sender, RoutedEventArgs e)
        {
            if (settings == null)
                settings = new Settings(Settings.HowUpload.AfterClick);
            else
                settings.How = Settings.HowUpload.AfterClick;
            settingsPage.SizeOfData.Visibility = System.Windows.Visibility.Hidden;
        }

        void IfSizeBigger_Checked(object sender, RoutedEventArgs e)
        {
            if (settings == null)
            {
                settings = new Settings(HOW_UPLOAD_DEFAULT, true);
            }
            else
                settings.ShowBalloon = true;
        }

        void IfSizeBigger_Unchecked(object sender, RoutedEventArgs e)
        {
            if (settings == null)
            {
                settings = new Settings(HOW_UPLOAD_DEFAULT, false);
            }
            else
                settings.ShowBalloon = false;

        }
    }
}
