using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;


[RequireComponent(typeof(NetworkRigidbody))]
public class NetworkedPlayerController : NetworkBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("Child transform used for horizontal body rotation (pointed at by PlayerCam).")]
    [SerializeField] private Transform orientation;

    [Tooltip("Child transform at head/eye level — MoveCamera will follow this position.")]
    [SerializeField] private Transform cameraPos;

    [Header("Camera")]
    [Tooltip("The CameraHolder prefab (contains MoveCamera + PlayerCam). NOT a scene object.")]
    [SerializeField] private GameObject cameraHolderPrefab;

    [Header("Movement")]
    [SerializeField] private float walkSpeed   = 7f;
    [SerializeField] private float sprintSpeed = 11f;
    [SerializeField] private float crouchSpeed = 3.5f;
    [SerializeField] private float groundDrag  = 5f;

    [Header("Jumping")]
    [SerializeField] private float jumpForce     = 12f;
    [SerializeField] private float jumpCooldown  = 0.25f;
    [SerializeField] private float airMultiplier = 0.4f;

    [Header("Crouching")]
    [SerializeField] private float crouchYScale = 0.5f;

    [Header("Ground Check")]
    [SerializeField] private float     playerHeight = 2f;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Slope Handling")]
    [SerializeField] private float maxSlopeAngle = 40f;

    [Header("Weapon")]
    [SerializeField] private Transform  weaponHolder;
    [SerializeField] private GameObject weaponPrefab;

    [Header("Health UI")]
    [Tooltip("The Image component of the Health Bar Fill. Must be a child of this prefab in world space.")]
    [SerializeField] private UnityEngine.UI.Image healthBarFill;

    // ── Networked State ────────────────────────────────────────────────────

    // Current Movement state
    [Networked] public MovementState State { get; private set; }

    // Current player health
    [Networked] public int Health { get; private set; }

    // Reference to the spawned weapon
    [Networked] public NetworkedWeapon Weapon { get; set; }

    public const int MaxHealth = 100;

    // ── Private Runtime ────────────────────────────────────────────────────

    private NetworkRigidbody _nrb;
    private Rigidbody        _rb;

    private float   _moveSpeed;
    private float   _startYScale;
    private bool    _readyToJump = true;
    private bool    _exitingSlope;
    private bool    _grounded;
    private RaycastHit _slopeHit;

    // Locally cached input
    private Vector2 _moveDir;
    private float   _yaw;
    private bool    _jumpPressed;
    private bool    _sprintHeld;
    private bool    _crouchHeld;
    private bool    _prevCrouchHeld;

    public enum MovementState { Walking, Sprinting, Crouching, Air }

    // ── Lifecycle ──────────────────────────────────────────────────────────

    public override void Spawned()
    {
        _nrb = GetComponent<NetworkRigidbody>();
        _rb  = GetComponent<Rigidbody>();

        _rb.freezeRotation = true;
        _startYScale       = transform.localScale.y;
        _readyToJump       = true;

        // don't let proxies fight the network interpolation with local physics
        _rb.isKinematic = Object.IsProxy;

        if (HasStateAuthority)
        {
            Health = MaxHealth;
        }

        // Local player only: set up camera and lock cursor
        if (HasInputAuthority)
        {
            SetupLocalCamera();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        // State authority (host): spawn the weapon
        if (HasStateAuthority)
        {
            SpawnWeapon();
        }
    }

    // ── Camera Setup ───────────────────────────────────────────────────────

    private void SetupLocalCamera()
    {
        if (cameraHolderPrefab == null)
        {
            Debug.LogError("[NetworkedPlayerController] cameraHolderPrefab is not assigned!");
            return;
        }
        if (cameraPos == null)
        {
            Debug.LogError("[NetworkedPlayerController] cameraPos is not assigned");
            return;
        }
        if (orientation == null)
        {
            Debug.LogError("[NetworkedPlayerController] orientation is not assigned!");
            return;
        }

        // Instantiate the CameraHolder as a standalone object
        GameObject cameraHolder = Instantiate(cameraHolderPrefab);

        // Wire MoveCamera to the position of the cameraPos
        MoveCamera moveCamera = cameraHolder.GetComponent<MoveCamera>();
        if (moveCamera != null)
            moveCamera.Init(cameraPos);
        else
            Debug.LogError("[NetworkedPlayerController] CameraHolder prefab has no MoveCamera component!");

        // Wire PlayerCam to the orientation transform for rotation
        PlayerCam playerCam = cameraHolder.GetComponentInChildren<PlayerCam>();
        if (playerCam != null)
            playerCam.Init(orientation);
        else
            Debug.LogError("[NetworkedPlayerController] CameraHolder prefab has no PlayerCam component in children!");
    }

    // ── Weapon Spawn ───────────────────────────────────────────────────────

    // Fusion spawns the weapon prefab and wires it to this player.
    // Only called on the StateAuthority 
    private void SpawnWeapon()
    {
        if (weaponPrefab == null)
        {
            Debug.LogError("[NetworkedPlayerController] weaponPrefab is not assigned!");
            return;
        }
        if (weaponHolder == null)
        {
            Debug.LogError("[NetworkedPlayerController] weaponHolder is not assigned!");
            return;
        }

        NetworkObject weaponNetObj = Runner.Spawn(
            weaponPrefab,
            weaponHolder.position,
            weaponHolder.rotation,
            Object.InputAuthority
        );

        if (weaponNetObj == null)
        {
            Debug.LogError("[NetworkedPlayerController] Runner.Spawn returned null for weapon prefab!");
            return;
        }

        Weapon = weaponNetObj.GetComponent<NetworkedWeapon>();
        if (Weapon == null)
        {
            Debug.LogError("[NetworkedPlayerController] Spawned weapon has no NetworkedWeapon component!");
            return;
        }

        Debug.Log("[NetworkedPlayerController] Weapon spawned on host.");
    }

    public override void Render()
    {
        // Update Health Bar UI
        if (healthBarFill != null)
        {
            // interpolate the visual health bar
            float targetFill = (float)Health / MaxHealth;
            healthBarFill.fillAmount = Mathf.Lerp(healthBarFill.fillAmount, targetFill, Time.deltaTime * 10f);
        }

        // Visual crouch scale
        float targetYScale = (State == MovementState.Crouching) ? crouchYScale : _startYScale;
        transform.localScale = new Vector3(transform.localScale.x, targetYScale, transform.localScale.z);

        // Make sure the weapon is visually parented to the player's weapon holder on all clients
        if (Weapon != null && Weapon.transform.parent != weaponHolder)
        {
            Weapon.transform.SetParent(weaponHolder, worldPositionStays: false);
            Weapon.transform.localPosition = Vector3.zero;
            Weapon.transform.localRotation = Quaternion.identity;
        }

        // Once the weapon reference is available on the local machine, assign the camera.
        // This handles the case where SpawnWeapon runs on the host and the local client
        // receives the weapon via state sync slightly later.
        if (HasInputAuthority && Weapon != null)
        {
            if (Weapon is NetworkedPistol pistol && Camera.main != null)
            {
                pistol.Init(Camera.main);
            }
        }
    }

    // ── Health API ─────────────────────────────────────────────────────────

    public void ApplyDamage(int damage)
    {
        if (!HasStateAuthority) return; // Only the server calculates damage
        if (Health <= 0) return;        // Already dead

        Health -= damage;
        Debug.Log($"[NetworkedPlayerController] Player {Object.InputAuthority} took {damage} damage! Health is now {Health}");

        if (Health <= 0)
        {
            Health = 0;
            // Notify the Game Manager that a player has died
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerDied(this);
            }
        }
    }

    public void Respawn(Vector3 spawnPosition)
    {
        if (!HasStateAuthority) return;

        Health = MaxHealth;
        
        // Reset physics
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        // Teleport to spawn
        _rb.position = spawnPosition;
        _rb.rotation = Quaternion.identity;

        Debug.Log($"[NetworkedPlayerController] Player {Object.InputAuthority} respawned at {spawnPosition}.");
    }

    // ── Fusion Tick ────────────────────────────────────────────────────────

    public override void FixedUpdateNetwork()
    {
        // Proxies shouldn't simulate physics or run local movement logic
        // they rely on NetworkRigidbody for interpolation and Networked properties.
        if (Object.IsProxy) return;

        // Pull the input struct for this tick
        if (GetInput(out NetworkInputData input))
        {
            _moveDir      = input.MoveDirection;
            _yaw          = input.CameraYaw;
            _jumpPressed  = input.Buttons.IsSet(NetworkInputData.BUTTON_JUMP);
            _sprintHeld   = input.Buttons.IsSet(NetworkInputData.BUTTON_SPRINT);
            _crouchHeld   = input.Buttons.IsSet(NetworkInputData.BUTTON_CROUCH);
            bool fireHeld = input.Buttons.IsSet(NetworkInputData.BUTTON_FIRE);

            // Weapon fire (server-authoritative raycast).
            if (fireHeld && Weapon != null)
            {
                // Calculate aim direction using the pitch and yaw from the network input
                Vector3 aimDir = Quaternion.Euler(input.CameraPitch, input.CameraYaw, 0f) * Vector3.forward;
                Weapon.NetworkFire(Runner, Object.InputAuthority, cameraPos.position, aimDir);
            }

            // Crouch scale — apply only when state changes to avoid per-tick assignments.
            if (_crouchHeld && !_prevCrouchHeld)
            {
                _rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
            }
            _prevCrouchHeld = _crouchHeld;

            // Jump
            if (_jumpPressed && _readyToJump && _grounded)
            {
                _readyToJump  = false;
                _exitingSlope = true;
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
                _rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);

                // Schedule jump reset
                Invoke(nameof(ResetJump), jumpCooldown);
            }
        }

        CheckGrounded();
        StateHandler();
        MovePlayer();
        SpeedControl();
        RotatePlayer();

        // Drag
        _rb.linearDamping = _grounded ? groundDrag : 0f;
    }

    // ── Movement Logic ──────────────────────────

    private void CheckGrounded()
    {
        _grounded = Physics.Raycast(transform.position, Vector3.down,
                                    playerHeight * 0.5f + 0.3f, whatIsGround);
    }

    private void StateHandler()
    {
        if (_crouchHeld)                     { State = MovementState.Crouching; _moveSpeed = crouchSpeed; }
        else if (_grounded && _sprintHeld)   { State = MovementState.Sprinting; _moveSpeed = sprintSpeed; }
        else if (_grounded)                  { State = MovementState.Walking;   _moveSpeed = walkSpeed;   }
        else                                 { State = MovementState.Air; }
    }

    private void MovePlayer()
    {
        Vector3 dir = transform.forward * _moveDir.y + transform.right * _moveDir.x;

        if (OnSlope() && !_exitingSlope)
        {
            _rb.AddForce(GetSlopeMoveDir(dir) * _moveSpeed * 20f, ForceMode.Force);
            if (_rb.linearVelocity.y > 0)
                _rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }
        else if (_grounded)
        {
            _rb.AddForce(dir.normalized * _moveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            _rb.AddForce(dir.normalized * _moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }

        _rb.useGravity = !OnSlope();
    }

    private void SpeedControl()
    {
        if (OnSlope() && !_exitingSlope)
        {
            if (_rb.linearVelocity.magnitude > _moveSpeed)
                _rb.linearVelocity = _rb.linearVelocity.normalized * _moveSpeed;
        }
        else
        {
            Vector3 flat = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            if (flat.magnitude > _moveSpeed)
            {
                Vector3 limited = flat.normalized * _moveSpeed;
                _rb.linearVelocity = new Vector3(limited.x, _rb.linearVelocity.y, limited.z);
            }
        }
    }

    private void RotatePlayer()
    {
        // Rotate the Rigidbody
        _rb.MoveRotation(Quaternion.Euler(0f, _yaw, 0f));
    }

    private void ResetJump()
    {
        _readyToJump  = true;
        _exitingSlope = false;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out _slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }

    private Vector3 GetSlopeMoveDir(Vector3 dir) =>
        Vector3.ProjectOnPlane(dir, _slopeHit.normal).normalized;

    // ── Public Helpers ─────────────────────────────────────────────────────

    public GameObject GetOrientation()
    {
        if (orientation == null)
        {
            Debug.LogWarning("Orientation transform is not assigned in NetworkedPlayerController.");
            return null;
        }
        return orientation.gameObject;
    }
}
