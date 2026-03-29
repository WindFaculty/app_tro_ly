using UnityEngine;

public sealed class FollowCamera : MonoBehaviour
{
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private Vector3 offset = new Vector3(0f, 4.5f, -7f);
    [SerializeField] private float followSpeed = 8f;

    private Transform _target;

    private void LateUpdate()
    {
        if (_target == null)
        {
            GameObject targetObject = GameObject.FindGameObjectWithTag(targetTag);
            if (targetObject != null)
            {
                _target = targetObject.transform;
            }
        }

        if (_target == null)
        {
            return;
        }

        Vector3 desiredPosition = _target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        transform.LookAt(_target.position + Vector3.up * 0.75f);
    }
}
