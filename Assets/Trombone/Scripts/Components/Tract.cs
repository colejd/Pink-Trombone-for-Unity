// Adapted from Pink Trombone. See Licenses/PinkTrombone_License.md for more information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using MathExtensions;

public class Tract {

    private Trombone trombone;

    static double EPSILON = 0.0005;

    public double blockTime;

    public int n = 44;
    public int bladeStart = 10;
    public int tipStart = 32;
    public int lipStart = 39;
    double[] R; //component going right
    double[] L; //component going left
    double[] reflection;
    double[] newReflection;
    double[] junctionOutputR;
    double[] junctionOutputL;
    double[] maxAmplitude;
    public double[] diameter;
    public double[] restDiameter;
    public double[] targetDiameter;
    double[] newDiameter;
    double[] A;
    double glottalReflection = 0.75;
    double lipReflection = -0.85;
    int lastObstruction = -1;
    double fade = 1.0; //0.9999,
    public double movementSpeed = 15; //cm per second
    List<Transient> transients = new List<Transient>();
    public double lipOutput;
    public double noseOutput;
    public double velumTarget = 0.01;
    double reflectionLeft;
    double reflectionRight;
    double reflectionNose;


    int noseLength;
    int noseStart;
    double[] noseR;
    double[] noseL;
    double[] noseJunctionOutputR;
    double[] noseJunctionOutputL;
    double[] noseReflection;
    double[] noseDiameter;
    double[] noseA;
    double[] noseMaxAmplitude;
    double newReflectionLeft;
    double newReflectionRight;
    double newReflectionNose;

    readonly Glottis glottis;

    System.Random rand = new System.Random();

    public Tract(Trombone trombone, Glottis glottis) {
        this.trombone = trombone;
        this.glottis = glottis;

        blockTime = (double)AudioSettings.GetConfiguration().dspBufferSize / trombone.sampleRate;

        bladeStart = (int)Math.Floor(bladeStart * (double)n / 44.0);
        tipStart = (int)Math.Floor(tipStart * (double)n / 44.0);
        lipStart = (int)Math.Floor(lipStart * (double)n / 44.0);
        diameter = new double[n];
        restDiameter = new double[n];
        targetDiameter = new double[n];
        newDiameter = new double[n];
        for (int i = 0; i < n; i++) {
            double diam = 0.0;
            if (i < 7.0 * n / 44.0 - 0.5) diam = 0.6;
            else if (i < 12.0 * n / 44.0) diam = 1.1;
            else diam = 1.5;
            diameter[i] = restDiameter[i] = targetDiameter[i] = newDiameter[i] = diam;
        }
        R = new double[n];
        L = new double[n];
        reflection = new double[n + 1];
        newReflection = new double[n + 1];
        junctionOutputR = new double[n + 1];
        junctionOutputL = new double[n + 1];
        A = new double[n];
        maxAmplitude = new double[n];

        noseLength = (int)Math.Floor(28.0 * n / 44.0);
        noseStart = n - noseLength + 1;
        noseR = new double[noseLength];
        noseL = new double[noseLength];
        noseJunctionOutputR = new double[noseLength + 1];
        noseJunctionOutputL = new double[noseLength + 1];
        noseReflection = new double[noseLength + 1];
        noseDiameter = new double[noseLength];
        noseA = new double[noseLength];
        noseMaxAmplitude = new double[noseLength];
        for (int i = 0; i < noseLength; i++) {
            double diam;
            double d = 2.0 * ((double)i / noseLength);
            if (d < 1.0) diam = 0.4 + 1.6f * d;
            else diam = 0.5 + 1.5 * (2.0 - d);
            diam = Math.Min(diam, 1.9);
            noseDiameter[i] = diam;
        }
        newReflectionLeft = newReflectionRight = newReflectionNose = 0;
        CalculateReflections();
        CalculateNoseReflections();
        noseDiameter[0] = velumTarget;
    }

