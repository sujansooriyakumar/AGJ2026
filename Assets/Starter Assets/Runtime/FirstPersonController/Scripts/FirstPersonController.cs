using UnityEngine;
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
        [Tooltip("Whether the air jump has been used since last touching the ground or a wall")]
        public bool doubleJumpUsed = false;

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

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;
        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 90.0f;
        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -90.0f;

        // cinemachine
        private float _cinemachineTargetPitch;
        private float _cameraRoll;

        // player
        private float _speed;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

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

        /// <summary>Seconds until the dash is ready again (0 when ready). Handy for a UI cooldown indicator.</summary>
        public float DashCooldownRemaining => Mathf.Max(_dashCooldownTimer, 0f);

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

            // default to the "Ledge" layer if nothing was assigned in the Inspector
            if (LedgeLayers.value == 0)
            {
                LedgeLayers = LayerMask.GetMask("Ledge");
            }

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
            // must be pushing forward, and optionally airborne
            if (_input.move.y <= 0f) return false;
            if (Grounded && !climbFromGround) return false;

            float radius = _controller.radius;

            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 feet = transform.position + FeetOffset;

            // cast straight down just in front of the player, from max grab height to min grab height.
            // if the obstacle is taller than ledgeMaxHeight the ray starts inside it and hits nothing, so no climb.
            Vector3 origin = feet + forward * (radius + ledgeReach) + Vector3.up * ledgeMaxHeight;
            float castDistance = ledgeMaxHeight - ledgeMinHeight;

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit topHit, castDistance, LedgeLayers, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // the top must be a walkable, fairly flat surface
            if (topHit.normal.y < 0.7f) return false;

            // stand one radius past the edge so the whole capsule ends up on the surface
            Vector3 targetFeet = topHit.point + forward * radius + Vector3.up * 0.05f;

            // make sure the player actually fits on top (no low ceiling or obstacle in the way)
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

            // cancel everything else that could be moving the player
            _verticalVelocity = 0f;
            _isDashing = false;
            _isWallRunning = false;
            _isVerticalWallRunning = false;
            _wallJumpVelocity = Vector3.zero;
            doubleJumpUsed = false;

            // disable the controller so it doesn't collide with the ledge edge while we move the transform directly
            _controller.enabled = false;
        }

        private void UpdateLedgeClimb()
        {
            _climbTimer += Time.deltaTime;
            float t = ledgeClimbDuration > 0f ? Mathf.Clamp01(_climbTimer / ledgeClimbDuration) : 1f;

            // two-phase path: straight up to the ledge height, then forward onto the surface
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

            // ignore jump/dash presses during the climb so they don't fire the moment it ends
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

            // front wall (vertical wall run)
            if (Physics.Raycast(origin, transform.forward, out RaycastHit frontHit, frontWallCheckDistance, WallLayers, QueryTriggerInteraction.Ignore))
            {
                // only count it if we're facing the wall fairly head-on
                if (Vector3.Dot(transform.forward, -frontHit.normal) >= frontWallMinFacing)
                {
                    touchingFrontWall = true;
                    _frontWallNormal = frontHit.normal;
                }
            }

            // side walls (horizontal wall run)
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
            // count down the cooldown
            if (_dashCooldownTimer > 0f)
            {
                _dashCooldownTimer -= Time.deltaTime;
            }

            // start a dash if pressed and off cooldown
            if (_input.dash && !_isDashing && _dashCooldownTimer <= 0f)
            {
                _isDashing = true;
                _dashTimer = dashDuration;
                _dashCooldownTimer = dashCooldown;

                // dash in the direction the player is facing, flattened so looking up/down doesn't matter
                _dashDirection = transform.forward;
                _dashDirection.y = 0f;
                _dashDirection.Normalize();

                // a dash overrides any wall run or leftover wall jump push
                _isWallRunning = false;
                _isVerticalWallRunning = false;
                _wallJumpVelocity = Vector3.zero;
            }

            // consume the input so a press during cooldown isn't saved for later
            _input.dash = false;

            // run the dash timer
            if (_isDashing)
            {
                _dashTimer -= Time.deltaTime;
                if (_dashTimer <= 0f)
                {
                    _isDashing = false;
                }
            }
        }

        private void CameraRotation()
        {
            // if there is an input
            if (_input.look.sqrMagnitude >= _threshold)
            {
                //Don't multiply mouse input by Time.deltaTime
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
                _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

                // clamp our pitch rotation
                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

                // rotate the player left and right
                transform.Rotate(Vector3.up * _rotationVelocity);
            }

            // tilt away from the wall while side wall running (flip the sign if it leans the wrong way)
            float targetRoll = 0f;
            if (_isWallRunning) targetRoll = _wallOnRight ? wallRunCameraTilt : -wallRunCameraTilt;

            _cameraRoll = Mathf.Lerp(_cameraRoll, targetRoll, Time.deltaTime * cameraTiltSpeed);

            // apply pitch and roll every frame so the tilt is never overwritten
            CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, _cameraRoll);
        }

        private void Move()
        {
            // while dashing, ignore normal movement and go straight along the dash direction
            if (_isDashing)
            {
                _controller.Move((_dashDirection * dashSpeed + new Vector3(0.0f, _verticalVelocity, 0.0f)) * Time.deltaTime);
                return;
            }

            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity (excluding the wall jump push)
            Vector3 horizontalVelocity = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z) - _wallJumpVelocity;
            float currentHorizontalSpeed = horizontalVelocity.magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            // (after a dash this also eases the player back down from dash speed)
            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // if there is a move input, move relative to the player's facing
            if (_input.move != Vector2.zero)
            {
                inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
            }

            // fade out the wall jump push over time
            _wallJumpVelocity = Vector3.MoveTowards(_wallJumpVelocity, Vector3.zero, wallJumpDecay * Time.deltaTime);

            // move the player
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

                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // landed, so the double jump is available again
                doubleJumpUsed = false;

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    jumped = true;

                    // consume the input so the same press doesn't also trigger the double jump
                    _input.jump = false;
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }

                UpdateVerticalWallRun();

                // side wall run: touching a side wall, pushing forward, and not running up a wall or dashing
                bool wasWallRunning = _isWallRunning;
                _isWallRunning = !_isDashing && !_isVerticalWallRunning && touchingWall && _input.move.y > 0f;

                // kill downward momentum when first latching onto the wall
                if (_isWallRunning && !wasWallRunning && _verticalVelocity < 0f)
                {
                    _verticalVelocity = 0f;
                }

                // touching a wall refreshes the double jump, so you can chain walls
                if (_isWallRunning || _isVerticalWallRunning) doubleJumpUsed = false;

                if (_input.jump)
                {
                    if (_isVerticalWallRunning)
                    {
                        // jump off the wall in front: up and backwards
                        _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                        _wallJumpVelocity = _frontWallNormal * verticalWallJumpBackForce;
                        _isVerticalWallRunning = false;
                        jumped = true;
                    }
                    else if (_isWallRunning)
                    {
                        // side wall jump: up and away from the wall
                        _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                        _wallJumpVelocity = _wallNormal * wallJumpSideForce;
                        _isWallRunning = false;
                        jumped = true;
                    }
                    else if (!doubleJumpUsed)
                    {
                        // double jump
                        _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                        doubleJumpUsed = true;
                        jumped = true;
                    }
                }

                // always consume jump input while airborne
                _input.jump = false;
            }

            // jumping cancels a dash early (the dash speed then eases off through Move's speed lerp)
            if (jumped) _isDashing = false;

            // apply gravity
            if (_isDashing)
            {
                // dash travels in a flat line: stay pinned to the ground, or hold height in the air
                _verticalVelocity = Grounded ? -2f : 0f;
            }
            else if (_isVerticalWallRunning)
            {
                // run straight up the wall at a constant speed
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
            // start: facing a wall, pushing forward, not dashing, and haven't used the vertical run since last landing
            if (!_isDashing && !_isVerticalWallRunning && !_verticalWallRunUsed && touchingFrontWall && _input.move.y > 0f)
            {
                _isVerticalWallRunning = true;
                _verticalWallRunUsed = true;
                _verticalWallRunTimer = verticalWallRunDuration;
            }

            if (!_isVerticalWallRunning) return;

            _verticalWallRunTimer -= Time.deltaTime;

            // stop when time runs out, the wall ends (e.g. reached a ledge), or the player lets go of forward.
            // the leftover upward velocity carries the player over a ledge naturally.
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

            // draw the wall check rays
            Vector3 origin = transform.position + Vector3.up * (controller.height * 0.5f);

            Gizmos.color = touchingWall ? Color.cyan : Color.yellow;
            Gizmos.DrawLine(origin, origin + transform.right * wallCheckDistance);
            Gizmos.DrawLine(origin, origin - transform.right * wallCheckDistance);

            Gizmos.color = touchingFrontWall ? Color.magenta : Color.yellow;
            Gizmos.DrawLine(origin, origin + transform.forward * frontWallCheckDistance);

            // draw the ledge detection ray (from max grab height down to min grab height)
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