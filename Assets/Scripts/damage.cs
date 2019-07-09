using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class damage : MonoBehaviour
{


    float health = 5;
    
    // update is called once per frame
    void OnCollisionEnter(Collision collision)
    {
        // Check that enemy collides with bullet
        if (collision.collider.name.Contains("Bullet"))
        {
            // check the health if 1 that means enemy took the last hit and we should destroy him
            if (health == 1)
            {
                Vector3 position = transform.position;
                position.x -= 50;
                collision.gameObject.transform.position = position;
                
                //increment kills
                KillsController.SharedInstance.kills++;

                if (KillsController.SharedInstance.kills == 10)
                {
                    Application.LoadLevel("level2");
                }

                Debug.Log("kills " + KillsController.SharedInstance.kills);
                health--;
                
                Destroy(gameObject);
            }
            
            //dicrement health
            health--;
        }

    }
}