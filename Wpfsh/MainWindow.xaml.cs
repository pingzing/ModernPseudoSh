using MiniTerm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using Wpfsh.Native;

namespace Wpfsh
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Terminal _terminal;
        private Stream _consoleInputStream;
        private StreamWriter _writer;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Composition.SetAcrylicBlur(this, new Color { A = 128, B = 200, G = 100, R = 0 });
            
            _consoleInputStream = new MemoryStream();
            _writer = new StreamWriter(_consoleInputStream);
            _writer.AutoFlush = true;
            _terminal = new Terminal(_consoleInputStream);
            Task.Run(() => _terminal.Run("pwsh.exe"));
            Task.Run(() => CopyConsoleToWindow());
        }

        private void CopyConsoleToWindow()
        {
            Thread.Sleep(4000);
            using (StreamReader reader = new StreamReader(_terminal.ConsoleOutStream))
            {
                int bytesRead;
                char[] buf = new char[8];
                while ((bytesRead = reader.ReadBlock(buf, 0, 2)) != 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        TerminalHistoryBlock.Text += new string(buf.Take(bytesRead).ToArray());
                    });                    
                }
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) { DragMove(); }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private bool _autoScroll = true;
        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // User scrolled...
            if (e.ExtentHeightChange == 0)
            {                
                //...down to the bottom. Re-engage autoscrolling.
                if (TerminalHistoryViewer.VerticalOffset == TerminalHistoryViewer.ScrollableHeight)
                {
                    _autoScroll = true;
                }
                //...elsewhere. Disengage autoscrolling.
                else
                {
                    _autoScroll = false;
                }

                // Autoscrolling is enabled, and content caused scrolling:
                if (_autoScroll && e.ExtentHeightChange != 0)
                {
                    TerminalHistoryViewer.ScrollToEnd();
                }
            }
        }

        private void TerminalEntryField_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                _writer.Write(e.Key.ToString());
                int length = e.Key.ToString().Length;
                _consoleInputStream.Seek(-length, SeekOrigin.Current);
            }
        }
    }
}
