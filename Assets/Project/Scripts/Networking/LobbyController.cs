using System.Collections.Generic;
using System.Linq;
using Mirror;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyController : MonoBehaviour
{
    public GameObject rolePrefab;
    
    public static LobbyController Instance;

    public TMP_Text lobbyName;
    public GameObject userViewContent;
    public GameObject userInfoPref;
    public GameObject localPlayerObj;

    public ulong lobbyID;
    public bool userInfoCreated;
    private List<UserInfo> _userInfos = new();
    public PlayerController localPlayerController;
    
    public Button startGameBtn;
    public Button randomiseRolesBtn;
    
    public TMP_Text refreshCount;

    [Header("Lobby Player Visuals")]
    [SerializeField] private LobbyPodiumDisplay podiumDisplay;
    private static UHG_NetworkManager Manager => NetworkManager.singleton as UHG_NetworkManager;
 
    private void Awake()
    {
        Instance = this;
        // LoadingScreen.Instance?.Show();
    }
    
    public void ReadyPlayer()
    {
        localPlayerController.ChangeReady();
    }

    /*private void UpdateButton()
        => readyBtn.text = localPlayerController.ready ? "Unready" : "Ready";*/
    
    private void IsEveryoneReady()
    {
        if (localPlayerController == null) return;
        
        var isEveryoneReady = false;
        foreach (var player in Manager.Players)
        {
            if(player.ready)
                isEveryoneReady = true;
            else 
            {
                isEveryoneReady = false;
                break;
            }
        }
 
        //show start button for host only
        if (localPlayerController.playerID == 1)
        {
            startGameBtn.gameObject.SetActive(true);
            startGameBtn.interactable = isEveryoneReady;
        }
        else startGameBtn.gameObject.SetActive(false);
    }

    public void OnRandomiseClicked()
    {
        if (localPlayerController == null) return;
        if (localPlayerController.playerID != 1) return; //only host can press

        RoleManager.Instance?.TryAssignRoles();
    }
    
    public void UpdateRefreshButton(bool locked, int rerollsRemaining)
    {
        if (randomiseRolesBtn == null) return;
        
        var isHost = localPlayerController.playerID == 1;
        randomiseRolesBtn.gameObject.SetActive(isHost);
        randomiseRolesBtn.interactable = isHost && !locked;
        
        refreshCount.text = locked
            ? "Roles locked"
            : $"Refreshes left: {rerollsRemaining}";
    }
    
    public void UpdateLobbyName()
    {
        if (Manager == null) return;
        var steamLobby = Manager.GetComponent<SteamLobby>();
        if (steamLobby == null) return;
    
        lobbyID = steamLobby.lobbyID;
        lobbyName.text = SteamMatchmaking.GetLobbyData(new CSteamID(lobbyID), "name");
    }
    
    public void UpdateUserList()
    {
        //one of these has always gone wrong :(
        if (Manager == null) return;
        if (!NetworkClient.isConnected) return;
        if (Manager.Players == null) return;
        if (userViewContent == null) return;
    
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
        //unhide loading screen once you join the lobby
        /*if (_userInfos.Count > 0)
            LoadingScreen.Instance?.Hide();*/
    }
    
    public void SetLocalPlayer(PlayerController controller)
    {
        localPlayerController = controller;
        localPlayerObj = controller.gameObject;
        
        //hide buttons for other clients
        //they dont deserve luxuries
        var isHost = controller.playerID == 1;
        randomiseRolesBtn.gameObject.SetActive(isHost);
        startGameBtn.gameObject.SetActive(isHost);
    }

    private void CreateHostUserInfo()
    {
        foreach (var player in Manager.Players)
        {
            var newPlayer = Instantiate(userInfoPref);
            var newUser = newPlayer.GetComponent<UserInfo>();
            
            newUser.userName = player.name;
            newUser.connectionID = player.connectionID;
            newUser.steamID = player.steamID;
            newUser.isReady = player.ready;
            newUser.SetUserValues();
            
            newUser.transform.SetParent(userViewContent.transform);
            newUser.transform.localScale = Vector3.one;
            _userInfos.Add(newUser);
        }
        
        userInfoCreated = true;
    }

    private void CreateClientUserInfo()
    {
        foreach (var player in Manager.Players)
        {
            if (_userInfos.All(b => b.connectionID != player.connectionID))
            {
                var newPlayer = Instantiate(userInfoPref);
                var newUser = newPlayer.GetComponent<UserInfo>();
            
                newUser.userName = player.playerName;
                newUser.connectionID = player.connectionID;
                newUser.steamID = player.steamID;
                newUser.isReady = player.ready;
                newUser.SetUserValues();
            
                newUser.transform.SetParent(userViewContent.transform);
                newUser.transform.localScale = Vector3.one;
                _userInfos.Add(newUser);
            }
        }
    }

    private void UpdateUserInfo()
    {
        foreach (var player in Manager.Players)
        {
            foreach (var user in _userInfos.Where(user => user.connectionID == player.connectionID))
            {
                user.userName = player.playerName;
                user.isReady = player.ready;
                user.playerRole = player.role;
                user.SetUserValues();
 
                /*if (player == localPlayerController)
                    UpdateButton();*/
            }
        }
        
        IsEveryoneReady();
    }

    private void RemoveUserInfo()
    {
        var userInfoToRemove = _userInfos.Where(userInfo => Manager.Players.All(b => b.connectionID != userInfo.connectionID)).ToList();
 
        if (userInfoToRemove.Count > 0)
        {
            foreach (var user in userInfoToRemove)
            {
                var objToRemove = user.gameObject;
                _userInfos.Remove(user);
                Destroy(objToRemove);
                objToRemove = null;
            }
        }
        
        RoleManager.Instance?.ResetRoles();
    }
 
    public void StartGame(string sceneName)
    {
        localPlayerController.CanStartGame(sceneName);
        //AudioManager.Instance.Play(AudioManager.Instance.buttonClick);
        
        //get all players, their roles, and assign a roleprefabobj
    }
 
    public void LeaveLobby()
    {
        if (lobbyID != 0)
        {
            SteamMatchmaking.SetLobbyType(new CSteamID(lobbyID), ELobbyType.k_ELobbyTypePrivate);
            SteamMatchmaking.SetLobbyJoinable(new CSteamID(lobbyID), false);
        }
        
        SteamMatchmaking.LeaveLobby(new CSteamID(lobbyID));
        
        if (NetworkServer.active && NetworkClient.isConnected)
            Manager.StopHost();
        else
            Manager.StopClient();
 
        userInfoCreated = false;
        _userInfos.Clear();
        localPlayerController = null;
        localPlayerObj = null;
        lobbyID = 0;
        
        //AudioManager.Instance.Play(AudioManager.Instance.buttonClick);
    }
}
