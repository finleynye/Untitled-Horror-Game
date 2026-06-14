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

    [Header("Roles")] 
    [SerializeField] private GameObject survivorRole;
    [SerializeField] private GameObject killerRole;
    [SerializeField] private Renderer characterRenderer;
    [SerializeField] private Material role1Material;
    [SerializeField] private Material role2Material;
    [SerializeField] private Material role3Material;
    [SerializeField] private Material role4Material;
    [SerializeField] private Material killerMaterial;
    [SerializeField] private Material unassignedMaterial;

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
        gameObject.name = "LocalPlayer";
 
        if (!InLobby) return;
        LobbyController.Instance.SetLocalPlayer(this);
        LobbyController.Instance.UpdateLobbyName();
    }
    
    public override void OnStartClient()
    {
        Manager.Players.Add(this);

        ApplyRoleMaterial(role);
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

        ApplyRoleMaterial(newRole);
        ApplyRoleObject(newRole);

        if (isClient && InLobby)
            LobbyController.Instance.UpdateUserList();
    }

    private void ApplyRoleObject(PlayerRole newRole)
    {
        var isKiller = newRole == PlayerRole.Killer;
        var isSurvivor = newRole != PlayerRole.Killer && newRole != PlayerRole.Unassigned;
        
        killerRole.SetActive(isKiller);
        survivorRole.SetActive(isSurvivor);
    }
    
    //when go into game scene
    private void ApplyRoleMaterial(PlayerRole newRole)
    {
        if (characterRenderer == null)
            return;

        Material materialToUse = unassignedMaterial;

        switch (newRole)
        {
            case PlayerRole.Role1:
                materialToUse = role1Material;
                break;

            case PlayerRole.Role2:
                materialToUse = role2Material;
                break;

            case PlayerRole.Role3:
                materialToUse = role3Material;
                break;

            case PlayerRole.Role4:
                materialToUse = role4Material;
                break;

            case PlayerRole.Killer:
                materialToUse = killerMaterial;
                break;

            case PlayerRole.Unassigned:
            default:
                materialToUse = unassignedMaterial;
                break;
        }

        if (materialToUse != null)
            characterRenderer.sharedMaterial = materialToUse;
    }

    public void CanStartGame(string sceneName)
    {
        if (isOwned)
            CmdCanStartGame(sceneName);
    }
}