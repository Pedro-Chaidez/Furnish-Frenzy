using UnityEngine;
using System.Collections;

public class Player : Entity
{
    [Header("Block & Parry Settings")]
    public bool isBlocking = false;
    private float blockStartTime;
    public float parryMinWindow = 0.05f;
    public float parryMaxWindow = 1.0f;

    [Header("Hitbox Settings")]
    public Transform parryHitboxCenter;
    public Vector3 parryHitboxSize = new Vector3(2f, 2f, 2f);

    [Header("Parry Feedback")]
    public AudioSource parryAudioSource;
    public AudioClip parrySound; // Drop your mp3 file here in the Inspector
    public GameObject parryVisualUI; // Drop your hidden UI image/panel here

    private PlayerMotor motor;

    private void Awake()
    {
        gameObject.tag = "Player";
        teamID = 0;
        motor = GetComponent<PlayerMotor>();
				parryVisualUI.SetActive(false);
    }

    public void SetBlock(bool state)
    {
        isBlocking = state;

        if (motor != null)
        {
            motor.isBlocking = state;
        }

        if (state)
        {
            blockStartTime = Time.time; // Start the block timer when button is pressed
        }
    }

    public override void TakeDamage(float amount, GameObject source = null)
    {
        bool parried = false;

        // Check if we are holding block and an object actually hit us
        if (isBlocking && source != null)
        {
            float timeSinceBlock = Time.time - blockStartTime;

            // Check if our block timing was inside the sweet spot
            if (timeSinceBlock >= parryMinWindow && timeSinceBlock <= parryMaxWindow)
            {
                // Create an invisible cube in front of the player and see what is inside it
                Collider[] objectsInHitbox = Physics.OverlapBox(parryHitboxCenter.position, parryHitboxSize / 2f, parryHitboxCenter.rotation);

                foreach (Collider col in objectsInHitbox)
                {
                    // If the item that hurt us is inside the cube, it's a parry
                    if (col.gameObject == source)
                    {
                        parried = true;
                        break;
                    }
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
        // Play the mp3 file
        if (parryAudioSource != null && parrySound != null)
        {
            parryAudioSource.PlayOneShot(parrySound);
        }

        // Flash the UI
        if (parryVisualUI != null)
        {
            StartCoroutine(ParryVisualRoutine());
        }
    }

    private IEnumerator ParryVisualRoutine()
    {
        parryVisualUI.SetActive(true); // Turn UI on
        yield return new WaitForSeconds(1f); // Wait 1 second
        parryVisualUI.SetActive(false); // Turn UI off
    }

    // This draws a green outline in the Unity Editor so you can easily see and adjust your hitbox
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