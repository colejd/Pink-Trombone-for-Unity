// Adapted from Pink Trombone. See Licenses/PinkTrombone_License.md for more information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TractUI {

    private Trombone trombone;
    readonly Tract tract;

    int originX = 340;
    int originY = 449;
    double scale = 60f;
    double tongueIndex = 12.9f;
    double tongueDiameter = 2.43f;
    double innerTongueControlRadius = 2.05f;
    double outerTongueControlRadius = 3.5f;
    double tongueTouch = 0;
    double angleScale = 0.64f;
    double angleOffset = -0.24f;
    double noseOffset = 0.8f;
    double gridOffset = 1.7f;

    /// Final openness of the mouth (closer to 0 is more closed)
    public double target = 0.1f;
    /// Index in the throat array to move to target
    public int index = 42;
    /// Number of throat segments to close around the index
    public int radius = 0;

    int tongueLowerIndexBound;
    int tongueUpperIndexBound;
    double tongueIndexCentre; 

    public TractUI(Trombone trombone, Tract tract) {
        this.trombone = trombone;
        this.tract = tract;

        SetRestDiameter();
        for (var i = 0; i < this.tract.n; i++) {
            this.tract.diameter[i] = this.tract.targetDiameter[i] = this.tract.restDiameter[i];
        }

        tongueLowerIndexBound = this.tract.bladeStart + 2;
        tongueUpperIndexBound = this.tract.tipStart - 3;
        tongueIndexCentre = 0.5 * (tongueLowerIndexBound + tongueUpperIndexBound);
    }

    double GetIndex(int x, int y) {
        var xx = x - originX; 
        var yy = y - originY;
        var angle = Math.Atan2(yy, xx);
        while (angle > 0) angle -= 2.0 * Math.PI;
        return (Math.PI + angle - angleOffset) * (tract.lipStart - 1) / (angleScale * Math.PI);
    }

    double GetDiameter(int x, int y) {
        var xx = x - originX; 
        var yy = y - originY;
        return (radius - Math.Sqrt(xx * xx + yy * yy)) / scale;
    }

    void SetRestDiameter() {

        for (var i = tract.bladeStart; i < tract.lipStart; i++) {
            double t = 1.1 * Math.PI * (tongueIndex - i) / (tract.tipStart - tract.bladeStart);
            double fixedTongueDiameter = 2.0 + (tongueDiameter - 2.0) / 1.5;
            double curve = (1.5 - fixedTongueDiameter + gridOffset) * Math.Cos(t);
            if (i == tract.bladeStart - 2 || i == tract.lipStart - 1) curve *= 0.8;
            if (i == tract.bladeStart || i == tract.lipStart - 2) curve *= 0.94;
            tract.restDiameter[i] = 1.5 - curve;
        }

    }

    /**
     * Sets the lips of the modeled tract to be closed by the specified amount.
     * @param {number} progress Percentage closed (number between 0 and 1)
     */
    public void SetLipsClosed(double progress) {

        SetRestDiameter();
        for (var i = 0; i < tract.n; i++) tract.targetDiameter[i] = tract.restDiameter[i];

        // Disable this behavior if the mouth is closed a certain amount
        //if (progress > 0.8 || progress < 0.1) return;

        for (var i = index - radius; i <= index + radius; i++) {
            if (i > tract.targetDiameter.Length || i < 0) continue;
            var interp = (double)Mathf.Lerp((float)tract.restDiameter[i], (float)target, (float)progress);
            tract.targetDiameter[i] = interp;
        }
    }


}
