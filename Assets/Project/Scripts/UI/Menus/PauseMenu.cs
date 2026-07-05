using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : NetworkBehaviour
{
    private PlayerMovement _playerMovement;
    private PlayerInput _playerInput;
    private GameObject _pauseMenuPanel;
    private bool _isPaused;

    private static readonly string[] ExcludedScenes = { "MainMenu", "Lobby" };

    private void Awake()
    {
        _playerInput = new PlayerInput();
        _playerInput.Player.Pause.performed += _ => TogglePause();
        _playerInput.Enable();
        

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (System.Array.IndexOf(ExcludedScenes, scene.name) >= 0) return;
        if (!isOwned) return;

        _isPaused = false;
        _playerMovement = NetworkClient.localPlayer?.GetComponentInChildren<PlayerMovement>();
        
        foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (obj.name != "PauseMenu" || obj.scene.name != scene.name) continue;
            _pauseMenuPanel = obj;
            break;
        }

        _pauseMenuPanel.SetActive(false);
        _pauseMenuPanel.transform.Find("ResumeBtn")?.GetComponent<Button>()?.onClick.AddListener(TogglePause);
        _pauseMenuPanel.transform.Find("LeaveBtn")?.GetComponent<Button>()?.onClick.AddListener(LeaveGame);
    }

    private void TogglePause()
    {
        if (!isOwned) return;
        _isPaused = !_isPaused;

        _pauseMenuPanel.SetActive(_isPaused);
        Cursor.lockState = _isPaused ? CursorLockMode.None : CursorLockMode.Locked;

        Debug.Log(_isPaused);
        Debug.Log(Cursor.lockState.ToString());
        Cursor.visible = _isPaused;
        _playerMovement.isPaused = _isPaused;
    }

    private void LeaveGame()
    {
        _isPaused = false;
        _playerMovement.isPaused = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        LobbyController.Instance.LeaveLobby();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        _playerInput?.Disable();
    }

    private void OnEnable()  => _playerInput?.Enable();
    private void OnDisable() => _playerInput?.Disable();
}