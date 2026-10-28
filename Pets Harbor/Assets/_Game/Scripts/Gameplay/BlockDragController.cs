using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PetsHarbor.SlideMerge
{
    public class BlockDragController : MonoBehaviour
    {
        public static BlockDragController Instance { get; private set; }

        public JellyBlock CurrentDraggedBlock { get; private set; }
        public bool IsDragging => CurrentDraggedBlock != null;
        public bool IsPointerHeld { get; private set; }
        public Vector3 CurrentPointerWorldPos { get; private set; }
        public float LastInteractionTime { get; private set; }

        private Camera _mainCam;
        private Plane _dragPlane;
        private Vector2 _grabGridOffset;
        private Vector2 _lastPointerGridPos;
        private float _mergeCooldownTimer;
        private bool _reanchorAfterMerge;

        private void Awake()
        {
            Instance = this;
            _dragPlane = new Plane(Vector3.up, new Vector3(0f, 0.35f, 0f));
        }

        private void Start()
        {
            _mainCam = Camera.main;
            LastInteractionTime = Time.time;
        }

        private void Update()
        {
            if (_mergeCooldownTimer > 0f)
            {
                _mergeCooldownTimer -= Time.deltaTime;
            }

            if (GameManager.Instance != null && GameManager.Instance.IsGameWon)
            {
                IsPointerHeld = false;
                if (CurrentDraggedBlock != null) ReleaseDraggedBlock();
                return;
            }

            if (_mainCam == null) _mainCam = Camera.main;
            if (_mainCam == null || BoardGridManager.Instance == null) return;

            ReadPointerState(out bool pressedThisFrame, out bool isHeld, out bool releasedThisFrame, out Vector2 screenPos);
            IsPointerHeld = pressedThisFrame || isHeld;

            if (pressedThisFrame)
            {
                LastInteractionTime = Time.time;
                TryBeginDrag(screenPos);
            }
            else if (isHeld && CurrentDraggedBlock != null)
            {
                LastInteractionTime = Time.time;
                UpdateDrag(screenPos);
            }
            else if (releasedThisFrame)
            {
                IsPointerHeld = false;
                LastInteractionTime = Time.time;
                if (CurrentDraggedBlock != null)
                {
                    ReleaseDraggedBlock();
                }
            }
        }

        private void ReadPointerState(out bool pressed, out bool held, out bool released, out Vector2 screenPos)
        {
            pressed = false;
            held = false;
            released = false;
            screenPos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                var touch = Touchscreen.current.primaryTouch;
                screenPos = touch.position.ReadValue();
                pressed = touch.press.wasPressedThisFrame;
                held = touch.press.isPressed;
                released = touch.press.wasReleasedThisFrame;
                return;
            }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            {
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
                released = true;
                return;
            }
            if (Mouse.current != null)
            {
                screenPos = Mouse.current.position.ReadValue();
                pressed = Mouse.current.leftButton.wasPressedThisFrame;
                held = Mouse.current.leftButton.isPressed;
                released = Mouse.current.leftButton.wasReleasedThisFrame;
                return;
            }
#else
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                screenPos = t.position;
                pressed = t.phase == UnityEngine.TouchPhase.Began;
                held = t.phase == UnityEngine.TouchPhase.Moved || t.phase == UnityEngine.TouchPhase.Stationary;
                released = t.phase == UnityEngine.TouchPhase.Ended || t.phase == UnityEngine.TouchPhase.Canceled;
            }
            else
            {
                screenPos = Input.mousePosition;
                pressed = Input.GetMouseButtonDown(0);
                held = Input.GetMouseButton(0);
                released = Input.GetMouseButtonUp(0);
            }
#endif
        }

        private void TryBeginDrag(Vector2 screenPos)
        {
            Ray ray = _mainCam.ScreenPointToRay(screenPos);

            // 1. Try Physics Raycast first
            JellyBlock pickedBlock = null;
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                pickedBlock = hit.collider.GetComponentInParent<JellyBlock>();
            }

            // 2. Fallback: Grid coordinate lookup on the drag plane
            if (_dragPlane.Raycast(ray, out float enter))
            {
                Vector3 planePoint = ray.GetPoint(enter);
                CurrentPointerWorldPos = planePoint;

                if (pickedBlock == null && BoardGridManager.Instance != null)
                {
                    pickedBlock = BoardGridManager.Instance.FindBlockAtWorldPoint(planePoint);
                }
            }

            if (pickedBlock != null && !pickedBlock.IsMerging)
            {
                CurrentDraggedBlock = pickedBlock;
                CurrentDraggedBlock.IsBeingDragged = true;
                CurrentDraggedBlock.TriggerJellyBounce(0.16f);

                Vector2 pointerGrid = BoardGridManager.Instance.WorldToGridPoint(CurrentPointerWorldPos);
                _grabGridOffset = new Vector2(pickedBlock.GridX, pickedBlock.GridY) - pointerGrid;
                _lastPointerGridPos = pointerGrid;
                _reanchorAfterMerge = false;
            }
        }

        private void UpdateDrag(Vector2 screenPos)
        {
            if (CurrentDraggedBlock == null) return;

            Ray ray = _mainCam.ScreenPointToRay(screenPos);
            if (!_dragPlane.Raycast(ray, out float enter)) return;

            Vector3 hitPoint = ray.GetPoint(enter);
            CurrentPointerWorldPos = hitPoint;
            Vector2 pointerGrid = BoardGridManager.Instance.WorldToGridPoint(hitPoint);

            float dt = Mathf.Max(0.001f, Time.deltaTime);
            Vector2 pointerVel = (pointerGrid - _lastPointerGridPos) / dt;
            _lastPointerGridPos = pointerGrid;

            // While the short merge suck-in is playing (~0.085s), keep offset synced to the pointer
            if (CurrentDraggedBlock.IsMerging)
            {
                _reanchorAfterMerge = true;
                return;
            }

            if (_reanchorAfterMerge)
            {
                _reanchorAfterMerge = false;
                CurrentDraggedBlock.IsBeingDragged = true;
                _grabGridOffset = new Vector2(CurrentDraggedBlock.GridX, CurrentDraggedBlock.GridY) - pointerGrid;
            }

            GameFeelConfig feel = GameFeelConfig.Instance;
            Vector2 desiredGridPos = pointerGrid + _grabGridOffset;

            // Soft Magnetic Grid Pull while holding pointer nearly stationary so the block always feels attracted to its grid cell
            float pointerSpeed = pointerVel.magnitude;
            if (pointerSpeed < feel.idleMagneticSpeedThreshold && feel.idleMagneticPullStrength > 0.01f)
            {
                float magStrength = Mathf.InverseLerp(feel.idleMagneticSpeedThreshold, 0.05f, pointerSpeed) * feel.idleMagneticPullStrength * dt;
                Vector2 nearestCell = new Vector2(
                    Mathf.Clamp(Mathf.Round(CurrentDraggedBlock.GridX), 0f, BoardGridManager.Instance.cols - CurrentDraggedBlock.GridWidth),
                    Mathf.Clamp(Mathf.Round(CurrentDraggedBlock.GridY), 0f, BoardGridManager.Instance.rows - CurrentDraggedBlock.GridHeight));
                desiredGridPos = Vector2.Lerp(desiredGridPos, nearestCell, Mathf.Clamp01(magStrength));
                _grabGridOffset = desiredGridPos - pointerGrid;
            }

            Vector2 prevPos = new Vector2(CurrentDraggedBlock.GridX, CurrentDraggedBlock.GridY);
            bool allowMerge = _mergeCooldownTimer <= 0f;
            bool merged = BoardGridManager.Instance.SlideBlockWithCollision(
                CurrentDraggedBlock,
                desiredGridPos,
                allowMerge,
                out JellyBlock mergedResult);

            if (merged)
            {
                _mergeCooldownTimer = feel.mergeCooldown;
                if (mergedResult != null && mergedResult.Tier < TierDatabase.MaxTier)
                {
                    CurrentDraggedBlock = mergedResult;
                    _grabGridOffset = new Vector2(mergedResult.GridX, mergedResult.GridY) - pointerGrid;
                    _reanchorAfterMerge = true;
                }
                else
                {
                    CurrentDraggedBlock = null;
                    _reanchorAfterMerge = false;
                }
                return;
            }

            if (CurrentDraggedBlock != null)
            {
                Vector2 actualPos = new Vector2(CurrentDraggedBlock.GridX, CurrentDraggedBlock.GridY);

                // Dynamic Leash Clamping: prevent the virtual grab point from pulling far past a solid wall,
                // eliminating sticky wall friction when changing slide direction (L-turns)
                float maxWallLeash = feel.maxWallLeashCells;
                Vector2 leashError = desiredGridPos - actualPos;
                if (Mathf.Abs(leashError.x) > maxWallLeash)
                {
                    desiredGridPos.x = actualPos.x + Mathf.Sign(leashError.x) * maxWallLeash;
                }
                if (Mathf.Abs(leashError.y) > maxWallLeash)
                {
                    desiredGridPos.y = actualPos.y + Mathf.Sign(leashError.y) * maxWallLeash;
                }
                _grabGridOffset = desiredGridPos - pointerGrid;

                Vector2 actualVel = (actualPos - prevPos) / dt;
                CurrentDraggedBlock.SetDragVelocity(actualVel);
            }
        }

        private void ReleaseDraggedBlock()
        {
            if (CurrentDraggedBlock != null)
            {
                CurrentDraggedBlock.IsBeingDragged = false;
                if (!CurrentDraggedBlock.IsMerging)
                {
                    BoardGridManager.Instance.SnapBlockToValidCell(CurrentDraggedBlock);
                }
                CurrentDraggedBlock = null;
                _reanchorAfterMerge = false;
            }
        }
    }
}
