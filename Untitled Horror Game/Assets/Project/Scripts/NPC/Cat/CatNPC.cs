using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class CatNPC : MonoBehaviour
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

    private bool isWaiting;
    private bool isSitting;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (catAudioSource == null)
            catAudioSource = GetComponent<AudioSource>();

        agent.speed = walkSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.updateRotation = false;
    }

    private void Start()
    {
        ResetSoundTimer();
        PickNewDestination();
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleAnimation();
        HandleRandomSound();
    }

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

    private void StopWaiting()
    {
        isWaiting = false;
        isSitting = false;

        if (agent != null)
            agent.isStopped = false;
    }

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

    //walk if moving, sit if isSitting true
    private void HandleAnimation()
    {
        if (animator == null)
            return;

        bool isMoving = agent.velocity.magnitude > 0.1f && !isWaiting;

        animator.SetBool("isWalking", isMoving);
        animator.SetBool("isSitting", isSitting);
    }

    private void HandleRandomSound()
    {
        if (catAudioSource == null)
            return;

        soundTimer -= Time.deltaTime;

        if (soundTimer > 0f)
            return;

        PlayCatSound();
        ResetSoundTimer();
    }

    private void PlayCatSound()
    {
        //if sitting, purr instead of meow
        if (isSitting && purrClip != null)
        {
            catAudioSource.PlayOneShot(purrClip, audioVolume);
            return;
        }

        if (meowClip != null)
            catAudioSource.PlayOneShot(meowClip, audioVolume);
    }

    public void PlayFootstepSound()
    {
        if (catAudioSource == null)
            return;

        if (footstepClip == null)
            return;

        catAudioSource.PlayOneShot(footstepClip, footstepVolume);
    }

    //a random time between (5s to 14s) of when it plays a sound
    private void ResetSoundTimer()
    {
        soundTimer = Random.Range(soundTimeMin, soundTimeMax);
    }

}