using UnityEngine;
using UnityEngine.Rendering;

namespace PetsHarbor.SlideMerge
{
    public class TutorialHandController : MonoBehaviour
    {
        private SpriteRenderer _handRenderer;
        private Material _overlayHandMat;
        private JellyBlock _hintFrom;
        private JellyBlock _hintTo;
        private float _nextHintLookupTime;
        private bool _hasInteractedOnce;

        public void InitializeHand()
        {
            if (_handRenderer != null) return;

            Transform existing = transform.Find("WhiteGloveHand3D");
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }

            GameObject handGo = new GameObject("WhiteGloveHand3D");
            handGo.transform.SetParent(transform, false);
            handGo.transform.localRotation = Quaternion.Euler(78f, 0f, 0f);
            handGo.transform.localScale = Vector3.one * 0.90f;

            _handRenderer = handGo.AddComponent<SpriteRenderer>();
            _handRenderer.sprite = ProceduralEmojiDrawer.GetOrCreateGloveHandSprite();
            _handRenderer.sortingOrder = 200;

            Shader uiShader = Shader.Find("UI/Default");
            if (uiShader == null) uiShader = Shader.Find("Sprites/Default");
            if (uiShader != null)
            {
                _overlayHandMat = new Material(uiShader)
                {
                    name = "Mat_TutorialHandOverlay",
                    renderQueue = 4000
                };
                if (_overlayHandMat.HasProperty("unity_GUIZTestMode"))
                {
                    _overlayHandMat.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
                }
                if (_overlayHandMat.HasProperty("_ZTest"))
                {
                    _overlayHandMat.SetInt("_ZTest", (int)CompareFunction.Always);
                }
                _handRenderer.sharedMaterial = _overlayHandMat;
            }
        }

        public void ResetTutorialState()
        {
            _hasInteractedOnce = false;
            _hintFrom = null;
            _hintTo = null;
            _nextHintLookupTime = 0f;
        }

        private void Start()
        {
            InitializeHand();
        }

        private void Update()
        {
            if (_handRenderer == null) InitializeHand();
            if (GameManager.Instance != null && GameManager.Instance.IsGameWon)
            {
                _handRenderer.enabled = false;
                return;
            }

            // Hide immediately whenever the player presses or holds mouse/touch
            if (BlockDragController.Instance != null &&
                (BlockDragController.Instance.IsPointerHeld || BlockDragController.Instance.IsDragging))
            {
                _hasInteractedOnce = true;
                _handRenderer.enabled = false;
                return;
            }

            float idleDuration = BlockDragController.Instance != null
                ? (Time.time - BlockDragController.Instance.LastInteractionTime)
                : 99f;

            bool shouldShowHint = !_hasInteractedOnce || idleDuration >= 3.0f;
            if (!shouldShowHint)
            {
                _handRenderer.enabled = false;
                return;
            }

            if (_hintFrom == null || _hintTo == null ||
                _hintFrom.Tier != _hintTo.Tier ||
                _hintFrom.IsMerging || _hintTo.IsMerging ||
                Time.time >= _nextHintLookupTime)
            {
                _nextHintLookupTime = Time.time + 1.0f;
                if (BoardGridManager.Instance != null)
                {
                    BoardGridManager.Instance.TryFindHintPair(out _hintFrom, out _hintTo);
                }
            }

            if (_hintFrom != null && _hintTo != null)
            {
                _handRenderer.enabled = true;
                float cycle = Mathf.Repeat(Time.time * 0.85f, 1f);
                float moveT = Mathf.Clamp01(cycle / 0.72f);
                float smoothT = moveT * moveT * (3f - 2f * moveT);

                Vector3 blockSurfaceStart = _hintFrom.transform.position + new Vector3(0f, 0.68f, -0.08f);
                Vector3 blockSurfaceEnd = _hintTo.transform.position + new Vector3(0f, 0.68f, -0.08f);
                Vector3 surfacePos = Vector3.Lerp(blockSurfaceStart, blockSurfaceEnd, smoothT);

                // Lift hand along the camera view ray so it sits physically above all 3D blocks and tray rims without perspective shift
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 toCamDir = (cam.transform.position - surfacePos).normalized;
                    _handRenderer.transform.position = surfacePos + toCamDir * 1.8f;
                    _handRenderer.transform.rotation = Quaternion.Euler(cam.transform.eulerAngles.x, 0f, 0f);
                }
                else
                {
                    _handRenderer.transform.position = surfacePos + new Vector3(0f, 1.6f, -0.25f);
                }

                float pulse = 0.85f + Mathf.Sin(cycle * Mathf.PI) * 0.08f;
                _handRenderer.transform.localScale = Vector3.one * pulse;
            }
            else
            {
                _handRenderer.enabled = false;
            }
        }
    }
}
