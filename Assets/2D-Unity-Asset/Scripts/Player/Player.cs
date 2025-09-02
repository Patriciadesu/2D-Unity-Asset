using NaughtyAttributes;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;


[ExecuteAlways]
public partial class Player : Singleton<Player>
{
    #region Player Properties

    #region Camera Settings
    public enum CameraType
    {
        FirstPerson,
        ThirdPerson
    }
    public float mouseSensitivity = 2f;
    [Foldout("Camera", true)] public CameraType cameraType;
    [Foldout("Camera", true), SerializeField, Range(30, 120)] float cameraFOV;
    [Foldout("Camera", true), ShowIf("cameraType", CameraType.ThirdPerson), Range(0, 1)] public float cameraSide = 0.5f;
    [Foldout("Camera", true), ShowIf("cameraType", CameraType.ThirdPerson)] public float cameraDistance = 5f;
    [Foldout("Camera", true), ShowIf("cameraType", CameraType.ThirdPerson), Range(-1, 2)] public float yOffset = -.4f;
    private float xRotation = 0f;
    private float tpsYaw = 0f;
    private float tpsPitch = 10f;
    [Foldout("DO NOT TOUCH")] public Camera camera;
    [Foldout("DO NOT TOUCH")] public Transform fpsCameraPivot;
    [Foldout("DO NOT TOUCH")] public Camera tpsCamera;
    [Foldout("DO NOT TOUCH")] public CinemachineThirdPersonFollow tpsVirtualCamera;
    [Foldout("DO NOT TOUCH")] public Transform tpsCameraPivot;
    #endregion

    #region Movement Settings
    public float Speed => (speed + additionalSpeed) * speedMultiplier;
    [Foldout("Movement Settings", true), SerializeField, Range(0, 100)] private float speed = 5f;
    [Foldout("Movement Settings", true), Range(0, 20)] public float jumpForce = 10f;
    [Foldout("Movement Settings", true), Range(0, 10)] public float fallMultiplier = 3f;
    [Foldout("Movement Settings", true), Range(0, 20)] public float gravityMultiplier = 2.5f;
    [HideInInspector] public float speedMultiplier = 1f;
    [HideInInspector] public float additionalSpeed = 0;
    [HideInInspector] public List<IUseStamina> staminaComponentStates = new List<IUseStamina>();
    public bool canGenerateStamina => staminaComponentStates.TrueForAll(x => !x.isUsingStamina);
    public bool canHit;
    
    #endregion

    #region Movement Buffer
    private float coyoteTime = 0.01f;
    private float jumpBufferTime = 0.05f;
    private float lastGroundedTime;
    private float lastJumpPressedTime;
    #endregion

    #region Ground Check
    private float groundCheckDistance = 0.5f;
    private float capsuleHeight => capsuleCollider.height;
    private float capsuleRadius => capsuleCollider.radius;
    #endregion

    #region Components
    [HideInInspector] public Rigidbody rigidbody;
    [HideInInspector] public Animator animator;
    [HideInInspector] public CapsuleCollider capsuleCollider;
    #endregion

    [HideInInspector] public Vector3 lastCheckpoint;
    [HideInInspector] public Vector3 spawnPoint;

    public bool isGrounded = true;
    [HideInInspector] public List<ICancleGravity> cancleGravityComponents = new List<ICancleGravity>();
    public bool canApplyGravity => cancleGravityComponents.TrueForAll(x => x.canApplyGravity);
    [HideInInspector] public bool canMove = true;
    [HideInInspector] public bool canRotateCamera = true;

    #region Player Delegates
    public delegate void FixedUpdateDelegate();
    public FixedUpdateDelegate OnFixedUpdate;
    public delegate void UpdateDelegate();
    public UpdateDelegate OnUpdate;
    public delegate void CollisionEnterDelegate(Collision collision);
    public CollisionEnterDelegate OnCollisionEnterEvent;
    public delegate void CollisionStayDelegate(Collision collision);
    public CollisionStayDelegate OnCollisionStayEvent;
    public delegate void CollisionExitDelegate(Collision collision);
    public CollisionExitDelegate OnCollisionExitEvent;
    public delegate void TriggerEnterDelegate(Collider other);
    public TriggerEnterDelegate OnTriggerEnterEvent;
    public delegate void TriggerStayDelegate(Collider other);
    public TriggerStayDelegate OnTriggerStayEvent;
    public delegate void TriggerExitDelegate(Collider other);
    public TriggerExitDelegate OnTriggerExitEvent;

    #endregion

    // Extensions
    private PlayerExtension[] extensions;

    #endregion
    
