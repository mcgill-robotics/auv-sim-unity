using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using UnityEngine.UI;
using TMPro;

public class FreeCameraController : MonoBehaviour
{
    [Header("Camera Reference")]
    [Tooltip("Camera component to control. Auto-assigned if null")]
    public Camera cam;
    
    [Space(10)]
    [Header("Movement Settings")]
    [Tooltip("Speed of camera movement when panning (middle mouse drag)")]
    [Range(0.1f, 10f)]
    public float moveSpeed = 2.5f;
    
    [Space(5)]
    [Tooltip("Speed of rotation around X axis (up/down)")]
    [Range(50f, 1000f)]
    public float xRotateSpeed = 400f;
    
    [Tooltip("Speed of rotation around Y axis (left/right). Negative inverts direction")]
    [Range(-1000f, 1000f)]
    public float yRotateSpeed = -400f;
    
    [Space(5)]
    [Tooltip("Speed of zooming with scroll wheel")]
    [Range(1f, 20f)]
    public float scrollSpeed = 6f;
    
    [Tooltip("Default pivot distance (m) when no object is under mouse cursor")]
    [Range(1f, 50f)]
    public float defaultPivotDistance = 5f;

    private bool isDragging = false;
    private bool isPanning = false;
    private Vector3 initialMousePosition;
    private Vector3 initialCamDirection;
    private int layerMask;

    private void Start()
    {
        if (cam == null) cam = GetComponent<Camera>();
        
        // Create a layer mask that includes all layers except "Water", "Air", and "Props"
        // We cache this to avoid string lookups in Update
        layerMask = ~((1 << LayerMask.NameToLayer("Water")) | (1 << LayerMask.NameToLayer("Air")) | (1 << LayerMask.NameToLayer("Props")));
    }

    private void Update()
    {
        // Don't control camera if interacting with UI
        if (IsMouseOverUI()) return;

        // Don't control if mouse is outside game window
        if (!IsMouseOverGameWindow())
        {
            isPanning = false;
            isDragging = false;
            return;
        }

        HandleInput();

        if (isDragging) UpdateDragging();
        if (isPanning) UpdatePanning();
        HandleZoom();
    }

    private void HandleInput()
    {
        // Left Click - Dragging (Movement)
        if (Input.GetMouseButtonDown(0))
        {
            initialMousePosition = GetWorldPointUnderMouse();
            initialCamDirection = cam.transform.forward;
            isDragging = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        // Right or Middle Click - Panning (Rotation)
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            initialMousePosition = GetWorldPointUnderMouse();
            isPanning = true;
        }
        else if (Input.GetMouseButtonUp(1) && Input.GetMouseButtonUp(2))
        {
            isPanning = false;
        }
    }

    private void HandleZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            float distanceToWorld = Mathf.Max(Vector3.Distance(cam.transform.position, GetWorldPointUnderMouse()), 5f);
            Vector3 moveDir = cam.transform.forward * scrollInput * scrollSpeed * distanceToWorld / 5f;
            cam.transform.position += moveDir;
        }
    }

    private bool IsMouseOverUI()
    {
        // UI Toolkit detection
        var hudDocument = SimulatorHUD.Instance?.uiDocument;
        if (hudDocument != null && hudDocument.rootVisualElement != null)
        {
            var panelPosition = RuntimePanelUtils.ScreenToPanel(
                hudDocument.rootVisualElement.panel,
                new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)
            );
            
            var pickedElement = hudDocument.rootVisualElement.panel.Pick(panelPosition);
            if (pickedElement != null && pickedElement != hudDocument.rootVisualElement)
            {
                return true; // Mouse is over a UI element
            }
        }
        
        return false;
    }

    private bool IsMouseOverGameWindow()
    {
        return !(0 > Input.mousePosition.x || 0 > Input.mousePosition.y || Screen.width < Input.mousePosition.x || Screen.height < Input.mousePosition.y);
    }

    private void UpdateDragging()
    {
        Vector3 mousePosScreen = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1f);
        Vector3 mouseDirectionFromCam = cam.ScreenToWorldPoint(mousePosScreen) - cam.transform.position;
        Vector3 newFocusedPoint = CalculateIntersection(cam.transform.position, mouseDirectionFromCam, initialMousePosition, initialCamDirection);
        
        // Move camera so that the point under mouse stays under mouse
        cam.transform.position += (initialMousePosition - newFocusedPoint);
    }

    private void UpdatePanning()
    {
        // Rotate around the point we clicked on
        cam.transform.RotateAround(initialMousePosition, Vector3.up, Input.GetAxis("Mouse X") * xRotateSpeed * Time.deltaTime);
        cam.transform.RotateAround(initialMousePosition, cam.transform.right, Input.GetAxis("Mouse Y") * yRotateSpeed * Time.deltaTime);
    }

    private Vector3 GetWorldPointUnderMouse()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            return hit.point;
        }
        return ray.GetPoint(defaultPivotDistance);
    }

    private Vector3 CalculateIntersection(Vector3 origin, Vector3 direction, Vector3 planePoint, Vector3 planeNormal)
    {
        float dotNumer = Vector3.Dot(planeNormal, planePoint - origin);
        float dotDenom = Vector3.Dot(planeNormal, direction);
        
        // Avoid division by zero
        if (Mathf.Abs(dotDenom) < Mathf.Epsilon) return origin + direction * defaultPivotDistance;

        float distanceToPlane = dotNumer / dotDenom;
        return origin + direction * distanceToPlane;
    }
}