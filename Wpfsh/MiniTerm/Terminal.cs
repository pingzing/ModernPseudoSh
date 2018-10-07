using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wpfsh;
using static Wpfsh.Native.ConsoleApi;

namespace MiniTerm
{
    /// <summary>
    /// The UI of the terminal. It's just a normal console window, but we're managing the input/output.
    /// In a "real" project this could be some other UI.
    /// </summary>
    internal sealed class Terminal
    {
        private const string ExitCommand = "exit\r";
        private const string CtrlC_Command = "\x3";
        private CONSOLE_SCREEN_BUFFER_INFO_EX _consoleScreenInfo;

        public FileStream ConsoleOutStream { get; private set; }
        public Stream ConsoleInStream { get; private set; }

        public Terminal(Stream consoleInStream)
        {
            ConsoleInStream = consoleInStream;
            InitializeConsole();
            EnableVirtualTerminalSequenceProcessing();
        }

        private void InitializeConsole()
        {
            if (GetConsoleWindow() == IntPtr.Zero)
            {
                bool createConsoleSuccess = AllocConsole();
                if (!createConsoleSuccess)
                {
                    string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                    throw new InvalidOperationException($"Could not allocate console for this process. Error message: {errorMessage}");
                }
            }
        }

        private SafeFileHandle GetConsoleScreenBuffer()
        {
            IntPtr file = CreateFileW(
                ConsoleOutPseudoFilename,
                GENERIC_WRITE | GENERIC_READ,
                FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero);

            if (file == new IntPtr(-1))
            {
                string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                throw new InvalidOperationException($"Could not get console screen buffer. Error message: {errorMessage}");
            }

            return new SafeFileHandle(file, true);
        }


        /// <summary>
        /// Newer versions of the windows console support interpreting virtual terminal sequences, we just have to opt-in
        /// </summary>
        private void EnableVirtualTerminalSequenceProcessing()
        {
            SafeFileHandle screenBuffer = GetConsoleScreenBuffer();
            if (!GetConsoleMode(screenBuffer, out uint outConsoleMode))
            {
                string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                throw new InvalidOperationException($"Could not get console mode. Error message: {errorMessage}");
            }

            outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
            if (!SetConsoleMode(screenBuffer, outConsoleMode))
            {
                string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                throw new InvalidOperationException($"Could not enable virtual terminal processing: {errorMessage}");
            }

            _consoleScreenInfo = new CONSOLE_SCREEN_BUFFER_INFO_EX();
            _consoleScreenInfo.cbSize = (uint)Marshal.SizeOf(_consoleScreenInfo);
            if (!GetConsoleScreenBufferInfoEx(screenBuffer, ref _consoleScreenInfo))
            {
                string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                throw new InvalidOperationException($"Could not enable console screen info: {errorMessage}");
            }
        }

        /// <summary>
        /// Start the psuedoconsole and run the process as shown in 
        /// https://docs.microsoft.com/en-us/windows/console/creating-a-pseudoconsole-session#creating-the-pseudoconsole
        /// </summary>
        /// <param name="command">the command to run, e.g. cmd.exe</param>
        public void Run(string command)
        {
            using (var inputPipe = new PseudoConsolePipe())
            using (var outputPipe = new PseudoConsolePipe())
            using (var pseudoConsole = PseudoConsole.Create(inputPipe.ReadSide, outputPipe.WriteSide, 80, 30))
            using (var process = ProcessFactory.Start(command, PseudoConsole.PseudoConsoleThreadAttribute, pseudoConsole.Handle))
            {
                // copy all pseudoconsole output to a FileStream and expose it to the rest of the app
                ConsoleOutStream = new FileStream(outputPipe.ReadSide, FileAccess.Read);
                // prompt for stdin input and send the result to the pseudoconsole
                Task.Run(() => CopyInputToPipe(inputPipe.WriteSide));
                // free resources in case the console is ungracefully closed (e.g. by the 'x' in the window titlebar)
                OnClose(() => DisposeResources(process, pseudoConsole, outputPipe, inputPipe));

                WaitForExit(process).WaitOne(Timeout.Infinite);
            }
        }

        /// <summary>
        /// Reads terminal input and copies it to the PseudoConsole
        /// </summary>
        /// <param name="inputWriteSide">the "write" side of the pseudo console input pipe</param>
        private void CopyInputToPipe(SafeFileHandle inputWriteSide)
        {
            using (var writer = new StreamWriter(new FileStream(inputWriteSide, FileAccess.Write)))
            {
                ForwardCtrlC(writer);
                writer.AutoFlush = true;
                writer.WriteLine(@"cd \");

                using (StreamReader reader = new StreamReader(ConsoleInStream))
                {
                    int bytesRead;
                    char[] buf = new char[8];
                    while (true)
                    {
                        bytesRead = reader.ReadBlock(buf, 0, 2);
                        if (bytesRead != 0)
                        {
                            // send input character-by-character to the pipe                    
                            writer.Write(buf.Take(bytesRead).ToArray());
                        }
                        Thread.Sleep(30);
                    }
                }
            }
        }

        /// <summary>
        /// Don't let ctrl-c kill the terminal, it should be sent to the process in the terminal.
        /// </summary>
        private static void ForwardCtrlC(StreamWriter writer)
        {
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                writer.Write(CtrlC_Command);
            };
        }

        /// <summary>
        /// Get an AutoResetEvent that signals when the process exits
        /// </summary>
        private static AutoResetEvent WaitForExit(Process process) =>
            new AutoResetEvent(false)
            {
                SafeWaitHandle = new SafeWaitHandle(process.ProcessInfo.hProcess, ownsHandle: false)
            };

        /// <summary>
        /// Set a callback for when the terminal is closed (e.g. via the "X" window decoration button).
        /// Intended for resource cleanup logic.
        /// </summary>
        private static void OnClose(Action handler)
        {
            SetConsoleCtrlHandler(eventType =>
            {
                if(eventType == CtrlTypes.CTRL_CLOSE_EVENT)
                {
                    handler();
                }
                return false;
            }, true);
        }

        private void DisposeResources(params IDisposable[] disposables)
        {
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }
    }
}
