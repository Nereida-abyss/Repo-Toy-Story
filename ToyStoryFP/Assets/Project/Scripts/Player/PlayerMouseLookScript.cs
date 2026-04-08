using UnityEngine;

public class MouseLookScript : MonoBehaviour
{
    private const string LookSensitivityKey = "settings.lookSensitivity";
    private const float DefaultLookSensitivity = 2f;
    private const float MinLookSensitivity = 0.5f;
    private const float MaxLookSensitivity = 5f;

    private enum JumpCameraPhase
    {
        None,
        Rise,
        Fall,
        BounceDrop,
        BounceRecover
    }

    public static MouseLookScript instance;

    [Header("Mouse Look Settings")]
    public Vector2 clampInDegrees = new Vector2(360, 180);
    public bool lockCursor = true;
    [SerializeField] private float postUnpauseLookBlockDuration = 0.08f;
    [SerializeField] private bool pauseInputResetOnOpen = true;
    [SerializeField] private bool pauseInputResetOnClose = true;
    [SerializeField] private float postUnpauseSpikeFilterDuration = 0.12f;
    [SerializeField] private float postUnpauseSpikeThreshold = 3.5f;
    [Header("Pause Pose Restore")]
    [SerializeField] private Transform pausePoseRoot;
    [SerializeField] private bool restorePoseOnUnpause = true;
    [SerializeField] private bool restoreCameraLocalPositionOnUnpause = true;
    [SerializeField] private bool clearAngularVelocityOnResume = true;
    [Space]
    private Vector2 sensitivity = new Vector2(2f, 2f); 
    [Space]
    public Vector2 smoothing = new Vector2(3f, 3f);

    [Header("First Person")]
    public GameObject characterBody;

    [Header("Recoil")]
    [SerializeField] private float recoilReturnTime = 0.08f;

    [Header("Jump Camera Lift")]
    [SerializeField] private float jumpLiftHeight = 0.04f;
    [SerializeField] private float jumpLiftUpDuration = 0.1f;
    [SerializeField] private float jumpLiftDownDuration = 0.16f;

    [Header("Jump Camera Bounce")]
    [SerializeField] private float jumpBounceDropHeight = 0.025f;
    [SerializeField] private float jumpBounceDropDuration = 0.04f;
    [SerializeField] private float jumpBounceRecoverDuration = 0.06f;

    [Header("Jump Camera Anticipation")]
    [SerializeField] private float anticipationDropHeight = 0.025f;
    [SerializeField] private float anticipationDropReturnDuration = 0.06f;

    private Vector2 targetDirection;
    private Vector2 targetCharacterDirection;

    private Vector2 _mouseAbsolute;
    private Vector2 _smoothMouse;

    private Vector2 mouseDelta;
    private float jumpPreparationPitchOffset;
    private Vector3 baseLocalPosition;
    private float recoilPitchOffset;
    private float recoilPitchVelocity;
    private float recoilYawOffset;
    private float recoilYawVelocity;
    private float anticipationDropOffset;
    private float anticipationDropVelocity;
    private float anticipationDropTarget;
    private float anticipationDropSmoothTime = 0.1f;
    private float jumpCameraOffset;
    private JumpCameraPhase jumpCameraPhase;
    private float jumpCameraPhaseTimer;
    private float postUnpauseLookBlockTimer;
    private float postUnpauseSpikeFilterTimer;
    private bool pauseStateInitialized;
    private bool lastKnownPauseState;
    private bool pauseEventSubscribed;

    [HideInInspector]
    public bool scoped;

    private struct PausePoseSnapshot
    {
        public bool isValid;
        public Vector3 rootPosition;
        public Quaternion rootRotation;
        public Quaternion cameraLocalRotation;
        public Vector3 cameraLocalPosition;
        public Quaternion characterBodyLocalRotation;
        public Vector2 cachedTargetDirection;
        public Vector2 cachedTargetCharacterDirection;
        public Vector2 cachedMouseAbsolute;
    }

    private PausePoseSnapshot pausePoseSnapshot;
    private bool pausePoseCapturedThisCycle;
    private Rigidbody pausePoseRootRigidbody;

    void OnEnable()
    {
        SubscribePauseStateEvents();
        InitializePauseStateTracking();
    }

