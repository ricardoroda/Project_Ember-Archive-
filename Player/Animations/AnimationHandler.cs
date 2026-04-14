using UnityEngine;
using UnityEngine.AI;

public class AnimationHandler : MonoBehaviour
{
    private Animator animator;
    public Vector2 moveInput;
    public Vector2 forward;
    public Vector2 right;
    public bool useInput = true;  // set to false for AI
    public bool isPlayer = true; // set to false for AI

    // smoothing for animator parameters
    [Tooltip("Damp time for animation parameters (0 = no damping)")]
    public float dampTime = 0.08f;

    public float speed { get; private set; }
    public float testSpeed;

    // cached components used when not using player input
    private NavMeshAgent navAgent;
    private Rigidbody rb;
    [Header("Animator Parameters")]
    [Tooltip("Name of the animator parameter for horizontal movement")]
    public string moveXParameter = "MoveX";
    [Tooltip("Name of the animator parameter for vertical movement")]
    public string moveYParameter = "MoveY";
    [Tooltip("Name of the animator parameter for speed")]
    public string speedParameter = "Speed";

    private int hashMoveX;
    private int hashMoveY;
    private int hashSpeed;
    private bool hasMoveX;
    private bool hasMoveY;
    private bool hasSpeed;
    [Header("Debug")]
    [Tooltip("When enabled, logs diagnostic info once per second to help debug idle-only enemies.")]
    public bool debugMode = false;
    private float debugTimer = 0f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

    hashMoveX = Animator.StringToHash(moveXParameter);
    hashMoveY = Animator.StringToHash(moveYParameter);
    hashSpeed = Animator.StringToHash(speedParameter);

        if (animator != null)
        {
            var parms = animator.parameters;
            for (int i = 0; i < parms.Length; i++)
            {
                int h = parms[i].nameHash;
                if (h == hashMoveX) hasMoveX = true;
                if (h == hashMoveY) hasMoveY = true;
                if (h == hashSpeed) hasSpeed = true;
            }

            if (!hasMoveX || !hasMoveY || !hasSpeed)
            {
                string missing = "";
                if (!hasMoveX) missing += moveXParameter + " ";
                if (!hasMoveY) missing += moveYParameter + " ";
                if (!hasSpeed) missing += speedParameter + " ";
                Debug.LogWarning($"{name}: Animator is missing parameters: {missing}. AnimationHandler will skip setting those parameters.");
            }
        }
    }

    void Update()
    {
        float moveX = moveInput.x;
        float moveY = moveInput.y;



        if (useInput)
        {
            forward.x = gameObject.transform.forward.x;
            forward.y = gameObject.transform.forward.z;
            right.x = gameObject.transform.forward.x;
            right.y = gameObject.transform.forward.z;
            moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            moveX = Input.GetAxisRaw("Vertical") * right.x + Input.GetAxisRaw("Horizontal") * right.y;
            moveY = Input.GetAxisRaw("Vertical") * forward.y + Input.GetAxisRaw("Horizontal") * forward.x;
        }
        else
        {
            if (moveInput.sqrMagnitude < 0.0001f)
            {
                Vector3 velocity = Vector3.zero;
                // only use navAgent velocity if agent is enabled and not stopped
                if (navAgent != null && navAgent.enabled && !navAgent.isStopped && navAgent.velocity.sqrMagnitude > 0.0001f)
                {
                    velocity = navAgent.velocity;
                }
                else if (rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f)
                {
                    velocity = rb.linearVelocity;
                }

                if (velocity.sqrMagnitude > 0.0001f)
                {
                    moveInput = new Vector2(velocity.x, velocity.z);
                    moveX = moveInput.x;
                    moveY = moveInput.y;
                }
            }
        }

        //if (CompareTag("Player"))
        //{
        //    Vector3 worldVelocity = gameObject.GetComponent<Rigidbody>().linearVelocity;
        //    Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

        //    moveX = localVelocity.x;
        //    moveY = localVelocity.z;

        //}

        speed = moveInput.magnitude;
        //if(gameObject.CompareTag("Player"))
        //{
        //    speed = moveInput.magnitude/5 * gameObject.GetComponent<PlayerStats>().currentMovementSpeed;
        //    //speed = testSpeed;
        //}

        // blend tree parameters
        if (animator != null)
        {
            if (dampTime > 0f)
            {
                animator.SetFloat(hashMoveX, moveX, dampTime, Time.deltaTime);
                animator.SetFloat(hashMoveY, moveY, dampTime, Time.deltaTime);
                animator.SetFloat(hashSpeed, speed, dampTime, Time.deltaTime);
            }
            else
            {
                animator.SetFloat(hashMoveX, moveX);
                animator.SetFloat(hashMoveY, moveY);
                animator.SetFloat(hashSpeed, speed);
            }
        }

        // debug diagnostics (throttled)
        if (debugMode)
        {
            debugTimer -= Time.deltaTime;
            if (debugTimer <= 0f)
            {
                debugTimer = 1f;
                Vector3 navVel = (navAgent != null && navAgent.enabled && !navAgent.isStopped) ? navAgent.velocity : Vector3.zero;
                Vector3 rbVel = rb != null ? rb.linearVelocity : Vector3.zero;
                string parms = $"hasMoveX={hasMoveX} hasMoveY={hasMoveY} hasSpeed={hasSpeed}";
                string floats = "";
                if (animator != null)
                {
                    if (hasMoveX) floats += $" {moveXParameter}={animator.GetFloat(hashMoveX):F2}";
                    if (hasMoveY) floats += $" {moveYParameter}={animator.GetFloat(hashMoveY):F2}";
                    if (hasSpeed) floats += $" {speedParameter}={animator.GetFloat(hashSpeed):F2}";
                }

                Debug.Log($"[AnimationHandler Debug] '{name}': useInput={useInput} moveInput={moveInput} speed={speed:F2} navVel={navVel} rbVel={rbVel} {parms} {floats}");
            }
        }
    }

    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
    }
}
