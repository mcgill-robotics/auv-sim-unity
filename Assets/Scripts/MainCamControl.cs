using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamControl : MonoBehaviour
{
    public Camera cam;
    public float moveSpeed;
    public float rotateSpeed;
    public float scrollSpeed;
    
    public GameObject auv;

    


    void Update()
    {

        if(Input.GetMouseButton(0)) {
            if (Input.GetAxis("Mouse X") > 0 || Input.GetAxis("Mouse X") < 0) {
                cam.transform.position += new Vector3(Input.GetAxisRaw("Mouse X") * Time.deltaTime * -moveSpeed,  Input.GetAxisRaw("Mouse Y") * Time.deltaTime * -moveSpeed, 0.0f);
            }
        }

        if (Input.GetMouseButton(1)) {
            transform.RotateAround(cam.transform.position,  new Vector3(Input.GetAxisRaw("Mouse Y"),  Input.GetAxisRaw("Mouse X"), 0.0f), rotateSpeed * Time.deltaTime);
            
        }
        Debug.Log(Input.GetAxis("Mouse ScrollWheel"));
        if (Input.GetAxis("Mouse ScrollWheel") > 0 || Input.GetAxis("Mouse ScrollWheel") < 0 ) {
            cam.transform.position += (cam.transform.forward * scrollSpeed * Input.GetAxis("Mouse ScrollWheel"));
         } 
    }


}