    void OnDisable()
    {
        UnsubscribePauseStateEvents();
    }

    void Start()
    {
        instance = this;
        SubscribePauseStateEvents();
        InitializePauseStateTracking();
        float savedLookSensitivity = PlayerPrefs.GetFloat(LookSensitivityKey, DefaultLookSensitivity);
        SetSensitivity(savedLookSensitivity);
        baseLocalPosition = transform.localPosition;

        targetDirection = transform.localRotation.eulerAngles;

        if (characterBody)
        {
            targetCharacterDirection = characterBody.transform.localRotation.eulerAngles;
        }

        if (lockCursor)
        {
            LockCursor();
        }
    }

    // Guarda una sensibilidad segura dentro de los límites permitidos.
    public void SetSensitivity(float value)
    {
        float clampedSensitivity = Mathf.Clamp(value, MinLookSensitivity, MaxLookSensitivity);
        sensitivity = new Vector2(clampedSensitivity, clampedSensitivity);
    }


    // Encierra el cursor para que la cámara no pierda el control al mover el ratón.
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        bool pausedNow = UIManager.IsGamePaused;

        if (!pauseStateInitialized || pausedNow != lastKnownPauseState)
        {
            HandlePauseStateChanged(pausedNow);
        }

        if (pausedNow)
        {
            return;
        }

        if (postUnpauseLookBlockTimer > 0f)
        {
            postUnpauseLookBlockTimer = Mathf.Max(0f, postUnpauseLookBlockTimer - Time.unscaledDeltaTime);
            return;
        }

        var targetOrientation = Quaternion.Euler(targetDirection);
        var targetCharacterOrientation = Quaternion.Euler(targetCharacterDirection);

        mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        if (postUnpauseSpikeFilterTimer > 0f)
        {
            postUnpauseSpikeFilterTimer = Mathf.Max(0f, postUnpauseSpikeFilterTimer - Time.unscaledDeltaTime);
            float spikeThreshold = Mathf.Max(0.1f, postUnpauseSpikeThreshold);

            if (mouseDelta.sqrMagnitude > spikeThreshold * spikeThreshold)
            {
                // Ignora picos raros del ratón justo al salir de pausa o ajustes.
                ResetLookInputBuffers();
                return;
            }
        }

        mouseDelta = Vector2.Scale(mouseDelta, new Vector2(sensitivity.x * smoothing.x, sensitivity.y * smoothing.y));

        _smoothMouse.x = Mathf.Lerp(_smoothMouse.x, mouseDelta.x, 1f / smoothing.x);
        _smoothMouse.y = Mathf.Lerp(_smoothMouse.y, mouseDelta.y, 1f / smoothing.y);

        _mouseAbsolute += _smoothMouse;

        if (clampInDegrees.x < 360)
            _mouseAbsolute.x = Mathf.Clamp(_mouseAbsolute.x, -clampInDegrees.x * 0.5f, clampInDegrees.x * 0.5f);

        if (clampInDegrees.y < 360)
            _mouseAbsolute.y = Mathf.Clamp(_mouseAbsolute.y, -clampInDegrees.y * 0.5f, clampInDegrees.y * 0.5f);

        recoilPitchOffset = Mathf.SmoothDamp(recoilPitchOffset, 0f, ref recoilPitchVelocity, Mathf.Max(0.01f, recoilReturnTime));
        recoilYawOffset = Mathf.SmoothDamp(recoilYawOffset, 0f, ref recoilYawVelocity, Mathf.Max(0.01f, recoilReturnTime));

        float combinedPitch = _mouseAbsolute.y + jumpPreparationPitchOffset + recoilPitchOffset;
        Quaternion pitchRotation = Quaternion.AngleAxis(-combinedPitch, targetOrientation * Vector3.right) * targetOrientation;
        Quaternion yawRecoilRotation = Quaternion.AngleAxis(recoilYawOffset, targetOrientation * Vector3.up);
        transform.localRotation = yawRecoilRotation * pitchRotation;
        UpdateCameraVerticalOffsets();

        if (characterBody)
        {
            var yRotation = Quaternion.AngleAxis(_mouseAbsolute.x, Vector3.up);
            characterBody.transform.localRotation = yRotation * targetCharacterOrientation;
        }

