using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.Plot2D;

unsafe partial class App {
    public string CurrentDirectory {
        get => Environment.CurrentDirectory.Trim('\\', '/');
        set => Environment.CurrentDirectory = value;
    }

    static void Main() {
        runCli(new App());
    }

    public static void runCli(App app) {
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
                HasCtrlBreak = Exec(
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
                    HasCtrlBreak = Exec(
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

    static bool Exec(
            App app,
            string cliString,
            Func<bool> IsTerminated) {
        if (cliString.StartsWith("--fit", StringComparison.OrdinalIgnoreCase)
                    || cliString.StartsWith("fit", StringComparison.OrdinalIgnoreCase)) {
            double[] W = new double[13];
            global::Random.Randomize(W);
            System.Ai.Fit.train(
                0.01,
                Sample: () => {
                    var X = new double[W.Length];
                    return X;
                },
                W: W,
                F: (X) => {
                    return false;
                },
                SetLoss: (loss) => {
                    Console.WriteLine(loss);
                },
                HasCtrlBreak: IsTerminated);
        } else if (cliString.StartsWith("--cbow", StringComparison.OrdinalIgnoreCase) 
                    || cliString.StartsWith("cbow", StringComparison.OrdinalIgnoreCase)) {
            return System.Ai.CBOW.Train(
                app.CurrentDirectory,
                "*.*",
                IsTerminated);
        } else if (cliString.StartsWith("--mic", StringComparison.OrdinalIgnoreCase) || cliString.StartsWith("mic", StringComparison.OrdinalIgnoreCase)) {
            return ShowMicWinUI(
                app,
                cliString,
                IsTerminated);
        } else if (cliString.StartsWith("--fft", StringComparison.OrdinalIgnoreCase) || cliString.StartsWith("fft", StringComparison.OrdinalIgnoreCase)) {
            return ShowFourierTransform(
                app,
                cliString,
                IsTerminated);
        } else if (cliString.StartsWith("cd", StringComparison.OrdinalIgnoreCase)) {
            var dir = cliString.Remove(0, "cd".Length).Trim();
            if (Directory.Exists(dir)) {
                app.CurrentDirectory = dir;
            }
        } else if (cliString.StartsWith("cls", StringComparison.OrdinalIgnoreCase)) {
            Console.Clear();
        } else {
            string outputFileName = @"D:\Mozart\src\App.cbow";

            var Model = System.Ai.CBOW.LoadFromFile(outputFileName, System.Ai.CBOW.SIZE,
                    out string fmt, out int dims);

            System.Ai.CBOW.RunFullCosineSort(new CSharp(), Model, cliString, 32);
        }
        return false;
    }

    Thread StartWinUI<T>(IPlot2DController controller, Plot2D<T>.DrawFrame onDrawFrame, Func<T> onGetFrame, string title,
        Color bgColor, Plot2D<T>.KeyDown onKeyDown = null, Action onDone = null, Icon hIcon = null,
        Size? size = null)
        where T : class {
        Thread t = new Thread(() => {
            IntPtr handl = IntPtr.Zero;
            Plot2D<T> hWnd = null;
            try {
                hWnd = new Plot2D<T>(controller, title,
                    onDrawFrame, onKeyDown,
                    TimeSpan.FromMilliseconds(1000),
                    onGetFrame, bgColor, hIcon, size);
                AddHandle(handl = hWnd.hWnd);
                hWnd.Show();
                while (User32.GetMessage(out MSG msg, hWnd.hWnd, 0, 0) != 0) {
                    User32.TranslateMessage(ref msg);
                    User32.DispatchMessage(ref msg);
                }
            } catch (Exception e) {
                Console.Error?.WriteLine(e);
            } finally {
                RemoveHandle(handl);
                hWnd?.Dispose();
                WinMM.PlaySound(null,
                        IntPtr.Zero,
                        WinMM.PLAYSOUNDFLAGS.SND_ASYNC |
                        WinMM.PLAYSOUNDFLAGS.SND_FILENAME |
                        WinMM.PLAYSOUNDFLAGS.SND_NODEFAULT |
                        WinMM.PLAYSOUNDFLAGS.SND_NOWAIT |
                        WinMM.PLAYSOUNDFLAGS.SND_PURGE);
                onDone?.Invoke();
            }
        });
        t.Start();
        return t;
    }

    int onKeyDown<T>(IntPtr hWnd, WM msg, IntPtr wParam, IntPtr lParam, T frame) {
        if (wParam == new IntPtr(0x20)) {
            WinMM.PlaySound(null,
                    IntPtr.Zero,
                    WinMM.PLAYSOUNDFLAGS.SND_ASYNC |
                    WinMM.PLAYSOUNDFLAGS.SND_FILENAME |
                    WinMM.PLAYSOUNDFLAGS.SND_NODEFAULT |
                    WinMM.PLAYSOUNDFLAGS.SND_NOWAIT |
                    WinMM.PLAYSOUNDFLAGS.SND_PURGE);
            Toggle();
        }
        return 0;
    }

    #region Events

    object _Win32Lock = new object();

    private IntPtr[] _handles;

    public void AddHandle(IntPtr hWnd) {
        lock (_Win32Lock) {
            if (_handles == null) {
                _handles = new IntPtr[0];
            }
            Array.Resize(ref _handles,
                _handles.Length + 1);
            _handles[_handles.Length - 1] = hWnd;
        }
    }

    public void ClearHandles() {
        lock (_Win32Lock) {
            _handles = null;
        }
    }

    public void RemoveHandle(IntPtr hWnd) {
        lock (_Win32Lock) {
            if (_handles != null) {
                for (int i = 0; i < _handles.Length; i++) {
                    if (_handles[i] == hWnd) {
                        _handles[i] = IntPtr.Zero;
                    }
                }
            }
        }
    }

    public unsafe void Notify(Microsoft.WinMM.Mic32 hMic, IntPtr hWaveHeader) {
        lock (_Win32Lock) {
            if (_handles == null) {
                return;
            }
            foreach (IntPtr hWnd in _handles) {
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