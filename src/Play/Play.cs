﻿using System;
using System.Audio;
using System.Diagnostics;
using System.IO;

partial class App {
    static bool PlayFrequency(App app,
        string cliScript,
        Func<bool> IsTerminated) {
        string title = cliScript;

        if (string.IsNullOrWhiteSpace(cliScript)) {
            return false;
        }

        var inWav = File.Exists(cliScript)
            ? Wav.Parse(cliScript)
            : Wav.Parse(cliScript, "MIDI");

        var wavOutFile = File.Exists(cliScript)
            ? Path.GetFullPath(Path.ChangeExtension(cliScript, ".g.wav"))
            : Path.GetFullPath(Path.ChangeExtension("cli.wav", ".g.wav"));

        Console.Write($"\r\nSynthesizing...\r\n\r\n");

        var data = Wav.Synthesize(inWav);

        Wav.Write(wavOutFile, data);

        Console.Write($"\r\nReady!\r\n\r\n");

        Microsoft.Win32.WinMM.PlaySound(wavOutFile,
            IntPtr.Zero,
            Microsoft.Win32.WinMM.PLAYSOUNDFLAGS.SND_ASYNC |
            Microsoft.Win32.WinMM.PLAYSOUNDFLAGS.SND_FILENAME |
            Microsoft.Win32.WinMM.PLAYSOUNDFLAGS.SND_NODEFAULT |
            Microsoft.Win32.WinMM.PLAYSOUNDFLAGS.SND_NOWAIT |
            // Microsoft.Win32.WinMM.PLAYSOUNDFLAGS.SND_LOOP |
            Microsoft.Win32.WinMM.PLAYSOUNDFLAGS.SND_PURGE);

        return false;
    }
}