using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Centralized input wrapper using Unity's New Input System.
/// All gameplay scripts read input from GameInput.Instance instead of legacy Input.GetXxx().
/// 
/// Setup:
///   1. Create an empty GameObject named "GameInput" in your first scene (MainMenu).
///   2. Attach this script.
///   3. Drag the InputSystem_Actions asset into the "Input Actions" field.
///   4. It persists across scenes via DontDestroyOnLoad.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    [Header("Input Actions Asset")]
    [SerializeField] private InputActionAsset inputActions;

    // Cached action references
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _attackAction;
    private InputAction _interactAction;
    private InputAction _inventoryAction;
    private InputAction _questLogAction;
    private InputAction _scrollAction;

    // ── Public Properties (read by gameplay scripts) ──────────────────────

    /// <summary>WASD / Left Stick movement (x = horizontal, y = vertical).</summary>
    public Vector2 MoveInput { get; private set; }

    /// <summary>Mouse delta / Right Stick. Scaled to approximate legacy sensitivity.</summary>
    public Vector2 LookDelta { get; private set; }

    /// <summary>Scroll wheel, scaled to match legacy Input.GetAxis("Mouse ScrollWheel").</summary>
    public float ScrollValue { get; private set; }

    /// <summary>True the frame attack is triggered (Left Mouse / Gamepad West).</summary>
    public bool AttackPressed { get; private set; }

    /// <summary>True the frame interact is triggered (E / Gamepad North).</summary>
    public bool InteractPressed { get; private set; }

    /// <summary>True the frame inventory toggle is triggered (B / Gamepad Select).</summary>
    public bool InventoryPressed { get; private set; }

    /// <summary>True the frame quest log toggle is triggered (J / Gamepad Start).</summary>
    public bool QuestLogPressed { get; private set; }

    /// <summary>True while right mouse button is held (camera orbit).</summary>
    public bool CameraRotateHeld { get; private set; }

    /// <summary>Horizontal look delta, scaled to legacy sensitivity.</summary>
    public float CameraRotateDelta { get; private set; }

    // Legacy Input.GetAxis sensitivity for Mouse axes was 0.1 by default.
    private const float LookSensitivityScale = 0.1f;

    // Legacy Mouse ScrollWheel returned ~0.1 per notch; New Input System returns ~120.
    private const float ScrollScale = 1f / 1200f;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeActions();
    }

    private void InitializeActions()
    {
        if (inputActions == null)
        {
            Debug.LogError("[GameInput] InputActionAsset not assigned! Drag InputSystem_Actions here.");
            return;
        }

        _moveAction      = inputActions.FindAction("Player/Move");
        _lookAction      = inputActions.FindAction("Player/Look");
        _attackAction    = inputActions.FindAction("Player/Attack");
        _interactAction  = inputActions.FindAction("Player/Interact");
        _inventoryAction = inputActions.FindAction("Player/Inventory");
        _questLogAction  = inputActions.FindAction("Player/QuestLog");
        _scrollAction    = inputActions.FindAction("UI/ScrollWheel");

        inputActions.Enable();
    }

    private void Update()
    {
        // Movement
        MoveInput = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;

        // Look delta (scaled to approximate legacy Input.GetAxis("Mouse X/Y"))
        Vector2 rawLook = _lookAction?.ReadValue<Vector2>() ?? Vector2.zero;
        LookDelta = rawLook * LookSensitivityScale;

        // Scroll wheel (scaled to match legacy ~0.1 per notch)
        Vector2 rawScroll = _scrollAction?.ReadValue<Vector2>() ?? Vector2.zero;
        ScrollValue = rawScroll.y * ScrollScale;

        // Button triggers
        AttackPressed    = _attackAction?.triggered ?? false;
        InteractPressed  = _interactAction?.triggered ?? false;
        InventoryPressed = _inventoryAction?.triggered ?? false;
        QuestLogPressed  = _questLogAction?.triggered ?? false;

        // Camera rotation (right mouse button)
        var mouse = Mouse.current;
        CameraRotateHeld = mouse != null && mouse.rightButton.isPressed;
        CameraRotateDelta = CameraRotateHeld ? LookDelta.x : 0f;
    }

    private void OnEnable()  { inputActions?.Enable(); }
    private void OnDisable() { inputActions?.Disable(); }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
