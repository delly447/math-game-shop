using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PepperButtons : MonoBehaviour
{
    public PepperGameManager ppp;
    void Start()
    {
        ppp = GameObject.Find("PepperGameManager").GetComponent<PepperGameManager>();
    }

    
    void OnMouseDown()
    {
        if (gameObject.name == "correct")
        {
             ppp.Check();
        }
        else if(gameObject.name=="clear")
        { 
            ppp.clear();ppp.Hint(); 
        }
    }

    
}
