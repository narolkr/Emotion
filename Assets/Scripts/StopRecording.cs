using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StopRecording : MonoBehaviour {

	public GameObject emo;
	Emotion emotion;

	void Start(){
		emotion = emo.GetComponent<Emotion> ();

	}


	public void onClick(){
		emotion.Active = false;
	}


}
