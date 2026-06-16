using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyController : MonoBehaviour
{
    public static LobbyController Instance;

    [Header("Lobby Info")]
    public TMP_Text lobbyName;
    public ulong lobbyID;

    [Header("User List")]
    public GameObject userViewContent;
    public GameObject userInfoPref;
    public bool userInfoCreated;

    [Header("Local Player")]
    public GameObject localPlayerObj;
    public PlayerController localPlayerController;

    [Header("Buttons")]
    public Button startGameBtn;
    public Button randomiseRolesBtn;

    [Header("Role Refresh UI")]
    public TMP_Text refreshCount;

    [Header("Lobby Player Visuals")]
    [SerializeField] private LobbyPodiumDisplay podiumDisplay;

    [Header("Start Game Warning")]
    [SerializeField] private TMP_Text startWarningText;
    [SerializeField] private float warningDisplayTime = 1.25f;

    [SerializeField] private UITextButton startUITextButton;

    private readonly List<UserInfo> _userInfos = new();
    private Coroutine warningRoutine;

    private static UHG_NetworkManager Manager => NetworkManager.singleton as UHG_NetworkManager;

    private void Awake()
    {
        Instance = this;
        //LoadingScreen.Instance?.Show();
    }

    public void SetLocalPlayer(PlayerController controller)
    {
        if (controller == null)
            return;

        localPlayerController = controller;
        localPlayerObj = controller.gameObject;

        bool isHost = IsLocalHost();

        if (randomiseRolesBtn != null)
            randomiseRolesBtn.gameObject.SetActive(isHost);

        if (startGameBtn != null)
            startGameBtn.gameObject.SetActive(isHost);

        UpdateStartButtonState();
        UpdateStartWarningText();
    }

    public void ReadyPlayer()
    {
        if (localPlayerController == null)
            return;

        localPlayerController.ChangeReady();
    }

    public void OnRandomiseClicked()
    {
        if (localPlayerController == null)
            return;

        if (!IsLocalHost())
            return;

        RoleManager.Instance?.TryAssignRoles();
    }

    public void UpdateRefreshButton(bool locked, int rerollsRemaining)
    {
        if (randomiseRolesBtn == null)
            return;

        bool isHost = IsLocalHost();

        randomiseRolesBtn.gameObject.SetActive(isHost);
        randomiseRolesBtn.interactable = isHost && !locked;

        if (refreshCount != null)
        {
            refreshCount.text = locked
                ? "Roles locked"
                : $"Refreshes left: {rerollsRemaining}";
        }

        UpdateStartButtonState();
        UpdateStartWarningText();

    }

    public void UpdateLobbyName()
    {
        if (Manager == null)
            return;

        SteamLobby steamLobby = Manager.GetComponent<SteamLobby>();

        if (steamLobby == null)
            return;

        lobbyID = steamLobby.lobbyID;

        if (lobbyName != null)
            lobbyName.text = SteamMatchmaking.GetLobbyData(new CSteamID(lobbyID), "name");
    }

    public void UpdateUserList()
    {
        if (Manager == null) return;
        if (!NetworkClient.isConnected) return;
        if (Manager.Players == null) return;
        if (userViewContent == null) return;
        if (userInfoPref == null) return;

        if (!userInfoCreated)
            CreateHostUserInfo();

        if (_userInfos.Count < Manager.Players.Count)
            CreateClientUserInfo();

        if (_userInfos.Count > Manager.Players.Count)
            RemoveUserInfo();

        if (_userInfos.Count == Manager.Players.Count)
            UpdateUserInfo();

        if (podiumDisplay != null)
            podiumDisplay.RefreshVisuals();

        //if (_userInfos.Count > 0)
        //LoadingScreen.Instance?.Hide();
    }

    private void CreateHostUserInfo()
    {
        foreach (PlayerController player in Manager.Players)
        {
            CreateUserInfo(player);
        }

        userInfoCreated = true;
    }

    private void CreateClientUserInfo()
    {
        foreach (PlayerController player in Manager.Players)
        {
            bool userAlreadyExists = _userInfos.Any(user => user.connectionID == player.connectionID);

            if (!userAlreadyExists)
                CreateUserInfo(player);
        }
    }

    private void CreateUserInfo(PlayerController player)
    {
        if (player == null)
            return;

        GameObject newPlayer = Instantiate(userInfoPref, userViewContent.transform);
        UserInfo newUser = newPlayer.GetComponent<UserInfo>();

        if (newUser == null)
            return;

        newUser.userName = string.IsNullOrEmpty(player.playerName) ? player.name : player.playerName;
        newUser.connectionID = player.connectionID;
        newUser.steamID = player.steamID;
        newUser.isReady = player.ready;
        newUser.playerRole = player.role;
        newUser.SetUserValues();

        newUser.transform.localScale = Vector3.one;

        _userInfos.Add(newUser);
    }

    private void UpdateUserInfo()
    {
        foreach (PlayerController player in Manager.Players)
        {
            UserInfo user = _userInfos.FirstOrDefault(info => info.connectionID == player.connectionID);

            if (user == null)
                continue;

            user.userName = player.playerName;
            user.isReady = player.ready;
            user.playerRole = player.role;
            user.SetUserValues();
        }

        UpdateStartButtonState();
        UpdateStartWarningText();
    }

    private void RemoveUserInfo()
    {
        List<UserInfo> usersToRemove = _userInfos
            .Where(userInfo => Manager.Players.All(player => player.connectionID != userInfo.connectionID))
            .ToList();

        foreach (UserInfo user in usersToRemove)
        {
            _userInfos.Remove(user);
            Destroy(user.gameObject);
        }

        RoleManager.Instance?.ResetRoles();

        UpdateStartButtonState();
        UpdateStartWarningText();
    }

    private void UpdateStartButtonState()
    {
        if (startGameBtn == null)
            return;

        if (localPlayerController == null)
        {
            startGameBtn.gameObject.SetActive(false);
            startGameBtn.interactable = false;

            if (startUITextButton != null)
                startUITextButton.SetTextVisualState(false);

            return;
        }

        bool isHost = IsLocalHost();
        bool everyoneReady = AllPlayersReady();
        bool everyoneHasRoles = AllPlayersHaveRoles();

        bool canStart = isHost && everyoneReady && everyoneHasRoles;

        startGameBtn.gameObject.SetActive(isHost);
        startGameBtn.interactable = canStart;

        if (startUITextButton != null)
            startUITextButton.SetTextVisualState(canStart);
    }

    private bool AllPlayersReady()
    {
        if (Manager == null) return false;
        if (Manager.Players == null) return false;
        if (Manager.Players.Count <= 0) return false;

        return Manager.Players.All(player => player != null && player.ready);
    }

    private bool AllPlayersHaveRoles()
    {
        if (Manager == null) return false;
        if (Manager.Players == null) return false;
        if (Manager.Players.Count <= 0) return false;

        return Manager.Players.All(player => player != null && player.role != PlayerRole.Unassigned);
    }

    public void StartGame(string sceneName)
    {
        if (localPlayerController == null)
            return;

        if (!IsLocalHost())
            return;

        if (!AllPlayersReady())
        {
            ShowStartWarning("All players must be ready.");
            return;
        }

        if (!AllPlayersHaveRoles())
        {
            ShowStartWarning("Assign roles before starting.");
            return;
        }

        localPlayerController.CanStartGame(sceneName);
        //AudioManager.Instance.Play(AudioManager.Instance.buttonClick);
    }

    private void UpdateStartWarningText()
    {
        if (startWarningText == null)
            return;

        if (!IsLocalHost())
        {
            startWarningText.text = "";
            startWarningText.gameObject.SetActive(false);
            return;
        }

        if (!AllPlayersReady())
        {
            startWarningText.gameObject.SetActive(true);
            startWarningText.text = "All players must be ready.";
            return;
        }

        if (!AllPlayersHaveRoles())
        {
            startWarningText.gameObject.SetActive(true);
            startWarningText.text = "Assign roles before starting.";
            return;
        }

        startWarningText.text = "";
        startWarningText.gameObject.SetActive(false);
    }

    private bool IsLocalHost()
    {
        return localPlayerController != null && localPlayerController.isOwned && NetworkServer.active;
    }

    private void ShowStartWarning(string message)
    {
        if (startWarningText == null)
            return;

        if (warningRoutine != null)
            StopCoroutine(warningRoutine);

        warningRoutine = StartCoroutine(ShowStartWarningRoutine(message));
    }

    private IEnumerator ShowStartWarningRoutine(string message)
    {
        startWarningText.gameObject.SetActive(true);
        startWarningText.text = message;

        yield return new WaitForSeconds(warningDisplayTime);

        warningRoutine = null;
        UpdateStartWarningText();
    }

    public void LeaveLobby()
    {
        if (lobbyID != 0)
        {
            CSteamID steamLobbyID = new CSteamID(lobbyID);

            SteamMatchmaking.SetLobbyType(steamLobbyID, ELobbyType.k_ELobbyTypePrivate);
            SteamMatchmaking.SetLobbyJoinable(steamLobbyID, false);
            SteamMatchmaking.LeaveLobby(steamLobbyID);
        }

        if (Manager != null)
        {
            if (NetworkServer.active && NetworkClient.isConnected)
                Manager.StopHost();
            else
                Manager.StopClient();
        }

        userInfoCreated = false;
        _userInfos.Clear();
        localPlayerController = null;
        localPlayerObj = null;
        lobbyID = 0;

        //AudioManager.Instance.Play(AudioManager.Instance.buttonClick);
    }

}
