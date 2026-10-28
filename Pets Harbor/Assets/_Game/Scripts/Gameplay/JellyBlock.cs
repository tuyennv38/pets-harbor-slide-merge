using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace PetsHarbor.SlideMerge
{
    [SelectionBase]
    public class JellyBlock : MonoBehaviour
    {
        public int Tier { get; private set; }
        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }

        /// <summary>
        /// Current floating-point top-left grid coordinate (Col in [0..7-W], Row in [0..8-H]).
        /// </summary>
        public float GridX { get; private set; }
        public float GridY { get; private set; }

        public int SnappedCol => Mathf.RoundToInt(GridX);
        public int SnappedRow => Mathf.RoundToInt(GridY);

        public bool IsMerging { get; set; }
        public bool IsBeingDragged { get; set; }
        public bool IsCoasting { get; set; }
        public Vector2 GridVelocity { get; set; }

        private BoardGridManager _board;
        private Transform _visualRoot;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private BoxCollider _boxCollider;
        private MeshRenderer _faceRenderer;
        private MeshRenderer _shadowRenderer;
        private Material _shadowMaterial;

        private float _impactTimer = 99f;
        private Vector2 _impactAxis;
        private float _impactStrength;

        private Vector2 _smoothedVelocity;
        private Vector2 _contactSquish;
        private Vector2 _targetContactSquish;
        private Vector3 _fusionScaleOverride = Vector3.one;
        private bool _useFusionScaleOverride = false;
        private float _idlePhaseOffset;
        private Coroutine _snapRoutine;

        private const float BlockThickness = 0.66f;
        private const float CellGap = 0.07f;

        /// <summary>
        /// Mass factor based on block footprint so larger blocks feel slightly heavier when accelerating.
        /// </summary>
        public float MassFactor => Mathf.Lerp(1.0f, 1.35f, Mathf.Clamp01((GridWidth * GridHeight - 1f) / 5f));

        public void Initialize(BoardGridManager board, int tier, float startGridX, float startGridY, bool playSpawnBounce = false)
        {
            _board = board;
            _idlePhaseOffset = Random.Range(0f, Mathf.PI * 2f);
            EnsureVisualComponents();
            ApplyTierConfig(tier);
            SetGridPositionImmediate(startGridX, startGridY);

            if (playSpawnBounce)
            {
                TriggerJellyBounce(0.32f);
            }
        }

        private void EnsureVisualComponents()
        {
            if (_visualRoot != null) return;

            _boxCollider = GetComponent<BoxCollider>();
            if (_boxCollider == null) _boxCollider = gameObject.AddComponent<BoxCollider>();

            Transform existingVisual = transform.Find("VisualRoot");
            Transform existingShadow = transform.Find("ShadowDecal");
            if (existingShadow != null)
            {
                _shadowRenderer = existingShadow.GetComponent<MeshRenderer>();
                _shadowMaterial = _shadowRenderer.sharedMaterial;
            }
            else
            {
                GameObject shadowGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                DestroyImmediate(shadowGo.GetComponent<Collider>());
                shadowGo.name = "ShadowDecal";
                shadowGo.transform.SetParent(transform, false);
                shadowGo.transform.localPosition = new Vector3(0f, 0.003f, 0f);
                shadowGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                _shadowRenderer = shadowGo.GetComponent<MeshRenderer>();
                _shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _shadowRenderer.receiveShadows = false;

                _shadowMaterial = new Material(ProceduralMeshGenerator.FindBestUnlitTransparentShader())
                {
                    name = "Mat_JellyContactShadow"
                };
                Texture2D shadowTex = ProceduralMeshGenerator.GetOrCreateSoftShadowTexture();
                if (_shadowMaterial.HasProperty("_MainTex")) _shadowMaterial.SetTexture("_MainTex", shadowTex);
                if (_shadowMaterial.HasProperty("_BaseMap")) _shadowMaterial.SetTexture("_BaseMap", shadowTex);
                _shadowRenderer.sharedMaterial = _shadowMaterial;
            }

            if (existingVisual != null)
            {
                _visualRoot = existingVisual;
                _meshFilter = _visualRoot.GetComponent<MeshFilter>();
                _meshRenderer = _visualRoot.GetComponent<MeshRenderer>();
                Transform faceTr = _visualRoot.Find("FaceDecal");
                if (faceTr != null) _faceRenderer = faceTr.GetComponent<MeshRenderer>();
                return;
            }

            _visualRoot = new GameObject("VisualRoot").transform;
            _visualRoot.SetParent(transform, false);
            _visualRoot.localPosition = Vector3.zero;

            _meshFilter = _visualRoot.gameObject.AddComponent<MeshFilter>();
            _meshRenderer = _visualRoot.gameObject.AddComponent<MeshRenderer>();

            // Create top facial expression quad
            GameObject faceGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            DestroyImmediate(faceGo.GetComponent<Collider>());
            faceGo.name = "FaceDecal";
            faceGo.transform.SetParent(_visualRoot, false);
            faceGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _faceRenderer = faceGo.GetComponent<MeshRenderer>();
            _faceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _faceRenderer.receiveShadows = false;
        }

        public void ApplyTierConfig(int newTier)
        {
            Tier = Mathf.Clamp(newTier, 1, TierDatabase.MaxTier);
            TierConfigData cfg = TierDatabase.GetTier(Tier);
            GridWidth = cfg.gridWidth;
            GridHeight = cfg.gridHeight;
            gameObject.name = $"JellyBlock_T{Tier:D2}_{cfg.idName}";

            float cellSize = _board != null ? _board.CellSize : 1.0f;
            float worldW = GridWidth * cellSize - CellGap;
            float worldD = GridHeight * cellSize - CellGap;

            if (_shadowRenderer != null)
            {
                _shadowRenderer.transform.localScale = new Vector3(worldW * 1.15f, worldD * 1.15f, 1f);
            }
            float cornerRadius = Mathf.Min(0.24f, Mathf.Min(worldW, worldD) * 0.28f);

            Mesh jellyMesh = ProceduralMeshGenerator.GetOrCreateJellyBoxMesh(worldW, BlockThickness, worldD, cornerRadius);
            _meshFilter.sharedMesh = jellyMesh;

            // Glossy Jelly Material (URP & Standard compatible)
            Shader litShader = ProceduralMeshGenerator.FindBestLitShader();
            Material bodyMat = new Material(litShader)
            {
                name = $"Mat_Jelly_T{Tier:D2}"
            };

            Texture2D glossTex = ProceduralMeshGenerator.GetOrCreateJellyBodyTexture();
            if (bodyMat.HasProperty("_BaseMap")) bodyMat.SetTexture("_BaseMap", glossTex);
            if (bodyMat.HasProperty("_MainTex")) bodyMat.SetTexture("_MainTex", glossTex);
            if (bodyMat.HasProperty("_BaseColor")) bodyMat.SetColor("_BaseColor", cfg.bodyColor);
            if (bodyMat.HasProperty("_Color")) bodyMat.SetColor("_Color", cfg.bodyColor);
            if (bodyMat.HasProperty("_Smoothness")) bodyMat.SetFloat("_Smoothness", 0.88f);
            if (bodyMat.HasProperty("_Glossiness")) bodyMat.SetFloat("_Glossiness", 0.88f);
            if (bodyMat.HasProperty("_Metallic")) bodyMat.SetFloat("_Metallic", 0.04f);

            // Subtle inner glow so jelly looks translucent and vibrant
            if (bodyMat.HasProperty("_EmissionColor"))
            {
                bodyMat.EnableKeyword("_EMISSION");
                bodyMat.SetColor("_EmissionColor", cfg.bodyColor * 0.14f);
            }

            _meshRenderer.sharedMaterial = bodyMat;

            // Configure top emoji face quad
            if (_faceRenderer != null)
            {
                float faceScaleW = worldW * 0.86f;
                float faceScaleD = worldD * 0.86f;
                // Keep facial features nicely proportioned on rectangular blocks (2x1, 3x2)
                float minSide = Mathf.Min(faceScaleW, faceScaleD);
                float maxSide = Mathf.Max(faceScaleW, faceScaleD);
                float balancedW = faceScaleW > faceScaleD ? Mathf.Lerp(minSide, maxSide, 0.55f) : minSide;
                float balancedD = faceScaleD > faceScaleW ? Mathf.Lerp(minSide, maxSide, 0.55f) : minSide;

                _faceRenderer.transform.localPosition = new Vector3(0f, BlockThickness + 0.018f, 0f);
                _faceRenderer.transform.localScale = new Vector3(balancedW, balancedD, 1f);

                Material faceMat = new Material(ProceduralMeshGenerator.FindBestUnlitTransparentShader())
                {
                    name = $"Mat_Face_T{Tier:D2}"
                };
                Texture2D faceTex = ProceduralEmojiDrawer.GetOrCreateFaceTexture(cfg.emojiFace);
                if (faceMat.HasProperty("_MainTex")) faceMat.SetTexture("_MainTex", faceTex);
                if (faceMat.HasProperty("_BaseMap")) faceMat.SetTexture("_BaseMap", faceTex);
                if (faceMat.HasProperty("_BaseColor")) faceMat.SetColor("_BaseColor", Color.white);
                if (faceMat.HasProperty("_Color")) faceMat.SetColor("_Color", Color.white);
                _faceRenderer.sharedMaterial = faceMat;
            }

            // Update Collider
            if (_boxCollider != null)
            {
                _boxCollider.center = new Vector3(0f, BlockThickness * 0.5f, 0f);
                _boxCollider.size = new Vector3(worldW, BlockThickness + 0.1f, worldD);
            }
        }

        public void SetGridPositionImmediate(float gx, float gy)
        {
            if (_snapRoutine != null)
            {
                StopCoroutine(_snapRoutine);
                _snapRoutine = null;
            }

            transform.DOKill();
            GridX = gx;
            GridY = gy;
            GridVelocity = Vector2.zero;
            if (_board != null)
            {
                transform.position = _board.GridToWorldPosition(GridX, GridY, GridWidth, GridHeight);
            }
        }

        public void SetGridPositionFromDrag(float gx, float gy)
        {
            if (_snapRoutine != null)
            {
                StopCoroutine(_snapRoutine);
                _snapRoutine = null;
            }

            transform.DOKill();
            GridX = gx;
            GridY = gy;
            if (_board != null)
            {
                Vector3 targetWorld = _board.GridToWorldPosition(GridX, GridY, GridWidth, GridHeight);
                if (Application.isPlaying && Time.deltaTime > 0f)
                {
                    float sharpness = GameFeelConfig.Instance.dragFollowSharpness;
                    float followT = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
                    transform.position = Vector3.Lerp(transform.position, targetWorld, followT);
                }
                else
                {
                    transform.position = targetWorld;
                }
            }
        }

        public void SetGridPositionSmooth(float targetGx, float targetGy, float duration = 0.09f)
        {
            if (_snapRoutine != null) StopCoroutine(_snapRoutine);
            GridX = targetGx;
            GridY = targetGy;
            GridVelocity = Vector2.zero;
            if (!Application.isPlaying)
            {
                if (_board != null) transform.position = _board.GridToWorldPosition(GridX, GridY, GridWidth, GridHeight);
                return;
            }

            transform.DOKill();
            Vector3 endWorld = _board != null
                ? _board.GridToWorldPosition(targetGx, targetGy, GridWidth, GridHeight)
                : transform.position;
            transform.DOMove(endWorld, duration).SetEase(Ease.OutQuad);
        }

        public void SetDragVelocity(Vector2 gridVel)
        {
            GridVelocity = gridVel;
        }

        /// <summary>
        /// Triggers a directional squash & rebound when a fast-sliding block hits a wall or obstacle.
        /// </summary>
        public void TriggerDirectionalImpact(Vector2 axis, float impactSpeed)
        {
            float strength = Mathf.Clamp(impactSpeed * 0.016f, 0.04f, 0.22f);
            if (_impactTimer < 0.15f && strength < _impactStrength) return;

            _impactAxis = new Vector2(Mathf.Abs(axis.x), Mathf.Abs(axis.y)).normalized;
            _impactStrength = strength;
            _impactTimer = 0f;
        }

        /// <summary>
        /// Sets tactile contact compression when pressing against a same-tier block prior to merging.
        /// </summary>
        public void SetContactSquish(Vector2 squish)
        {
            _targetContactSquish = new Vector2(
                Mathf.Clamp(squish.x, 0f, 0.24f),
                Mathf.Clamp(squish.y, 0f, 0.24f));
        }

        public void SetFusionVisualState(Vector3 worldPos, Vector3 scaleOverride)
        {
            transform.position = worldPos;
            _useFusionScaleOverride = true;
            _fusionScaleOverride = scaleOverride;
            if (_visualRoot != null)
            {
                _visualRoot.DOKill();
                _visualRoot.localRotation = Quaternion.identity;
            }
        }

        public void ClearFusionVisualState()
        {
            _useFusionScaleOverride = false;
            _targetContactSquish = Vector2.zero;
            _contactSquish = Vector2.zero;
            GridVelocity = Vector2.zero;
            _smoothedVelocity = Vector2.zero;
            if (_visualRoot != null)
            {
                _visualRoot.DOKill();
                _visualRoot.localRotation = Quaternion.identity;
            }
        }

        public void TriggerJellyBounce(float amplitude = 0.28f)
        {
            if (_visualRoot == null) return;
            _visualRoot.DOKill();
            _visualRoot.localScale = Vector3.one;
            _visualRoot.DOPunchScale(new Vector3(amplitude, -amplitude * 0.72f, amplitude), 0.36f, 7, 0.65f);
        }

        public void TriggerMergeEvolutionPop(float duration = 0.34f)
        {
            ClearFusionVisualState();
            _impactTimer = 99f;

            if (_visualRoot == null) return;
            _visualRoot.DOKill();
            _visualRoot.localScale = new Vector3(0.52f, 1.48f, 0.52f);

            Sequence popSeq = DOTween.Sequence();
            popSeq.Append(_visualRoot.DOScale(new Vector3(1.26f, 0.76f, 1.26f), duration * 0.38f).SetEase(Ease.OutQuad));
            popSeq.Append(_visualRoot.DOScale(new Vector3(0.92f, 1.08f, 0.92f), duration * 0.28f).SetEase(Ease.InOutSine));
            popSeq.Append(_visualRoot.DOScale(Vector3.one, duration * 0.34f).SetEase(Ease.OutBack));
        }

        private void OnDestroy()
        {
            transform.DOKill();
            if (_visualRoot != null) _visualRoot.DOKill();
        }

        private void Update()
        {
            if (_visualRoot == null) return;

            if (_useFusionScaleOverride)
            {
                _visualRoot.localScale = _fusionScaleOverride;
                return;
            }

            // Smoothly follow GridX/GridY in world space when idle
            if (_board != null && _snapRoutine == null && !DOTween.IsTweening(transform) && !IsMerging && !IsBeingDragged && !IsCoasting)
            {
                Vector3 targetWorld = _board.GridToWorldPosition(GridX, GridY, GridWidth, GridHeight);
                float lerpFactor = 1f - Mathf.Exp(-32f * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, targetWorld, lerpFactor);
            }

            _smoothedVelocity = Vector2.Lerp(_smoothedVelocity, GridVelocity, 1f - Mathf.Exp(-20f * Time.deltaTime));

            // Smoothly interpolate contact squish and decay target so it relaxes when contact ends
            _contactSquish = Vector2.Lerp(_contactSquish, _targetContactSquish, 1f - Mathf.Exp(-26f * Time.deltaTime));
            _targetContactSquish = Vector2.MoveTowards(_targetContactSquish, Vector2.zero, Time.deltaTime * 1.8f);

            // Sub-surface face parallax: shift face slightly in drag direction
            if (_faceRenderer != null)
            {
                float faceShiftX = Mathf.Clamp(_smoothedVelocity.x * 0.007f, -0.035f, 0.035f);
                float faceShiftZ = Mathf.Clamp(-_smoothedVelocity.y * 0.007f, -0.035f, 0.035f);
                _faceRenderer.transform.localPosition = new Vector3(faceShiftX, BlockThickness + 0.018f, faceShiftZ);
            }

            // Soft contact shadow: fade & expand slightly when lifted
            if (_shadowMaterial != null)
            {
                bool isFloating = IsBeingDragged || IsCoasting;
                float targetAlpha = isFloating ? 0.20f : 0.42f;
                Color c = _shadowMaterial.color;
                c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * 14f);
                _shadowMaterial.color = c;
            }

            // If DOTween is running a punch or evolution pop, let DOTween handle localScale
            if (DOTween.IsTweening(_visualRoot))
            {
                return;
            }

            float sx = 1f;
            float sz = 1f;

            // 1. Subtle jelly breathing idle
            float idle = Mathf.Sin(Time.time * 2.6f + _idlePhaseOffset) * 0.010f;
            sx += idle;
            sz += idle;

            // 2. Directional Wall/Obstacle Impact Squash & Rebound
            if (_impactTimer < 0.28f)
            {
                _impactTimer += Time.deltaTime;
                float damp = Mathf.Exp(-11f * _impactTimer) * Mathf.Sin(26f * _impactTimer);
                float impX = _impactAxis.x * _impactStrength * damp;
                float impZ = _impactAxis.y * _impactStrength * damp;
                sx *= (1f - impX + impZ * 0.55f);
                sz *= (1f - impZ + impX * 0.55f);
            }

            // 3. Contact squish when pressing into a matching block before reaching deep merge depth
            if (_contactSquish.sqrMagnitude > 0.0001f)
            {
                sx *= (1f - _contactSquish.x + _contactSquish.y * 0.52f);
                sz *= (1f - _contactSquish.y + _contactSquish.x * 0.52f);
            }

            // 4. Velocity Stretch & Acceleration Lean
            if (IsBeingDragged || IsCoasting)
            {
                GameFeelConfig feel = GameFeelConfig.Instance;
                float maxStretch = feel.jellyVelocityStretch;
                float maxTilt = feel.jellyVelocityTiltAngle;

                float stretchX = Mathf.Clamp(Mathf.Abs(_smoothedVelocity.x) * 0.012f, 0f, maxStretch);
                float stretchZ = Mathf.Clamp(Mathf.Abs(_smoothedVelocity.y) * 0.012f, 0f, maxStretch);
                sx += stretchX - stretchZ * 0.42f;
                sz += stretchZ - stretchX * 0.42f;

                float tiltZ = Mathf.Clamp(-_smoothedVelocity.x * (maxTilt / 12f), -maxTilt, maxTilt);
                float tiltX = Mathf.Clamp(-_smoothedVelocity.y * (maxTilt / 12f), -maxTilt, maxTilt);
                _visualRoot.localRotation = Quaternion.Lerp(_visualRoot.localRotation, Quaternion.Euler(tiltX, 0f, tiltZ), Time.deltaTime * 18f);
                _visualRoot.localPosition = Vector3.Lerp(_visualRoot.localPosition, new Vector3(0f, 0.045f, 0f), Time.deltaTime * 16f);
            }
            else
            {
                GridVelocity = Vector2.Lerp(GridVelocity, Vector2.zero, Time.deltaTime * 16f);
                _visualRoot.localRotation = Quaternion.Lerp(_visualRoot.localRotation, Quaternion.identity, Time.deltaTime * 18f);
                _visualRoot.localPosition = Vector3.Lerp(_visualRoot.localPosition, Vector3.zero, Time.deltaTime * 16f);
            }

            // Volume-preserving deformation: Sy = 1 / (Sx * Sz)
            float sy = Mathf.Clamp(1.0f / Mathf.Max(0.1f, sx * sz), 0.68f, 1.45f);

            _visualRoot.localScale = new Vector3(sx, sy, sz);
        }
    }
}