    void CalculateReflections() {
        for (int i = 0; i < n; i++) {
            A[i] = diameter[i] * diameter[i]; //ignoring PI etc.
        }
        for (var i = 1; i < n; i++) {
            reflection[i] = newReflection[i];
            if (Math.Abs(A[i]) < EPSILON) newReflection[i] = 0.999; //to prevent some bad behaviour if 0
            else newReflection[i] = (A[i - 1] - A[i]) / (A[i - 1] + A[i]);
        }

        //now at junction with nose

        reflectionLeft = newReflectionLeft;
        reflectionRight = newReflectionRight;
        reflectionNose = newReflectionNose;
        var sum = A[noseStart] + A[noseStart + 1] + noseA[0];
        newReflectionLeft = (2.0 * A[noseStart] - sum) / sum;
        newReflectionRight = (2.0 * A[noseStart + 1] - sum) / sum;
        newReflectionNose = (2.0 * noseA[0] - sum) / sum;
    }

    void CalculateNoseReflections() {
        for (int i = 0; i < noseLength; i++) {
            noseA[i] = noseDiameter[i] * noseDiameter[i];
        }
        for (int i = 1; i < noseLength; i++) {
            noseReflection[i] = (noseA[i - 1] - noseA[i]) / (noseA[i - 1] + noseA[i]);
        }
    }

    // TODO: Optimize here
    public void RunStep(double glottalOutput, double turbulenceNoise, double lambda) {
        var updateAmplitudes = (rand.NextDouble() < 0.1);

        //mouth
        ProcessTransients();
        AddTurbulenceNoise(turbulenceNoise);

        // Really slow part is below here somewhere

        //this.glottalReflection = -0.8 + 1.6 * Glottis.newTenseness;
        junctionOutputR[0] = L[0] * glottalReflection + glottalOutput;
        junctionOutputL[n] = R[n - 1] * lipReflection;

        for (int i = 1; i < n; i++) {
            var r = reflection[i] * (1 - lambda) + newReflection[i] * lambda;
            var w = r * (R[i - 1] + L[i]);
            junctionOutputR[i] = R[i - 1] - w;
            junctionOutputL[i] = L[i] + w;
        }

        //now at junction with nose
        int _i = noseStart;

        double _r;

        _r = newReflectionLeft * (1.0 - lambda) + reflectionLeft * lambda;
        junctionOutputL[_i] = _r * R[_i - 1] + (1.0 + _r) * (noseL[0] + L[_i]);

        _r = newReflectionRight * (1.0 - lambda) + reflectionRight * lambda;
        junctionOutputR[_i] = _r * L[_i] + (1.0 + _r) * (R[_i - 1] + noseL[0]);

        _r = newReflectionNose * (1.0 - lambda) + reflectionNose * lambda;
        noseJunctionOutputR[0] = _r * noseL[0] + (1.0 + _r) * (L[_i] + R[_i - 1]);

        for (var i = 0; i < n; i++) {
            R[i] = junctionOutputR[i] * 0.999;
            L[i] = junctionOutputL[i + 1] * 0.999;

            //this.R[i] = Math.clamp(this.junctionOutputR[i] * this.fade, -1, 1);
            //this.L[i] = Math.clamp(this.junctionOutputL[i+1] * this.fade, -1, 1);

            if (updateAmplitudes) {
                var amplitude = Math.Abs(R[i] + L[i]);
                if (amplitude > maxAmplitude[i]) maxAmplitude[i] = amplitude;
                else maxAmplitude[i] *= 0.999;
            }
        }

        lipOutput = R[n - 1];

        //nose
        noseJunctionOutputL[noseLength] = noseR[noseLength - 1] * lipReflection;

        for (var i = 1; i < noseLength; i++) {
            var w = noseReflection[i] * (noseR[i - 1] + noseL[i]);
            noseJunctionOutputR[i] = noseR[i - 1] - w;
            noseJunctionOutputL[i] = noseL[i] + w;
        }

        for (var i = 0; i < noseLength; i++) {
            noseR[i] = noseJunctionOutputR[i] * fade;
            noseL[i] = noseJunctionOutputL[i + 1] * fade;

            //this.noseR[i] = Math.clamp(this.noseJunctionOutputR[i] * this.fade, -1, 1);
            //this.noseL[i] = Math.clamp(this.noseJunctionOutputL[i+1] * this.fade, -1, 1);

            if (updateAmplitudes) {
                var amplitude = Math.Abs(noseR[i] + noseL[i]);
                if (amplitude > noseMaxAmplitude[i]) noseMaxAmplitude[i] = amplitude;
                else noseMaxAmplitude[i] *= 0.999;
            }
        }

        noseOutput = noseR[noseLength - 1];

    }

