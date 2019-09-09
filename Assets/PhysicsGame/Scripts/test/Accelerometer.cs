using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Accelerometer : MonoBehaviour
{
    private void Start(){
        
    }

    private void Update(){
        float temp = Input.acceleration.x;
        Debug.Log(temp);
    }
}
