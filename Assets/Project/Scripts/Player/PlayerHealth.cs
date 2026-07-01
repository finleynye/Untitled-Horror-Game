using UnityEngine;
using Mirror;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private float maxHealth;
    [SyncVar(hook = nameof(OnHealthChanged))] private float _health;
    public float Health => _health;
    public float MaxHealth => maxHealth;
    public bool IsDead => _health <= 0f;
    
    public event System.Action<float, float> OnHealthUpdated;
    
    public override void OnStartServer()
        => _health = maxHealth;
    
    [Server]
    public void ApplyDamage(float amount)
    {
        if (IsDead) return;

        _health = Mathf.Clamp(_health - amount, 0f, maxHealth);
        TargetUpdateHealthUI(connectionToClient, _health, maxHealth);

        if (IsDead)
            ServerHandleDeath();
    }
    
    [Server]
    public void ApplyHealing(float amount)
    {
        if (IsDead) return;

        _health = Mathf.Clamp(_health + amount, 0f, maxHealth);
        TargetUpdateHealthUI(connectionToClient, _health, maxHealth);
    }
    
    [TargetRpc]
    private void TargetUpdateHealthUI(NetworkConnectionToClient conn, float current, float max)
    {
        //update health UI stuff here please fin x
        OnHealthUpdated?.Invoke(current, max);
        Debug.Log($"health: {current}/{max}");
    }
    
    private void OnHealthChanged(float _, float newHealth)
        => TargetUpdateHealthUI(connectionToClient, newHealth, maxHealth);
    
    [Server]
    private void ServerHandleDeath()
    {
        var movement = GetComponent<PlayerMovement>();
        movement.TargetSetFree(connectionToClient);
        
        //TODO: trigger death animation, ragdoll, spectator mode or whatever idrk
        Debug.LogError($"{GetComponentInParent<PlayerController>()?.playerName} died.");
    }
}