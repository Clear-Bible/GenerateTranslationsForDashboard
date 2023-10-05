using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GenerateTranslationsForDashboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private MainWindowViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();

            _vm = this.DataContext as MainWindowViewModel;
        }

        private void TheWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Clean up the file watcher
            _vm.Closing();

        }
    }
}
