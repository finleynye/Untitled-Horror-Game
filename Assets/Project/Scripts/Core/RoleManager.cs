using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RoleManager : MonoBehaviour
{
    public static RoleManager Instance;

    public int maxRefreshes;
    
    private int _currentRefreshes;
    private bool _rolesLocked;
    
    private static UHG_NetworkManager Manager => NetworkManager.singleton as UHG_NetworkManager;
    public bool RolesLocked => _rolesLocked;
    public int RerollsRemaining => Mathf.Max(0, maxRefreshes - _currentRefreshes);
    
    private void Awake()
        => Instance = this;

    public bool TryAssignRoles()
    {
        //this script hella ugly, maybe try to refactor later on
        //idk what i was thinking with ts
        //genuinely buns im so sorry for this monstrosity
        if (!NetworkServer.active) return false;
        if (_rolesLocked) return false;

        var players = Manager.Players;
        if (players.Count <= 0) return false;

        if (players.Count == 1)
        {
            players[0].role = PlayerRole.Killer;
            _rolesLocked = true;
            LobbyController.Instance?.UpdateRefreshButton(_rolesLocked, 0);
            return true;
        }
        
        //shuffle copy of the player list
        var shuffled = new List<PlayerController>(players);
        for (var i = shuffled.Count - 1; i > 0; i--)
        {
            var j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        //shuffle survivor roles around
        var survivorRoles = new List<PlayerRole>
        {
            PlayerRole.Role1,
            PlayerRole.Role2,
            PlayerRole.Role3,
            PlayerRole.Role4
        };

        for (var i = survivorRoles.Count - 1; i > 0; i--)
        {
            var j = Random.Range(0, i + 1);
            (survivorRoles[i], survivorRoles[j]) = (survivorRoles[j], survivorRoles[i]);
        }
        
        //ROLES ASSIGNED HERE VVV
        shuffled[0].role = PlayerRole.Killer;
        
        for (var i = 1; i < shuffled.Count; i++)
            shuffled[i].role = survivorRoles[(i - 1) % survivorRoles.Count];

        _currentRefreshes++;

        if (_currentRefreshes >= maxRefreshes)
            _rolesLocked = true;
        
        LobbyController.Instance?.UpdateRefreshButton(_rolesLocked, maxRefreshes - _currentRefreshes);
        return true;
    }
    
    //reset roles if someone leaves the lobby (user might've been the killer)
    public void ResetRoles()
    {
        if (!NetworkServer.active) return;

        _currentRefreshes = 0;
        _rolesLocked = false;

        foreach (var player in Manager.Players)
            player.role = PlayerRole.Unassigned;

        LobbyController.Instance?.UpdateRefreshButton(false, maxRefreshes);
    }
}
