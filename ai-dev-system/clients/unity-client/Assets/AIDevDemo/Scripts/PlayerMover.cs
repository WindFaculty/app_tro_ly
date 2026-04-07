using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class PlayerMover : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;

    private Rigidbody _rigidbody = null!;
    private Vector3 _input;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            _rigidbody = gameObject.AddComponent<Rigidbody>();
        }

        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            _input = Vector3.zero;
            return;
        }

        Vector2 move = Vector2.zero;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            move.x -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            move.x += 1f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            move.y -= 1f;
        }

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            move.y += 1f;
        }

        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        _input = new Vector3(move.x, 0f, move.y);
#else
        _input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;
#endif
    }

    private void FixedUpdate()
    {
        Vector3 delta = _input * (moveSpeed * Time.fixedDeltaTime);
        _rigidbody.MovePosition(_rigidbody.position + delta);
    }
}