    public void FinishBlock() {
        ReshapeTract(blockTime);
        CalculateReflections();
    }

    void ReshapeTract(double deltaTime) {
        var amount = deltaTime * movementSpeed;
        var newLastObstruction = -1;
        for (var i = 0; i < n; i++) {
            var diam = diameter[i];
            var targetDiam = targetDiameter[i];
            if (diam <= 0) newLastObstruction = i;
            double slowReturn;
            if (i < noseStart) slowReturn = 0.6;
            else if (i >= tipStart) slowReturn = 1.0;
            else slowReturn = 0.6 + 0.4 * (i - noseStart) / (tipStart - noseStart);
            diameter[i] = Mathd.MoveTowards(diam, targetDiam, slowReturn * amount, 2.0 * amount);
        }
        if (lastObstruction > -1.0 && Math.Abs(newLastObstruction - -1.0) < EPSILON && noseA[0] < 0.05) {
            AddTransient(lastObstruction);
        }
        lastObstruction = newLastObstruction;

        amount = deltaTime * movementSpeed;
        noseDiameter[0] = Mathd.MoveTowards(noseDiameter[0], velumTarget, amount * 0.25, amount * 0.1);
        noseA[0] = noseDiameter[0] * noseDiameter[0];
    }

    void AddTransient(int position) {
        var trans = new Transient {
            position = position,
            timeAlive = 0,
            lifeTime = 0.2,
            strength = 0.3,
            exponent = 200
        };
        transients.Add(trans);
    }

    void ProcessTransients() {
        for (var i = 0; i < transients.Count; i++) {
            var trans = transients[i];
            double amplitude = trans.strength * Math.Pow(2, -trans.exponent * trans.timeAlive);
            R[trans.position] += amplitude / 2.0;
            L[trans.position] += amplitude / 2.0;
            trans.timeAlive += 1.0 / (trombone.sampleRate * 2.0);
        }
        for (var i = transients.Count - 1; i >= 0; i--) {
            var trans = transients[i];
            if (trans.timeAlive > trans.lifeTime) {
                transients.RemoveAt(i);
                //this.transients.splice(i, 1);
            }
        }
    }

    void AddTurbulenceNoise(double turbulenceNoise) {
        // Empty
    }

    void AddTurbulenceNoiseAtIndex(double turbulenceNoise, double index, double diam) {
        int i = (int)Math.Floor(index);
        var delta = index - i;
        turbulenceNoise *= glottis.GetNoiseModulator();
        var thinness0 = Mathd.Saturate(8.0 * (0.7 - diam));
        var openness = Mathd.Saturate(30.0 * (diam - 0.3));
        var noise0 = turbulenceNoise * (1 - delta) * thinness0 * openness;
        var noise1 = turbulenceNoise * delta * thinness0 * openness;
        R[i + 1] += noise0 / 2.0;
        L[i + 1] += noise0 / 2.0;
        R[i + 2] += noise1 / 2.0;
        L[i + 2] += noise1 / 2.0;
    }

}

struct Transient {
    public int position;
    public double timeAlive;
    public double lifeTime;
    public double strength;
    public double exponent;
}
