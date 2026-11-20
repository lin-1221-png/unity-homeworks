using NUnit.Framework;
using System;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;

public class bag : MonoBehaviour
{
    public class bags
    {
       public int id;
      public  string type;
        
   
    }
    public bags gun =new bags();
    public bags knife = new bags();
    public List<bags> littlebag = new List<bags> { };



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gun.id = 1;
        gun.type = "Wea";
        knife.id = 2;
        knife.type = "Wea";
        littlebag = new List<bags> {gun,knife };
        foreach (var item in littlebag)
        {
            Debug.Log($"{ item.type}");
        }






    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
