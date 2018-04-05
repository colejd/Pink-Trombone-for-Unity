// Adapted from Pink Trombone. See Licenses/PinkTrombone_License.md for more information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using System;

using MathExtensions;

public class Tract {

    private Trombone trombone;

    static float EPSILON = 0.0005f;

    public float blockTime;

    public int n = 44;
    public int bladeStart = 10;
    public int tipStart = 32;
    public int lipStart = 39;
    float[] R; //component going right
    float[] L; //component going left
    float[] reflection;
    float[] newReflection;
    float[] junctionOutputR;
    float[] junctionOutputL;
    public float[] diameter;
    public float[] restDiameter;
    public float[] targetDiameter;
    float[] A;
    float glottalReflection = 0.75f;
    float lipReflection = -0.85f;
    int lastObstruction = -1;
    float fade = 1.0f; //0.9999,
    public float movementSpeed = 15; //cm per second
    List<Transient> transients = new List<Transient>();
    public float lipOutput;
    public float noseOutput;
    public float velumTarget = 0.01f;
    float reflectionLeft;
    float reflectionRight;
    float reflectionNose;

    public int noseLength;
    public int noseStart;
    float[] noseR;
    float[] noseL;
    float[] noseJunctionOutputR;
    float[] noseJunctionOutputL;
    float[] noseReflection;
    float[] noseDiameter;
    float[] noseA;
    float newReflectionLeft;
    float newReflectionRight;
    float newReflectionNose;

    readonly Glottis glottis;

    public Tract(Trombone trombone, Glottis glottis) {
        this.trombone = trombone;
        this.glottis = glottis;

        blockTime = (float)trombone.bufferSize / trombone.sampleRate;

        bladeStart = (int)Mathf.Floor(bladeStart * (float)n / 44.0f);
        tipStart = (int)Mathf.Floor(tipStart * (float)n / 44.0f);
        lipStart = (int)Mathf.Floor(lipStart * (float)n / 44.0f);
        diameter = new float[n];
        restDiameter = new float[n];
        targetDiameter = new float[n];
        for (int i = 0; i < n; i++) {
            float diam = 0.0f;
            if (i < 7.0f * n / 44.0f - 0.5f) diam = 0.6f;
            else if (i < 12.0f * n / 44.0f) diam = 1.1f;
            else diam = 1.5f;
            diameter[i] = restDiameter[i] = targetDiameter[i] = diam;
        }
        R = new float[n];
        L = new float[n];
        reflection = new float[n + 1];
        newReflection = new float[n + 1];
        junctionOutputR = new float[n + 1];
        junctionOutputL = new float[n + 1];
        A = new float[n];

        noseLength = (int)Mathf.Floor(28.0f * n / 44.0f);
        noseStart = n - noseLength + 1;
        noseR = new float[noseLength];
        noseL = new float[noseLength];
        noseJunctionOutputR = new float[noseLength + 1];
        noseJunctionOutputL = new float[noseLength + 1];
        noseReflection = new float[noseLength + 1];
        noseDiameter = new float[noseLength];
        noseA = new float[noseLength];
        for (int i = 0; i < noseLength; i++) {
            float diam;
            float d = 2.0f * ((float)i / noseLength);
            if (d < 1.0f) diam = 0.4f + 1.6f * d;
            else diam = 0.5f + 1.5f * (2.0f - d);
            diam = Mathf.Min(diam, 1.9f);
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
            if (Mathf.Abs(A[i]) < EPSILON) newReflection[i] = 0.999f; //to prevent some bad behaviour if 0
            else newReflection[i] = (A[i - 1] - A[i]) / (A[i - 1] + A[i]);
        }

        //now at junction with nose

        reflectionLeft = newReflectionLeft;
        reflectionRight = newReflectionRight;
        reflectionNose = newReflectionNose;
        float sum = A[noseStart] + A[noseStart + 1] + noseA[0];
        newReflectionLeft = (2.0f * A[noseStart] - sum) / sum;
        newReflectionRight = (2.0f * A[noseStart + 1] - sum) / sum;
        newReflectionNose = (2.0f * noseA[0] - sum) / sum;
    }

    void CalculateNoseReflections() {
        for (int i = 0; i < noseLength; i++) {
            noseA[i] = noseDiameter[i] * noseDiameter[i];
        }
        for (int i = 1; i < noseLength; i++) {
            noseReflection[i] = (noseA[i - 1] - noseA[i]) / (noseA[i - 1] + noseA[i]);
        }
    }

