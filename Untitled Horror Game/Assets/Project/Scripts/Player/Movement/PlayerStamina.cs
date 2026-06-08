using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina Settings")]
    public float maxStamina = 100f; //max stamina allowed
    public float currentStamina = 100f; //current stamina value

    [Header("Drain & Regen")]
    [SerializeField] private float staminaDrainRate = 25f; //how fast stamina drains while sprinting
    [SerializeField] private float staminaRegenRate = 20f; //how fast stamina comes back
    [SerializeField] private float staminaRegenDelay = 1f; //how long before stamina starts regenerating

    [Header("Stamina States")]
    public bool isUsingStamina = false;
    public bool isStaminaEmpty = false;
    public bool canUseStamina = true;

    private float regenTimer = 0f;

    private void Start()
    {
        currentStamina = maxStamina;
    }

    private void Update()
    {
        if (!isUsingStamina)
        {
            regenTimer += Time.deltaTime;

            if (regenTimer >= staminaRegenDelay)
                RegenStamina();
        }

        CheckStaminaState();
    }

    public void UseStamina()
    {
        if (!canUseStamina)
            return;

        isUsingStamina = true;
        regenTimer = 0f;

        currentStamina -= staminaDrainRate * Time.deltaTime;
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

        if (currentStamina <= 0f)
        {
            currentStamina = 0f;
            isStaminaEmpty = true;
            canUseStamina = false;

            Debug.Log("Stamina is empty");
        }

        //Debug.Log("Using Stamina: " + currentStamina);
    }

    public void StopUsingStamina()
    {
        isUsingStamina = false;
    }

    private void RegenStamina()
    {
        if (currentStamina >= maxStamina)
            return;

        currentStamina += staminaRegenRate * Time.deltaTime;
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
    }

    private void CheckStaminaState()
    {
        if (currentStamina > 10f)
        {
            isStaminaEmpty = false;
            canUseStamina = true;
        }
    }

    public float GetStaminaPercent()
    {
        return currentStamina / maxStamina;
    }
}