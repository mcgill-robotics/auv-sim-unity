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
		    cam.transform.Rotate(new Vector3(Input.GetAxis("Mouse Y")*7 , -Input.GetAxis("Mouse X") * 2, 0));
        }
        cam.transform.Translate(new Vector3(xAxisValue/5, yAxisValue*2, zAxisValue/5));

        


    }


}
