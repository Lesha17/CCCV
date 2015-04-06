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

namespace CCCV
{
    /// <summary>
    /// Логика взаимодействия для SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        public SettingsPage(Settings settings)
        {
            InitializeComponent();
            switch (settings.How)
            {
                case Settings.HowUpload.AfterClick:
                    RB1.IsChecked = true;
                    break;
                case Settings.HowUpload.Auto:
                    RB2.IsChecked = true;
                    break;
                case Settings.HowUpload.Allways:
                    RB3.IsChecked = true;
                    break;
            }
            SizeOfData.Text = (settings.SizeOfData / (1024 * 1024)).ToString();
            IfSizeBigger.IsChecked = settings.ShowBalloon;
            
        }

    }
}
