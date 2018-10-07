using ConPty;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uwpsh
{    
    public sealed partial class MainPage : Page
    {
        private Terminal _terminal;

        public MainPage()
        {
            this.InitializeComponent();
            CoreWindow.GetForCurrentThread().KeyDown += CoreWindow_KeyDown;            
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _terminal = new Terminal();
            Task.Run(() => _terminal.Start("pwsh.exe"));
            _terminal.OutputReady += Terminal_OutputReady;            
        }

        private void Terminal_OutputReady(object sender, EventArgs e)
        {
            Task.Run(() => CopyConsoleToWindow());
        }

        private async void CopyConsoleToWindow()
        {
            using (StreamReader reader = new StreamReader(_terminal.ConsoleOutStream))
            {
                int bytesRead;
                char[] buf = new char[8];
                while ((bytesRead = reader.ReadBlock(buf, 0, 2)) != 0)
                {
                    // This is where you'd parse and tokenize the incoming VT100 text, most likely.
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, 
                        () =>
                        {
                            // ...and then you'd do something to render it.
                            TerminalHistoryBlock.Text += new string(buf.Take(bytesRead).ToArray());
                        });
                }
            }
        }

        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            bool ctrlIsDown = sender.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            bool shiftIsDown = sender.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            bool capsEnabled = sender.GetKeyState(VirtualKey.CapitalLock).HasFlag(CoreVirtualKeyStates.Locked)
                || sender.GetKeyState(VirtualKey.CapitalLock).HasFlag(CoreVirtualKeyStates.Down);

            // This is where you'd take the pressed key, and convert it to a 
            // VT100 code before sending it along. For now, just send something.
            _terminal.WriteToPseudoConsole(args.VirtualKey.ToString());
        }
    }
}
