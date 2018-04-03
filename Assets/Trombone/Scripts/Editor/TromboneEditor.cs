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

        using (new EditorGUI.DisabledScope(!Application.isPlaying)) {

            showGlottis = EditorGUILayout.Foldout(showGlottis, "Glottis");
            if (showGlottis && trombone != null && trombone.glottis != null) {
                
                trombone.glottis.UIFrequency = EditorGUILayout.IntSlider("Frequency", (int)trombone.glottis.UIFrequency, 1, 1000);
                trombone.glottis.UITenseness = DoubleSlider("Tenseness", trombone.glottis.UITenseness, 0, 1);
                trombone.glottis.vibratoAmount = DoubleSlider("Vibrato", trombone.glottis.vibratoAmount, 0, 0.5);
                trombone.glottis.loudness = DoubleSlider("Loudness", trombone.glottis.loudness, 0, 1);

                trombone.glottis.autoWobble = EditorGUILayout.Toggle("Wobble", trombone.glottis.autoWobble);
                trombone.glottis.addPitchVariance = EditorGUILayout.Toggle("Pitch Variance", trombone.glottis.addPitchVariance);
                trombone.glottis.addTensenessVariance = EditorGUILayout.Toggle("Tenseness Variance", trombone.glottis.addTensenessVariance);

            }

            showTract = EditorGUILayout.Foldout(showTract, "Tract");
            if (showTract && trombone != null && trombone.tract != null && trombone.tractUI != null) {
                
                trombone.tract.movementSpeed = EditorGUILayout.IntSlider("Movement Speed", (int)trombone.tract.movementSpeed, 1, 30);
                trombone.tract.velumTarget = DoubleSlider("Velum Target", trombone.tract.velumTarget, 0.001, 2);

                trombone.tractUI.target = DoubleSlider("Target", trombone.tractUI.target, 0.001, 1);
                trombone.tractUI.index = EditorGUILayout.IntSlider("Tongue Tip Position", trombone.tractUI.index, 0, 43);
                trombone.tractUI.radius = EditorGUILayout.IntSlider("Tongue Tip Radius", trombone.tractUI.radius, 0, 5);

            }

        }
    }

    private static double DoubleSlider(string label, double value, double leftValue, double rightValue, params GUILayoutOption[] options) {
        return EditorGUILayout.Slider(label, (float)value, (float)leftValue, (float)rightValue, options);
    }

}
