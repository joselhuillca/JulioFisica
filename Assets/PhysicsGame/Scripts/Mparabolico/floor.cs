using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class floor : MonoBehaviour
{
    public GameObject panelEndGame;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Awake ()
    {
            panelEndGame.gameObject.SetActive (false);
    }

    void OnTriggerEnter(Collider other)
    {
        panelEndGame.gameObject.SetActive (true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
