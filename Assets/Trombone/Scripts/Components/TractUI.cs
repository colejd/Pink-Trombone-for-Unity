// Adapted from Pink Trombone. See Licenses/PinkTrombone_License.md for more information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using MathExtensions;

public class TractUI {

    private Trombone trombone;
    readonly Tract tract;

    int originX = 340;
    int originY = 449;
    float scale = 60f;
    float tongueIndex = 12.9f;
    float tongueDiameter = 2.43f;
    float innerTongueControlRadius = 2.05f;
    float outerTongueControlRadius = 3.5f;
    //float tongueTouch = 0;
    public TractTouch tongueTouch;// = new TractTouch(10, 150);
    float angleScale = 0.64f;
    float angleOffset = -0.24f;
    float noseOffset = 0.8f;
    float gridOffset = 1.7f;

    /// Final openness of the mouth (closer to 0 is more closed)
    public float target = 0.1f;
    /// Index in the throat array to move to target
    public int index = 42;
    /// Number of throat segments to close around the index
    public int radius = 0;

    int tongueLowerIndexBound;
    int tongueUpperIndexBound;
    float tongueIndexCentre;

    public TractUI(Trombone trombone, Tract tract) {
        this.trombone = trombone;
        this.tract = tract;

        SetRestDiameter();
        for (var i = 0; i < this.tract.n; i++) {
            this.tract.diameter[i] = this.tract.targetDiameter[i] = this.tract.restDiameter[i];
        }

        tongueLowerIndexBound = this.tract.bladeStart + 2;
        tongueUpperIndexBound = this.tract.tipStart - 3;
        tongueIndexCentre = 0.5f * (tongueLowerIndexBound + tongueUpperIndexBound);
    }

    float GetIndex(int x, int y) {
        var xx = x - originX; 
        var yy = y - originY;
        var angle = Mathf.Atan2(yy, xx);
        while (angle > 0) angle -= 2.0f * Mathf.PI;
        return (Mathf.PI + angle - angleOffset) * (tract.lipStart - 1) / (angleScale * Mathf.PI);
    }

    float GetDiameter(int x, int y) {
        var xx = x - originX; 
        var yy = y - originY;
        return (radius - Mathf.Sqrt(xx * xx + yy * yy)) / scale;
    }

    void SetRestDiameter() {

        for (var i = tract.bladeStart; i < tract.lipStart; i++) {
            float t = 1.1f * Mathf.PI * (tongueIndex - i) / (tract.tipStart - tract.bladeStart);
            float fixedTongueDiameter = 2.0f + (tongueDiameter - 2.0f) / 1.5f;
            float curve = (1.5f - fixedTongueDiameter + gridOffset) * Mathf.Cos(t);
            if (i == tract.bladeStart - 2 || i == tract.lipStart - 1) curve *= 0.8f;
            if (i == tract.bladeStart || i == tract.lipStart - 2) curve *= 0.94f;
            tract.restDiameter[i] = 1.5f - curve;
        }

    }

    /**
     * Sets the lips of the modeled tract to be closed by the specified amount.
     * @param {number} progress Percentage closed (number between 0 and 1)
     */
    public void SetLipsClosed(float progress) {

        SetRestDiameter();
        for (var i = 0; i < tract.n; i++) tract.targetDiameter[i] = tract.restDiameter[i];

        // Disable this behavior if the mouth is closed a certain amount
        //if (progress > 0.8 || progress < 0.1) return;

        for (var i = index - radius; i <= index + radius; i++) {
            if (i >= tract.targetDiameter.Length || i >= tract.restDiameter.Length || i < 0) continue;
            float interp = Mathf.Lerp(tract.restDiameter[i], target, progress);
            tract.targetDiameter[i] = interp;
        }
    }

    public void FinishBlock() {
        
        if (this.tongueTouch == null)
        {        
            //for (var j=0; j<UI.touchesWithMouse.length; j++)  
            //{
            //    var touch = UI.touchesWithMouse[j];
            //    if (!touch.alive) continue;
            //    if (touch.fricative_intensity == 1) continue; //only new touches will pass this
            //    var x = touch.x;
            //    var y = touch.y;        
            //    var index = TractUI.getIndex(x,y);
            //    var diameter = TractUI.getDiameter(x,y);
            //    if (index >= this.tongueLowerIndexBound-4 && index<=this.tongueUpperIndexBound+4 
            //        && diameter >= this.innerTongueControlRadius-0.5 && diameter <= this.outerTongueControlRadius+0.5)
            //    {
            //        this.tongueTouch = touch;
            //    }
            //}    
        }
        
        if (this.tongueTouch != null)
        {
            var x = this.tongueTouch.x;
            var y = this.tongueTouch.y;        
            var index = GetIndex((int)x,(int)y);
            var diameter = GetDiameter((int)x,(int)y);
            var fromPoint = (this.outerTongueControlRadius-diameter)/(this.outerTongueControlRadius-this.innerTongueControlRadius);
            fromPoint = Mathf.Clamp01(fromPoint);
            fromPoint = Mathf.Pow(fromPoint, 0.58f) - 0.2f*(fromPoint*fromPoint-fromPoint); //horrible kludge to fit curve to straight line
            this.tongueDiameter = diameter.ClampBetween(this.innerTongueControlRadius, this.outerTongueControlRadius);
            //this.tongueIndex = Mathf.clamp(index, this.tongueLowerIndexBound, this.tongueUpperIndexBound);
            var output = fromPoint*0.5f*(this.tongueUpperIndexBound-this.tongueLowerIndexBound);
            this.tongueIndex = index.ClampBetween(this.tongueIndexCentre-output, this.tongueIndexCentre+output);
        }
        
        SetRestDiameter();   

        for (var i=0; i<tract.n; i++) tract.targetDiameter[i] = tract.restDiameter[i];        
        
        ////other constrictions and nose
        tract.velumTarget = 0.01f;

        var touch = tongueTouch;
        if (touch != null) {
            float x = touch.x;
            float y = touch.y;
            float index = GetIndex((int)x, (int)y);
            float diameter = GetDiameter((int)x, (int)y);
            if (index > tract.noseStart && diameter < -this.noseOffset) {
                tract.velumTarget = 0.4f;
            }
            if (diameter < -0.85f - this.noseOffset) return;
            diameter -= 0.3f;
            if (diameter < 0) diameter = 0;
            int width = 2;
            if (index < 25) width = 10;
            else if (index >= tract.tipStart) width = 5;
            else width = (int)(10 - 5 * ((float)index - 25) / ((float)tract.tipStart - 25));
            if (index >= 2 && index < tract.n && diameter < 3) {
                int intIndex = (int)Mathf.Round(index);
                for (int i = (int)-Mathf.Ceil((float)width) - 1; i < width + 1; i++) {
                    if (intIndex + i < 0 || intIndex + i >= tract.n) continue;
                    float relpos = (intIndex + i) - index;
                    relpos = Mathf.Abs(relpos) - 0.5f;
                    float shrink;
                    if (relpos <= 0) shrink = 0;
                    else if (relpos > width) shrink = 1;
                    else shrink = 0.5f * (1f - Mathf.Cos(Mathf.PI * relpos / width));
                    if ((float)diameter < (float)tract.targetDiameter[intIndex + i]) {
                        tract.targetDiameter[intIndex + i] = diameter + (tract.targetDiameter[intIndex + i] - diameter) * shrink;
                    }
                }
            }
        }
    }


}

public class TractTouch {
    public float x;
    public float y;

    public TractTouch(float x, float y) {
        this.x = x;
        this.y = y;
    }
}
