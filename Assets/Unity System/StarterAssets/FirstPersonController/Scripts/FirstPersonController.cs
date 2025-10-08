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
		public float MoveSpeed = 2.0f;
		[Tooltip("Sprint speed of the character in m/s")]
		public float SprintSpeed = 3.0f;
		[Tooltip("Rotation speed of the character")]
		public float RotationSpeed = 1.0f;
		[Tooltip("Acceleration and deceleration")]
		public float SpeedChangeRate = 10.0f;

		[Space(10)]
		[Tooltip("The height the player can jump")]
		public float JumpHeight = 0.15f;
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

		[Header("Cinemachine")]
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		public GameObject CinemachineCameraTarget;
		[Tooltip("How far in degrees can you move the camera up")]
		public float TopClamp = 90.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		public float BottomClamp = -90.0f;

		// 👇 NEW CROUCH VARIABLES
		[Header("Crouch Settings")]
		[Tooltip("Movement speed while crouching")]
		public float CrouchSpeed = 1.0f;
		[Tooltip("Reduced height while crouched")]
		public float CrouchHeight = 0.8f;
		[Tooltip("Original standing height")]
		public float StandHeight = 1.6f;
		[Tooltip("How quickly to transition between heights")]
		public float CrouchTransitionSpeed = 8f;
		[Tooltip("Camera height offset when crouched")]
		public float CrouchCameraOffset = 0.5f;

		private bool _isCrouching = false;
		private Vector3 _cameraDefaultPosition;

		[Header("Cat Procedural Sway")]
		[Tooltip("Horizontal sway (side-to-side)")]
		public float CatSwayHorizontal = 0.1f;
		[Tooltip("Vertical sway (up-and-down)")]
		public float CatSwayVertical = 0.08f;
		[Tooltip("Head roll tilt in degrees")]
		public float CatSwayRoll = 3f;
		[Tooltip("Base frequency of the sway")]
		public float CatSwayFrequency = 2.5f;

		private float _catSwayTimer = 0f;
		private Vector3 _cameraOriginalPos;

		// cinemachine
		private float _cinemachineTargetPitch;

		// player
		private float _speed;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;

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
			_cameraOriginalPos = CinemachineCameraTarget.transform.localPosition;
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();
		#if ENABLE_INPUT_SYSTEM
			_playerInput = GetComponent<PlayerInput>();
		#endif

			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;

			_cameraDefaultPosition = CinemachineCameraTarget.transform.localPosition;
			// _initialCameraPosition = _cameraDefaultPosition; // store for sway
			StandHeight = _controller.height;
		}

		private void Update()
		{
			GroundedCheck();
			HandleCrouch(); // 👈 NEW
			JumpAndGravity();
			Move();
		}

		private void LateUpdate()
		{
			CameraRotation();
			ApplyCatSway();
		}

		// ----- change ApplyCatSway to use GetMoveInput() instead of _input.move -----
		private void ApplyCatSway()
		{
			Vector2 moveInput = GetMoveInput(); // <- updated here
			float speedFactor = _speed / Mathf.Max(MoveSpeed, 0.0001f);

			if (moveInput.sqrMagnitude > 0.01f)
			{
				// increment timer based on speed
				_catSwayTimer += Time.deltaTime * CatSwayFrequency * speedFactor;

				// horizontal sway (side-to-side)
				float swayX = Mathf.Sin(_catSwayTimer) * CatSwayHorizontal;

				// vertical bob (up-down) with slight asymmetry
				float swayY = Mathf.Sin(_catSwayTimer * 2f + Mathf.PI / 2) * CatSwayVertical;

				// slight roll (head tilt)
				float swayZ = Mathf.Sin(_catSwayTimer) * CatSwayRoll;

				// preserve crouch offset
				Vector3 basePos = _cameraOriginalPos;
				if (_isCrouching)
					basePos -= new Vector3(0, CrouchCameraOffset, 0);

				// combine offsets
				Vector3 targetPos = basePos + transform.right * swayX + transform.up * swayY;
				CinemachineCameraTarget.transform.localPosition =
					Vector3.Lerp(CinemachineCameraTarget.transform.localPosition, targetPos, Time.deltaTime * 10f);

				// apply roll
				CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0f, swayZ);
			}
			else
			{
				// reset sway when stationary
				_catSwayTimer = 0f;
				Vector3 basePos = _cameraOriginalPos;
				if (_isCrouching)
					basePos -= new Vector3(0, CrouchCameraOffset, 0);

				CinemachineCameraTarget.transform.localPosition =
					Vector3.Lerp(CinemachineCameraTarget.transform.localPosition, basePos, Time.deltaTime * 10f);
				CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0f, 0f);
			}
		}


		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}

		private void CameraRotation()
		{
			if (_input.look.sqrMagnitude >= _threshold)
			{
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

				_cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
				_rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

				_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);
				CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);
				transform.Rotate(Vector3.up * _rotationVelocity);
			}
		}

		// ----- replace your Move() method with this updated version -----
		private void Move()
		{
			// read the current move input robustly
			Vector2 moveInput = GetMoveInput();

			// 🐾 Determine target speed based on state
			float targetSpeed = _isCrouching ? CrouchSpeed : (_input.sprint ? SprintSpeed : MoveSpeed);
			if (moveInput == Vector2.zero) targetSpeed = 0.0f;

			// Smooth speed transition (honors analogMovement)
			float inputMagnitude = _input.analogMovement ? moveInput.magnitude : (moveInput == Vector2.zero ? 0f : 1f);
			_speed = Mathf.Lerp(_speed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
			_speed = Mathf.Round(_speed * 1000f) / 1000f;

			// Compute direction from current transform and current input (always recalculated)
			Vector3 inputDirection = new Vector3(moveInput.x, 0.0f, moveInput.y);
			Vector3 moveDirection = (transform.right * inputDirection.x) + (transform.forward * inputDirection.z);

			// Apply movement (normalized so diagonals aren't faster)
			if (moveInput != Vector2.zero)
			{
				_controller.Move(moveDirection.normalized * (_speed * Time.deltaTime)
					+ Vector3.up * _verticalVelocity * Time.deltaTime);
			}
			else
			{
				// only vertical movement if no horizontal input
				_controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
			}
		}

		// ----- add this helper anywhere inside the class -----
		private Vector2 GetMoveInput()
		{
		#if ENABLE_INPUT_SYSTEM
			// prefer the StarterAssets input vector, but if keyboard keys are pressed read them directly
			Vector2 inMove = _input.move;

			var kb = UnityEngine.InputSystem.Keyboard.current;
			if (kb != null)
			{
				bool w = kb.wKey.isPressed;
				bool s = kb.sKey.isPressed;
				bool a = kb.aKey.isPressed;
				bool d = kb.dKey.isPressed;

				// if any WASD key is pressed, compose from those keys (guarantees exact combination)
				if (w || s || a || d)
				{
					Vector2 kbVec = Vector2.zero;
					if (w) kbVec.y += 1f;
					if (s) kbVec.y -= 1f;
					if (a) kbVec.x -= 1f;
					if (d) kbVec.x += 1f;

					return kbVec == Vector2.zero ? Vector2.zero : kbVec.normalized;
				}
			}

			// otherwise return whatever the StarterAssets input has (gamepad, remapped keys, etc.)
			return inMove;
		#else
			// legacy Input Manager fallback (if not using the new input system)
			float h = Input.GetAxisRaw("Horizontal");
			float v = Input.GetAxisRaw("Vertical");
			Vector2 legacy = new Vector2(h, v);
			return legacy == Vector2.zero ? _input.move : (legacy.normalized);
		#endif
		}

		private void JumpAndGravity()
		{
			if (Grounded)
			{
				_fallTimeoutDelta = FallTimeout;

				// ✅ Always reset vertical velocity when grounded (prevents boosted jumps)
				if (_verticalVelocity < 0.0f || Mathf.Abs(_verticalVelocity) < 0.5f)
					_verticalVelocity = -2f;

				// Jump
				if (_input.jump && _jumpTimeoutDelta <= 0.0f && !_isCrouching)
				{
					// Calculate the jump velocity needed for the desired height
					_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
				}

				// Reset jump timeout
				if (_jumpTimeoutDelta >= 0.0f)
					_jumpTimeoutDelta -= Time.deltaTime;
			}
			else
			{
				// Reset the jump timeout timer
				_jumpTimeoutDelta = JumpTimeout;

				// Apply fall timeout (used for step-down smoothing)
				if (_fallTimeoutDelta >= 0.0f)
					_fallTimeoutDelta -= Time.deltaTime;

				// Disable jump input while in the air
				_input.jump = false;
			}

			// Apply gravity when not grounded
			if (_verticalVelocity < _terminalVelocity)
			{
				_verticalVelocity += Gravity * Time.deltaTime;
			}

			// ✅ Ceiling collision protection (prevents getting stuck)
			if ((_controller.collisionFlags & CollisionFlags.Above) != 0 && _verticalVelocity > 0f)
			{
				_verticalVelocity = 0f;
			}
		}
		
		// 🐱 CROUCH LOGIC
		private void HandleCrouch()
		{
			// Hold-to-crouch: crouch only while the button is held down
			_isCrouching = _input.crouch;

			float targetHeight = _isCrouching ? CrouchHeight : StandHeight;
			_controller.height = Mathf.Lerp(_controller.height, targetHeight, Time.deltaTime * CrouchTransitionSpeed);

			Vector3 targetCamPos = _cameraDefaultPosition;
			if (_isCrouching)
				targetCamPos = _cameraDefaultPosition - new Vector3(0, CrouchCameraOffset, 0);

			CinemachineCameraTarget.transform.localPosition =
				Vector3.Lerp(CinemachineCameraTarget.transform.localPosition, targetCamPos, Time.deltaTime * CrouchTransitionSpeed);
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
			Gizmos.color = Grounded ? transparentGreen : transparentRed;
			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
		}
	}
}
