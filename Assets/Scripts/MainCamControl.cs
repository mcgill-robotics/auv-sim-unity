using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamControl : MonoBehaviour
{
    public GameObject cam;

    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float xAxisValue = Input.GetAxis("Horizontal");
        float zAxisValue = Input.GetAxis("Vertical");
        float yAxisValue = Input.GetAxis("Mouse ScrollWheel");
        if(Input.GetMouseButton(0)) {
		    cam.transform.Rotate(new Vector3(Input.GetAxis("Mouse Y") , -Input.GetAxis("Mouse X"), 0));
        }
        cam.transform.Translate(new Vector3(xAxisValue/15, yAxisValue, zAxisValue/15));

        


    }


}
