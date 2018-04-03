// Adapted from Pink Trombone. See Licenses/PinkTrombone_License.md for more information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Noise;

public class Glottis {

    private Trombone trombone;

    double timeInWaveform;
    double oldFrequency = 140;
    double newFrequency = 140;
    public double UIFrequency = 140;
    double smoothFrequency = 140;
    double oldTenseness = 0.6f;
    double newTenseness = 0.6f;
    public double UITenseness = 0.6f;
    double totalTime;
    public double vibratoAmount = 0.005f;
    double vibratoFrequency = 6;
    double intensity = 1;
    public double loudness = 1;
    //double baseNote = 87.3071f; //F

    /// Allow pitch to wobble over time
    public bool addPitchVariance = true;
    /// Allow tenseness to wobble over time
    public bool addTensenessVariance = true;

    public bool autoWobble = true;

    bool isTouched = true;

    private OpenSimplexNoise noise = new OpenSimplexNoise();

    private double alpha;
    private double E0;
    private double epsilon;
    private double shift;
    private double Delta;
    private double Te;
    private double omega;

    private double waveformLength;

    public Glottis(Trombone trombone) {
        this.trombone = trombone;
        SetupWaveform(0);
    }

    void SetupWaveform(double lambda) {
        double frequency = oldFrequency * (1.0 - lambda) + newFrequency * lambda;
        double tenseness = oldTenseness * (1.0 - lambda) + newTenseness * lambda;
        waveformLength = 1.0 / frequency;

        double Rd = 3.0 * (1.0 - tenseness);
        if (Rd < 0.5) Rd = 0.5;
        if (Rd > 2.7) Rd = 2.7;
        // normalized to time = 1, Ee = 1
        double Ra = -0.01 + 0.048 * Rd;
        double Rk = 0.224 + 0.118 * Rd;
        double Rg = (Rk / 4.0) * (0.5 + 1.2 * Rk) / (0.11 * Rd - Ra * (0.5 + 1.2 * Rk));

        double Ta = Ra;
        double Tp = 1.0 / (2.0 * Rg);
        Te = Tp + Tp * Rk;

        epsilon = 1.0 / Ta;
        shift = Math.Exp(-epsilon * (1.0 - Te));
        Delta = 1.0 - shift; //divide by this to scale RHS

        double RHSIntegral = (1.0 / epsilon) * (shift - 1.0) + (1.0 - Te) * shift;
        RHSIntegral = RHSIntegral / Delta;

        double totalLowerIntegral = -(Te - Tp) / 2.0 + RHSIntegral;
        double totalUpperIntegral = -totalLowerIntegral;

        omega = Math.PI / Tp;
        double s = Math.Sin(omega * Te);
        double y = -Math.PI * s * totalUpperIntegral / (Tp * 2.0);
        double z = Math.Log(y);
        alpha = z / (Tp / 2.0 - Te);
        E0 = -1.0 / (s * Math.Exp(alpha * Te));

    }

    public double RunStep(double lambda, double noiseSource) {
        double timeStep = 1.0 / trombone.sampleRate;
        timeInWaveform += timeStep;
        totalTime += timeStep;
        if (timeInWaveform > waveformLength) {
            timeInWaveform -= waveformLength;
            SetupWaveform(lambda);
        }
        double newOutput = NormalizedLFWaveform(timeInWaveform / waveformLength);
        var aspiration = intensity * (1.0 - Math.Sqrt(UITenseness)) * GetNoiseModulator() * noiseSource;
        aspiration *= 0.2 + 0.02 * GetNoise(totalTime * 1.99);
        newOutput += aspiration;
        return newOutput;
    }

    public double GetNoiseModulator() {
        double voiced = 0.1 + 0.2 * Math.Max(0.0, Math.Sin(Math.PI * 2.0 * timeInWaveform / waveformLength));
        //return 0.3;
        return UITenseness * intensity * voiced + (1.0 - UITenseness * intensity) * 0.3;
    }

    public void FinishBlock() {
        double vibrato = 0.0;
        if (addPitchVariance) {
            // Add small imperfections to the vocal output
            vibrato += vibratoAmount * Math.Sin(2.0 * Math.PI * totalTime * vibratoFrequency);
            vibrato += 0.02 * GetNoise(totalTime * 4.07);
            vibrato += 0.04 * GetNoise(totalTime * 2.15);
        }

        if (autoWobble) {
            vibrato += 0.2 * GetNoise(totalTime * 0.98);
            vibrato += 0.4 * GetNoise(totalTime * 0.5);
        }

        if (UIFrequency > smoothFrequency)
            smoothFrequency = Math.Min(smoothFrequency * 1.1, UIFrequency);
        if (UIFrequency < smoothFrequency)
            smoothFrequency = Math.Max(smoothFrequency / 1.1, UIFrequency);
        oldFrequency = newFrequency;
        newFrequency = smoothFrequency * (1.0 + vibrato);
        oldTenseness = newTenseness;

        if (addTensenessVariance)
            newTenseness = UITenseness + 0.1 * GetNoise(totalTime * 0.46) + 0.05 * GetNoise(totalTime * 0.36);
        else
            newTenseness = UITenseness;

        if (!isTouched) newTenseness += (3.0 - UITenseness) * (1.0 - intensity);

        if (isTouched)
            intensity += 0.13;
        intensity = Clamp(intensity, 0.0, 1.0);
    }

    double NormalizedLFWaveform(double t) {
        double output;
        if (t > Te) {
            output = (-Math.Exp(-epsilon * (t - Te)) + shift) / Delta;
        }
        else {
            output = E0 * Math.Exp(alpha * t) * Math.Sin(omega * t);
        }

        return output * intensity * loudness;
    }

    private double GetNoise(double t) {
        double x = t * 1.2;
        double y = -t * 0.7;
        return noise.Evaluate(x * 2, y * 2);
    }

    public static double Clamp(double value, double min, double max) {
        return (value < min) ? min : (value > max) ? max : value;
    }

}
