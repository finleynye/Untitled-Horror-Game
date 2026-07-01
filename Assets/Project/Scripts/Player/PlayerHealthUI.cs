using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    private PlayerHealth _playerHealth;

    private void Start()
        => FindCorrectPlayer();

    private void FindCorrectPlayer()
    {
        healthSlider = GetComponent<Slider>();
        
        foreach (var health in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
        {
            if (!health.isOwned) continue;

            _playerHealth = health;
            _playerHealth.OnHealthUpdated += UpdateSlider;

            UpdateSlider(health.Health, health.MaxHealth);
            return;
        }
    }

    private void UpdateSlider(float current, float max)
    {
        var t = max > 0f ? current / max : 0f;
        healthSlider.value = t;
        
        //hide the fill area thing at 0hp cause idk how to remove the small sliver
        //uncomment ts to see what i mean VVV
        healthSlider.fillRect.gameObject.SetActive(current > 0f);
    }
    
    private void OnDestroy()
        => _playerHealth.OnHealthUpdated -= UpdateSlider;
}
