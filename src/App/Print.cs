﻿using System;
using System.Collections.Generic;

public static class Print {
    public static void Dump(IEnumerable<Complex[]> fft) {
        foreach (Complex[] i in fft) {
            int cc = 0;
            for (int s = 0; s < i.Length; s++) {
                if (s == 0) {
                    Console.Write($"{i.Length}");
                }
                Console.Write($" {i[s].ToString()}");
                cc++;
            }
            if (cc > 0) {
                Console.WriteLine();
            }
        }
    }

    public static void Dump(IEnumerable<Complex[]> fft, int hz) {
        foreach (Complex[] i in fft) {
            Dump(i, hz);
        }
    }

    public static void Dump(Complex[] fft, int hz) {
        var samples = fft.Length;
        double duration
            = Math.Round(samples / (double)hz, 4);
        double h = hz
            / (double)samples;
        int cc = 0;
        for (int s = 0; s < samples / 2; s++) {
            var f =
                    h * 0.5 + (s * h);
            double vol = 2 * fft[s].Magnitude;
            var dB = System.Audio.dB.FromAmplitude(vol);
            if (!Ranges.IsInRange(f, dB)) {
                vol = 0;
                dB = int.MinValue;
            }
            if ((dB != int.MinValue) && vol > 0 && vol <= 1) {
                if (cc == 0) {
                    Console.Write($"{duration}s ║");
                }
                if (dB < 0) {
                    Console.Write($" {f:n2}Hz{dB}dB");
                } else if (dB > 0) {
                    Console.Write($" {f:n2}Hz+{dB}dB");
                } else {
                    Console.Write($" {f:n2}Hz±0dB");
                }
                cc++;
            }
        }
        if (cc > 0) {
            Console.WriteLine();
        }
    }
}
