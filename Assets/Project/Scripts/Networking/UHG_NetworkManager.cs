using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Steamworks;
using Mirror;
using UnityEngine.SceneManagement;

public class UHG_NetworkManager : NetworkManager
{
    [SerializeField] private PlayerController playerObj;
    [SerializeField] private string gameplaySceneName = "GrayBoxScene";
    public List<PlayerController> Players { get; } = new();

    public override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);
    }

    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
    {
        //i think it uses "LoadSceneAsync" so it starts loading the scene in background, meaning players will actively see scenes load and unload
        //so we can do some shit like adding a loading screen for transitions
        
        base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (SceneManager.GetActiveScene().name == "Lobby")
        {
            var steamPlayer = Instantiate(playerObj);
            steamPlayer.connectionID = conn.connectionId;
            steamPlayer.playerID = GetNextPlayerID();
            steamPlayer.steamID =
                (ulong)SteamMatchmaking.GetLobbyMemberByIndex((CSteamID)SteamLobby.Instance.lobbyID, Players.Count);

            NetworkServer.AddPlayerForConnection(conn, steamPlayer.gameObject);
        }
    }

    public void StartGame(string nextScene)
    {
        SteamLobby.Instance.SetLobbyPrivate();
        ServerChangeScene(nextScene);
    }

    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();

        if (SceneManager.GetActiveScene().name != gameplaySceneName) return;
        
        var localPlayer = NetworkClient.localPlayer;
        if (localPlayer == null) return;

        var movement = localPlayer.GetComponentInChildren<PlayerMovement>(); //gets first active
        movement?.ClientSetHubPosition();

        //happens after the scene has been changed, tells the server the client is ready to spawn objects here
    }

    private int GetNextPlayerID()
    {
        var id = 1;
        while (Players.Any(p => p.playerID == id))
            id++;

        return id;
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        //tells the server someone left, so can be used to clean up a player list or remove objects specific to said player
        //PartyPlaygrounds uses it to announce to everyone that the player left through a message announcement
        //probably gonna wanna save any data in this one, just to keep it clean
        
        base.OnServerDisconnect(conn);
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        //runs only on the client, so if the host leaves, they'll call server disconnect for themselves, but will call client disconnect for all other clients
        //probably gonna want a loading screen here too since a player disconnecting (not closing the game) just takes them to the offline scene (main menu)
    }

    public override void OnDestroy()
    {
        if (singleton == this)
            singleton = null;
 
        base.OnDestroy();
    }
}
