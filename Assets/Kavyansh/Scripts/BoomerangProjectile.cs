
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BoomerangProjectile : MonoBehaviour
{
    public enum BoomerangState
    {
        Outbound,
        Returning
    }

    [Header("Movement")]
    [SerializeField] private float throwSpeed = 18f;
    [SerializeField] private float maxOutboundTime = 2f;

    [Header("Return")]
    [SerializeField] private float returnDuration = 0.65f;
    [SerializeField] private float returnArcHeight = 2f;
    [SerializeField] private float catchDistance = 0.35f;

    [Header("Visual")]
    [SerializeField] private float spinSpeed = 900f;

    private Rigidbody2D rb;

    private Transform owner;
    private Transform hand;

    private BoomerangState state;

    private Vector2 throwDirection;
    private Vector2 returnStart;
    private Vector2 returnControl;

    private float outboundTimer;
    private float returnTimer;

    private bool initialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }

    public void Initialize(
        Transform player,
        Transform playerHand,
        Vector2 direction)
    {
        owner = player;
        hand = playerHand;

        throwDirection = direction.normalized;

        state = BoomerangState.Outbound;

        rb.position = hand.position;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
            return;

        transform.Rotate(
            0f,
            0f,
            -spinSpeed * Time.deltaTime
        );
    }

    private void FixedUpdate()
    {
        if (!initialized)
            return;

        if (state == BoomerangState.Outbound)
        {
            MoveForward();
        }
        else
        {
            ReturnToPlayer();
        }
    }

    private void MoveForward()
    {
        outboundTimer += Time.fixedDeltaTime;

        Vector2 nextPosition =
            rb.position +
            throwDirection * throwSpeed *
            Time.fixedDeltaTime;

        rb.MovePosition(nextPosition);

        // Safety return if nothing gets hit.
        if (outboundTimer >= maxOutboundTime)
        {
            StartReturn();
        }
    }

    private void StartReturn()
    {
        if (state == BoomerangState.Returning)
            return;

        state = BoomerangState.Returning;

        returnTimer = 0f;
        returnStart = rb.position;

        Vector2 target = hand.position;

        Vector2 midpoint = (returnStart + target) * 0.5f;

        Vector2 perpendicular = new Vector2(
            -throwDirection.y,
            throwDirection.x
        );

        returnControl = midpoint +
                        perpendicular * returnArcHeight;
    }

    private void ReturnToPlayer()
    {
        returnTimer += Time.fixedDeltaTime;

        float t = Mathf.Clamp01(
            returnTimer / returnDuration
        );

        // Quadratic Bezier curve.
        Vector2 currentHandPosition = hand.position;

        Vector2 position =
            Mathf.Pow(1f - t, 2f) * returnStart +
            2f * (1f - t) * t * returnControl +
            Mathf.Pow(t, 2f) * currentHandPosition;

        rb.MovePosition(position);

        // Catch the boomerang.
        if (Vector2.Distance(
            rb.position,
            currentHandPosition
        ) <= catchDistance || t >= 1f)
        {
            CatchBoomerang();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (state != BoomerangState.Outbound)
            return;

        // Ignore the player and its child colliders.
        if (owner != null &&
            (other.transform == owner ||
             other.transform.IsChildOf(owner)))
        {
            return;
        }

        StartReturn();
    }

    private void CatchBoomerang()
    {
        if (owner != null)
        {
            Boomerang_Mechanic controller =
                owner.GetComponent<Boomerang_Mechanic>();

            if (controller != null)
                controller.OnBoomerangCaught();
        }

        Destroy(gameObject);
    }
}
