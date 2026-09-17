using Fusion;
using UnityEngine;


//Flat input struct transmitted from each client to the server every simulation tick.

public struct NetworkInputData : INetworkInput
{
    // ── Movement ────────────────────────────────────────────────────────────
    // Normalised WASD / stick axes (x = horizontal, y = vertical).
    public Vector2 MoveDirection;

    // ── Look ────────────────────────────────────────────────────────────────
    // Horizontal camera / player yaw angle.
    public float CameraYaw;

    // Vertical camera pitch angle clamped to +/-90 degrees.
    public float CameraPitch;

    // ── Action buttons ───────────────────────────────────────────────────────
    public NetworkButtons Buttons;

    // Button indices
    public const int BUTTON_JUMP    = 0;
    public const int BUTTON_SPRINT  = 1;
    public const int BUTTON_CROUCH  = 2;
    public const int BUTTON_FIRE    = 3;
}
