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
        private Vector2 _virtualTargetGridPos;
        private Vector2 _anchorGridCell;
        private Vector2 _slideIntentDir;
        private Vector2 _lastPointerGridPos;
        private Vector2 _smoothedPointerVel;
        private Vector2 _recentPushVelocity;
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
                CurrentDraggedBlock.IsCoasting = false;
                CurrentDraggedBlock.IsBeingDragged = true;
                CurrentDraggedBlock.TriggerJellyBounce(0.16f);

                Vector2 pointerGrid = BoardGridManager.Instance.WorldToGridPoint(CurrentPointerWorldPos);
                _anchorGridCell = new Vector2(pickedBlock.GridX, pickedBlock.GridY);
                _virtualTargetGridPos = pointerGrid - _anchorGridCell; // Stores exact 1:1 grab offset on the block!
                _slideIntentDir = Vector2.zero;
                _lastPointerGridPos = pointerGrid;
                _smoothedPointerVel = Vector2.zero;
                _recentPushVelocity = Vector2.zero;
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
            Vector2 pointerDelta = pointerGrid - _lastPointerGridPos;
            Vector2 rawPointerVel = pointerDelta / dt;
            _lastPointerGridPos = pointerGrid;

            // Smooth pointer velocity & preserve peak push velocity over a short ~75ms window
            _smoothedPointerVel = Vector2.Lerp(_smoothedPointerVel, rawPointerVel, 1f - Mathf.Exp(-38f * dt));
            if (_smoothedPointerVel.sqrMagnitude >= _recentPushVelocity.sqrMagnitude * 0.65f ||
                Vector2.Dot(_smoothedPointerVel, _recentPushVelocity) < 0f)
            {
                _recentPushVelocity = _smoothedPointerVel;
            }
            else
            {
                _recentPushVelocity = Vector2.MoveTowards(_recentPushVelocity, _smoothedPointerVel, 22f * dt);
            }

            // Track player's directional slide intent from pointer movement
            if (pointerDelta.sqrMagnitude > 0.0001f)
            {
                if (Mathf.Abs(pointerDelta.x) >= Mathf.Abs(pointerDelta.y))
                {
                    _slideIntentDir = new Vector2(Mathf.Sign(pointerDelta.x), 0f);
                }
                else
                {
                    _slideIntentDir = new Vector2(0f, Mathf.Sign(pointerDelta.y));
                }
            }

            // While the short merge suck-in is playing, wait to re-anchor grab offset to the newly evolved block
            if (CurrentDraggedBlock.IsMerging)
            {
                _reanchorAfterMerge = true;
                return;
            }

            if (_reanchorAfterMerge)
            {
                _reanchorAfterMerge = false;
                CurrentDraggedBlock.IsBeingDragged = true;
                _virtualTargetGridPos = new Vector2(
                    Mathf.Clamp(pointerGrid.x - CurrentDraggedBlock.GridX, 0.1f, CurrentDraggedBlock.GridWidth - 0.1f),
                    Mathf.Clamp(pointerGrid.y - CurrentDraggedBlock.GridY, 0.1f, CurrentDraggedBlock.GridHeight - 0.1f));
                _anchorGridCell = new Vector2(Mathf.Round(CurrentDraggedBlock.GridX), Mathf.Round(CurrentDraggedBlock.GridY));
            }

            GameFeelConfig feel = GameFeelConfig.Instance;

            // 1:1 Direct Pointer Tracking:
            // Desired block position is directly where the mouse cursor is holding the block (pointerGrid - grabOffset).
            Vector2 desiredGridPos = pointerGrid - _virtualTargetGridPos;

            Vector2 prevPos = new Vector2(CurrentDraggedBlock.GridX, CurrentDraggedBlock.GridY);
            bool allowMerge = _mergeCooldownTimer <= 0f;
            bool merged = BoardGridManager.Instance.SlideBlockWithCollision(
                CurrentDraggedBlock,
                desiredGridPos,
                allowMerge,
                out JellyBlock mergedResult);

            if (merged)
            {
                _mergeCooldownTimer = feel.MergeCooldown;
                if (mergedResult != null && mergedResult.Tier < TierDatabase.MaxTier)
                {
                    CurrentDraggedBlock = mergedResult;
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
                Vector2 actualVel = (actualPos - prevPos) / dt;
                CurrentDraggedBlock.SetDragVelocity(actualVel);
            }
        }

        private void ReleaseDraggedBlock()
        {
            if (CurrentDraggedBlock == null) return;

            JellyBlock releasedBlock = CurrentDraggedBlock;
            releasedBlock.IsBeingDragged = false;
            CurrentDraggedBlock = null;
            _reanchorAfterMerge = false;

            if (releasedBlock.IsMerging || BoardGridManager.Instance == null) return;

            GameFeelConfig feel = GameFeelConfig.Instance;
            Vector2 pushVel = _recentPushVelocity.sqrMagnitude > releasedBlock.GridVelocity.sqrMagnitude
                ? _recentPushVelocity
                : releasedBlock.GridVelocity;

            float pushSpeed = pushVel.magnitude;
            if (pushSpeed > 2.2f && feel.pushForcePower > 0.05f)
            {
                // Align push impulse strictly to the active orthogonal grid rail so the block glides cleanly along the row/column
                if (Mathf.Abs(pushVel.x) >= Mathf.Abs(pushVel.y))
                {
                    pushVel.y = 0f;
                }
                else
                {
                    pushVel.x = 0f;
                }

                Vector2 launchVel = Vector2.ClampMagnitude(pushVel * feel.pushForcePower, 28f);
                BoardGridManager.Instance.LaunchBlockCoasting(releasedBlock, launchVel);
            }
            else
            {
                // On a gentle slide or release without a strong flick, snap smoothly to the grid cell in _slideIntentDir
                BoardGridManager.Instance.SnapBlockToValidCell(releasedBlock, _slideIntentDir);
            }
        }
    }
}
