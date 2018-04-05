// Adapted from Pink Trombone. See Licenses/PinkTrombone_License.md for more information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Noise;

public class Glottis {

    private Trombone trombone;

    float timeInWaveform;
    float oldFrequency = 140;
    float newFrequency = 140;
    public float UIFrequency = 140;
    float smoothFrequency = 140;
    float oldTenseness = 0.6f;
    float newTenseness = 0.6f;
    public float UITenseness = 0.6f;
    float totalTime;
    public float vibratoAmount = 0.005f;
    float vibratoFrequency = 6;
    float intensity = 1;
    public float loudness = 1;
    //float baseNote = 87.3071f; //F

    /// Allow pitch to wobble over time
    public bool addPitchVariance = true;
    /// Allow tenseness to wobble over time
    public bool addTensenessVariance = true;

    public bool autoWobble = true;

    bool isTouched = true;

    private OpenSimplexNoise noise = new OpenSimplexNoise();

    private float alpha;
    private float E0;
    private float epsilon;
    private float shift;
    private float Delta;
    private float Te;
    private float omega;

    private float waveformLength;

    public Glottis(Trombone trombone) {
        this.trombone = trombone;
        SetupWaveform(0);
    }

    void SetupWaveform(float lambda) {
        float frequency = oldFrequency * (1.0f - lambda) + newFrequency * lambda;
        float tenseness = oldTenseness * (1.0f - lambda) + newTenseness * lambda;
        waveformLength = 1.0f / (frequency * 1f);

        float Rd = 3.0f * (1.0f - tenseness);
        if (Rd < 0.5f) Rd = 0.5f;
        if (Rd > 2.7f) Rd = 2.7f;
        // normalized to time = 1, Ee = 1
        float Ra = -0.01f + 0.048f * Rd;
        float Rk = 0.224f + 0.118f * Rd;
        float Rg = (Rk / 4.0f) * (0.5f + 1.2f * Rk) / (0.11f * Rd - Ra * (0.5f + 1.2f * Rk));

        float Ta = Ra;
        float Tp = 1.0f / (2.0f * Rg);
        Te = Tp + Tp * Rk;

        epsilon = 1.0f / Ta;
        shift = Mathf.Exp(-epsilon * (1.0f - Te));
        Delta = 1.0f - shift; //divide by this to scale RHS

        float RHSIntegral = (1.0f / epsilon) * (shift - 1.0f) + (1.0f - Te) * shift;
        RHSIntegral = RHSIntegral / Delta;

        float totalLowerIntegral = -(Te - Tp) / 2.0f + RHSIntegral;
        float totalUpperIntegral = -totalLowerIntegral;

        omega = Mathf.PI / Tp;
        float s = Mathf.Sin(omega * Te);
        float y = -Mathf.PI * s * totalUpperIntegral / (Tp * 2.0f);
        float z = Mathf.Log(y);
        alpha = z / (Tp / 2.0f - Te);
        E0 = -1.0f / (s * Mathf.Exp(alpha * Te));

    }

    public float RunStep(float lambda, float noiseSource) {
        float timeStep = 1.0f / ((float)trombone.sampleRate / trombone.downsamplingFactor);
        timeInWaveform += timeStep;
        totalTime += timeStep;
        if (timeInWaveform > waveformLength) {
            timeInWaveform -= waveformLength;
            SetupWaveform(lambda);
        }
        float newOutput = NormalizedLFWaveform(timeInWaveform / waveformLength);
        var aspiration = intensity * (1.0f - Mathf.Sqrt(UITenseness)) * GetNoiseModulator() * noiseSource;
        aspiration *= 0.2f + 0.02f * GetNoise(totalTime * 1.99f);
        newOutput += aspiration;
        return newOutput;
    }

    public float GetNoiseModulator() {
        float voiced = 0.1f + 0.2f * Mathf.Max(0.0f, Mathf.Sin(Mathf.PI * 2.0f * timeInWaveform / waveformLength));
        //return 0.3;
        return UITenseness * intensity * voiced + (1.0f - UITenseness * intensity) * 0.3f;
    }

    public void FinishBlock() {
        float vibrato = 0.0f;
        if (addPitchVariance) {
            // Add small imperfections to the vocal output
            vibrato += vibratoAmount * Mathf.Sin(2.0f * Mathf.PI * totalTime * vibratoFrequency);
            vibrato += 0.02f * GetNoise(totalTime * 4.07f);
            vibrato += 0.04f * GetNoise(totalTime * 2.15f);
        }

        if (autoWobble) {
            vibrato += 0.2f * GetNoise(totalTime * 0.98f);
            vibrato += 0.4f * GetNoise(totalTime * 0.5f);
        }

        if (UIFrequency > smoothFrequency)
            smoothFrequency = Mathf.Min(smoothFrequency * 1.1f, UIFrequency);
        if (UIFrequency < smoothFrequency)
            smoothFrequency = Mathf.Max(smoothFrequency / 1.1f, UIFrequency);
        oldFrequency = newFrequency;
        newFrequency = smoothFrequency * (1.0f + vibrato);
        oldTenseness = newTenseness;

        if (addTensenessVariance)
            newTenseness = UITenseness + 0.1f * GetNoise(totalTime * 0.46f) + 0.05f * GetNoise(totalTime * 0.36f);
        else
            newTenseness = UITenseness;

        if (!isTouched) newTenseness += (3.0f - UITenseness) * (1.0f - intensity);

        if (isTouched)
            intensity += 0.13f;
        intensity = Clamp(intensity, 0.0f, 1.0f);
    }

    float NormalizedLFWaveform(float t) {
        float output;
        if (t > Te) {
            output = (-Mathf.Exp(-epsilon * (t - Te)) + shift) / Delta;
        }
        else {
            output = E0 * Mathf.Exp(alpha * t) * Mathf.Sin(omega * t);
        }

        return output * intensity * loudness;
    }

    private float GetNoise(float t) {
        float x = t * 1.2f;
        float y = -t * 0.7f;
        return (float)noise.Evaluate(x * 2, y * 2);
    }

    public static float Clamp(float value, float min, float max) {
        return (value < min) ? min : (value > max) ? max : value;
    }

}
