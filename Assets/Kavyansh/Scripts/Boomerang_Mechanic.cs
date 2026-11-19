
using UnityEngine;

public class Boomerang_Mechanic : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform aimPivot;
    [SerializeField] private GameObject aimArrow;
    [SerializeField] private Transform boomerangHand;
    [SerializeField] private GameObject boomerangPrefab;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Aiming")]
    [SerializeField] private KeyCode aimKey = KeyCode.Q;
    [SerializeField] private float aimRotationSpeed = 120f;
    [SerializeField] private float maxAimAngle = 90f;

    [Header("Slow Motion")]
    [SerializeField, Range(0.05f, 1f)]
    private float slowMotionScale = 0.2f;

    private bool isAiming;
    private bool boomerangActive;

    private float aimAngle;
    private float originalTimeScale = 1f;

    private GameObject activeBoomerang;

    public bool IsAiming => isAiming;

    private void Update()
    {
        bool grounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundRadius,
            groundLayer
        ) != null;

        // Aim only while airborne and boomerang is available.
        if (!grounded && !boomerangActive)
        {
            if (Input.GetKeyDown(aimKey))
                StartAiming();
        }

        if (isAiming)
        {
            HandleAiming();

            if (Input.GetKeyUp(aimKey))
                ThrowBoomerang();
        }

        // Safety: cancel aiming if the player lands.
        if (isAiming && grounded)
            CancelAiming();
    }

    private void StartAiming()
    {
        isAiming = true;
        aimAngle = 0f;

        originalTimeScale = Time.timeScale;
        Time.timeScale = slowMotionScale;

        aimArrow.SetActive(true);
    }

    private void HandleAiming()
    {
        float input = 0f;

        if (Input.GetKey(KeyCode.A))
            input -= 1f;

        if (Input.GetKey(KeyCode.D))
            input += 1f;

        aimAngle += input *
                    aimRotationSpeed *
                    Time.unscaledDeltaTime;

        aimAngle = Mathf.Clamp(
            aimAngle,
            -maxAimAngle,
            maxAimAngle
        );

        // Determine which direction the player faces.
        float facingDirection =
            transform.localScale.x >= 0f ? 1f : -1f;

        float baseAngle = facingDirection > 0f ? 0f : 180f;

        aimPivot.rotation = Quaternion.Euler(
            0f,
            0f,
            baseAngle + aimAngle
        );
    }

    private void ThrowBoomerang()
    {
        Vector2 throwDirection = aimPivot.right;

        CancelAiming();

        activeBoomerang = Instantiate(
            boomerangPrefab,
            boomerangHand.position,
            Quaternion.identity
        );

        BoomerangProjectile projectile =
            activeBoomerang.GetComponent<BoomerangProjectile>();

        projectile.Initialize(
            transform,
            boomerangHand,
            throwDirection
        );

        boomerangActive = true;
    }

    private void CancelAiming()
    {
        isAiming = false;

        aimArrow.SetActive(false);

        Time.timeScale = originalTimeScale;
        originalTimeScale = 1f;

        aimPivot.localRotation = Quaternion.identity;
    }

    public void OnBoomerangCaught()
    {
        boomerangActive = false;
        activeBoomerang = null;
    }

    private void OnDisable()
    {
        if (isAiming)
        {
            isAiming = false;
            aimArrow.SetActive(false);
            Time.timeScale = originalTimeScale;
            originalTimeScale = 1f;
        }
    }
}
