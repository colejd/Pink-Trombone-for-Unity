using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Threading;

using Filter;

[RequireComponent(typeof(AudioSource))]
public class Trombone : MonoBehaviour {

    /// <summary>
    /// Enables threading for audio synthesis. Disabling may improve audio quality, 
    /// but will increase CPU usage on Unity's audio thread.
    /// </summary>
    public static bool DO_THREADING = false;

    public Glottis glottis;
    public Tract tract;
    public TractUI tractUI;

    BiQuadFilter aspirateFilter;
    BiQuadFilter fricativeFilter;

    [HideInInspector]
    public int sampleRate;

    private readonly System.Random rand = new System.Random();

    // Threading members
    private Thread _thread;
    private bool _threadRunning;
    private bool doWork = false;
    private float[] audioData;
    private int numAudioChannels;

	// Use this for initialization
	void Start () {

        // Hook up trombone components
        sampleRate = AudioSettings.outputSampleRate;
        glottis = new Glottis(this);
        tract = new Tract(this, glottis);
        tractUI = new TractUI(this, tract);

        aspirateFilter = BiQuadFilter.BandPassFilterConstantSkirtGain(sampleRate, 500, 0.5f);
        fricativeFilter = BiQuadFilter.BandPassFilterConstantSkirtGain(sampleRate, 1000, 0.5f);

        // Dispatch thread if requested
        if (DO_THREADING) {
            _thread = new Thread(ThreadedWork);
            _thread.IsBackground = true;
            _thread.Start();
        }
	}

    void OnDisable() {
        if (DO_THREADING) {
            // If the thread is still running, we should shut it down,
            // otherwise it can prevent the game from exiting correctly.
            if (_threadRunning) {
                // This forces the while loop in the ThreadedWork function to abort.
                _threadRunning = false;

                // This waits until the thread exits,
                // ensuring any cleanup we do after this is safe. 
                _thread.Join();
            }

            // Thread is guaranteed no longer running. Do other cleanup tasks.
        }
    }
	
	// Update is called once per frame
	void Update () {

	}

    void ThreadedWork() {
        _threadRunning = true;
        bool workDone = false;

        // This pattern lets us interrupt the work at a safe point if neeeded.
        while (_threadRunning && !workDone) {
            if (doWork) {
                lock (audioData.SyncRoot) {
                    GenerateAudioData(audioData, numAudioChannels);
                }
                doWork = false;
            }

        }
        _threadRunning = false;
    }

    void OnAudioFilterRead(float[] data, int channels) {
        if (DO_THREADING) {
            if (audioData == null) {
                audioData = new float[data.Length];
                numAudioChannels = channels;
            }
            doWork = true;
            lock (audioData.SyncRoot) {
                audioData.CopyTo(data, 0);
            }
        }
        else {
            GenerateAudioData(data, channels);
        }
    }

    /// <summary>
    /// Runs the audio synthesis algorithms and writes the results into `data`.
    /// </summary>
    void GenerateAudioData(float[] data, int channels) {

        for (int i = 0; i < data.Length; i += channels) {

            float whiteNoise = (float)rand.NextDouble();
            float aspirateVal = aspirateFilter.Transform(whiteNoise);
            float fricativeVal = fricativeFilter.Transform(whiteNoise);

            double lambda1 = (double)i / data.Length;
            double lambda2 = (i + 0.5) / data.Length;

            double glottalOutput = glottis.RunStep(lambda1, aspirateVal);

            double vocalOutput = 0;
            //Tract runs at twice the sample rate 
            tract.RunStep(glottalOutput, fricativeVal, lambda1);
            vocalOutput += tract.lipOutput + tract.noseOutput;
            tract.RunStep(glottalOutput, fricativeVal, lambda2);
            vocalOutput += tract.lipOutput + tract.noseOutput;
            float output = (float)(vocalOutput * 0.125);

            // Channels are interleaved
            for (int channel = 0; channel < channels; channel++) {
                data[i + channel] = output;
            }
        }

        glottis.FinishBlock();
        tract.FinishBlock();

    }

}
