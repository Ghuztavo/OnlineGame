using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Drives the Lobby screen UI built with Unity's UI Toolkit.
///
/// ── Quick Setup ──────────────────────────────────────────────────────────
///   1. Create a new UI Document asset (Project → Create → UI Toolkit → UI Document).
///   2. Open the UI Builder, copy the UXML structure from the comments below or
///      use the companion LobbyUI.uxml file.
///   3. Add a UIDocument component to the LobbyUI GameObject and assign the asset.
///   4. Attach this script to the same GameObject.
///   5. Make sure a <see cref="NetworkManager"/> GameObject is also in the Lobby scene.
///
/// ── Expected UXML element names (set in UI Builder) ──────────────────────
///   • "nickname-field"   — TextField
///   • "session-field"    — TextField
///   • "host-button"      — Button
///   • "join-button"      — Button
///   • "status-label"     — Label
///   • "lobby-container"  — VisualElement (root panel)
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class LobbyUI : MonoBehaviour
{
    // ── UXML element names ─────────────────────────────────────────────────
    private const string NicknameFieldName  = "nickname-field";
    private const string SessionFieldName   = "session-field";
    private const string HostButtonName     = "host-button";
    private const string JoinButtonName     = "join-button";
    private const string StatusLabelName    = "status-label";
    private const string LobbyContainerName = "lobby-container";

    // ── UI References ──────────────────────────────────────────────────────
    private TextField     _nicknameField;
    private TextField     _sessionField;
    private Button        _hostButton;
    private Button        _joinButton;
    private Label         _statusLabel;
    private VisualElement _lobbyContainer;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _nicknameField  = root.Q<TextField>(NicknameFieldName);
        _sessionField   = root.Q<TextField>(SessionFieldName);
        _hostButton     = root.Q<Button>(HostButtonName);
        _joinButton     = root.Q<Button>(JoinButtonName);
        _statusLabel    = root.Q<Label>(StatusLabelName);
        _lobbyContainer = root.Q<VisualElement>(LobbyContainerName);

        // Restore last-used values from PlayerPrefs.
        _nicknameField.value = PlayerPrefs.GetString("Nickname", "Player");
        _sessionField.value  = PlayerPrefs.GetString("LastSession", "my-room");

        _hostButton.clicked += OnHostClicked;
        _joinButton.clicked += OnJoinClicked;

        SetStatus("Enter a session name and click Host or Join.");
    }

    private void OnDisable()
    {
        _hostButton.clicked -= OnHostClicked;
        _joinButton.clicked -= OnJoinClicked;
    }

    // ── Button Handlers ────────────────────────────────────────────────────

    private void OnHostClicked()
    {
        if (!ValidateInputs(out string nickname, out string session)) return;

        SetStatus("Starting host session…");
        SetButtonsInteractable(false);
        NetworkManager.Instance.StartHost(session, nickname);
    }

    private void OnJoinClicked()
    {
        if (!ValidateInputs(out string nickname, out string session)) return;

        SetStatus($"Joining session '{session}'…");
        SetButtonsInteractable(false);
        NetworkManager.Instance.JoinSession(session, nickname);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private bool ValidateInputs(out string nickname, out string session)
    {
        nickname = _nicknameField.value.Trim();
        session  = _sessionField.value.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            SetStatus("Please enter a nickname.");
            nickname = session = null;
            return false;
        }

        if (string.IsNullOrEmpty(session))
        {
            SetStatus("Please enter a session name.");
            nickname = session = null;
            return false;
        }

        // Persist for next time.
        PlayerPrefs.SetString("Nickname",    nickname);
        PlayerPrefs.SetString("LastSession", session);
        PlayerPrefs.Save();
        return true;
    }

    private void SetStatus(string message)
    {
        if (_statusLabel != null)
            _statusLabel.text = message;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        _hostButton.SetEnabled(interactable);
        _joinButton.SetEnabled(interactable);
    }
}
