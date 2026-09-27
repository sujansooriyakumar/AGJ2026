using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 4.0f;
        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 6.0f;
        [Tooltip("Rotation speed of the character")]
        public float RotationSpeed = 1.0f;
        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;
        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.1f;
        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;
        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;
        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.5f;
        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Double Jump")]
        [Tooltip("How many extra jumps the player can make in the air. 1 = double jump, 2 = triple jump, 0 = none")]
        [Min(0)]
        public int doubleJumpCount = 1;

        [Header("Dash")]
        [Tooltip("Speed of the dash in m/s")]
        public float dashSpeed = 20f;
        [Tooltip("How long the dash lasts in seconds")]
        public float dashDuration = 0.2f;
        [Tooltip("Time in seconds after a dash starts before another dash can be used")]
        public float dashCooldown = 1.0f;

        [Header("Ledge Climb")]
        [Tooltip("Layers whose top surfaces can be climbed onto. Defaults to the 'Ledge' layer if left empty")]
        public LayerMask LedgeLayers;
        [Tooltip("How far in front of the player's body to look for a ledge, in metres")]
        public float ledgeReach = 0.4f;
        [Tooltip("Lowest ledge (above the player's feet) that triggers a climb. Lower obstacles are left to the CharacterController's step offset")]
        public float ledgeMinHeight = 0.5f;
        [Tooltip("Highest ledge (above the player's feet) the player can grab")]
        public float ledgeMaxHeight = 2.2f;
        [Tooltip("How long the climb takes in seconds")]
        public float ledgeClimbDuration = 0.5f;
        [Tooltip("Fraction of the climb spent moving up; the rest moves forward onto the surface")]
        [Range(0.1f, 0.9f)]
        public float ledgeClimbUpPortion = 0.6f;
        [Tooltip("Allow climbing from the ground (vaulting onto waist/chest-high ledges), not just while airborne")]
        public bool climbFromGround = true;

        [Header("Wallrun")]
        [Tooltip("If the character is touching a wall on either side")]
        public bool touchingWall;
        [Tooltip("What layers the character uses as walls")]
        public LayerMask WallLayers;
        [Tooltip("Side raycast length. Should be a bit more than the CharacterController radius")]
        public float wallCheckDistance = 0.8f;
        [Tooltip("Fraction of normal gravity applied while wall running")]
        public float wallRunGravityMultiplier = 0.25f;
        [Tooltip("Maximum downward speed while wall running. 0 = no sliding down")]
        public float wallRunMaxFallSpeed = 2f;
        [Tooltip("Camera roll in degrees while wall running")]
        public float wallRunCameraTilt = 15f;
        [Tooltip("How quickly the camera eases into and out of the tilt")]
        public float cameraTiltSpeed = 8f;
        [Tooltip("Horizontal push away from the wall when wall jumping")]
        public float wallJumpSideForce = 7f;
        [Tooltip("How quickly the wall jump push fades out (m/s per second)")]
        public float wallJumpDecay = 10f;
        [Tooltip("Time after a wall jump before a new wall run can start. Stops the player instantly re-latching (and paying AP again) on the wall they just left")]
        public float wallReattachDelay = 0.3f;

        [Header("Vertical Wallrun")]
        [Tooltip("If the character is facing a wall directly in front")]
        public bool touchingFrontWall;
        [Tooltip("Forward raycast length. Should be a bit more than the CharacterController radius")]
        public float frontWallCheckDistance = 0.8f;
        [Tooltip("How head-on you must face the wall (1 = perfectly straight, 0.7 = within ~45 degrees)")]
        [Range(0f, 1f)]
        public float frontWallMinFacing = 0.7f;
        [Tooltip("Upward speed while running up a wall in m/s")]
        public float verticalWallRunSpeed = 6f;
        [Tooltip("How long the player can run up a wall before falling off, in seconds")]
        public float verticalWallRunDuration = 0.6f;
        [Tooltip("Backward push away from the wall when jumping off during a vertical wall run")]
        public float verticalWallJumpBackForce = 6f;
        [Tooltip("How far the camera pitches up (degrees) while running up a wall, so it feels like your feet are on the wall")]
        public float verticalWallRunCameraPitch = 20f;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;
        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 90.0f;
        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -90.0f;

        [Header("Events")]
        public UnityEvent OnDashStart;
        public UnityEvent OnDashComplete;

        public UnityEvent OnSprintStart;
        public UnityEvent OnSprintComplete;


        // cinemachine
        private float _cinemachineTargetPitch;
        private float _cameraRoll;
        private float _cameraPitchOffset;

        // player
        private float _speed;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // air jumps left before touching the ground or a wall again
        private int _airJumpsRemaining;

        // dash
        private bool _isDashing;
        private float _dashTimer;
        private float _dashCooldownTimer;
        private Vector3 _dashDirection;

        // ledge climb
        private bool _isClimbing;
        private float _climbTimer;
        private Vector3 _climbStart;
        private Vector3 _climbTarget;

        // side wall run
        private bool _isWallRunning;
        private bool _wallOnRight;
        private Vector3 _wallNormal;
        private Vector3 _wallJumpVelocity;
        private float _wallReattachTimer;

        // vertical wall run
        private bool _isVerticalWallRunning;
        private bool _verticalWallRunUsed;
        private float _verticalWallRunTimer;
        private Vector3 _frontWallNormal;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private const float _threshold = 0.01f;
        private ActionPointManager actionPointManager;

        /// <summary>Current action points (0 if no ActionPointManager was found).</summary>
        public int CurrentAP => actionPointManager != null ? actionPointManager.GetAP() : 0;

        /// <summary>True if the player has enough AP for an ability.</summary>
        private bool HasAP => CurrentAP >= 1;

        /// <summary>Seconds until the dash is ready again (0 when ready). Handy for a UI cooldown indicator.</summary>
        public float DashCooldownRemaining => Mathf.Max(_dashCooldownTimer, 0f);

        /// <summary>Air jumps left before the player needs to touch the ground or a wall. Handy for UI.</summary>
        public int AirJumpsRemaining => _airJumpsRemaining;

        /// <summary>True while a dash is in progress.</summary>
        public bool IsDashing => _isDashing;

        /// <summary>True while climbing onto a ledge.</summary>
        public bool IsClimbing => _isClimbing;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            // find the action point manager: on the player first, otherwise anywhere in the scene
            actionPointManager = GetComponent<ActionPointManager>();
            if (actionPointManager == null)
            {
                actionPointManager = FindObjectOfType<ActionPointManager>();
            }
            if (actionPointManager == null)
            {
                Debug.LogWarning("FirstPersonController: no ActionPointManager found. AP abilities (wall run, wall jump, air jump, dash) will be unavailable.");
            }

            // default to the "Ledge" layer if nothing was assigned in the Inspector
            if (LedgeLayers.value == 0)
            {
                LedgeLayers = LayerMask.GetMask("Ledge");
            }

            // start with a full set of air jumps
            _airJumpsRemaining = doubleJumpCount;

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            GroundedCheck();

            // a ledge climb takes full control of the player until it finishes
            if (_isClimbing)
            {
                UpdateLedgeClimb();
                return;
            }

            if (TryStartLedgeClimb())
            {
                return;
            }

            // run the checks first so jump/gravity use this frame's state
            WallCheck();
            Dash();
            JumpAndGravity();
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        /// <summary>Spends one action point. Only call after checking HasAP.</summary>
        private void SpendAP()
        {
            if (actionPointManager != null)
            {
                actionPointManager.UseAP();
            }
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        }

        // offset from transform.position to the bottom of the CharacterController capsule
        private Vector3 FeetOffset => _controller.center - Vector3.up * (_controller.height * 0.5f);

        private bool TryStartLedgeClimb()
        {
            if (_input.move.y <= 0f) return false;
            if (Grounded && !climbFromGround) return false;

            float radius = _controller.radius;

            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 feet = transform.position + FeetOffset;

         
            Vector3 origin = feet + forward * (radius + ledgeReach) + Vector3.up * ledgeMaxHeight;
            float castDistance = ledgeMaxHeight - ledgeMinHeight;

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit topHit, castDistance, LedgeLayers, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (topHit.normal.y < 0.7f) return false;

            Vector3 targetFeet = topHit.point + forward * radius + Vector3.up * 0.05f;

            int blockers = GroundLayers | WallLayers | LedgeLayers;
            Vector3 capsuleBottom = targetFeet + Vector3.up * (radius + 0.05f);
            Vector3 capsuleTop = targetFeet + Vector3.up * (_controller.height - radius);
            if (Physics.CheckCapsule(capsuleBottom, capsuleTop, radius * 0.95f, blockers, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            StartLedgeClimb(targetFeet - FeetOffset);
            return true;
        }

        private void StartLedgeClimb(Vector3 targetPosition)
        {
            _isClimbing = true;
            _climbTimer = 0f;
            _climbStart = transform.position;
            _climbTarget = targetPosition;

            _verticalVelocity = 0f;
            _isDashing = false;
            _isWallRunning = false;
            _isVerticalWallRunning = false;
            _wallJumpVelocity = Vector3.zero;
            _airJumpsRemaining = doubleJumpCount;

            _controller.enabled = false;
        }

        private void UpdateLedgeClimb()
        {
            _climbTimer += Time.deltaTime;
            float t = ledgeClimbDuration > 0f ? Mathf.Clamp01(_climbTimer / ledgeClimbDuration) : 1f;

            Vector3 mid = new Vector3(_climbStart.x, _climbTarget.y, _climbStart.z);

            if (t < ledgeClimbUpPortion)
            {
                float upT = Mathf.SmoothStep(0f, 1f, t / ledgeClimbUpPortion);
                transform.position = Vector3.Lerp(_climbStart, mid, upT);
            }
            else
            {
                float forwardT = Mathf.SmoothStep(0f, 1f, (t - ledgeClimbUpPortion) / (1f - ledgeClimbUpPortion));
                transform.position = Vector3.Lerp(mid, _climbTarget, forwardT);
            }

            _input.jump = false;
            _input.dash = false;

            if (t >= 1f)
            {
                _isClimbing = false;
                _controller.enabled = true;
                _verticalVelocity = -2f;
            }
        }

        private void WallCheck()
        {
            touchingWall = false;
            touchingFrontWall = false;
            if (Grounded) return;

            Vector3 origin = transform.position + Vector3.up * (_controller.height * 0.5f);

            if (Physics.Raycast(origin, transform.forward, out RaycastHit frontHit, frontWallCheckDistance, WallLayers, QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Dot(transform.forward, -frontHit.normal) >= frontWallMinFacing)
                {
                    touchingFrontWall = true;
                    _frontWallNormal = frontHit.normal;
                }
            }

            if (Physics.Raycast(origin, transform.right, out RaycastHit hit, wallCheckDistance, WallLayers, QueryTriggerInteraction.Ignore))
            {
                touchingWall = true;
                _wallOnRight = true;
                _wallNormal = hit.normal;
            }
            else if (Physics.Raycast(origin, -transform.right, out hit, wallCheckDistance, WallLayers, QueryTriggerInteraction.Ignore))
            {
                touchingWall = true;
                _wallOnRight = false;
                _wallNormal = hit.normal;
            }
        }

        private void Dash()
        {
            if (_dashCooldownTimer > 0f)
            {
                _dashCooldownTimer -= Time.deltaTime;
            }

            if (_input.dash && !_isDashing && _dashCooldownTimer <= 0f && HasAP)
            {
                SpendAP();
                OnDashStart?.Invoke();
                _isDashing = true;
                _dashTimer = dashDuration;
                _dashCooldownTimer = dashCooldown;

                _dashDirection = transform.forward;
                _dashDirection.y = 0f;
                _dashDirection.Normalize();

                _isWallRunning = false;
                _isVerticalWallRunning = false;
                _wallJumpVelocity = Vector3.zero;
            }

            _input.dash = false;

            if (_isDashing)
            {
                _dashTimer -= Time.deltaTime;
                if (_dashTimer <= 0f)
                {
                    _isDashing = false;
                    OnDashComplete?.Invoke();
                }
            }
        }

        private void CameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
                _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

                transform.Rotate(Vector3.up * _rotationVelocity);
            }

            float targetRoll = 0f;
            if (_isWallRunning) targetRoll = _wallOnRight ? wallRunCameraTilt : -wallRunCameraTilt;

            _cameraRoll = Mathf.Lerp(_cameraRoll, targetRoll, Time.deltaTime * cameraTiltSpeed);


            float targetPitchOffset = _isVerticalWallRunning ? verticalWallRunCameraPitch : 0f;
            _cameraPitchOffset = Mathf.Lerp(_cameraPitchOffset, targetPitchOffset, Time.deltaTime * cameraTiltSpeed);

            float finalPitch = ClampAngle(_cinemachineTargetPitch - _cameraPitchOffset, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(finalPitch, 0.0f, _cameraRoll);
        }

        private void Move()
        {
            if (_isDashing)
            {
                _controller.Move((_dashDirection * dashSpeed + new Vector3(0.0f, _verticalVelocity, 0.0f)) * Time.deltaTime);
                return;
            }

            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
            if (_input.sprint && _input.move.magnitude > 0.0f)
            {
                OnSprintStart?.Invoke();
            }

            if (!_input.sprint || _input.move.magnitude <= 0.0f)
            {
                OnSprintComplete?.Invoke();
            }
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            Vector3 horizontalVelocity = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z) - _wallJumpVelocity;
            float currentHorizontalSpeed = horizontalVelocity.magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;


            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            if (_input.move != Vector2.zero)
            {
                inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
            }

            _wallJumpVelocity = Vector3.MoveTowards(_wallJumpVelocity, Vector3.zero, wallJumpDecay * Time.deltaTime);

            _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime)
                + (_wallJumpVelocity + new Vector3(0.0f, _verticalVelocity, 0.0f)) * Time.deltaTime);
        }

        private void JumpAndGravity()
        {
            bool jumped = false;

            if (Grounded)
            {
                _isWallRunning = false;
                _isVerticalWallRunning = false;
                _verticalWallRunUsed = false;
                _wallReattachTimer = 0f;

                _fallTimeoutDelta = FallTimeout;

                _airJumpsRemaining = doubleJumpCount;

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    jumped = true;

                    _input.jump = false;
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }

                if (_wallReattachTimer > 0f)
                {
                    _wallReattachTimer -= Time.deltaTime;
                }

                UpdateVerticalWallRun();

                bool wasWallRunning = _isWallRunning;
                bool wantsWallRun = !_isDashing && !_isVerticalWallRunning && touchingWall && _input.move.y > 0f;

                if (wantsWallRun && !wasWallRunning)
                {
                    if (_wallReattachTimer <= 0f && HasAP)
                    {
                        SpendAP();
                        _isWallRunning = true;

                        if (_verticalVelocity < 0f) _verticalVelocity = 0f;
                    }
                    else
                    {
                        _isWallRunning = false;
                    }
                }
                else
                {
                    _isWallRunning = wantsWallRun;
                }

                if (_isWallRunning || _isVerticalWallRunning) _airJumpsRemaining = doubleJumpCount;

                if (_input.jump && HasAP)
                {
                    if (_isVerticalWallRunning)
                    {
                        SpendAP();
                        _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                        _wallJumpVelocity = _frontWallNormal * verticalWallJumpBackForce;
                        _isVerticalWallRunning = false;
                        _wallReattachTimer = wallReattachDelay;
                        jumped = true;
                    }
                    else if (_isWallRunning)
                    {
                        SpendAP();
                        _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                        _wallJumpVelocity = _wallNormal * wallJumpSideForce;
                        _isWallRunning = false;
                        _wallReattachTimer = wallReattachDelay;
                        jumped = true;
                    }
                    else if (_airJumpsRemaining > 0)
                    {
                        SpendAP();
                        _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                        _airJumpsRemaining--;
                        jumped = true;
                    }
                }

                _input.jump = false;
            }

            if (jumped) _isDashing = false;

            if (_isDashing)
            {
                _verticalVelocity = Grounded ? -2f : 0f;
            }
            else if (_isVerticalWallRunning)
            {
                _verticalVelocity = verticalWallRunSpeed;
            }
            else if (_isWallRunning)
            {
                _verticalVelocity += Gravity * wallRunGravityMultiplier * Time.deltaTime;
                _verticalVelocity = Mathf.Max(_verticalVelocity, -wallRunMaxFallSpeed);
            }
            else if (_verticalVelocity > -_terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private void UpdateVerticalWallRun()
        {
            if (!_isDashing && !_isVerticalWallRunning && !_verticalWallRunUsed && touchingFrontWall && _input.move.y > 0f
                && _wallReattachTimer <= 0f && HasAP)
            {
                SpendAP();
                _isVerticalWallRunning = true;
                _verticalWallRunUsed = true;
                _verticalWallRunTimer = verticalWallRunDuration;
            }

            if (!_isVerticalWallRunning) return;

            _verticalWallRunTimer -= Time.deltaTime;

            if (_verticalWallRunTimer <= 0f || !touchingFrontWall || _input.move.y <= 0f)
            {
                _isVerticalWallRunning = false;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);

            CharacterController controller = GetComponent<CharacterController>();
            if (controller == null) return;

            Vector3 origin = transform.position + Vector3.up * (controller.height * 0.5f);

            Gizmos.color = touchingWall ? Color.cyan : Color.yellow;
            Gizmos.DrawLine(origin, origin + transform.right * wallCheckDistance);
            Gizmos.DrawLine(origin, origin - transform.right * wallCheckDistance);

            Gizmos.color = touchingFrontWall ? Color.magenta : Color.yellow;
            Gizmos.DrawLine(origin, origin + transform.forward * frontWallCheckDistance);

            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 feet = transform.position + controller.center - Vector3.up * (controller.height * 0.5f);
            Vector3 ledgeTop = feet + forward * (controller.radius + ledgeReach) + Vector3.up * ledgeMaxHeight;
            Vector3 ledgeBottom = feet + forward * (controller.radius + ledgeReach) + Vector3.up * ledgeMinHeight;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(ledgeTop, ledgeBottom);
        }
    }
}