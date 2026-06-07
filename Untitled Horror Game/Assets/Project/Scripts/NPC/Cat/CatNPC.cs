using Mirror;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class CatNPC : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource catAudioSource;

    [Header("wander settings")]
    [SerializeField] private float wanderRadius = 8f;
    [SerializeField] private float waitTimeMin = 2f;
    [SerializeField] private float waitTimeMax = 5f;
    [SerializeField] private float stoppingDistance = 0.4f;

    [Header("Sitting Settings")]
    [SerializeField] private float sitChance = 0.45f;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float turnSpeed = 8f;

    [Header("Cat Audio")]
    [SerializeField] private AudioClip meowClip;
    [SerializeField] private AudioClip purrClip;
    [SerializeField] private float audioVolume = 0.7f;
    [SerializeField] private float soundTimeMin = 5f;
    [SerializeField] private float soundTimeMax = 14f;

    [Header("Footstep Audio")]
    [SerializeField] private AudioClip footstepClip;
    [SerializeField] private float footstepVolume = 0.4f;

    private float waitTimer;
    private float soundTimer;

    [SyncVar] private float syncedSpeed;
    [SyncVar] private bool isSitting;
    private bool isWaiting;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (catAudioSource == null)
            catAudioSource = GetComponent<AudioSource>();
    }

    public override void OnStartServer()
    {
        //only the server controls the cat brain
        agent.enabled = true;
        agent.speed = walkSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.updateRotation = false;

        ResetSoundTimer();
        PickNewDestination();
    }

    public override void OnStartClient()
    {
        //clients receive the car movement through NetworkTransform
        //the server is the only object that should calculate navmesh movement
        if (!isServer && agent != null)
            agent.enabled = false;
    }

    private void Update()
    {
        if (isServer)
        {
            HandleMovement();
            HandleRotation();
            HandleRandomSound();
            UpdateServerAnimationState();
        }

        HandleAnimation();
    }

    [Server]
    //random navigation
    private void HandleMovement()
    {
        if (agent == null)
            return;

        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0f)
            {
                StopWaiting();
                PickNewDestination();
            }

            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= stoppingDistance)
            StartWaiting();
    }

    [Server]
    //picks a random direction from its wander radius
    private void PickNewDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }
        else
            StartWaiting();
        
    }

    [Server]
    //waits between a random time (2s to 5s)
    private void StartWaiting()
    {
        isWaiting = true;
        waitTimer = Random.Range(waitTimeMin, waitTimeMax);

        if (agent != null)
            agent.isStopped = true;

        //sometimes sit when the cat stops
        isSitting = Random.value <= sitChance;
    }

    [Server]
    private void StopWaiting()
    {
        isWaiting = false;
        isSitting = false;

        if (agent != null)
            agent.isStopped = false;
    }

    [Server]
    private void HandleRotation()
    {
        if (agent == null)
            return;

        if (isWaiting)
            return;

        if (agent.velocity.sqrMagnitude <= 0.01f)
            return;

        Vector3 direction = agent.velocity.normalized;
        direction.y = 0f;

        if (direction == Vector3.zero)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    [Server]
    private void UpdateServerAnimationState()
    {
        if (agent == null)
            return;

        if (isWaiting || isSitting)
        {
            syncedSpeed = 0f;
            return;
        }

        //normalises the navmesh speed into a 0 to 1 blend tree value
        syncedSpeed = agent.velocity.magnitude / walkSpeed;
        syncedSpeed = Mathf.Clamp01(syncedSpeed);
    }


    private void HandleAnimation()
    {
        if (animator == null)
            return;

        animator.SetFloat("Speed", syncedSpeed);
        animator.SetBool("isSitting", isSitting);
    }

    [Server]
    private void HandleRandomSound()
    {
        soundTimer -= Time.deltaTime;

        if (soundTimer > 0f)
            return;

        if (isSitting)
            RpcPlayPurr();
        else
            RpcPlayMeow();

        ResetSoundTimer();
    }

    [Server]
    private void ResetSoundTimer()
    {
        soundTimer = Random.Range(soundTimeMin, soundTimeMax);
    }

    [ClientRpc]
    private void RpcPlayMeow()
    {
        PlayCatClip(meowClip, audioVolume);
    }

    [ClientRpc]
    private void RpcPlayPurr()
    {
        PlayCatClip(purrClip, audioVolume);
    }

    //animation event on the walk animation for sound
    public void PlayFootstepSound()
    {
        PlayCatClip(footstepClip, footstepVolume);
    }

    private void PlayCatClip(AudioClip clip, float volume)
    {
        if (catAudioSource == null)
            return;

        if (clip == null)
            return;

        catAudioSource.PlayOneShot(clip, volume);
    }

}