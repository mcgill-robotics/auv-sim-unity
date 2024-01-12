using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MainCamControl : MonoBehaviour
{
    public Camera cam;
    public float moveSpeed = 2.5f;
    public float XRotateSpeed = 400f;
    public float YRotateSpeed = -400f;
    public float scrollSpeed = 6f;

    public float noObjectUnderMouseDefaultPivotDistance = 5f;
    
    public GameObject auv;

    
    private bool isDragging = false;
    private bool isPanning = false;
    private Vector3 initialMousePosition;
    private Vector3 initialCamDirection;

    void Update()
    {
        FixedUpdate();
    }

    void FixedUpdate()
    {
        bool IsMouseOverGameWindow = !(0 > Input.mousePosition.x || 0 > Input.mousePosition.y || Screen.width < Input.mousePosition.x || Screen.height < Input.mousePosition.y);
        if (!IsMouseOverGameWindow) {
            isPanning = false;
            isDragging = false;
            return;
        }
        if (Input.GetMouseButton(0) && !isDragging)
        {
            initialMousePosition = getWorldPointUnderMouse();
            initialCamDirection = cam.transform.forward;
            isDragging = true;
        }
        else if (!Input.GetMouseButton(0))
        {
            isDragging = false;
        }
        if (!isPanning && (Input.GetMouseButton(1) || Input.GetMouseButton(2)))
        {
            initialMousePosition = getWorldPointUnderMouse();
            isPanning = true;
        }
        else if (!(Input.GetMouseButton(1) || Input.GetMouseButton(2)))
        {
            isPanning = false;
        }

        if (isDragging)
        {
            UpdateDragging();
        }
        
        if (isPanning) {
            UpdatePanning();
        }

        if (Input.GetAxis("Mouse ScrollWheel") > 0 || Input.GetAxis("Mouse ScrollWheel") < 0 ) {
            float distanceToWorld = Vector3.Distance(cam.transform.position, getWorldPointUnderMouse());
            cam.transform.position = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, scrollSpeed * Input.GetAxis("Mouse ScrollWheel") * distanceToWorld/5));
        } 
        
    }

    void UpdateDragging()
    {
        Vector3 mouseDirectionFromCam = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1f)) - cam.transform.position;
        Vector3 newFocusedPoint = calculateIntersection(cam.transform.position, mouseDirectionFromCam, initialMousePosition, initialCamDirection);
        cam.transform.position += (initialMousePosition - newFocusedPoint);
    }
    
    void UpdatePanning()
    {
        cam.transform.RotateAround(initialMousePosition, Vector3.up, Input.GetAxisRaw("Mouse X") * XRotateSpeed * Time.deltaTime);
        cam.transform.RotateAround(initialMousePosition, transform.right, Input.GetAxisRaw("Mouse Y") * YRotateSpeed * Time.deltaTime);
    }

    Vector3 getWorldPointUnderMouse()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Create a layer mask that includes all layers except "Water" and "Air"
        int layerMask = ~((1 << LayerMask.NameToLayer("Water")) | (1 << LayerMask.NameToLayer("Air")));

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            return hit.point;
        }
        else
        {
            return ray.GetPoint(noObjectUnderMouseDefaultPivotDistance);
        }
    }

    Vector3 calculateIntersection(Vector3 origin, Vector3 direction, Vector3 planePoint, Vector3 planeNormal)
    {
        // Calculate the distance from the plane to the origin of the vector
        float distanceToPlane = Vector3.Dot(planeNormal, planePoint - origin) / Vector3.Dot(planeNormal, direction);
        // Calculate the intersection point
        Vector3 intersectionPoint = origin + direction * distanceToPlane;

        return intersectionPoint;
    }

}
