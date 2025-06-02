using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public float moveSpeed = 1f;

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        transform.position += new Vector3(moveX, moveY, 0f) * moveSpeed * Time.deltaTime;
    }
}