        else
        {
            var yRotation = Quaternion.AngleAxis(_mouseAbsolute.x, transform.InverseTransformDirection(Vector3.up));
            transform.localRotation *= yRotation;
        }

    }

    // Se engancha al evento de pausa una sola vez para no duplicar avisos.
    private void SubscribePauseStateEvents()
    {
        if (pauseEventSubscribed)
        {
            return;
        }

        UIManager.PauseStateChanged += HandlePauseStateChanged;
        pauseEventSubscribed = true;
    }

    // Se desengancha del evento al desactivar el componente para no dejar suscripciones colgadas.
    private void UnsubscribePauseStateEvents()
    {
        if (!pauseEventSubscribed)
        {
            return;
        }

        UIManager.PauseStateChanged -= HandlePauseStateChanged;
        pauseEventSubscribed = false;
    }

    // Toma una foto inicial del estado de pausa y limpia temporizadores de protección.
    private void InitializePauseStateTracking()
    {
        bool pausedNow = UIManager.IsGamePaused;
        pauseStateInitialized = true;
        lastKnownPauseState = pausedNow;
        postUnpauseLookBlockTimer = 0f;
        postUnpauseSpikeFilterTimer = 0f;
        pausePoseCapturedThisCycle = false;

        if (pausedNow && restorePoseOnUnpause)
        {
            CapturePausePoseSnapshot();
        }

        if (pausedNow && pauseInputResetOnOpen)
        {
            ResetLookInputBuffers();
        }
    }

    // Este bloque decide qué guardar al pausar y qué restaurar al volver.
    // La idea es que la cámara y el cuerpo vuelvan exactamente al mismo sitio
    // sin saltos bruscos ni giros raros al cerrar el menú.
    private void HandlePauseStateChanged(bool paused)
    {
        if (pauseStateInitialized && paused == lastKnownPauseState)
        {
            return;
        }

        lastKnownPauseState = paused;
        pauseStateInitialized = true;

        if (paused)
        {
            if (restorePoseOnUnpause)
            {
                CapturePausePoseSnapshot();
            }

            postUnpauseLookBlockTimer = 0f;
            postUnpauseSpikeFilterTimer = 0f;

            if (pauseInputResetOnOpen)
            {
                ResetLookInputBuffers();
            }

            return;
        }

        if (restorePoseOnUnpause)
        {
            RestorePausePoseSnapshot();
        }

        if (pauseInputResetOnClose)
        {
            ResetLookInputBuffers();
        }

        postUnpauseLookBlockTimer = Mathf.Max(0f, postUnpauseLookBlockDuration);
        postUnpauseSpikeFilterTimer = Mathf.Max(0f, postUnpauseSpikeFilterDuration);
        pausePoseCapturedThisCycle = false;
    }

    // Borra la inercia del ratón y del recoil acumulado para empezar desde un estado limpio.
    private void ResetLookInputBuffers()
    {
        _smoothMouse = Vector2.zero;
        mouseDelta = Vector2.zero;
        recoilPitchVelocity = 0f;
        recoilYawVelocity = 0f;
        anticipationDropVelocity = 0f;
    }

    // Guarda una foto completa de la pose actual antes de pausar:
    // raíz, cámara, cuerpo y acumuladores de giro.
    private void CapturePausePoseSnapshot()
    {
        if (pausePoseCapturedThisCycle)
        {
            return;
        }

        Transform snapshotRoot = ResolvePausePoseRoot();
        pausePoseSnapshot.rootPosition = snapshotRoot.position;
        pausePoseSnapshot.rootRotation = snapshotRoot.rotation;
        pausePoseSnapshot.cameraLocalRotation = transform.localRotation;
        pausePoseSnapshot.cameraLocalPosition = transform.localPosition;
        pausePoseSnapshot.characterBodyLocalRotation = characterBody != null
            ? characterBody.transform.localRotation
            : Quaternion.identity;
        pausePoseSnapshot.cachedTargetDirection = targetDirection;
        pausePoseSnapshot.cachedTargetCharacterDirection = targetCharacterDirection;
        pausePoseSnapshot.cachedMouseAbsolute = _mouseAbsolute;
        pausePoseSnapshot.isValid = true;
        pausePoseCapturedThisCycle = true;
    }

    // Restaura la foto guardada al salir de pausa.
    // Esto evita que la cámara reaparezca descolocada o con física residual.
    private void RestorePausePoseSnapshot()
    {
        if (!pausePoseSnapshot.isValid)
        {
            return;
        }

        Transform snapshotRoot = ResolvePausePoseRoot();
        snapshotRoot.SetPositionAndRotation(pausePoseSnapshot.rootPosition, pausePoseSnapshot.rootRotation);
        transform.localRotation = pausePoseSnapshot.cameraLocalRotation;

        if (restoreCameraLocalPositionOnUnpause)
        {
            transform.localPosition = pausePoseSnapshot.cameraLocalPosition;
        }

        if (characterBody != null)
        {
            characterBody.transform.localRotation = pausePoseSnapshot.characterBodyLocalRotation;
        }

        targetDirection = pausePoseSnapshot.cachedTargetDirection;
        targetCharacterDirection = pausePoseSnapshot.cachedTargetCharacterDirection;
        _mouseAbsolute = pausePoseSnapshot.cachedMouseAbsolute;

        if (clearAngularVelocityOnResume)
        {
            Rigidbody poseRootRigidbody = ResolvePausePoseRootRigidbody(snapshotRoot);

            if (poseRootRigidbody != null)
            {
                poseRootRigidbody.angularVelocity = Vector3.zero;
            }
        }
    }

    // Decide cuál es la raíz que se debe congelar y restaurar durante la pausa.
    private Transform ResolvePausePoseRoot()
    {
        if (pausePoseRoot != null)
        {
            return pausePoseRoot;
        }

        return transform.root != null ? transform.root : transform;
    }

    // Busca el Rigidbody que puede seguir girando mientras el juego está pausado.
    // Si existe, luego lo frenamos para que la vuelta al juego no pegue un latigazo.
    private Rigidbody ResolvePausePoseRootRigidbody(Transform snapshotRoot)
    {
        if (snapshotRoot == null)
        {
            return null;
        }

        if (pausePoseRootRigidbody != null && pausePoseRootRigidbody.transform == snapshotRoot)
        {
            return pausePoseRootRigidbody;
        }

        pausePoseRootRigidbody = snapshotRoot.GetComponent<Rigidbody>();

        if (pausePoseRootRigidbody == null)
        {
            pausePoseRootRigidbody = snapshotRoot.GetComponentInParent<Rigidbody>();
        }

        return pausePoseRootRigidbody;
    }

    // Baja un poco la cámara antes del salto para que el cuerpo parezca coger impulso.
    public void PlayJumpPreparationDip(float downwardAngle, float duration)
    {
        if (duration <= 0f)
        {
            jumpPreparationPitchOffset = 0f;
            return;
        }

        CancelInvoke(nameof(ResetJumpPreparationDip));
        jumpPreparationPitchOffset = downwardAngle;
        Invoke(nameof(ResetJumpPreparationDip), duration);
    }

    // Arranca la secuencia principal del salto: subir y luego caer suavemente.
    public void PlayJumpCameraSequence()
    {
        ReleaseJumpAnticipationDrop();
        jumpCameraPhase = JumpCameraPhase.Rise;
        jumpCameraPhaseTimer = 0f;
        jumpCameraOffset = 0f;
    }

    // Alias simple para lanzar la animación vertical del salto.
    public void PlayJumpLift()
    {
        PlayJumpCameraSequence();
    }

    // Hace una pequeña bajada previa al salto.
    // Piensa en ella como agachar un poco la cámara antes de empujar hacia arriba.
    public void PlayJumpAnticipationDrop(float anticipationDuration)
    {
        if (anticipationDuration <= 0f || anticipationDropHeight <= 0f)
        {
            ReleaseJumpAnticipationDrop();
            return;
        }

        CancelInvoke(nameof(BeginAnticipationDropReturn));
        anticipationDropTarget = anticipationDropHeight;
        anticipationDropSmoothTime = Mathf.Max(0.01f, anticipationDuration);
        Invoke(nameof(BeginAnticipationDropReturn), anticipationDuration);
    }

    // Suelta la bajada previa y empieza a devolver la cámara a su altura normal.
    public void ReleaseJumpAnticipationDrop()
    {
        CancelInvoke(nameof(BeginAnticipationDropReturn));
        BeginAnticipationDropReturn();
    }

    // Suma recoil a cámara en vertical y horizontal.
    // No mueve la cámara de golpe: deja que luego vuelva suave a su sitio.
    public void ApplyRecoil(float pitchKick, float yawKick)
    {
        recoilPitchOffset += pitchKick;
        recoilYawOffset += yawKick;
    }

    // Borra la inclinación previa al salto.
    private void ResetJumpPreparationDip()
    {
        jumpPreparationPitchOffset = 0f;
    }

    // Empieza el regreso suave de la bajada previa del salto.
    private void BeginAnticipationDropReturn()
    {
        anticipationDropTarget = 0f;
        anticipationDropSmoothTime = Mathf.Max(0.01f, anticipationDropReturnDuration);
    }

    // Mezcla todos los offsets verticales de salto y anticipación
    // y los aplica a la posición local de la cámara.
    private void UpdateCameraVerticalOffsets()
    {
        anticipationDropOffset = Mathf.SmoothDamp(
            anticipationDropOffset,
            anticipationDropTarget,
            ref anticipationDropVelocity,
            anticipationDropSmoothTime);

        UpdateJumpCameraOffset();

        float combinedOffset = jumpCameraOffset - anticipationDropOffset;
        Vector3 desiredPosition = baseLocalPosition + Vector3.up * combinedOffset;
        transform.localPosition = desiredPosition;
    }

    // Resuelve en qué fase del salto estamos y cuánto debe subir o bajar la cámara.
    private void UpdateJumpCameraOffset()
    {
        if (jumpCameraPhase == JumpCameraPhase.None)
        {
            jumpCameraOffset = 0f;
            return;
        }

        jumpCameraPhaseTimer += Time.deltaTime;

        if (jumpCameraPhase == JumpCameraPhase.Rise)
        {
            float duration = Mathf.Max(0.001f, jumpLiftUpDuration);
            float t = Mathf.Clamp01(jumpCameraPhaseTimer / duration);
            jumpCameraOffset = Mathf.Lerp(0f, jumpLiftHeight, t);

            if (t >= 1f)
            {
                jumpCameraPhase = JumpCameraPhase.Fall;
                jumpCameraPhaseTimer = 0f;
            }

            return;
        }

        if (jumpCameraPhase == JumpCameraPhase.Fall)
        {
            float duration = Mathf.Max(0.001f, jumpLiftDownDuration);
            float t = Mathf.Clamp01(jumpCameraPhaseTimer / duration);
            jumpCameraOffset = Mathf.Lerp(jumpLiftHeight, 0f, t);

            if (t >= 1f)
            {
                if (jumpBounceDropHeight > 0f && jumpBounceDropDuration > 0f && jumpBounceRecoverDuration > 0f)
                {
                    jumpCameraPhase = JumpCameraPhase.BounceDrop;
                    jumpCameraPhaseTimer = 0f;
                }
                else
                {
                    jumpCameraPhase = JumpCameraPhase.None;
                    jumpCameraPhaseTimer = 0f;
                    jumpCameraOffset = 0f;
                }
            }

            return;
        }

        if (jumpCameraPhase == JumpCameraPhase.BounceDrop)
        {
            float duration = Mathf.Max(0.001f, jumpBounceDropDuration);
            float t = Mathf.Clamp01(jumpCameraPhaseTimer / duration);
            jumpCameraOffset = Mathf.Lerp(0f, -jumpBounceDropHeight, t);

            if (t >= 1f)
            {
                jumpCameraPhase = JumpCameraPhase.BounceRecover;
                jumpCameraPhaseTimer = 0f;
            }

            return;
        }

        float recoverDuration = Mathf.Max(0.001f, jumpBounceRecoverDuration);
        float recoverT = Mathf.Clamp01(jumpCameraPhaseTimer / recoverDuration);
        jumpCameraOffset = Mathf.Lerp(-jumpBounceDropHeight, 0f, recoverT);

        if (recoverT >= 1f)
        {
            jumpCameraPhase = JumpCameraPhase.None;
            jumpCameraPhaseTimer = 0f;
            jumpCameraOffset = 0f;
        }
    }
}
