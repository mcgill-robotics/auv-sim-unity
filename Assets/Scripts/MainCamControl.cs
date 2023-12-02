using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamControl : MonoBehaviour
{
    public Camera cam;
    public float moveSpeed;
    public float XRotateSpeed;
    public float YRotateSpeed;
    public float scrollSpeed;
    
    public GameObject auv;

    


    void Update()
    {

        if(Input.GetMouseButton(0)) {
            if (Input.GetAxis("Mouse X") > 0 || Input.GetAxis("Mouse X") < 0) {
                cam.transform.Translate(new Vector3(Input.GetAxisRaw("Mouse X") * Time.deltaTime * -moveSpeed,  Input.GetAxisRaw("Mouse Y") * Time.deltaTime * -moveSpeed, 0.0f));
            }
        }

        if (Input.GetMouseButton(2)) {
            transform.Rotate(Vector3.up * Input.GetAxisRaw("Mouse X") * XRotateSpeed * Time.deltaTime);
            transform.Rotate(Vector3.right * Input.GetAxisRaw("Mouse Y") * YRotateSpeed * Time.deltaTime);
            Vector3 currentCamRotation = cam.transform.rotation.eulerAngles;
            cam.transform.rotation = Quaternion.Euler(currentCamRotation.x, currentCamRotation.y, 0); // always flatten camera
        }

        if (Input.GetAxis("Mouse ScrollWheel") > 0 || Input.GetAxis("Mouse ScrollWheel") < 0 ) {
            cam.transform.position += (cam.transform.forward * scrollSpeed * Input.GetAxis("Mouse ScrollWheel"));
         } 
        
    }


}
