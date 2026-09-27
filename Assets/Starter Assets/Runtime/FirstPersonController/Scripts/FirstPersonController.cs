using System.Reflection;
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
        [Tooltip("How quickly the player reaches their target speed on the ground (higher = snappier)")]
        public float SpeedChangeRate = 10.0f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;
        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Minimum time between ground jumps, counted from the moment of the jump")]
        public float JumpTimeout = 0.1f;
        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Jump Feel")]
        public float coyoteTime = 0.15f;
        [Tooltip("How long a jump press is remembered. Pressing jump slightly before landing will still jump")]
        public float jumpBufferTime = 0.15f;
        [Tooltip("Gravity multiplier while falling. Higher = snappier, less floaty descent")]
        public float fallGravityMultiplier = 2.0f;
        [Tooltip("Release jump early to jump lower")]
        public bool variableJumpHeight = true;
        [Tooltip("Upward speed is multiplied by this when jump is released early (lower = shorter tap jumps)")]
        [Range(0f, 1f)]
        public float jumpCutMultiplier = 0.5f;

        [Header("Air Control")]
        [Tooltip("Steering strength in the air as a fraction of ground control. Lower = more momentum")]
        [Range(0f, 1f)]
        public float airControl = 0.35f;
        [Tooltip("How quickly horizontal speed bleeds off in the air with no movement input")]
        public float airDrag = 0.3f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;
        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;
        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.5f;
        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;
        [Tooltip("Ignore wall layers in the grounded sphere check so brushing a wall mid-air doesn't count as grounded. Standing on top of a wall still counts, via the CharacterController's own collision")]
        public bool excludeWallsFromGroundCheck = true;

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
        [Tooltip("Fraction of dash speed kept when the dash ends, which then eases down to normal speed")]
        [Range(0f, 1f)]
        public float dashExitSpeedMultiplier = 0.5f;

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
        [Tooltip("How quickly a fast fall is caught when latching onto a wall (m/s per second)")]
        public float wallRunCatchStrength = 40f;
        [Tooltip("Seconds on a wall before the side wall run costs AP. Brief accidental brushes are free")]
        public float wallRunCostDelay = 0.15f;
        [Tooltip("Camera roll in degrees while wall running")]
        public float wallRunCameraTilt = 15f;
        [Tooltip("How quickly the camera eases into and out of the tilt")]
        public float cameraTiltSpeed = 8f;
        [Tooltip("Horizontal push away from the wall when wall jumping")]
        public float wallJumpSideForce = 7f;
        [Tooltip("Time after a wall jump before a new wall run can start. Stops the player instantly re-latching (and paying AP again) on the wall they just left")]
        public float wallReattachDelay = 0.3f;
        [Tooltip("After a wall jump, air steering is reduced for this long so the push off the wall can't be immediately cancelled")]
        public float wallJumpAirControlLockTime = 0.3f;
        [Tooltip("Air steering strength right after a wall jump, as a fraction of normal air control. Eases back to full over the lock time")]
        [Range(0f, 1f)]
        public float wallJumpAirControlMultiplier = 0.1f;

        [Header("Wall Jump Turn")]
        [Tooltip("Automatically turn the player around to face away from the wall when jumping off a vertical wall run")]
        public bool rotateOnWallJump = false;
        [Tooltip("How long the automatic turn takes in seconds")]
        public float wallJumpTurnDuration = 0.15f;
        [Tooltip("Moving the mouse/stick during the turn cancels it and hands control back to the player")]
        public bool wallJumpTurnCancelOnLook = true;
        [Tooltip("Degrees of look rotation in a single frame needed to cancel the automatic turn")]
        public float wallJumpTurnCancelThreshold = 2f;

        [Header("Vertical Wallrun")]
        [Tooltip("If the character is facing a wall directly in front")]
        public bool touchingFrontWall;
        [Tooltip("Forward raycast length. Should be a bit more than the CharacterController radius")]
        public float frontWallCheckDistance = 0.8f;
        [Tooltip("How head-on you must face the wall (1 = perfectly straight, 0.7 = within ~45 degrees)")]
        [Range(0f, 1f)]
        public float frontWallMinFacing = 0.7f;
        [Tooltip("Upward speed at the start of a vertical wall run in m/s")]
        public float verticalWallRunSpeed = 6f;
        [Tooltip("Upward speed at the end of the run, as a fraction of the starting speed. The run eases between the two")]
        [Range(0f, 1f)]
        public float verticalWallRunEndSpeedFraction = 0.35f;
        [Tooltip("How quickly the player reaches wall-run speed after latching on (m/s per second)")]
        public float verticalWallRunAcceleration = 60f;
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

        [Header("Camera Feel")]
        [Tooltip("The Cinemachine virtual camera (or a plain Camera) whose field of view is changed. If empty, looks for 'PlayerFollowCamera'")]
        public GameObject fovCamera;
        [Tooltip("Extra FOV while sprinting")]
        public float sprintFovBoost = 5f;
        [Tooltip("Extra FOV while wall running")]
        public float wallRunFovBoost = 6f;
        [Tooltip("Extra FOV while dashing")]
        public float dashFovBoost = 12f;
        [Tooltip("How quickly the FOV eases to its target")]
        public float fovChangeSpeed = 8f;
        [Tooltip("Vertical head bob size in metres while moving on the ground. 0 = off")]
        public float headBobAmount = 0.05f;
        [Tooltip("Head bob speed at walking pace (scales with movement speed)")]
        public float headBobFrequency = 10f;
        [Tooltip("How far the camera dips on a hard landing, in metres. 0 = off")]
        public float landingDipAmount = 0.15f;
        [Tooltip("Falls slower than this (m/s) cause no landing dip")]
        public float landingDipMinFallSpeed = 5f;
        [Tooltip("Fall speed (m/s) that gives the full landing dip")]
        public float landingDipFullFallSpeed = 15f;
        [Tooltip("How quickly the camera recovers from a landing dip")]
        public float landingRecoverSpeed = 8f;

        [Header("Events")]
        public UnityEvent OnDashStart;
        public UnityEvent OnDashComplete;

        public UnityEvent OnSprintStart;
        public UnityEvent OnSprintComplete;

        private float _cinemachineTargetPitch;
        private float _cameraRoll;
        private float _cameraPitchOffset;

        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private Vector3 _horizontalVelocity;

        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _jumpCutAvailable;
        private bool _wasGrounded = true;
        private float _lastAirVerticalVelocity;

        private int _airJumpsRemaining;

        private bool _isDashing;
        private float _dashTimer;
        private float _dashCooldownTimer;
        private Vector3 _dashDirection;

        private bool _isSprintingState;

        private bool _isClimbing;
        private float _climbTimer;
        private Vector3 _climbStart;
        private Vector3 _climbTarget;

        private bool _isWallRunning;
        private bool _wallOnRight;
        private Vector3 _wallNormal;
        private float _wallReattachTimer;
        private float _wallRunTime;
        private bool _wallRunPaid;
        private float _airControlLockTimer;

        private bool _isAutoTurning;
        private float _autoTurnAngle;
        private float _autoTurnApplied;
        private float _autoTurnElapsed;

        private bool _isVerticalWallRunning;
        private bool _verticalWallRunUsed;
        private Vector3 _lastVerticalWallNormal;
        private float _verticalWallRunTimer;
        private Vector3 _frontWallNormal;

        private Vector3 _cameraTargetBaseLocalPos;
        private Vector3 _bobOffset;
        private float _bobTimer;
        private float _landingOffset;
        private bool _fovReady;
        private float _baseFov;
        private float _currentFov;
        private Component _lensOwner;
        private FieldInfo _lensField;
        private FieldInfo _lensFovField;
        private Camera _fovPlainCamera;

        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
        private InputAction _jumpAction;
