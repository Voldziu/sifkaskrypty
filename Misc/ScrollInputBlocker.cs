using UnityEngine;
using UnityEngine.EventSystems;

public class ScrollInputBlocker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Camera Control")]
    public CameraControl cameraControl;

    void Start()
    {
        // Auto-find camera control if not assigned
        if (cameraControl == null)
        {
            cameraControl = FindObjectOfType<CameraControl>();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (cameraControl != null)
        {
            cameraControl.enabled = false;
            Debug.Log($"Camera control disabled for {gameObject.name}");
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (cameraControl != null)
        {
            cameraControl.enabled = true;
            Debug.Log($"Camera control enabled for {gameObject.name}");
        }
    }

    void OnDisable()
    {
        if (cameraControl != null)
        {
            cameraControl.enabled = true;
        }
    }
}