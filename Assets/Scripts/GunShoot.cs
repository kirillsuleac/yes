using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class GunShoot : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}

    public Rigidbody projectile;
    public float speed = 20;
    private int count = 0;
	
	// Update is called once per frame
	void Update () {
		if (Input.GetButtonDown ("Fire1")) {
            count = count + 1;
            // Debug.Log("COUNT: " + count);
            Rigidbody instantiatedProjectile = Instantiate (projectile, transform.position, transform.rotation) as Rigidbody;
            //Debug.Log("Bullet spawn: ", instantiatedProjectile);
			instantiatedProjectile.velocity = transform.TransformDirection (new Vector3 (0, 0, speed));
            //Physics.IgnoreCollision(instantiatedProjectile.GetComponent<Collider>(), transform.root.GetComponent<Collider>());
		}
	}
}
