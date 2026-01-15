using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TorpedoSpawnerScript : MonoBehaviour
{
    public GameObject torpedo;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            SpawnTorpedo();
        }
    }

    void SpawnTorpedo()
    {
        Instantiate(torpedo, transform.position, transform.rotation);// need to rotate since it's facing up lol
    }
}
