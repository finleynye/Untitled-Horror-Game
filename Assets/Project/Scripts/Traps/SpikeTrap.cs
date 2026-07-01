using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class SpikeTrap : NetworkBehaviour
{
    [SerializeField] private Collider triggerZone;
    private readonly HashSet<PlayerHealth> _trappedPlayers = new();

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInChildren<PlayerHealth>();
        if (player.IsDead) return;

        if (_trappedPlayers.Add(player) && _trappedPlayers.Count == 1)
            InvokeRepeating(nameof(TickDamage), 1, 1);
    }
    
    [ServerCallback]
    private void OnTriggerExit(Collider other)
    {
        var player = other.GetComponentInChildren<PlayerHealth>();
        _trappedPlayers.Remove(player);

        if (_trappedPlayers.Count == 0)
            CancelInvoke(nameof(TickDamage));
    }
    
    [Server]
    private void TickDamage()
    {
        foreach (var health in new List<PlayerHealth>(_trappedPlayers))
        {
            if (health.IsDead)
            {
                _trappedPlayers.Remove(health);
                continue;
            }

            health.ApplyDamage(1);
        }

        if (_trappedPlayers.Count == 0)
            CancelInvoke(nameof(TickDamage));
    }
}
