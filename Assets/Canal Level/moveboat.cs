using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class moveboat : MonoBehaviour 
{
		void Update()
		{
		if (Input.GetKeyDown("w"))
			{
				Vector3 position = transform.position;
				position.x -= 10;
			}
		}
	}
