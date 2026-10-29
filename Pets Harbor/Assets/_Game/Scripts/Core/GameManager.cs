using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace PetsHarbor.SlideMerge
{
    [ExecuteAlways]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public bool IsGameWon { get; private set; }

        private BoardGridManager _board;
        private GameUIManager _ui;
        private BlockDragController _drag;
        private MergeVFXManager _vfx;
        private TutorialHandController _tutorialHand;
        private bool _isAutoStepping;
        private float _lastCamAspect = -1f;

        private void Awake()
        {
            Instance = this;
            if (Application.isPlaying)
            {
                Application.targetFrameRate = 60;
            }
        }

        private void Start()
        {
            UpdateCameraToFitTray(true);
            if (Application.isPlaying)
            {
                SetupAndStartGame();
            }
        }

        private void Update()
        {
            UpdateCameraToFitTray(false);
        }

        /// <summary>
        /// Dynamically adjusts Main Camera distance along its 78-degree view ray based on screen aspect ratio
        /// so the entire 7x8 wooden tray (width 7.84 + side margins = 8.9 units) ALWAYS fits 100% inside
        /// any portrait screen (9:16, iPhone 9:19.5 Simulator, 9:21, etc.) without clipping left/right edges.
        /// </summary>
        public void UpdateCameraToFitTray(bool force)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            float screenAspect = Screen.height > 0 ? ((float)Screen.width / Screen.height) : cam.aspect;
            float aspect = Mathf.Min(cam.aspect, screenAspect);
            if (aspect <= 0.01f) return;
            if (!force && Mathf.Abs(aspect - _lastCamAspect) < 0.001f) return;
            _lastCamAspect = aspect;

            const float fov = 44.0f;
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 60f;
            cam.allowHDR = false;

            var uCam = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (uCam != null)
            {
                uCam.renderPostProcessing = false;
            }

            float halfFovRad = fov * 0.5f * Mathf.Deg2Rad;
            float tanHalfV = Mathf.Tan(halfFovRad);

            // Wooden tray total width with rims is 7.84 units; require 8.85 units visible horizontally
            const float requiredHalfWidth = 4.425f;
            // Require 7.5 units half-height vertically so top header + full tray + bottom margin fit cleanly
            const float requiredHalfHeight = 7.55f;

            float distForWidth = requiredHalfWidth / (tanHalfV * aspect);
            float distForHeight = requiredHalfHeight / tanHalfV;
            float distance = Mathf.Max(distForWidth, distForHeight);

            Quaternion rot = Quaternion.Euler(78f, 0f, 0f);
            // Shift focus point slightly up in +Z so the tray sits in the lower 71% of the screen below the Top Header
            Vector3 focusPoint = new Vector3(0f, 0f, 1.05f);

            cam.transform.rotation = rot;
            cam.transform.position = focusPoint - (rot * Vector3.forward) * distance;
        }

        public void SetupAndStartGame()
        {
            IsGameWon = false;
            UpdateCameraToFitTray(true);

            _board = GetComponentInChildren<BoardGridManager>();
            if (_board == null)
            {
                GameObject boardGo = new GameObject("BoardGridManager");
                boardGo.transform.SetParent(transform, false);
                _board = boardGo.AddComponent<BoardGridManager>();
            }

            _vfx = GetComponentInChildren<MergeVFXManager>();
            if (_vfx == null)
            {
                GameObject vfxGo = new GameObject("MergeVFXManager");
                vfxGo.transform.SetParent(transform, false);
                _vfx = vfxGo.AddComponent<MergeVFXManager>();
            }

            _drag = GetComponentInChildren<BlockDragController>();
            if (_drag == null)
            {
                GameObject dragGo = new GameObject("BlockDragController");
                dragGo.transform.SetParent(transform, false);
                _drag = dragGo.AddComponent<BlockDragController>();
            }

            _tutorialHand = GetComponentInChildren<TutorialHandController>();
            if (_tutorialHand == null)
            {
                GameObject handGo = new GameObject("TutorialHandController");
                handGo.transform.SetParent(transform, false);
                _tutorialHand = handGo.AddComponent<TutorialHandController>();
            }

            _ui = GetComponentInChildren<GameUIManager>();
            if (_ui == null)
            {
                GameObject uiGo = new GameObject("GameUIManager");
                uiGo.transform.SetParent(transform, false);
                _ui = uiGo.AddComponent<GameUIManager>();
            }

            _board.InitializeBoard();
            _tutorialHand.InitializeHand();
            _ui.BuildUI();
            _ui.HideEndcard();
        }

        public void RestartLevel()
        {
            StopAllCoroutines();
            _isAutoStepping = false;
            IsGameWon = false;
            if (_ui != null) _ui.HideEndcard();
            if (_board != null) _board.SpawnInitialLevel();
            if (_tutorialHand != null) _tutorialHand.ResetTutorialState();
        }

        public void NotifyVictory(JellyBlock goalBlock)
        {
            if (IsGameWon) return;
            IsGameWon = true;
            StartCoroutine(VictorySequenceRoutine());
        }

        private IEnumerator VictorySequenceRoutine()
        {
            yield return new WaitForSeconds(1.35f);
            if (_ui != null)
            {
                _ui.ShowEndcard();
            }
        }

        public void TriggerAutoMergeStep()
        {
            if (IsGameWon || _isAutoStepping || _board == null) return;
            if (_board.TryFindHintPairWithPath(out JellyBlock from, out JellyBlock to, out List<Vector2Int> path))
            {
                StartCoroutine(AutoSlidePairRoutine(from, to, path));
            }
        }

        private IEnumerator AutoSlidePairRoutine(JellyBlock from, JellyBlock to, List<Vector2Int> path)
        {
            _isAutoStepping = true;
            if (from != null && to != null && path != null && path.Count > 0)
            {
                from.transform.DOKill();
                float stepDuration = Mathf.Clamp(0.24f / Mathf.Max(1, path.Count - 1), 0.045f, 0.085f);

                for (int i = 1; i < path.Count; i++)
                {
                    if (from == null || to == null) break;
                    Vector2Int cell = path[i];
                    from.SetGridPositionSmooth(cell.x, cell.y, stepDuration);
                    yield return new WaitForSeconds(stepDuration);
                }

                if (from != null && to != null)
                {
                    Vector2 dir = new Vector2(to.GridX - from.GridX, to.GridY - from.GridY);
                    _board.ExecuteMerge(from, to, dir);
                }
            }
            yield return new WaitForSeconds(0.18f);
            _isAutoStepping = false;
        }
    }
}