#endif
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private const float _threshold = 0.01f;
        private ActionPointManager actionPointManager;

        public int CurrentAP => actionPointManager != null ? actionPointManager.GetAP() : 0;

        private bool HasAP => CurrentAP >= 1;

        public float DashCooldownRemaining => Mathf.Max(_dashCooldownTimer, 0f);

        public int AirJumpsRemaining => _airJumpsRemaining;

        public bool IsDashing => _isDashing;

        public bool IsClimbing => _isClimbing;

        public Vector3 HorizontalVelocity => _horizontalVelocity;

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

        private bool JumpHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _jumpAction == null || _jumpAction.IsPressed();
#else
                return true;
#endif
            }
        }

        private void Awake()
        {
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
            if (_playerInput != null && _playerInput.actions != null)
            {
                _jumpAction = _playerInput.actions.FindAction("Jump");
            }
#else
            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            actionPointManager = GetComponent<ActionPointManager>();
            if (actionPointManager == null)
            {
                actionPointManager = FindObjectOfType<ActionPointManager>();
            }
            if (actionPointManager == null)
            {
                Debug.LogWarning("FirstPersonController: no ActionPointManager found. AP abilities (wall run, wall jump, air jump, dash) will be unavailable.");
            }

            if (LedgeLayers.value == 0)
            {
                LedgeLayers = LayerMask.GetMask("Ledge");
            }

            _cameraTargetBaseLocalPos = CinemachineCameraTarget.transform.localPosition;
            SetupFov();

            _airJumpsRemaining = doubleJumpCount;

            _jumpTimeoutDelta = 0f;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            GroundedCheck();

            if (_isClimbing)
            {
                UpdateLedgeClimb();
                UpdateSprintEvents();
                return;
            }

            if (TryStartLedgeClimb())
            {
                return;
            }

            BufferJumpInput();

            WallCheck();
            Dash();
            JumpAndGravity();
            Move();
            UpdateSprintEvents();
        }

        private void LateUpdate()
        {
            CameraRotation();
            UpdateCameraEffects();
        }

        private void SpendAP()
        {
            if (actionPointManager != null)
            {
                actionPointManager.UseAP();
            }
        }

        private float JumpVelocity => Mathf.Sqrt(JumpHeight * -2f * Gravity);

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);

            int mask = GroundLayers.value;
            if (excludeWallsFromGroundCheck) mask &= ~WallLayers.value;

            bool sphereHit = mask != 0 && Physics.CheckSphere(spherePosition, GroundedRadius, mask, QueryTriggerInteraction.Ignore);

            bool controllerHit = excludeWallsFromGroundCheck && _controller.enabled
                && (_controller.collisionFlags & CollisionFlags.Below) != 0;

            Grounded = sphereHit || controllerHit;
        }

        private void BufferJumpInput()
        {
            if (_jumpBufferTimer > 0f)
            {
                _jumpBufferTimer -= Time.deltaTime;
            }

            if (_input.jump)
            {
                _jumpBufferTimer = Mathf.Max(jumpBufferTime, 0.001f);
                _input.jump = false;
            }
        }

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

            EndDash(false);
            _verticalVelocity = 0f;
            _horizontalVelocity = Vector3.zero;
            _isWallRunning = false;
            _isVerticalWallRunning = false;
            _airJumpsRemaining = doubleJumpCount;
            _isAutoTurning = false;
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
            _jumpCutAvailable = false;
            _airControlLockTimer = 0f;

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
            _jumpBufferTimer = 0f;

            if (t >= 1f)
            {
                _isClimbing = false;
                _controller.enabled = true;
                _verticalVelocity = -2f;
                _wasGrounded = true;
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

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Vector3 n = hit.normal;
            if (Mathf.Abs(n.y) > 0.3f) return;

            n.y = 0f;
            if (n.sqrMagnitude < 0.0001f) return;
            n.Normalize();

            float into = Vector3.Dot(_horizontalVelocity, n);
            if (into < 0f)
            {
                _horizontalVelocity -= n * into;
            }
        }

        private static Vector3 Flatten(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.zero;
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

                _dashDirection = Flatten(transform.forward);

                _isWallRunning = false;
                _isVerticalWallRunning = false;
            }

            _input.dash = false;

            if (_isDashing)
            {
                _dashTimer -= Time.deltaTime;
                if (_dashTimer <= 0f)
                {
                    EndDash(true);
                }
            }
        }

        private void EndDash(bool keepMomentum)
        {
            if (!_isDashing) return;

            _isDashing = false;
            if (keepMomentum)
            {
                _horizontalVelocity = _dashDirection * (dashSpeed * dashExitSpeedMultiplier);
            }
            OnDashComplete?.Invoke();
        }

        private void StartWallJumpTurn(Vector3 wallNormal)
        {
            if (!rotateOnWallJump) return;

            Vector3 forward = Flatten(transform.forward);
            Vector3 away = Flatten(wallNormal);
            if (forward == Vector3.zero || away == Vector3.zero) return;

            float angle = Vector3.SignedAngle(forward, away, Vector3.up);

            if (Mathf.Abs(angle) < 0.5f) return;

            _autoTurnAngle = angle;
            _autoTurnApplied = 0f;
            _autoTurnElapsed = 0f;
            _isAutoTurning = true;
        }

        private void UpdateWallJumpTurn()
        {
            if (!_isAutoTurning) return;

            _autoTurnElapsed += Time.deltaTime;
            float t = wallJumpTurnDuration > 0f ? Mathf.Clamp01(_autoTurnElapsed / wallJumpTurnDuration) : 1f;

            float targetApplied = _autoTurnAngle * Mathf.SmoothStep(0f, 1f, t);
            transform.Rotate(Vector3.up * (targetApplied - _autoTurnApplied));
            _autoTurnApplied = targetApplied;

            if (t >= 1f)
            {
                _isAutoTurning = false;
            }
        }

        private void CameraRotation()
        {
            _rotationVelocity = 0f;

            if (_input.look.sqrMagnitude >= _threshold)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
                _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

                transform.Rotate(Vector3.up * _rotationVelocity);
            }

            if (_isAutoTurning && wallJumpTurnCancelOnLook && Mathf.Abs(_rotationVelocity) >= wallJumpTurnCancelThreshold)
            {
                _isAutoTurning = false;
            }

            UpdateWallJumpTurn();

            float targetRoll = 0f;
            if (_isWallRunning) targetRoll = _wallOnRight ? wallRunCameraTilt : -wallRunCameraTilt;

            _cameraRoll = Mathf.Lerp(_cameraRoll, targetRoll, Time.deltaTime * cameraTiltSpeed);

            float targetPitchOffset = _isVerticalWallRunning ? verticalWallRunCameraPitch : 0f;
            _cameraPitchOffset = Mathf.Lerp(_cameraPitchOffset, targetPitchOffset, Time.deltaTime * cameraTiltSpeed);

            float finalPitch = ClampAngle(_cinemachineTargetPitch - _cameraPitchOffset, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(finalPitch, 0.0f, _cameraRoll);
        }

        private void UpdateCameraEffects()
        {
            float dt = Time.deltaTime;

            if (_fovReady)
            {
                float targetFov = _baseFov;
                if (_isSprintingState) targetFov += sprintFovBoost;
                if (_isWallRunning || _isVerticalWallRunning) targetFov += wallRunFovBoost;
                if (_isDashing) targetFov += dashFovBoost;

                _currentFov = Mathf.Lerp(_currentFov, targetFov, 1f - Mathf.Exp(-fovChangeSpeed * dt));
                ApplyFov(_currentFov);
            }

            float horizontalSpeed = _horizontalVelocity.magnitude;
            Vector3 bobTarget = Vector3.zero;
            if (headBobAmount > 0f && Grounded && !_isDashing && !_isClimbing && horizontalSpeed > 0.5f)
            {
                _bobTimer += dt * headBobFrequency * (horizontalSpeed / Mathf.Max(MoveSpeed, 0.01f));
                bobTarget = new Vector3(
                    Mathf.Cos(_bobTimer * 0.5f) * headBobAmount * 0.5f,
                    Mathf.Sin(_bobTimer) * headBobAmount,
                    0f);
            }
            _bobOffset = Vector3.Lerp(_bobOffset, bobTarget, 1f - Mathf.Exp(-12f * dt));

            _landingOffset = Mathf.Lerp(_landingOffset, 0f, 1f - Mathf.Exp(-landingRecoverSpeed * dt));

            CinemachineCameraTarget.transform.localPosition = _cameraTargetBaseLocalPos + _bobOffset + Vector3.up * _landingOffset;
        }

        private void OnLanded()
        {
            float fallSpeed = -_lastAirVerticalVelocity;
            _lastAirVerticalVelocity = 0f;

            if (landingDipAmount <= 0f || fallSpeed < landingDipMinFallSpeed) return;

            float strength = Mathf.Clamp01(fallSpeed / Mathf.Max(landingDipFullFallSpeed, 0.01f));
            _landingOffset = -landingDipAmount * strength;
        }

        private void SetupFov()
        {
            GameObject target = fovCamera != null ? fovCamera : GameObject.Find("PlayerFollowCamera");
            if (target == null)
            {
                Debug.LogWarning("FirstPersonController: no FOV camera assigned or found. FOV effects are disabled.");
                return;
            }

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            foreach (Component c in target.GetComponents<Component>())
            {
                if (c == null) continue;
                System.Type type = c.GetType();
                FieldInfo lens = type.GetField("Lens", flags) ?? type.GetField("m_Lens", flags);
                if (lens == null) continue;
                FieldInfo fov = lens.FieldType.GetField("FieldOfView", flags);
                if (fov == null) continue;

                _lensOwner = c;
                _lensField = lens;
                _lensFovField = fov;
                _baseFov = (float)fov.GetValue(lens.GetValue(c));
                _fovReady = true;
                break;
            }

            if (!_fovReady)
            {
                _fovPlainCamera = target.GetComponent<Camera>();
                if (_fovPlainCamera != null)
                {
                    _baseFov = _fovPlainCamera.fieldOfView;
                    _fovReady = true;
                }
            }

            if (!_fovReady)
            {
                Debug.LogWarning($"FirstPersonController: couldn't find a lens or Camera on '{target.name}'. FOV effects are disabled.");
            }

            _currentFov = _baseFov;
        }

        private void ApplyFov(float fov)
        {
            if (_lensOwner != null)
            {
                object lens = _lensField.GetValue(_lensOwner);
                _lensFovField.SetValue(lens, fov);
                _lensField.SetValue(_lensOwner, lens);
            }
            else if (_fovPlainCamera != null)
            {
                _fovPlainCamera.fieldOfView = fov;
            }
        }

        private void Move()
        {
            if (_isDashing)
            {
                _horizontalVelocity = _dashDirection * dashSpeed;
            }
            else
            {
                Vector3 inputDirection = Vector3.zero;
                if (_input.move != Vector2.zero)
                {
                    inputDirection = (transform.right * _input.move.x + transform.forward * _input.move.y).normalized;
                }

                float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
                if (_input.move == Vector2.zero) targetSpeed = 0f;
                float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;
                Vector3 targetVelocity = inputDirection * (targetSpeed * inputMagnitude);

                bool fullControl = Grounded || _isWallRunning || _isVerticalWallRunning;
                float rate;
                if (fullControl) rate = SpeedChangeRate;
                else if (_input.move == Vector2.zero) rate = airDrag;
                else rate = SpeedChangeRate * airControl * AirControlLockFactor();

                _horizontalVelocity = Vector3.Lerp(_horizontalVelocity, targetVelocity, 1f - Mathf.Exp(-rate * Time.deltaTime));
            }

            _controller.Move((_horizontalVelocity + Vector3.up * _verticalVelocity) * Time.deltaTime);
        }

        private float AirControlLockFactor()
        {
            if (_airControlLockTimer <= 0f || wallJumpAirControlLockTime <= 0f) return 1f;

            float progress = 1f - Mathf.Clamp01(_airControlLockTimer / wallJumpAirControlLockTime);
            return Mathf.Lerp(wallJumpAirControlMultiplier, 1f, Mathf.SmoothStep(0f, 1f, progress));
        }

        private void UpdateSprintEvents()
        {
            bool sprinting = !_isDashing && !_isClimbing && _input.sprint && _input.move.sqrMagnitude > 0f;

            if (sprinting && !_isSprintingState) OnSprintStart?.Invoke();
            else if (!sprinting && _isSprintingState) OnSprintComplete?.Invoke();

            _isSprintingState = sprinting;
        }

        private void JumpAndGravity()
        {
            bool jumped = false;
            bool jumpRequested = _jumpBufferTimer > 0f;

            if (_jumpTimeoutDelta > 0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }

            if (_airControlLockTimer > 0f)
            {
                _airControlLockTimer -= Time.deltaTime;
            }

            if (Grounded)
            {
                _airControlLockTimer = 0f;

                if (!_wasGrounded) OnLanded();

                _isWallRunning = false;
                _isVerticalWallRunning = false;
                _verticalWallRunUsed = false;
                _wallReattachTimer = 0f;

                _fallTimeoutDelta = FallTimeout;

                _airJumpsRemaining = doubleJumpCount;

                if (_verticalVelocity <= 0f)
                {
                    _coyoteTimer = coyoteTime;
                }

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (jumpRequested && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = JumpVelocity;
                    _coyoteTimer = 0f;
                    _jumpTimeoutDelta = JumpTimeout;
                    jumped = true;
                }
            }
            else
            {
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }

                if (!_isDashing && _coyoteTimer > 0f)
                {
                    _coyoteTimer -= Time.deltaTime;
                }

                if (_wallReattachTimer > 0f)
                {
                    _wallReattachTimer -= Time.deltaTime;
                }

                UpdateVerticalWallRun();
                UpdateSideWallRun();

                if ((_isWallRunning && _wallRunPaid) || _isVerticalWallRunning)
                {
                    _airJumpsRemaining = doubleJumpCount;
                }

                if (jumpRequested)
                {
                    if (_isVerticalWallRunning && HasAP)
                    {
                        SpendAP();
                        _verticalVelocity = JumpVelocity;

                        _horizontalVelocity = Flatten(_frontWallNormal) * verticalWallJumpBackForce;

                        _isVerticalWallRunning = false;
                        _wallReattachTimer = wallReattachDelay;
                        _airControlLockTimer = wallJumpAirControlLockTime;
                        StartWallJumpTurn(_frontWallNormal);
                        jumped = true;
                    }
                    else if (_isWallRunning && HasAP)
                    {
                        SpendAP();
                        _verticalVelocity = JumpVelocity;

                        Vector3 away = Flatten(_wallNormal);
                        float into = Vector3.Dot(_horizontalVelocity, away);
                        if (into < 0f) _horizontalVelocity -= away * into;
                        _horizontalVelocity += away * wallJumpSideForce;

                        _isWallRunning = false;
                        _wallReattachTimer = wallReattachDelay;
                        _airControlLockTimer = wallJumpAirControlLockTime;
                        jumped = true;
                    }
                    else if (_coyoteTimer > 0f)
                    {
                        _verticalVelocity = JumpVelocity;
                        _coyoteTimer = 0f;
                        _jumpTimeoutDelta = JumpTimeout;
                        jumped = true;
                    }
                    else if (_airJumpsRemaining > 0 && HasAP)
                    {
                        SpendAP();
                        _verticalVelocity = JumpVelocity;
                        _airJumpsRemaining--;
                        jumped = true;
                    }
                }
            }

            if (jumped)
            {
                _jumpBufferTimer = 0f;
                _jumpCutAvailable = true;
                EndDash(true);
            }

            if (_jumpCutAvailable)
            {
                if (_verticalVelocity <= 0f || _isWallRunning || _isVerticalWallRunning)
                {
                    _jumpCutAvailable = false;
                }
                else if (variableJumpHeight && !JumpHeld)
                {
                    _verticalVelocity *= jumpCutMultiplier;
                    _jumpCutAvailable = false;
                }
            }

            if (_isDashing)
            {
                _verticalVelocity = Grounded ? -2f : 0f;
            }
            else if (_isVerticalWallRunning)
            {
                float progress = verticalWallRunDuration > 0f ? 1f - Mathf.Clamp01(_verticalWallRunTimer / verticalWallRunDuration) : 1f;
                float targetSpeed = Mathf.Lerp(verticalWallRunSpeed, verticalWallRunSpeed * verticalWallRunEndSpeedFraction, Mathf.SmoothStep(0f, 1f, progress));
                _verticalVelocity = Mathf.MoveTowards(_verticalVelocity, targetSpeed, verticalWallRunAcceleration * Time.deltaTime);
            }
            else if (_isWallRunning)
            {
                _verticalVelocity += Gravity * wallRunGravityMultiplier * Time.deltaTime;

                if (_verticalVelocity < -wallRunMaxFallSpeed)
                {
                    _verticalVelocity = Mathf.MoveTowards(_verticalVelocity, -wallRunMaxFallSpeed, wallRunCatchStrength * Time.deltaTime);
                }
            }
            else
            {
                float gravity = _verticalVelocity < 0f ? Gravity * fallGravityMultiplier : Gravity;
                _verticalVelocity += gravity * Time.deltaTime;
                _verticalVelocity = Mathf.Max(_verticalVelocity, -_terminalVelocity);
            }

            if (!Grounded) _lastAirVerticalVelocity = _verticalVelocity;
            _wasGrounded = Grounded;
        }

        private void UpdateSideWallRun()
        {
            bool wasWallRunning = _isWallRunning;
            bool wantsWallRun = !_isDashing && !_isVerticalWallRunning && touchingWall && _input.move.y > 0f;

            if (wantsWallRun && !wasWallRunning)
            {
                if (_wallReattachTimer <= 0f && HasAP)
                {
                    _isWallRunning = true;
                    _wallRunTime = 0f;
                    _wallRunPaid = false;
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

            if (!_isWallRunning) return;

            _wallRunTime += Time.deltaTime;
            if (!_wallRunPaid && _wallRunTime >= wallRunCostDelay)
            {
                if (HasAP)
                {
                    SpendAP();
                    _wallRunPaid = true;
                }
                else
                {
                    _isWallRunning = false;
                }
            }
        }

        private void UpdateVerticalWallRun()
        {
            bool differentWall = Vector3.Dot(_frontWallNormal, _lastVerticalWallNormal) < 0.7f;
            bool canStartVerticalRun = !_verticalWallRunUsed || differentWall;

            if (!_isDashing && !_isVerticalWallRunning && canStartVerticalRun && touchingFrontWall && _input.move.y > 0f
                && _wallReattachTimer <= 0f && HasAP)
            {
                SpendAP();
                _isVerticalWallRunning = true;
                _verticalWallRunUsed = true;
                _lastVerticalWallNormal = _frontWallNormal;
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