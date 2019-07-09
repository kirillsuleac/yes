using UnityEngine;
using System;
public class Bullet : MonoBehaviour {

	public float enemyDamage = 25f;

    
    public String Name { get; set; }


    void OnCollisionEnter(Collision other)
    {
        //Debug.Log("OBJECT: ", gameObject);
        Destroy(gameObject);
        //Destroy(other.gameObject);

    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    Console.WriteLine("BLA: ", gameObject);
    //    Debug.Log("OBJECT: ", gameObject);
    //    Destroy(gameObject);
    //}

    // Use this for initialization
    void Start () {
        Name = "Bullet";
        //Debug.Log("Creating bullet");
    }

    // Update is called once per frame
    void Update() {
        //Debug.Log("KIRILL");
    }   


	//void FixedUpdate ()
	//{
	//	RaycastHit hit;
	//	Ray ray = new Ray (transform.position, transform.forward);
	//	if (Physics.Raycast (ray, out hit, 100f)) {
	//		if (hit.transform.tag == "AIThirdPersonController") {
	//			hit.transform.GetComponent<damage>().RemoveHealth(enemyDamage);
	//		} 
	//	}
	//}
}


