using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    [SerializeField] private float sensX;
    [SerializeField] private float sensY;

    // Set via Init() by NetworkedPlayerController when the local player spawns.
    private Transform _orientation;

    private float xRotation;
    private float yRotation;

    private bool _initialized;

    // world-space ya the camera is currently at
    public static float YRotation { get; private set; }

    // world-space pitch the camera is currently at, clamped to +/-90 degrees
    public static float XRotation { get; private set; }

    // ── Initialization ─────────────────────────────────────────────────────

    // Called by NetworkedPlayerController after instantiating the CameraHolder prefab.
    public void Init(Transform orientation)
    {
        _orientation = orientation;
        _initialized = true;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (!_initialized)
            Debug.LogWarning("[PlayerCam] Init() was never called. " +
                             "Make sure NetworkedPlayerController calls Init() after instantiating the CameraHolder prefab.");
    }

    // Update is called once per frame
    void Update()
    {
        if (!_initialized) return;

        // get mouse input
        float mouseX = Input.GetAxisRaw("Mouse X") * sensX * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensY * Time.deltaTime;

        yRotation += mouseX;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Publish to static properties so InputHandler reads the same values.
        YRotation = yRotation;
        XRotation = xRotation;

        // rotate cam and orientation
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        _orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }

    public Transform GetTransform()
    {
        return transform;
    }
}
