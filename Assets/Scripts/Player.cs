using UnityEngine;
using System.Collections;

public class Player : Entity
{
    [Header("Block & Parry States")]
    public bool isBlocking = false;
    public bool isParryActive = false;
    public bool cannotBlock = false;

    [Header("Block & Parry Settings")]
    public float parryMinWindow = 0.05f;
    public float parryMaxWindow = 1.0f; // Note: 1.0f equals 1 second
    public float blockLingerTime = 0.06f; // How long block/parry stays active after letting go
    public float blockCooldownTime = 0.5f; // How long the player is locked out of blocking 

    private float blockStartTime;
    private Coroutine blockLingerRoutine;
    private Coroutine parryVisualRoutineTracker;

    [Header("Hitbox Settings")]
    public Transform parryHitboxCenter;
    public Vector3 parryHitboxSize = new Vector3(2f, 2f, 2f);

    [Header("Parry Feedback")]
    public AudioSource parryAudioSource;
    public AudioClip parrySound;
    public GameObject parryVisualUI;

    private PlayerMotor motor;

    private void Awake()
    {
        gameObject.tag = "Player";
        teamID = 0;
        motor = GetComponent<PlayerMotor>();

        if (parryVisualUI != null) parryVisualUI.SetActive(false);
    }

    private void Update()
    {
        // Constantly evaluate if the parry window is currently active
        if (isBlocking)
        {
            float timeSinceBlock = Time.time - blockStartTime;
            isParryActive = (timeSinceBlock >= parryMinWindow && timeSinceBlock <= parryMaxWindow);
        }
        else
        {
            isParryActive = false;
        }
    }

    public void SetBlock(bool state)
    {
        // Prevent blocking if we are in the cooldown state
        if (state && cannotBlock) return;

        if (state)
        {
            // If the player presses block while the linger is still happening, cancel the linger
            if (blockLingerRoutine != null)
            {
                StopCoroutine(blockLingerRoutine);
                blockLingerRoutine = null;
            }

            isBlocking = true;
            blockStartTime = Time.time;

            if (motor != null) motor.isBlocking = true;
        }
        else
        {
            // When the player lets go of the button, start the 0.06s linger timer
            if (isBlocking && gameObject.activeInHierarchy)
            {
                blockLingerRoutine = StartCoroutine(BlockLingerCoroutine());
            }
        }
    }

    private IEnumerator BlockLingerCoroutine()
    {
        // 1. Linger State: Keep everything active for 0.06 seconds
        yield return new WaitForSeconds(blockLingerTime);

        // 2. Turn off block
        isBlocking = false;
        isParryActive = false;
        if (motor != null) motor.isBlocking = false;

        // 3. Cooldown State: Lock the player out of blocking
        cannotBlock = true;
        yield return new WaitForSeconds(blockCooldownTime);
        cannotBlock = false;
    }

    public override void TakeDamage(float amount, GameObject source = null)
    {
        bool parried = false;

        // Check our real-time boolean instead of doing math here
        if (isParryActive && source != null)
        {
            Collider[] objectsInHitbox = Physics.OverlapBox(parryHitboxCenter.position, parryHitboxSize / 2f, parryHitboxCenter.rotation);

            foreach (Collider col in objectsInHitbox)
            {
                if (col.gameObject == source)
                {
                    parried = true;
                    break;
                }
            }
        }

        if (parried)
        {
            SuccessfulParry();
            return; // Stop here, take no damage
        }

        if (isBlocking)
        {
            amount /= 2f; // If we missed the parry but are still blocking, cut damage in half
        }

        base.TakeDamage(amount, source);
    }

    private void SuccessfulParry()
    {
        Debug.Log("Successful Parry Executed!");

        // Play the mp3 file safely
        if (parryAudioSource != null && parrySound != null)
        {
            parryAudioSource.PlayOneShot(parrySound);
        }
        else
        {
            Debug.LogWarning("Parry missed audio: AudioSource or AudioClip is not assigned in the Inspector!");
        }

        // Flash the UI safely
        if (parryVisualUI != null)
        {
            // If they parry twice rapidly, stop the old UI animation and restart it
            if (parryVisualRoutineTracker != null) StopCoroutine(parryVisualRoutineTracker);
            parryVisualRoutineTracker = StartCoroutine(ParryVisualFlash());
        }
        else
        {
            Debug.LogWarning("Parry missed visual: UI GameObject is not assigned in the Inspector!");
        }
    }

    private IEnumerator ParryVisualFlash()
    {
        parryVisualUI.SetActive(true);
        yield return new WaitForSeconds(1f);
        parryVisualUI.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (parryHitboxCenter != null)
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = Matrix4x4.TRS(parryHitboxCenter.position, parryHitboxCenter.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, parryHitboxSize);
        }
    }
}