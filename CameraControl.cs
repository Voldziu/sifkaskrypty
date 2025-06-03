using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float dragSpeed = 0.5f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 2f;
    public float minZoom = 3f;
    public float maxZoom = 20f;

    [Header("Map Bounds")]
    public bool constrainToMap = true;
    public Vector2 mapMin = new Vector2(-20, -20);
    public Vector2 mapMax = new Vector2(20, 20);

    private Camera cam;
    private Vector3 dragOrigin;
    private bool isDragging;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("CameraControl: No Camera component found!");
        }
    }

    void Update()
    {
        HandleKeyboardMovement();
        HandleMouseDrag();
        HandleMouseScroll();
    }

    void HandleKeyboardMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveX, moveY, 0f) * moveSpeed * Time.deltaTime;
        Vector3 newPosition = transform.position + movement;

        if (constrainToMap)
        {
            newPosition.x = Mathf.Clamp(newPosition.x, mapMin.x, mapMax.x);
            newPosition.y = Mathf.Clamp(newPosition.y, mapMin.y, mapMax.y);
        }

        transform.position = newPosition;
    }

    void HandleMouseDrag()
    {
        // Left mouse button for drag
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
            isDragging = true;
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 currentPos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 difference = dragOrigin - currentPos;

            Vector3 newPosition = transform.position + new Vector3(difference.x, difference.y, 0) * dragSpeed;

            if (constrainToMap)
            {
                newPosition.x = Mathf.Clamp(newPosition.x, mapMin.x, mapMax.x);
                newPosition.y = Mathf.Clamp(newPosition.y, mapMin.y, mapMax.y);
            }

            transform.position = newPosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    void HandleMouseScroll()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0f)
        {
            float newSize = cam.orthographicSize - scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }
    }
}