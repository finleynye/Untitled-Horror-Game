using UnityEngine;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;

public class PlayerStamina : NetworkBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    public float currentStamina = 100f;

    [Header("Drain & Regen")]
    [SerializeField] private float staminaDrainRate = 25f;
    [SerializeField] private float staminaRegenRate = 20f;
    [SerializeField] private float staminaRegenDelay = 1f;

    [Header("Recovery State Rules")]
    [SerializeField] private float staminaUseRecoveryThreshold = 10f;

    [Header("Stamina Audio")]
    [SerializeField] private AudioSource staminaAudioSource;
    [SerializeField] private AudioClip staminaExhaustClip;
    [SerializeField] private float staminaExhaustVolume = 1f;
    [SerializeField] private float staminaExhaustSoundCooldown = 1.5f;

    private float regenTimer = 0f;
    private float staminaExhaustSoundTimer = 0f;

    public bool isUsingStamina = false;
    public bool isStaminaEmpty = false;
    public bool canUseStamina = true;

    public bool IsStaminaEmpty => isStaminaEmpty;
    public bool CanUseStamina => canUseStamina;
    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;

    private void Start()
    {
        currentStamina = maxStamina;
    }

    public override void OnStartLocalPlayer()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Update()
    {
        if (!isOwned) return;

        if (staminaExhaustSoundTimer > 0f)
            staminaExhaustSoundTimer -= Time.deltaTime;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!isOwned) return;
        if (scene.name != "Game") return;
    }

    public bool TickStamina(bool shouldUseStamina)
    {
        if (!isOwned) return false;

        bool becameExhaustedThisFrame = false;

        if (shouldUseStamina && canUseStamina)
        {
            UseStamina();

            if (currentStamina <= 0f && !isStaminaEmpty)
            {
                currentStamina = 0f;
                isStaminaEmpty = true;
                canUseStamina = false;
                becameExhaustedThisFrame = true;

                PlayStaminaExhaustSound();
            }
        }
        else
        {
            StopUsingStamina();
            HandleStaminaRegen();
        }

        CheckStaminaState();

        return becameExhaustedThisFrame;
    }

    private void UseStamina()
    {
        isUsingStamina = true;
        regenTimer = 0f;

        currentStamina -= staminaDrainRate * Time.deltaTime;
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
    }

    private void StopUsingStamina()
    {
        isUsingStamina = false;
    }

    private void HandleStaminaRegen()
    {
        if (isUsingStamina)
            return;

        regenTimer += Time.deltaTime;

        if (regenTimer >= staminaRegenDelay)
            RegenStamina();
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
        if (currentStamina > staminaUseRecoveryThreshold)
        {
            isStaminaEmpty = false;
            canUseStamina = true;
        }
    }

    private void PlayStaminaExhaustSound()
    {
        if (staminaExhaustSoundTimer > 0f)
            return;

        if (staminaAudioSource == null)
            return;

        if (staminaExhaustClip == null)
            return;

        //set cooldown before playing so this cannot be called twice in the same moment
        staminaExhaustSoundTimer = staminaExhaustSoundCooldown;

        //stop any existing stamina exhaust sound before replaying it
        staminaAudioSource.Stop();

        staminaAudioSource.clip = staminaExhaustClip;
        staminaAudioSource.volume = staminaExhaustVolume;
        staminaAudioSource.loop = false;
        staminaAudioSource.Play();
    }

    public override void OnStopLocalPlayer()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}