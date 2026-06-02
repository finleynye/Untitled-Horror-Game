using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;

public class SteamLobby : MonoBehaviour
{
    public static SteamLobby Instance;
    
    protected Callback<LobbyCreated_t> LobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> JoinRequested;
    protected Callback<LobbyEnter_t> LobbyEnter;
    protected Callback<LobbyMatchList_t> LobbyList;
    protected Callback<LobbyDataUpdate_t> LobbyDataUpdated;

    public ulong lobbyID; //the players own lobby ID
    public List<CSteamID> lobbyIDs = new(); //all IDs in the lobby browser thing if we end up doing that
    private const string HostAddr = "HostAddress";
    private static UHG_NetworkManager Manager => NetworkManager.singleton as UHG_NetworkManager;

    private bool _lobbyListRefreshing;
    private int _pendingLobbyRequests;

    private void Start()
    {
        Instance = this;

        //Steam's gonna use a txt file with a steamapp_id of 480, but if this project goes anywhere and actually makes it to Steam we'll get a proper ID,
        //so im gonna have to rewrite how this gets initialised
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("Steam App not found");
            return;
        }
        
        LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        JoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
        LobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        LobbyList = Callback<LobbyMatchList_t>.Create(OnGetLobbyList);
        LobbyDataUpdated = Callback<LobbyDataUpdate_t>.Create(OnGetLobbyData);
    }

    //server host presses this to create the lobby from main menu
    public void HostLobby()
    {
        //maybe before going straight into a lobby, a second menu pops up giving the host a "lobby settings" screen? 
        //that way they could make the lobby public or private before creating it, as well as giving them the ability to set their own player limit
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, Manager.maxConnections);
    }

    public void Quit()
    {
        Application.Quit();
    }
    
    public void SetLobbyPrivate()
        => SteamMatchmaking.SetLobbyType(new CSteamID(lobbyID), ELobbyType.k_ELobbyTypePrivate);

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK) return;
        if (NetworkServer.active || NetworkClient.active) return;

        if (!NetworkClient.isConnected)
            Manager.StartHost();
        
        //all developer games use the steam_appid "480", so if its a multiplayer game we can see EVERYONES lobbies.
        //gotta filter it out by using the gameID key
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "GameID", "UHG");
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddr, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "name", $"{SteamFriends.GetPersonaName()}'s Lobby");
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "maxPlayers", "8");
    }
    
    private void OnJoinRequested(GameLobbyJoinRequested_t callback)
        => SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    
    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        lobbyID = callback.m_ulSteamIDLobby;
        
        if (NetworkServer.active || NetworkClient.active) return;
        Manager.networkAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddr);
        Manager.StartClient();
    }
    
    private void OnGetLobbyList(LobbyMatchList_t callback)
    {
        lobbyIDs.Clear();
        LobbyBrowser.Instance.DestroyLobbies();
        _pendingLobbyRequests = (int)callback.m_nLobbiesMatching;

        if (_pendingLobbyRequests == 0)
        {
            _lobbyListRefreshing = false;
            return;
        }
        
        for (var i = 0; i < callback.m_nLobbiesMatching; i++)
        {
            var lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            lobbyIDs.Add(lobbyID);
            SteamMatchmaking.RequestLobbyData(lobbyID);
        }
    }
    
    private void OnGetLobbyData(LobbyDataUpdate_t callback)
    {
        if (!_lobbyListRefreshing) return;
        if(!lobbyIDs.Exists(id => id.m_SteamID == callback.m_ulSteamIDLobby)) return;
        
        LobbyBrowser.Instance.DisplayLobbies(lobbyIDs, callback);
        
        _pendingLobbyRequests--;
        if (_pendingLobbyRequests <= 0)
            _lobbyListRefreshing = false;
    }

    public void GetLobbyList()
    {
        _lobbyListRefreshing = true;
        
        //only get lobbies with "UHG" gameID
        SteamMatchmaking.AddRequestLobbyListStringFilter("GameID", "UHG", ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.RequestLobbyList();
    }

    public void JoinLobby(CSteamID lobbyID)
        => SteamMatchmaking.JoinLobby(lobbyID);
}
