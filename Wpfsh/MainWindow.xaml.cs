using Wpfsh.ConPTY;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpfsh.Native;
using System.Windows.Media;

namespace Wpfsh
{
    public partial class MainWindow : Window
    {
        private Terminal _terminal;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.SetAcrylicBlur(new Color { A = 128, B = 200, G = 100, R = 0 });

            // Start up the console, and point it to cmd.exe.
            _terminal = new Terminal();
            Task.Run(() => _terminal.Start("cmd.exe"));
            _terminal.OutputReady += Terminal_OutputReady;
        }

        private async void Terminal_OutputReady(object sender, EventArgs e)
        {
            // Start a long-lived thread for the "read console" task, so that we don't use a standard thread pool thread.
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Factory.StartNew(() => CopyConsoleToWindow(), TaskCreationOptions.LongRunning);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            Dispatcher.Invoke(() => { TitleBarTitle.Text = "Wpfsh - cmd.exe"; });

            // Dirty hack to force the buffer to flush. There's a _correct_ way to do this, but eh, this is a proof-of-concept anyway.
            await Task.Delay(100);        
            _terminal.WriteToPseudoConsole("0");
        }

        private void CopyConsoleToWindow()
        {
            using (StreamReader reader = new StreamReader(_terminal.ConsoleOutStream))
            {                
                // Read the console's output 1 character at a time
                int bytesRead;
                char[] buf = new char[1];
                while ((bytesRead = reader.ReadBlock(buf, 0, 1)) != 0)
                {
                    // This is where you'd parse and tokenize the incoming VT100 text, most likely.
                    Dispatcher.Invoke(() =>
                    {
                        // ...and then you'd do something to render it.
                        // For now, just emit raw VT100 to the primary TextBlock.
                        TerminalHistoryBlock.Text += new string(buf.Take(bytesRead).ToArray());
                    });
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                // This is where you'd take the pressed key, and convert it to a 
                // VT100 code before sending it along. For now, we'll just send _something_.
                _terminal.WriteToPseudoConsole(e.Key.ToString());
            }
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

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) { DragMove(); }
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
                MaximizeRestoreButton.Content = "\uE923";
            }
            else if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaximizeRestoreButton.Content = "\uE922";
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
