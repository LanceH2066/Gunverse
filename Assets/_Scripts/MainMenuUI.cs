using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the Main Menu canvas.
///
/// Canvas hierarchy expected:
///   MainMenuCanvas
///     MainPanel
///       HostButton        (Button)
///       JoinButton        (Button)
///     JoinPanel           (shown when Join is clicked)
///       CodeInputField    (TMP_InputField)
///       ConfirmButton     (Button)
///       BackButton        (Button)
///     StatusPanel
///       StatusText        (TMP_Text)   ← spinner / error messages
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    // ── Inspector wiring ─────────────────────────────────────────────────────
    [Header("Panels")]
    [SerializeField] private GameObject _mainPanel;
    [SerializeField] private GameObject _joinPanel;
    [SerializeField] private GameObject _statusPanel;

    [Header("Main panel")]
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _joinButton;

    [Header("Join panel")]
    [SerializeField] private TMP_InputField _codeInput;
    [SerializeField] private Button         _confirmButton;
    [SerializeField] private Button         _backButton;

    [Header("Status")]
    [SerializeField] private TMP_Text _statusText;

    // ─────────────────────────────────────────────────────────────────────────
    #region Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Wire buttons
        _hostButton.onClick.AddListener(OnHostClicked);
        _joinButton.onClick.AddListener(OnJoinClicked);
        _confirmButton.onClick.AddListener(OnConfirmJoinClicked);
        _backButton.onClick.AddListener(OnBackClicked);

        // Force uppercase and strip spaces as the player types
        _codeInput.onValueChanged.AddListener(v =>
            _codeInput.SetTextWithoutNotify(v.ToUpper().Replace(" ", ""))
        );

        // Subscribe to NetworkManager events
        NetworkManager.Instance.OnSessionStarted += HandleSessionStarted;
        NetworkManager.Instance.OnSessionFailed  += HandleSessionFailed;

        // Initial state
        ShowMain();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance == null) return;
        NetworkManager.Instance.OnSessionStarted -= HandleSessionStarted;
        NetworkManager.Instance.OnSessionFailed  -= HandleSessionFailed;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Button handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void OnHostClicked()
    {
        ShowStatus("Creating session…");
        NetworkManager.Instance.HostGame();
    }

    private void OnJoinClicked()
    {
        ShowJoin();
    }

    private void OnConfirmJoinClicked()
    {
        string code = _codeInput.text.Trim();

        if (code.Length != 6)
        {
            SetStatus("Room code must be 6 characters.", isError: true);
            return;
        }

        ShowStatus($"Joining {code}…");
        NetworkManager.Instance.JoinGame(code);
    }

    private void OnBackClicked()
    {
        ShowMain();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region NetworkManager event handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleSessionStarted()
    {
        SetStatus("Loading lobby…", isError: false);
        SetButtonsInteractable(false);
        
        // Destroy the entire canvas so it cannot interfere with lobby UI
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            Destroy(canvas.gameObject);
        else
            Destroy(gameObject);
    }

    private void HandleSessionFailed(string error)
    {
        SetStatus(error, isError: true);
        ShowMain();          // go back so the player can retry
        SetButtonsInteractable(true);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Panel helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void ShowMain()
    {
        _mainPanel.SetActive(true);
        _joinPanel.SetActive(false);
        _statusPanel.SetActive(false);
        SetButtonsInteractable(true);
    }

    private void ShowJoin()
    {
        _mainPanel.SetActive(false);
        _joinPanel.SetActive(true);
        _statusPanel.SetActive(false);
        _codeInput.text = string.Empty;
        _codeInput.ActivateInputField();
    }

    private void ShowStatus(string message)
    {
        _mainPanel.SetActive(false);
        _joinPanel.SetActive(false);
        _statusPanel.SetActive(true);
        SetStatus(message, isError: false);
        SetButtonsInteractable(false);
    }

    private void SetStatus(string message, bool isError)
    {
        if (_statusText == null) return;
        _statusText.text  = message;
        _statusText.color = isError
            ? new Color(1f, 0.35f, 0.35f)   // soft red
            : Color.white;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        _hostButton.interactable    = interactable;
        _joinButton.interactable    = interactable;
        _confirmButton.interactable = interactable;
    }

    #endregion
}