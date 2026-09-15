using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 15, -10);
    public float smoothSpeed = 5f;

    void Start()
    {
        FindTargetIfNull();
    }

    void LateUpdate()
    {
        if (target == null)
        {
            FindTargetIfNull();
            if (target == null) return;
        }

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }

    private void FindTargetIfNull()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }
}