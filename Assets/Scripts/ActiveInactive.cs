using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveInactive : MonoBehaviour {

	public GameObject[] objectA;
	public GameObject[] objectD;

	public void onClick(){
		for (int i = 0; i < objectA.Length; i++) {
			objectA[i].SetActive (true);
		}
		for (int i = 0; i < objectD.Length; i++) {
			objectD[i].SetActive (false);
		}
	}
}
