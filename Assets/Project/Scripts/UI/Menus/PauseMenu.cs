using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : NetworkBehaviour
{
    private PlayerMovement _playerMovement;
    private PlayerInput _playerInput;
    public GameObject _pauseMenuPanel;
    private bool _isPaused;
    private static readonly string[] ExcludedScenes = { "Main Menu", "MainMenu", "Lobby" };

    private void Awake()
    {
        _playerInput = new PlayerInput();
        _playerInput.Player.Pause.performed += _ => TogglePause();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        _playerInput?.Enable();
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (System.Array.IndexOf(ExcludedScenes, scene.name) >= 0)
            return;

        if (netIdentity == null || !isOwned)
            return;

        _isPaused = false;
        _playerMovement = NetworkClient.localPlayer?.GetComponentInChildren<PlayerMovement>();

        _pauseMenuPanel = null;

        foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (obj == null)
                continue;

            if (!obj.scene.IsValid())
                continue;

            if (obj.scene.name != scene.name)
                continue;

            if (obj.name != "PauseMenu")
                continue;

            _pauseMenuPanel = obj;
            break;
        }

        if (_pauseMenuPanel == null)
            return;
        
        _pauseMenuPanel.SetActive(false);

        Transform resume = _pauseMenuPanel.transform.Find("Menu Buttons/ResumeBtn");
        Transform leave = _pauseMenuPanel.transform.Find("Menu Buttons/LeaveBtn");

        if (resume != null && resume.TryGetComponent(out Button resumeButton))
        {
            resumeButton.onClick.RemoveListener(TogglePause);
            resumeButton.onClick.AddListener(TogglePause);
        }

        if (leave != null && leave.TryGetComponent(out Button leaveButton))
        {
            leaveButton.onClick.RemoveListener(LeaveGame);
            leaveButton.onClick.AddListener(LeaveGame);
        }
    }

    private void TogglePause()
    {
        if (netIdentity == null || !isOwned)
            return;

        if (_pauseMenuPanel == null)
        {
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

            if (_pauseMenuPanel == null)
                return;
        }

        if (_playerMovement == null)
            _playerMovement = NetworkClient.localPlayer?.GetComponentInChildren<PlayerMovement>();

        if (_playerMovement == null)
            return;
        
        _isPaused = !_isPaused;

        _pauseMenuPanel.SetActive(_isPaused);
        Cursor.lockState = _isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = _isPaused;
        _playerMovement.isPaused = _isPaused;
    }

    private void LeaveGame()
    {
        if (netIdentity == null || !isOwned)
            return;

        _isPaused = false;

        if (_playerMovement != null)
            _playerMovement.isPaused = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (LobbyController.Instance != null)
        {
            LobbyController.Instance.LeaveLobby();
            return;
        }

        //fallback for when we are in the game scene and LobbyController no longer exists
        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();
        
        else if (NetworkServer.active)
            NetworkManager.singleton.StopServer();
        
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (_playerInput != null)
        {
            _playerInput.Disable();
            _playerInput.Dispose();
        }
    }
}