    public void RunStep(float glottalOutput, float turbulenceNoise, float lambda) {

        //mouth
        ProcessTransients();
        AddTurbulenceNoise(turbulenceNoise);

        //this.glottalReflection = -0.8 + 1.6 * glottis.newTenseness;
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

        float _r;
        float oneMinusLambda = 1.0f - lambda;

        _r = newReflectionLeft * oneMinusLambda + reflectionLeft * lambda;
        junctionOutputL[_i] = _r * R[_i - 1] + (1.0f + _r) * (noseL[0] + L[_i]);

        _r = newReflectionRight * oneMinusLambda + reflectionRight * lambda;
        junctionOutputR[_i] = _r * L[_i] + (1.0f + _r) * (R[_i - 1] + noseL[0]);

        _r = newReflectionNose * oneMinusLambda + reflectionNose * lambda;
        noseJunctionOutputR[0] = _r * noseL[0] + (1.0f + _r) * (L[_i] + R[_i - 1]);

        for (int i = 0; i < n; i++) {
            R[i] = junctionOutputR[i] * 0.999f;
            L[i] = junctionOutputL[i + 1] * 0.999f;
        }

        lipOutput = R[n - 1];

        //nose
        noseJunctionOutputL[noseLength] = noseR[noseLength - 1] * lipReflection;

        for (int i = 1; i < noseLength; i++) {
            var w = noseReflection[i] * (noseR[i - 1] + noseL[i]);
            noseJunctionOutputR[i] = noseR[i - 1] - w;
            noseJunctionOutputL[i] = noseL[i] + w;
        }

        for (int i = 0; i < noseLength; i++) {
            noseR[i] = noseJunctionOutputR[i] * fade;
            noseL[i] = noseJunctionOutputL[i + 1] * fade;
        }

        noseOutput = noseR[noseLength - 1];

    }

    public void FinishBlock() {
        ReshapeTract(blockTime);
        CalculateReflections();
    }

    void ReshapeTract(float deltaTime) {
        float amount = deltaTime * movementSpeed;
        int newLastObstruction = -1;
        for (int i = 0; i < n; i++) {
            float diam = diameter[i];
            float targetDiam = targetDiameter[i];
            if (diam <= 0) newLastObstruction = i;
            float slowReturn;
            if (i < noseStart) slowReturn = 0.6f;
            else if (i >= tipStart) slowReturn = 1.0f;
            else slowReturn = 0.6f + 0.4f * (i - noseStart) / (tipStart - noseStart);
            diameter[i] = MathfExtensions.MoveTowards(diam, targetDiam, slowReturn * amount, 2.0f * amount);
        }
        if (lastObstruction > -1.0 && Mathf.Abs(newLastObstruction - -1.0f) < EPSILON && noseA[0] < 0.05f) {
            AddTransient(lastObstruction);
        }
        lastObstruction = newLastObstruction;

        amount = deltaTime * movementSpeed;
        noseDiameter[0] = MathfExtensions.MoveTowards(noseDiameter[0], velumTarget, amount * 0.25f, amount * 0.1f);
        noseA[0] = noseDiameter[0] * noseDiameter[0];
    }

    void AddTransient(int position) {
        transients.Add(new Transient {
            position = position,
            timeAlive = 0f,
            lifeTime = 0.2f,
            strength = 0.3f,
            exponent = 200f
        });
    }

    void ProcessTransients() {
        int transientCount = transients.Count;
        for (var i = 0; i < transientCount; i++) {
            Transient trans = transients[i];
            float amplitude = trans.strength * Mathf.Pow(2, -trans.exponent * trans.timeAlive);
            R[trans.position] += amplitude / 2.0f;
            L[trans.position] += amplitude / 2.0f;
            trans.timeAlive += 1.0f / (trombone.sampleRate * 2.0f);
        }
        for (var i = transientCount - 1; i >= 0; i--) {
            Transient trans = transients[i];
            if (trans.timeAlive > trans.lifeTime) {
                transients.RemoveAt(i);
            }
        }
    }

    void AddTurbulenceNoise(float turbulenceNoise) {
        // TODO: Reimplement at some point. Supposed to add turbulence noise for
        // the obstruction created by touching/clicking the simulation.
    }

    void AddTurbulenceNoiseAtIndex(float turbulenceNoise, float index, float diam) {
        int i = (int)Mathf.Floor(index);
        float delta = index - i;
        turbulenceNoise *= glottis.GetNoiseModulator();
        float thinness0 = Mathf.Clamp01(8.0f * (0.7f - diam));
        float openness = Mathf.Clamp01(30.0f * (diam - 0.3f));
        float noise0 = turbulenceNoise * (1.0f - delta) * thinness0 * openness;
        float noise1 = turbulenceNoise * delta * thinness0 * openness;
        R[i + 1] += noise0 / 2.0f;
        L[i + 1] += noise0 / 2.0f;
        R[i + 2] += noise1 / 2.0f;
        L[i + 2] += noise1 / 2.0f;
    }

}

struct Transient {
    public int position;
    public float timeAlive;
    public float lifeTime;
    public float strength;
    public float exponent;
}
