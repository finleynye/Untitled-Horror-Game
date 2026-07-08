using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.SceneManagement;

public enum PlayerRole
{
    Unassigned, //default role until host randomly assigns them/people manually assign them?? (maybe)
    Role1,
    Role2,
    Role3,
    Role4,
    Killer
}

public class PlayerController : NetworkBehaviour
{
    [SyncVar] public int connectionID;
    [SyncVar] public int playerID;
    [SyncVar] public ulong steamID;
    [SyncVar(hook = nameof(PlayerNameUpdate))] public string playerName;
    [SyncVar(hook = nameof(PlayerReady))] public bool ready;
    [SyncVar(hook = nameof(OnRoleChanged))] public PlayerRole role = PlayerRole.Unassigned;

    [SerializeField] private GameObject[] roles;

    private static bool InLobby => SceneManager.GetActiveScene().name == "Lobby";
    public event System.Action<string> OnNameChanged;
 
    private UHG_NetworkManager _manager;
    private UHG_NetworkManager Manager
    {
        get
        {
            if (_manager is not null)
                return _manager;
            return _manager = NetworkManager.singleton as UHG_NetworkManager;
        }
    }
 
    private void Start()
        => DontDestroyOnLoad(gameObject);
    
    public override void OnStartAuthority()
    {
        CmdSetPlayerName(SteamFriends.GetPersonaName());
 
        if (!InLobby) return;
        LobbyController.Instance.SetLocalPlayer(this);
        LobbyController.Instance.UpdateLobbyName();
    }
    
    public override void OnStartClient()
    {
        Manager.Players.Add(this);
        
        ApplyRoleObject(role);

        if (!InLobby) return;
        LobbyController.Instance.UpdateLobbyName();
        LobbyController.Instance.UpdateUserList();
    }
 
    public override void OnStopClient()
    {
        Manager.Players.Remove(this);
        if (!InLobby) return;
        LobbyController.Instance?.UpdateUserList();
    }
 
    [Command] 
    private void CmdSetPlayerName(string name) 
        => PlayerNameUpdate(playerName, name);
    
    [Command] 
    private void CmdSetPlayerReady() 
        => PlayerReady(ready, !ready);
    
    [Command] 
    private void CmdCanStartGame(string sceneName) 
        => Manager.StartGame(sceneName);
    
    public void ChangeReady()
    {
        if (isOwned)
            CmdSetPlayerReady();
    }
    
    private void PlayerNameUpdate(string oldName, string newName)
    {
        if (isServer)
            playerName = newName;
        if (isClient)
        {
            //tired of game objects being called LocalPlayer or PlayerRoot
            //so moving the name change here and setting it to player's steam name so i can tell whos who
            gameObject.name = playerName; 
            OnNameChanged?.Invoke(newName);
            if (InLobby)
                LobbyController.Instance.UpdateUserList();
        }
    }
    
    private void PlayerReady(bool oldReady, bool newReady)
    {
        if (isServer)
            ready = newReady;
        if (isClient && InLobby)
            LobbyController.Instance.UpdateUserList();
    }

    private void OnRoleChanged(PlayerRole oldRole, PlayerRole newRole)
    {
        if (isServer)
            role = newRole;
        
        ApplyRoleObject(newRole);

        if (isClient && InLobby)
            LobbyController.Instance.UpdateUserList();
    }

    private void ApplyRoleObject(PlayerRole newRole)
    {
        var targetRoleIndex = (int)newRole;
        for (var i = 0; i < roles.Length; i++)
        {
            if(roles[i] != null)
                roles[i].SetActive(i == targetRoleIndex);
        }
    }


    public GameObject GetCurrentRoleObject()
    {
        var targetRoleIndex = (int)role;

        if (roles != null && targetRoleIndex >= 0 && targetRoleIndex < roles.Length && roles[targetRoleIndex] != null)
            return roles[targetRoleIndex];

        if (roles == null)
            return null;

        foreach (var roleObject in roles)
        {
            if (roleObject != null && roleObject.activeInHierarchy)
                return roleObject;
        }

        return null;
    }

    public void CanStartGame(string sceneName)
    {
        if (isOwned)
            CmdCanStartGame(sceneName);
    }
}