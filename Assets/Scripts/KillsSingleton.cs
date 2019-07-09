using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillsController {

    private static KillsController instance = null;

    public static KillsController SharedInstance
    {
        get
        {
            if (instance == null)
            {
                instance = new KillsController();
            }
            return instance;
        }
    }
    public float kills;
}
