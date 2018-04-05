using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Trombone))]
public class TromboneEditor : Editor {

    public Trombone trombone;

    private bool showGlottis = false;
    private bool showTract = false;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnEnable() {
        trombone = target as Trombone;
        showGlottis = showTract = Application.isPlaying;
    }

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        EditorGUI.BeginChangeCheck();
        trombone.downsamplingLevel = EditorGUILayout.IntSlider("Downsampling Level", trombone.downsamplingLevel, 0, 8);
        if (EditorGUI.EndChangeCheck()) {
            trombone.SetDownsamplingLevel(trombone.downsamplingLevel);
        }

        using (new EditorGUI.DisabledScope(!Application.isPlaying)) {

            showGlottis = EditorGUILayout.Foldout(showGlottis, "Glottis");
            if (showGlottis && trombone != null && trombone.glottis != null) {
                
                trombone.glottis.UIFrequency = EditorGUILayout.IntSlider("Frequency", (int)trombone.glottis.UIFrequency, 1, 1000);
                trombone.glottis.UITenseness = EditorGUILayout.Slider("Tenseness", trombone.glottis.UITenseness, 0, 1);
                trombone.glottis.vibratoAmount = EditorGUILayout.Slider("Vibrato", trombone.glottis.vibratoAmount, 0, 0.5f);
                trombone.glottis.loudness = EditorGUILayout.Slider("Loudness", trombone.glottis.loudness, 0, 1);

                trombone.glottis.autoWobble = EditorGUILayout.Toggle("Wobble", trombone.glottis.autoWobble);
                trombone.glottis.addPitchVariance = EditorGUILayout.Toggle("Pitch Variance", trombone.glottis.addPitchVariance);
                trombone.glottis.addTensenessVariance = EditorGUILayout.Toggle("Tenseness Variance", trombone.glottis.addTensenessVariance);

            }

            showTract = EditorGUILayout.Foldout(showTract, "Tract");
            if (showTract && trombone != null && trombone.tract != null && trombone.tractUI != null) {
                
                trombone.tract.movementSpeed = EditorGUILayout.IntSlider("Movement Speed", (int)trombone.tract.movementSpeed, 1, 30);
                trombone.tract.velumTarget = EditorGUILayout.Slider("Velum Target", trombone.tract.velumTarget, 0f, 2f);

                trombone.tractUI.target = EditorGUILayout.Slider("Target", trombone.tractUI.target, 0f, 1f);
                trombone.tractUI.index = EditorGUILayout.IntSlider("Tongue Tip Position", trombone.tractUI.index, 0, 43);
                trombone.tractUI.radius = EditorGUILayout.IntSlider("Tongue Tip Radius", trombone.tractUI.radius, 0, 5);

                if (trombone.tractUI.tongueTouch != null) {
                    trombone.tractUI.tongueTouch.x = EditorGUILayout.Slider("x", trombone.tractUI.tongueTouch.x, 0, 1000);
                    trombone.tractUI.tongueTouch.y = EditorGUILayout.Slider("y", trombone.tractUI.tongueTouch.y, 0, 1000);
                }

            }

        }
    }

}
