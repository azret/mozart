using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.Plot2D;
using Mozart.Properties;

unsafe partial class App {
    public string CurrentDirectory {
        get => Environment.CurrentDirectory.Trim('\\', '/');
        set => Environment.CurrentDirectory = value;
    }

    static IDictionary<string, Func<App, string, Func<bool>, bool>> _handlers = new Dictionary<string, Func<App, string, Func<bool>, bool>>();

    static void InitCliHandlers() {
        _handlers["--mic"] = (
            App app,
            string cliString,
            Func<bool> IsTerminated) => {
                return StartMicWinUI(
                    app,
                    cliString,
                    IsTerminated);
            };
        _handlers["--curves"] = (
            App app,
            string cliString,
            Func<bool> IsTerminated) => {
                app.StartWin32UI<System.Audio.IStream>(null,
                        Curves.DrawCurves, () => null, "Curves",
                        Color.White,
                        Resources.Oxygen,
                        new Size(623, 400));
                return true;
            };
        _handlers["--fit"] = (
            App app,
            string cliString,
            Func<bool> IsTerminated) => {
                return System.Ai.CBOW.Train(
                    app.CurrentDirectory,
                    "*.*",
                    IsTerminated);
            };
    }

    static void Main() {
        InitCliHandlers();
        RunCli(new App());
    }

    #region Cli

    public static void RunCli(App app) {
        var HasCtrlBreak = false;
        Console.CancelKeyPress += OnCancelKey;
        try {
            Console.InputEncoding
                = Console.OutputEncoding = Encoding.UTF8;
            var cliScript = Environment.CommandLine;
            string exe = Environment.GetCommandLineArgs().First();
            if (cliScript.StartsWith($"\"{exe}\"")) {
                cliScript = cliScript.Remove(0, $"\"{exe}\"".Length);
            } else if (cliScript.StartsWith(exe)) {
                cliScript = cliScript.Remove(0, exe.Length);
            }
            cliScript
                = cliScript.Trim();
            if (!string.IsNullOrWhiteSpace(cliScript)) {
                HasCtrlBreak = ExecCli(
                    app,
                    cliScript,
                    () => HasCtrlBreak);
            }
            while (!HasCtrlBreak) {
                Console.Write($"\r\n{app.CurrentDirectory}>");
                Console.Title = Path.GetFileNameWithoutExtension(
                    typeof(App).Assembly.Location);
                cliScript = Console.ReadLine();
                if (cliScript == null) {
                    HasCtrlBreak = false;
                    continue;
                }
                try {
                    HasCtrlBreak = ExecCli(
                        app,
                        cliScript,
                        () => HasCtrlBreak);
                } catch (Exception e) {
                    Console.WriteLine(e.ToString());
                }
            }
        } finally {
            Console.CancelKeyPress -= OnCancelKey;
        }
        void OnCancelKey(object sender, ConsoleCancelEventArgs e) {
            HasCtrlBreak = true;
            e.Cancel = true;
        }
    }

    static bool ExecCli(
            App app,
            string cliString,
            Func<bool> IsTerminated) {
        if (cliString.StartsWith("cd", StringComparison.OrdinalIgnoreCase)) {
            var dir = cliString.Remove(0, "cd".Length).Trim();
            if (Directory.Exists(dir)) {
                app.CurrentDirectory = dir;
            }
        } else if (cliString.StartsWith("cls", StringComparison.OrdinalIgnoreCase)) {
            Console.Clear();
        } else {
            foreach (var h in _handlers) {
                if (cliString.StartsWith(h.Key, StringComparison.OrdinalIgnoreCase)) {
                    return h.Value(app, cliString, IsTerminated);
                }
            }
            string outputFileName = @"D:\Mozart\src\App.cbow";
            var Model = System.Ai.CBOW.LoadFromFile(outputFileName, System.Ai.CBOW.SIZE,
                    out string fmt, out int dims);
            System.Ai.CBOW.RunFullCosineSort(new CSharp(), Model, cliString, 32);
        }
        return false;
    }

    #endregion

    #region Win32

    public Thread StartWin32UI<T>(IPlot2DController controller, Plot2D<T>.DrawFrame onDrawFrame, Func<T> onGetFrame, string title,
        Color bgColor, Icon hIcon = null, Size? size = null)
        where T : class {
        Thread t = new Thread(() => {
            IntPtr handl = IntPtr.Zero;
            Plot2D<T> hWnd = null;
            try {
                hWnd = new Plot2D<T>(controller, title,
                    onDrawFrame,
                    TimeSpan.FromMilliseconds(300),
                    onGetFrame, bgColor, hIcon, size);
                AddWinMMHandle(handl = hWnd.hWnd);
                hWnd.Show();
                while (User32.GetMessage(out MSG msg, hWnd.hWnd, 0, 0) != 0) {
                    User32.TranslateMessage(ref msg);
                    User32.DispatchMessage(ref msg);
                }
            } catch (Exception e) {
                Console.Error?.WriteLine(e);
            } finally {
                RemoveWinMMHandle(handl);
                hWnd?.Dispose();
                WinMM.PlaySound(null,
                        IntPtr.Zero,
                        WinMM.PLAYSOUNDFLAGS.SND_ASYNC |
                        WinMM.PLAYSOUNDFLAGS.SND_FILENAME |
                        WinMM.PLAYSOUNDFLAGS.SND_NODEFAULT |
                        WinMM.PLAYSOUNDFLAGS.SND_NOWAIT |
                        WinMM.PLAYSOUNDFLAGS.SND_PURGE);
            }
        });
        t.Start();
        return t;
    }

    object _WinMMLock = new object();

    private IntPtr[] _WinUIHandles;

    public void AddWinMMHandle(IntPtr hWnd) {
        lock (_WinMMLock) {
            if (_WinUIHandles == null) {
                _WinUIHandles = new IntPtr[0];
            }
            Array.Resize(ref _WinUIHandles,
                _WinUIHandles.Length + 1);
            _WinUIHandles[_WinUIHandles.Length - 1] = hWnd;
        }
    }

    public void ClearWinMMHandles() {
        lock (_WinMMLock) {
            _WinUIHandles = null;
        }
    }

    public void RemoveWinMMHandle(IntPtr hWnd) {
        lock (_WinMMLock) {
            if (_WinUIHandles != null) {
                for (int i = 0; i < _WinUIHandles.Length; i++) {
                    if (_WinUIHandles[i] == hWnd) {
                        _WinUIHandles[i] = IntPtr.Zero;
                    }
                }
            }
        }
    }

    public unsafe void PostWinMMMessage(Microsoft.WinMM.Mic32 hMic, IntPtr hWaveHeader) {
        lock (_WinMMLock) {
            if (_WinUIHandles == null) {
                return;
            }
            foreach (IntPtr hWnd in _WinUIHandles) {
                if (hWnd != IntPtr.Zero) {
                    User32.PostMessage(hWnd, WM.WINMM,
                        hMic != null
                            ? hMic.Handle
                            : IntPtr.Zero,
                        hWaveHeader);
                }
            }
        }
    }

    #endregion
}