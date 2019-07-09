using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOver : MonoBehaviour {

	// Use this for initialization
	void OnCollisionEnter (Collision other) {
        Debug.Log("Collided");        
        if (other.collider.name.Contains("hitbox")) {
			Application.LoadLevel ("gameOver");
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}


}
