using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    // Set with the Init() by NetworkedPlayerController when the local player spawns
    private Transform _cameraPosition;

    private bool _initialized;

    // ── Initialization ─────────────────────────────────────────────────────

    // Called by NetworkedPlayerController after instantiating the CameraHolder prefab
    // pass the player's CameraPos child transform so the CameraHolder snaps to the player's head position every frame
    public void Init(Transform cameraPosition)
    {
        _cameraPosition = cameraPosition;
        _initialized = true;
    }

    private void Start()
    {
        if (!_initialized)
            Debug.LogWarning("[MoveCamera] Init() was never called. " +
                             "Make sure NetworkedPlayerController calls Init() after instantiating the CameraHolder prefab.");
    }

    // Update is called once per frame
    void Update()
    {
        if (!_initialized) return;

        transform.position = _cameraPosition.position;
    }
}
