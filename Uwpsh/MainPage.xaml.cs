using MiniTerm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Uwpsh
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Terminal _terminal;

        public MainPage()
        {
            this.InitializeComponent();
            CoreWindow.GetForCurrentThread().KeyDown += MainPage_CharacterReceived;            
        }

        private void MainPage_CharacterReceived(CoreWindow sender, KeyEventArgs args)
        {
            bool ctrlIsDown = sender.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            bool shiftIsDown = sender.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            bool capsEnabled = sender.GetKeyState(VirtualKey.CapitalLock).HasFlag(CoreVirtualKeyStates.Locked) 
                || sender.GetKeyState(VirtualKey.CapitalLock).HasFlag(CoreVirtualKeyStates.Down);


            _terminal.WriteToPseudoConsole(args.VirtualKey.ToString());
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _terminal = new Terminal();
            Task.Run(() => _terminal.Run("pwsh.exe"));
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
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, 
                        () =>
                        {
                            TerminalHistoryBlock.Text += new string(buf.Take(bytesRead).ToArray());
                        });
                }
            }
        }        
    }
}
