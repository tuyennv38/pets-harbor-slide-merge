using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace PetsHarbor.SlideMerge
{
    public class GameUIManager : MonoBehaviour
    {
        public static GameUIManager Instance { get; private set; }

        private Canvas _canvas;
        private GameObject _endcardPanel;
        private CanvasGroup _endcardCanvasGroup;

        private void Awake()
        {
            Instance = this;
        }

        public void BuildUI()
        {
            EnsureEventSystem();

            Transform existingCanvas = transform.Find("GameCanvas");
            if (existingCanvas != null)
            {
                existingCanvas.name = "GameCanvas_Old";
                existingCanvas.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(existingCanvas.gameObject);
                else DestroyImmediate(existingCanvas.gameObject);
            }

            GameObject canvasGo = new GameObject("GameCanvas");
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.AddComponent<Canvas>();
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                _canvas.renderMode = RenderMode.ScreenSpaceCamera;
                _canvas.worldCamera = mainCam;
                _canvas.planeDistance = 2.5f;
            }
            else
            {
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            _canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // 1. Top Header Container (Occupies top 27% of screen, responsive to any aspect ratio)
            GameObject headerAreaGo = new GameObject("TopHeaderArea");
            headerAreaGo.transform.SetParent(canvasGo.transform, false);
            RectTransform areaRt = headerAreaGo.AddComponent<RectTransform>();
            areaRt.anchorMin = new Vector2(0.015f, 0.725f);
            areaRt.anchorMax = new Vector2(0.985f, 0.992f);
            areaRt.offsetMin = Vector2.zero;
            areaRt.offsetMax = Vector2.zero;

            GameObject headerGo = new GameObject("TopHeaderBanner");
            headerGo.transform.SetParent(headerAreaGo.transform, false);
            RectTransform headerRt = headerGo.AddComponent<RectTransform>();
            headerRt.anchorMin = Vector2.zero;
            headerRt.anchorMax = Vector2.one;
            headerRt.offsetMin = Vector2.zero;
            headerRt.offsetMax = Vector2.zero;

            AspectRatioFitter fitter = headerGo.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1080f / 545f;

            RawImage headerImg = headerGo.AddComponent<RawImage>();
            headerImg.raycastTarget = false;
            Texture2D headerTex = Resources.Load<Texture2D>("UI/TopHeaderBanner");
            if (headerTex != null)
            {
                headerImg.texture = headerTex;
                headerImg.color = Color.white;
            }
            else
            {
                headerImg.color = new Color(0f, 0f, 0f, 0f);
                BuildFallbackProceduralHeader(headerRt);
            }

            // 2. Invisible/Subtle Quick Action Buttons at very top corners (Reset & Auto-Step Merge for easy testing)
            CreateCornerButton(
                canvasGo.transform,
                "Btn_ResetLevel",
                "↻ Reset",
                new Vector2(1f, 0f),
                new Vector2(-28f, 28f),
                new Vector2(180f, 64f),
                new Color(0.32f, 0.18f, 0.09f, 0.85f),
                () =>
                {
                    if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
                });

            CreateCornerButton(
                canvasGo.transform,
                "Btn_AutoStep",
                "⚡ Auto Merge",
                new Vector2(0f, 0f),
                new Vector2(28f, 28f),
                new Vector2(230f, 64f),
                new Color(0.45f, 0.16f, 0.65f, 0.88f),
                () =>
                {
                    if (GameManager.Instance != null) GameManager.Instance.TriggerAutoMergeStep();
                });

            // 3. Endcard Victory Screen (00:30 - 00:32 Pets Harbor Endcard)
            _endcardPanel = new GameObject("EndcardPanel");
            _endcardPanel.transform.SetParent(canvasGo.transform, false);
            RectTransform endRt = _endcardPanel.AddComponent<RectTransform>();
            endRt.anchorMin = Vector2.zero;
            endRt.anchorMax = Vector2.one;
            endRt.offsetMin = Vector2.zero;
            endRt.offsetMax = Vector2.zero;

            _endcardCanvasGroup = _endcardPanel.AddComponent<CanvasGroup>();
            _endcardCanvasGroup.alpha = 0f;

            RawImage endImg = _endcardPanel.AddComponent<RawImage>();
            Texture2D endTex = Resources.Load<Texture2D>("UI/EndcardScreen");
            if (endTex != null)
            {
                endImg.texture = endTex;
                endImg.color = Color.white;
            }
            else
            {
                endImg.color = new Color(0.15f, 0.52f, 0.88f, 0.96f);
            }

            // Play Again CTA Button on Endcard
            CreateCornerButton(
                _endcardPanel.transform,
                "Btn_PlayAgain",
                "PLAY AGAIN",
                new Vector2(0.5f, 0.12f),
                Vector2.zero,
                new Vector2(460f, 128f),
                new Color(0.24f, 0.82f, 0.18f, 1f),
                () =>
                {
                    if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
                },
                44);

            _endcardPanel.SetActive(false);
        }

        private void BuildFallbackProceduralHeader(RectTransform parent)
        {
            for (int i = 1; i <= 12; i++)
            {
                int row = (i - 1) / 6;
                int col = (i - 1) % 6;
                GameObject iconGo = new GameObject($"TierIcon_{i}");
                iconGo.transform.SetParent(parent, false);
                RectTransform rt = iconGo.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(-360f + col * 144f, -340f - row * 110f);
                rt.sizeDelta = new Vector2(88f, 88f);
                Image img = iconGo.AddComponent<Image>();
                img.sprite = ProceduralEmojiDrawer.GetOrCreateTierIconSprite(i);
            }
        }

        private void CreateCornerButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchor,
            Vector2 anchoredPos,
            Vector2 size,
            Color bgColor,
            UnityEngine.Events.UnityAction onClick,
            int fontSize = 28)
        {
            GameObject btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);

            RectTransform rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            if (anchor == new Vector2(0.5f, 0.12f)) rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image bg = btnGo.AddComponent<Image>();
            bg.color = bgColor;

            Button btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = bg;
            if (Application.isPlaying && onClick != null)
            {
                btn.onClick.AddListener(onClick);
            }

            GameObject txtGo = new GameObject("Label");
            txtGo.transform.SetParent(btnGo.transform, false);
            RectTransform txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            Text txt = txtGo.AddComponent<Text>();
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.font = defaultFont;
        }

        private void EnsureEventSystem()
        {
            EventSystem es = FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.transform.SetParent(transform, false);
                esGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esGo.AddComponent<InputSystemUIInputModule>();
#else
                esGo.AddComponent<StandaloneInputModule>();
#endif
            }
        }

        public void ShowEndcard()
        {
            if (_endcardPanel == null) BuildUI();
            _endcardPanel.SetActive(true);
            if (_endcardCanvasGroup != null)
            {
                _endcardCanvasGroup.DOKill();
                _endcardCanvasGroup.alpha = 0f;
                _endcardCanvasGroup.DOFade(1f, 0.45f).SetEase(Ease.OutQuad);
            }
        }

        public void HideEndcard()
        {
            if (_endcardPanel != null)
            {
                if (_endcardCanvasGroup != null) _endcardCanvasGroup.DOKill();
                _endcardPanel.SetActive(false);
            }
        }
    }
}
