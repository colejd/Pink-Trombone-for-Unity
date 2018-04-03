using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flapper : MonoBehaviour {

    public bool moveJaw = true;
    public float jawFlapSpeed = 20.0f;

    private Trombone trombone;

	// Use this for initialization
	void Start () {
        trombone = GetComponent<Trombone>();
	}
	
	// Update is called once per frame
	void Update () {
        if (moveJaw) {
            float percent = (Mathf.Sin(Time.time * this.jawFlapSpeed) + 1.0f) / 2.0f;
            trombone.tractUI.SetLipsClosed(1.0 - percent);
        }
	}
}