    #region Player Stats
    [Foldout("Player Stats", true), SerializeField, Range(0, 1000)] public float maxhealth = 100f;
    [Foldout("Player Stats", true), SerializeField, Range(0, 1000)] public float maxstamina = 100f;
    [Foldout("Player Stats", true), SerializeField, Range(0, 50)] public float staminaRegenRate = 20f; // New: Stamina regen per second
    [HideInInspector] public float currenthealth;
    [HideInInspector] public float currentstamina;
    #endregion

    #region Unity Methods
    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        currenthealth = maxhealth;
        currentstamina = maxstamina;
    }
    void Start()
    {
        if (Application.isPlaying)
        {
            SetExtensions();
            SetUpdate();
            SetFixedUpdate();
            SetSpawnPoint(transform.position);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            staminaComponentStates.Clear();
            staminaComponentStates.AddRange(GetComponents<IUseStamina>());

            foreach (var extension in extensions)
            {
                extension.OnStart(this);
            }
        }
    }
    void Update()
    {
        if (Application.isPlaying)
        {
            OnUpdate?.Invoke();
            if (isGrounded)
            {
                lastGroundedTime = Time.time;
                RegenerateStamina(); // Regenerate stamina when grounded
            }
            if (cameraType == CameraType.FirstPerson)
            {
                camera.transform.position = fpsCameraPivot.transform.position;
            }
            if (!canRotateCamera)
            {
                
            }
        }
        else
        {
            SetUpCamera();
            foreach (GameObject cam in GameObject.FindGameObjectsWithTag("MainCamera"))
            {
                if (cam != camera.gameObject)
                {
                    DestroyImmediate(cam);
                }
            }
        }
    }
    void FixedUpdate()
    {
        if (Application.isPlaying)
        {
            OnFixedUpdate?.Invoke();
        }
    }
    private void RegenerateStamina()
    {
        // Skip regeneration during sprint, roll, or dash
        if (currentstamina < maxstamina && canGenerateStamina)
        {
            currentstamina += staminaRegenRate * Time.deltaTime;
            if (currentstamina > maxstamina)
            {
                currentstamina = maxstamina;
            }
        }
    }
    public void TakeDamage(float amount)
    {
        if (canHit)
        {
            currenthealth -= Mathf.Max(amount, 0);
            animator.SetTrigger("GetHit");
            Debug.Log("Player took damage: " + amount + ", Current Health: " + currenthealth);
            if (currenthealth <= 0)
            {
                currenthealth = 0;
                Respawn();
            }
        }else
        {
           canHit = true;
           animator.SetBool("isBlocking", false);
        }
    }
    void OnCollisionEnter(Collision collision)
    {
        if (Application.isPlaying)
        {
            OnCollisionEnterEvent?.Invoke(collision);
        }
    }
    void OnCollisionStay(Collision collision)
    {
        if (Application.isPlaying)
        {
            OnCollisionStayEvent?.Invoke(collision);
        }
    }
    void OnCollisionExit(Collision collision)
    {
        if (Application.isPlaying)
        {
            OnCollisionExitEvent?.Invoke(collision);
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (Application.isPlaying)
        {
            OnTriggerEnterEvent?.Invoke(other);
        }
    }
    void OnTriggerStay(Collider other)
    {
        if (Application.isPlaying)
        {
            OnTriggerStayEvent?.Invoke(other);
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (Application.isPlaying)
        {
            OnTriggerExitEvent?.Invoke(other);
        }
    }
    #endregion

    #region Player Methods

    #region Setup Methods

    public void SetExtensions()
    {
        extensions = GetComponents<PlayerExtension>();
        foreach (var extension in extensions)
        {
            extension.OnStart(this);
        }
    }

    public void SetUpdate()
    {
        OnUpdate = null;
        OnUpdate += CheckGrounded;
        OnUpdate += JumpHandler;

        OnUpdate += HandleMouseLook;
    }

    public void SetFixedUpdate()
    {
        OnFixedUpdate = null;
        OnFixedUpdate += ApplyGravity;
        OnFixedUpdate += Move;

    }

    public void SetSpawnPoint(Vector3 spawnPoint)
    {
        this.spawnPoint = spawnPoint;
    }

    void SetUpCamera()
    {
        switch (cameraType)
        {
            case CameraType.FirstPerson:
                tpsCamera.gameObject.SetActive(false);
                camera.gameObject.SetActive(true);
                camera.transform.position = fpsCameraPivot.position;
                camera.transform.rotation = Quaternion.Euler(transform.forward);
                camera.fieldOfView = cameraFOV;
                break;
            case CameraType.ThirdPerson:
                tpsCamera.gameObject.SetActive(true);
                camera.gameObject.SetActive(false);
                tpsVirtualCamera.GetComponent<CinemachineCamera>().Lens.FieldOfView = cameraFOV;
                tpsVirtualCamera.CameraDistance = cameraDistance;
                tpsVirtualCamera.CameraSide = cameraSide;
                tpsVirtualCamera.ShoulderOffset.y = yOffset;
                break;
        }

    }

    #endregion

    #region Movement Methods
    void Move()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move;
        if (canMove)
        {
            move = (transform.right * horizontal + transform.forward * vertical).normalized;
            rigidbody.linearVelocity = new Vector3(move.x * Speed, rigidbody.linearVelocity.y, move.z * Speed);
            animator.SetFloat("MoveX", horizontal);
            animator.SetFloat("MoveY", vertical);
            animator.SetBool("isRun", horizontal != 0 || vertical != 0);
        }
    }
    public void JumpHandler()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            lastJumpPressedTime = Time.time;
        }
        if (Time.time - lastJumpPressedTime <= jumpBufferTime && Time.time - lastGroundedTime <= coyoteTime)
        {
            Jump();
            lastJumpPressedTime = -999f; // Reset to prevent double fire
        }
    }
    public void Jump()
    {
        animator.SetTrigger("jump");
        rigidbody.linearVelocity = new Vector3(rigidbody.linearVelocity.x, 0f, rigidbody.linearVelocity.z);
        rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
    #endregion

    #region Graivty Methods

    void ApplyGravity()
    {
        Debug.Log("Applying Gravity");
        float _fallMultiplier = canApplyGravity ? 1 : fallMultiplier;
        if (rigidbody.linearVelocity.y <= 0)
        {
            rigidbody.linearVelocity += Vector3.up * Physics.gravity.y * (_fallMultiplier - 1) * Time.deltaTime;
        }
        else if (rigidbody.linearVelocity.y > 0 && !Input.GetButton("Jump"))
        {
            rigidbody.linearVelocity += Vector3.up * Physics.gravity.y * (gravityMultiplier - 1) * Time.deltaTime;
        }
        if (isGrounded && rigidbody.linearVelocity.y < 0)
        {
           rigidbody.linearVelocity = new Vector3(rigidbody.linearVelocity.x, -2f, rigidbody.linearVelocity.z);
        }


    }
    void CheckGrounded()
    {
        Vector3 center = transform.position + capsuleCollider.center;
        float radius = capsuleCollider.radius * 0.95f;
        float height = capsuleCollider.height * 0.5f - radius;

        Vector3 point1 = center + Vector3.up * height;
        Vector3 point2 = center - Vector3.up * height;

        Vector3 direction = Vector3.down;
        float distance = 0.2f;

        RaycastHit[] hits = Physics.CapsuleCastAll(
            point1, point2, radius, direction, distance,
            ~0, // Everything
            QueryTriggerInteraction.Ignore
        );

        isGrounded = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != capsuleCollider)
            {
                isGrounded = true;
                break;
            }
        }
    }


    #endregion

    void HandleMouseLook()
    {
        if (!canRotateCamera) return;
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (cameraType == CameraType.FirstPerson)
        {
            transform.Rotate(Vector3.up * mouseX);
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            camera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        else if (cameraType == CameraType.ThirdPerson)
        {
            tpsYaw += mouseX;
            tpsPitch -= mouseY;
            tpsPitch = Mathf.Clamp(tpsPitch, -20f, 60f);
            tpsCameraPivot.rotation = Quaternion.Euler(tpsPitch, tpsYaw, 0f);
            transform.rotation = Quaternion.Euler(0f, tpsYaw, 0f);
        }
    }

    public void Respawn()
    {
        rigidbody.linearVelocity = Vector3.zero;
        Debug.Log("Respawning");
        if (lastCheckpoint == Vector3.zero)
        {
            Debug.Log("Last Checkpoint is null");
            this.transform.position = spawnPoint;
        }
        else this.transform.position = lastCheckpoint;

        currenthealth = maxhealth;
    }
    //Done
    public float GetAnimationLength(string animationName)
    {
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == animationName)
            {
                return clip.length;
            }
        }
        return 0f;
    }

    #endregion

    #region Gizmos
    //Done
    void OnDrawGizmosSelected()
    {

        Vector3 start = transform.position + capsuleCollider.center;
        float radius = capsuleRadius * 0.95f;
        float height = capsuleHeight * 0.5f - radius;

        Vector3 point1 = start + Vector3.up * height;
        Vector3 point2 = start - Vector3.up * height - Vector3.up * groundCheckDistance;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(point1 - Vector3.up * groundCheckDistance, radius);
        Gizmos.DrawWireSphere(point2, radius);
        Gizmos.DrawLine(point1 - Vector3.up * groundCheckDistance + Vector3.left * radius, point2 + Vector3.left * radius);
        Gizmos.DrawLine(point1 - Vector3.up * groundCheckDistance + Vector3.right * radius, point2 + Vector3.right * radius);
    }

    #endregion

}
