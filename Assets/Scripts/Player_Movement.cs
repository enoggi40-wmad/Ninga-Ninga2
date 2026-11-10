using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Player_Movement : MonoBehaviour
{
    [Header("Refs")]
    public Transform cam;

    [Header("Move")]
    public float walkSpeed = 4f;
    public float runSpeed = 6f;
    public float slowSpeed = 2f;
    float currentSpeed;

    [Header("Jump")]
    public float jumpSpeed = 7f;
    public float gravity = 20f;

    [Header("Jump Count")]
    [SerializeField] int maxJumps = 1;   // 기본은 1회 점프
    int jumpsUsed = 0;

    [Header("Animator")]
    public Animator animator;
    public string jumpStateName = "Player_Jump";
    

    CharacterController cc;

    Vector3 moveDirWorld;
    float yVel;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        yVel = -2f; // 바닥에 붙어있게
        if (animator)
            animator.SetBool("IsJumping", false);
    }

    void Update()
    {
        ReadMovementInput_CameraRelative();

        HandleJump();
        ApplyGravity();
        ApplyMove();

        UpdateAnimator();
    }

    void LateUpdate()
    {
        FollowCameraYaw();
    }

    void FollowCameraYaw() // [MOD]
    {
        if (!cam) return;  // [MOD]

        Vector3 e = transform.eulerAngles; // [MOD]
        e.y = cam.eulerAngles.y;           // [MOD] 카메라 yaw로 맞춤
        transform.eulerAngles = e;         // [MOD]
    }

    void ReadMovementInput_CameraRelative()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 1f) input.Normalize();

        if (!cam)
        {
            // 카메라 참조 없으면 월드축 기준으로라도 동작하게
            moveDirWorld = input;
            return;
        }

        // 카메라의 forward/right를 평면(XZ)으로 투영
        Vector3 camForward = cam.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cam.right;
        camRight.y = 0f;
        camRight.Normalize();

        // 입력을 카메라 기준 월드 방향으로 변환
        moveDirWorld = (camForward * input.z + camRight * input.x);
        if (moveDirWorld.sqrMagnitude > 1f) moveDirWorld.Normalize();
    }

    void UpdateAnimator()
    {
        if (!animator) return;

        float horizontalSpeed = new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude;
        animator.SetFloat("Speed", horizontalSpeed / runSpeed);

        animator.SetBool("IsJumping", !cc.isGrounded);

    }

    void HandleJump()
    {
        // 착지하면 점프 횟수 리셋
        if (cc.isGrounded)
        {
            if (yVel < 0f) yVel = -2f;
            jumpsUsed = 0;
        }

        // 점프 입력
        if (Input.GetButtonDown("Jump") && jumpsUsed < maxJumps)
        {
            yVel = jumpSpeed;
            jumpsUsed++;

            if (animator)
            {
                animator.ResetTrigger("Jump"); // 연타 안정화
                animator.SetTrigger("Jump");   // 어떤 모션이든 점프 애니로 강제 진입 (B)
            }
        }
    }

    void ApplyGravity()
    {
        yVel -= gravity * Time.deltaTime;
    }

    void ApplyMove()
    {
        bool isSlow = Input.GetKey(KeyCode.Z);
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        if (isSlow) currentSpeed = slowSpeed;       // Ctrl이 최우선
        else currentSpeed = isRunning ? runSpeed : walkSpeed;

        Vector3 velocity = moveDirWorld * currentSpeed;
        velocity.y = yVel;

        cc.Move(velocity * Time.deltaTime);
    }

    // ===== 스킬 해금용 API =====
    public void EnableDoubleJump()
    {
        maxJumps = 2;
    }


}
