using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    //Events
    public static Transform InstanceTransform { get; private set; }
    public static event Action<Transform> OnPlayerSpawned;
    [Header("Rotation Settings")]
    [Tooltip("Camera used to project mouse into the world")]
    [SerializeField] private Camera rotationCamera;
    [Tooltip("Layers considered ground for mouse look")]
    [SerializeField] private LayerMask groundLayer = 0;

    //Player Movement Variables
    [SerializeField] private float rotationSpeed = 1080f; // degrees per second for fast rotation

    //Player Associated Variables
    private Rigidbody rb;
    private Vector2 moveInput;
    public Vector3 rotatedDirection;
    private Quaternion targetRotation;

    //Ability handler variables
    private PlayerStats playerStats;

    private void Awake()
    {
        InstanceTransform = transform;

        rb = GetComponent<Rigidbody>();
        playerStats = GetComponent<PlayerStats>();
        targetRotation = transform.rotation;
        // Safer physics settings to reduce solver popping when colliding with scene geometry
        try
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        catch { }
    }

    private void Start()
    {
        OnPlayerSpawned?.Invoke(transform);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
            moveInput = context.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        if (playerStats.isDashing || playerStats.isFrozen || playerStats.isStunned) return;

        // raw input to avoid input lag
        Vector2 rawInput = GetRawMoveInput();
        // fallback input
        if (rawInput == Vector2.zero) rawInput = moveInput;

        // world-relative
        Vector3 right = Vector3.right;
        Vector3 forward = Vector3.forward;

        // keep movement on the horizontal plane
        right.y = 0f;
        forward.y = 0f;

        Vector3 direction = (right.normalized * rawInput.x) + (forward.normalized * rawInput.y);

        // avoid faster diagonal movement
        if (direction.sqrMagnitude > 1f) direction.Normalize();

        // movement vector for animations (still local, but based on world movement)
        Vector3 localDirection = transform.InverseTransformDirection(direction);
        localDirection.y = 0f;
        rotatedDirection = localDirection;

        rb.MovePosition(rb.position + direction * (playerStats.currentMovementSpeed * Time.fixedDeltaTime));

        // rotation
        if (targetRotation != Quaternion.identity)
        {
            Quaternion newRot = Quaternion.RotateTowards(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRot);
        }
    }

    // player rotation (for animations)
    void Update()
    {
        // compute target rotation from mouse each frame so we can apply it in FixedUpdate via Rigidbody
            Camera cam = rotationCamera != null ? rotationCamera : Camera.main;
            if (cam == null || Mouse.current == null) return;
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

            // Try physics raycast against configured ground layer
            bool didHit = false;
            RaycastHit hit = new RaycastHit();
            if (groundLayer != 0)
            {
                didHit = Physics.Raycast(ray, out hit, 100f, groundLayer);
            }

            if (!didHit)
            {
                // cast against a horizontal plane as fallback
                Plane fallbackPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
                if (fallbackPlane.Raycast(ray, out float enter))
                {
                    Vector3 point = ray.GetPoint(enter);
                    hit = new RaycastHit();
                    hit.point = point;
                    didHit = true;
                }
            }

            if (didHit)
        {
                Vector3 lookPoint = hit.point;
            lookPoint.y = transform.position.y; // keep player up
            Vector3 direction = (lookPoint - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(direction);
            }
        }
    }

    // read raw input
    private Vector2 GetRawMoveInput()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            float x = 0f;
            float y = 0f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
            input = new Vector2(x, y);
            return input;
        }

        return Vector2.zero;
    }
}
