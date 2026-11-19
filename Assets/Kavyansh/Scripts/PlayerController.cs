
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float minJumpForce = 8f;
    [SerializeField] private float maxJumpForce = 18f;
    [SerializeField] private float maxChargeTime = 1.2f;

    [Header("Direction Settings")]
    [SerializeField] private float maxLaunchAngle = 65f;
    [SerializeField] private float directionSpeed = 90f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationInfluence = 0.65f;
    [SerializeField] private float rotationReturnSpeed = 180f;
    [SerializeField] private float aimRotationSpeed = 240f;

    [Header("Camera Tension")]
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private float maxZoomIn = 1.5f;
    [SerializeField] private float zoomSmoothTime = 0.25f;

    private float originalCameraSize;
    private float zoomVelocity;

    private Rigidbody2D rb;

    private bool isGrounded;
    private bool isCharging;
    private bool launchPending;
    private bool wasGrounded;

    private float chargeTimer;
    private float launchAngle;
    private float targetRotation;

    public float ChargePercent =>
        Mathf.Clamp01(chargeTimer / maxChargeTime);

    private void Awake()
    {
        if (gameplayCamera != null)
        {
            originalCameraSize = gameplayCamera.orthographicSize;
        }

        rb = GetComponent<Rigidbody2D>();

        // Rotation is controlled manually.
        rb.freezeRotation = true;
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (isGrounded && !isCharging && !launchPending &&
            Input.GetKeyDown(KeyCode.Space))
        {
            StartCharging();
        }

        if (isCharging)
        {
            // Increase jump charge.
            chargeTimer = Mathf.Min(
                chargeTimer + Time.deltaTime,
                maxChargeTime
            );

            // Adjust launch direction.
            float directionInput = 0f;

            if (Input.GetKey(KeyCode.A))
                directionInput -= 1f;

            if (Input.GetKey(KeyCode.D))
                directionInput += 1f;

            launchAngle += directionInput *
                           directionSpeed *
                           Time.deltaTime;

            launchAngle = Mathf.Clamp(
                launchAngle,
                -maxLaunchAngle,
                maxLaunchAngle
            );

            // Release Space to launch.
            if (Input.GetKeyUp(KeyCode.Space))
            {
                isCharging = false;
                launchPending = true;
            }
        }
    }

    private void StartCharging()
    {
        isCharging = true;
        chargeTimer = 0f;
        launchAngle = 0f;
    }

    private void FixedUpdate()
    {
        CheckGround();

        // Stop horizontal sliding immediately on landing.
        if (isGrounded)
        {
            Vector2 velocity = rb.linearVelocity;
            velocity.x = 0f;
            rb.linearVelocity = velocity;
        }

        if (launchPending)
        {
            LaunchPlayer();
        }

        UpdateRotation();
    }

    private void LateUpdate()
    {
        if (gameplayCamera == null)
            return;

        // Calculate zoom based on current jump charge.
        float charge = isCharging ? ChargePercent : 0f;

        // Higher charge = smaller orthographic size.
        float targetSize = originalCameraSize - (maxZoomIn * charge);

        // Smooth camera transition.
        gameplayCamera.orthographicSize = Mathf.SmoothDamp(
            gameplayCamera.orthographicSize,
            targetSize,
            ref zoomVelocity,
            zoomSmoothTime
        );
    }

    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        ) != null;

        // Don't consider the player grounded while rising.
        if (rb.linearVelocity.y > 0.1f)
            isGrounded = false;
    }

    private void LaunchPlayer()
    {
        launchPending = false;

        float chargePercent = ChargePercent;

        float jumpForce = Mathf.Lerp(
            minJumpForce,
            maxJumpForce,
            chargePercent
        );

        // 0 degrees = straight up.
        float angleRadians = launchAngle * Mathf.Deg2Rad;

        Vector2 launchDirection = new Vector2(
            Mathf.Sin(angleRadians),
            Mathf.Cos(angleRadians)
        );

        // Apply launch velocity.
        rb.linearVelocity = launchDirection * jumpForce;

/*        // Tilt the character towards the launch direction.
        targetRotation = -launchAngle * rotationInfluence;

        transform.rotation = Quaternion.Euler(
            0f, 0f, targetRotation
        );*/

        chargeTimer = 0f;
        launchAngle = 0f;
    }

    private void UpdateRotation()
    {
        float targetAngle = 0f;

        // Visually tilt while charging the jump.
        if (isCharging)
        {
            targetAngle = -launchAngle * rotationInfluence;
        }

        // Smoothly rotate towards the desired angle.
        float newAngle = Mathf.MoveTowardsAngle(
            rb.rotation,
            targetAngle,
            (isCharging ? aimRotationSpeed : rotationReturnSpeed)
            * Time.fixedDeltaTime
        );

        rb.MoveRotation(newAngle);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius
        );
    }
}
