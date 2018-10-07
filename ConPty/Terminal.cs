using ConPty.Processes;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using static ConPty.Native.ConsoleApi;

namespace ConPty
{
    /// <summary>
    /// Class for managing communication with the underlying console, and communicating with its pseudoconsole.
    /// </summary>
    public sealed class Terminal
    {
        private const string ExitCommand = "exit\r";
        private const string CtrlC_Command = "\x3";
        private CONSOLE_SCREEN_BUFFER_INFO_EX _consoleScreenInfo;
        private SafeFileHandle _consoleInputPipeWriteHandle;
        private StreamWriter _consoleInputWriter;

        public FileStream ConsoleOutStream { get; private set; }

        /// <summary>
        /// Fired once the console has been hooked up and is ready to receive input.
        /// </summary>
        public event EventHandler OutputReady;

        public Terminal()
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

            EnableVirtualTerminalSequenceProcessing();
        }        
        
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
        /// <param name="consoleHeight">The height (in characters) to start the pseudoconsole with. Defaults to 80.</param>
        /// <param name="consoleWidth">The width (in characters) to start the pseudoconsole with. Defaults to 30.</param>
        public void Start(string command, int consoleWidth = 80, int consoleHeight = 30)
        {
            using (var inputPipe = new PseudoConsolePipe())
            using (var outputPipe = new PseudoConsolePipe())
            using (var pseudoConsole = PseudoConsole.Create(inputPipe.ReadSide, outputPipe.WriteSide, consoleWidth, consoleHeight))
            using (var process = ProcessFactory.Start(command, PseudoConsole.PseudoConsoleThreadAttribute, pseudoConsole.Handle))
            {
                // copy all pseudoconsole output to a FileStream and expose it to the rest of the app
                ConsoleOutStream = new FileStream(outputPipe.ReadSide, FileAccess.Read);
                OutputReady.Invoke(this, EventArgs.Empty);

                // Store input pipe handle, and a writer for later reuse
                _consoleInputPipeWriteHandle = inputPipe.WriteSide;
                _consoleInputWriter = new StreamWriter(new FileStream(_consoleInputPipeWriteHandle, FileAccess.Write))
                {
                    AutoFlush = true
                };

                // free resources in case the console is ungracefully closed (e.g. by the 'x' in the window titlebar)
                OnClose(() => DisposeResources(process, pseudoConsole, outputPipe, inputPipe, _consoleInputWriter));

                WaitForExit(process).WaitOne(Timeout.Infinite);
            }
        }

        /// <summary>
        /// Sends the given string to the anonymous pipe that writes to the active pseudoconsole.
        /// </summary>
        /// <param name="input"></param>
        public void WriteToPseudoConsole(string input)
        {
            if (_consoleInputWriter == null)
            {
                throw new InvalidOperationException("There is no writer attached to a pseudoconsole. Have you called Start on this instance yet?");
            }
            _consoleInputWriter.Write(input);
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

        // TODO: Should this class just implement IDisposable?
        private void DisposeResources(params IDisposable[] disposables)
        {
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
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
    }
}
