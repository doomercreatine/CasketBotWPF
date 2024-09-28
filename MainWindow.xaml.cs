using CasketChatBot.ViewModels;
using System;
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

namespace CasketChatBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            
            InitializeComponent();
        }

        private void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow.WindowState != WindowState.Maximized)
                Application.Current.MainWindow.WindowState = WindowState.Maximized;
            else
                Application.Current.MainWindow.WindowState = WindowState.Normal;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            //MessageBoxResult result = MessageBox.Show("Are you sure you want to close?",
            //                              "Confirmation",
            //                              MessageBoxButton.YesNo,
            //                              MessageBoxImage.Question);
            //if (result == MessageBoxResult.Yes)
            //{
                Application.Current.Shutdown();
            //}

        }
        private void KeepOnTop_Checked(object sender, RoutedEventArgs e)
        {
                           
                this.Topmost = true;
            
        }

        private void KeepOnTop_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
        }
    }
}