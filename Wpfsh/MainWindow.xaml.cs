using ConPty;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Wpfsh.Native;

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
            Composition.SetAcrylicBlur(this, new Color { A = 128, B = 200, G = 100, R = 0 });

            _terminal = new Terminal();
            Task.Run(() => _terminal.Start("pwsh.exe"));
            _terminal.OutputReady += Terminal_OutputReady;
        }

        private void Terminal_OutputReady(object sender, EventArgs e)
        {
            Task.Run(() => CopyConsoleToWindow());
        }

        private void CopyConsoleToWindow()
        {
            using (StreamReader reader = new StreamReader(_terminal.ConsoleOutStream))
            {
                int bytesRead;
                char[] buf = new char[8];
                while ((bytesRead = reader.ReadBlock(buf, 0, 2)) != 0)
                {
                    // This is where you'd parse and tokenize the incoming VT100 text, most likely.
                    Dispatcher.Invoke(() =>
                    {
                        // ...and then you'd do something to render it.
                        TerminalHistoryBlock.Text += new string(buf.Take(bytesRead).ToArray());
                    });
                }
            }
        }

        private void TerminalEntryField_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                // This is where you'd take the pressed key, and convert it to a 
                // VT100 code before sending it along. For now, just send something.
                _terminal.WriteToPseudoConsole(e.Key.ToString());
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
    }
}
