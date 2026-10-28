using System.Collections.Generic;
using UnityEngine;

namespace PetsHarbor.SlideMerge
{
    public static class ProceduralEmojiDrawer
    {
        private static readonly Dictionary<EmojiFaceType, Texture2D> FaceTexCache = new Dictionary<EmojiFaceType, Texture2D>();
        private static readonly Dictionary<int, Sprite> TierIconSpriteCache = new Dictionary<int, Sprite>();
        private static Sprite _checkMarkSprite;
        private static Sprite _gloveHandSprite;

        public static void ClearCaches()
        {
            FaceTexCache.Clear();
            TierIconSpriteCache.Clear();
            _checkMarkSprite = null;
            _gloveHandSprite = null;
        }

        private static float SmoothStep01(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / Mathf.Max(0.00001f, edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        public static Texture2D GetOrCreateFaceTexture(EmojiFaceType faceType)
        {
            if (FaceTexCache.TryGetValue(faceType, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            const int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"EmojiFace_{faceType}",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            Color darkBrown = new Color(0.16f, 0.08f, 0.04f, 1f);
            Color blushPink = new Color(0.96f, 0.45f, 0.48f, 0.78f);
            Color mouthRed = new Color(0.55f, 0.10f, 0.14f, 1f);
            Color tonguePink = new Color(0.95f, 0.48f, 0.52f, 1f);
            Color eyeRed = new Color(0.84f, 0.18f, 0.16f, 1f);

            // 1. Glossy jelly highlight spots in upper corners
            DrawSoftEllipse(pixels, size, 0.15f, 0.85f, 0.045f, 0.09f, new Color(1f, 1f, 1f, 0.65f));
            DrawSoftEllipse(pixels, size, 0.84f, 0.86f, 0.03f, 0.06f, new Color(1f, 1f, 1f, 0.42f));

            // 2. Cute pink blush cheeks on all faces
            DrawSoftEllipse(pixels, size, 0.24f, 0.36f, 0.075f, 0.042f, blushPink);
            DrawSoftEllipse(pixels, size, 0.76f, 0.36f, 0.075f, 0.042f, blushPink);

            // 3. Specific facial expression per Tier (1..12)
            switch (faceType)
            {
                case EmojiFaceType.T01_AngryFrown:
                    // Angry eyebrows + round eyes + small frown
                    DrawLine(pixels, size, 0.24f, 0.58f, 0.37f, 0.52f, 0.022f, darkBrown);
                    DrawLine(pixels, size, 0.76f, 0.58f, 0.63f, 0.52f, 0.022f, darkBrown);
                    DrawEyeWithHighlight(pixels, size, 0.31f, 0.46f, 0.042f, darkBrown);
                    DrawEyeWithHighlight(pixels, size, 0.69f, 0.46f, 0.042f, darkBrown);
                    DrawArc(pixels, size, 0.50f, 0.27f, 0.10f, 35f, 145f, 0.022f, darkBrown);
                    break;

                case EmojiFaceType.T02_HappySmile:
                    // Cute round eyes + open D smile with pink tongue
                    DrawEyeWithHighlight(pixels, size, 0.31f, 0.48f, 0.045f, darkBrown);
                    DrawEyeWithHighlight(pixels, size, 0.69f, 0.48f, 0.045f, darkBrown);
                    DrawOpenMouth(pixels, size, 0.50f, 0.35f, 0.09f, 0.06f, darkBrown, tonguePink, true);
                    break;

                case EmojiFaceType.T03_WinkCat:
                    // Left round eye + right wink '<' + cat ':3' mouth
                    DrawEyeWithHighlight(pixels, size, 0.30f, 0.47f, 0.048f, darkBrown);
                    DrawLine(pixels, size, 0.74f, 0.53f, 0.64f, 0.47f, 0.024f, darkBrown);
                    DrawLine(pixels, size, 0.64f, 0.47f, 0.74f, 0.41f, 0.024f, darkBrown);
                    DrawCatMouth(pixels, size, 0.50f, 0.37f, 0.065f, 0.022f, darkBrown);
                    break;

                case EmojiFaceType.T04_CalmSmile:
                    // Closed happy arched eyes ^ ^ + gentle smile
                    DrawArc(pixels, size, 0.31f, 0.45f, 0.065f, 25f, 155f, 0.024f, darkBrown);
                    DrawArc(pixels, size, 0.69f, 0.45f, 0.065f, 25f, 155f, 0.024f, darkBrown);
                    DrawArc(pixels, size, 0.50f, 0.42f, 0.095f, 205f, 335f, 0.024f, darkBrown);
                    break;

                case EmojiFaceType.T05_WorriedSweat:
                    // Worried outer-slanted eyebrows + round eyes + slanted wavy mouth
                    DrawLine(pixels, size, 0.24f, 0.54f, 0.36f, 0.60f, 0.020f, darkBrown);
                    DrawLine(pixels, size, 0.64f, 0.60f, 0.76f, 0.54f, 0.020f, darkBrown);
                    DrawEyeWithHighlight(pixels, size, 0.32f, 0.46f, 0.044f, darkBrown);
                    DrawEyeWithHighlight(pixels, size, 0.68f, 0.46f, 0.044f, darkBrown);
                    DrawLine(pixels, size, 0.41f, 0.32f, 0.57f, 0.37f, 0.022f, darkBrown);
                    break;

                case EmojiFaceType.T06_SquintCat:
                    // Tight squinting > < eyes + open happy mouth
                    DrawLine(pixels, size, 0.24f, 0.54f, 0.36f, 0.47f, 0.026f, darkBrown);
                    DrawLine(pixels, size, 0.36f, 0.47f, 0.24f, 0.40f, 0.026f, darkBrown);
                    DrawLine(pixels, size, 0.76f, 0.54f, 0.64f, 0.47f, 0.026f, darkBrown);
                    DrawLine(pixels, size, 0.64f, 0.47f, 0.76f, 0.40f, 0.026f, darkBrown);
                    DrawOpenMouth(pixels, size, 0.50f, 0.34f, 0.095f, 0.068f, darkBrown, tonguePink, true);
                    break;

                case EmojiFaceType.T07_GrumpyZigzag:
                    // Fierce V eyebrows + shiny eyes + zigzag 'www' mouth
                    DrawLine(pixels, size, 0.22f, 0.59f, 0.37f, 0.51f, 0.026f, darkBrown);
                    DrawLine(pixels, size, 0.78f, 0.59f, 0.63f, 0.51f, 0.026f, darkBrown);
                    DrawEyeWithHighlight(pixels, size, 0.31f, 0.44f, 0.046f, darkBrown);
                    DrawEyeWithHighlight(pixels, size, 0.69f, 0.44f, 0.046f, darkBrown);
                    DrawZigZagMouth(pixels, size, 0.35f, 0.65f, 0.31f, 0.024f, 4, 0.020f, darkBrown);
                    break;

                case EmojiFaceType.T08_SurprisedO:
                    // Wide white scleras with dark pupils O_O + open oval mouth
                    DrawArc(pixels, size, 0.31f, 0.58f, 0.075f, 35f, 145f, 0.020f, darkBrown);
                    DrawArc(pixels, size, 0.69f, 0.58f, 0.075f, 35f, 145f, 0.020f, darkBrown);
                    DrawSurprisedEye(pixels, size, 0.31f, 0.49f, 0.082f, darkBrown);
                    DrawSurprisedEye(pixels, size, 0.69f, 0.49f, 0.082f, darkBrown);
                    DrawOpenMouth(pixels, size, 0.50f, 0.31f, 0.078f, 0.095f, darkBrown, tonguePink, false);
                    break;

                case EmojiFaceType.T09_AngryShout:
                    // Angry eyebrows + white sclera with red pupils + wide shouting mouth
                    DrawLine(pixels, size, 0.22f, 0.62f, 0.38f, 0.55f, 0.024f, darkBrown);
                    DrawLine(pixels, size, 0.78f, 0.62f, 0.62f, 0.55f, 0.024f, darkBrown);
                    DrawSurprisedEye(pixels, size, 0.31f, 0.47f, 0.078f, eyeRed);
                    DrawSurprisedEye(pixels, size, 0.69f, 0.47f, 0.078f, eyeRed);
                    DrawOpenMouth(pixels, size, 0.50f, 0.31f, 0.11f, 0.075f, mouthRed, tonguePink, false);
                    break;

                case EmojiFaceType.T10_SadPuppy:
                    // Sad worried eyebrows + big glossy eyes + sad upside-down U mouth
                    DrawLine(pixels, size, 0.23f, 0.55f, 0.35f, 0.61f, 0.024f, darkBrown);
                    DrawLine(pixels, size, 0.65f, 0.61f, 0.77f, 0.55f, 0.024f, darkBrown);
                    DrawEyeWithHighlight(pixels, size, 0.33f, 0.46f, 0.052f, darkBrown);
                    DrawEyeWithHighlight(pixels, size, 0.67f, 0.46f, 0.052f, darkBrown);
                    DrawArc(pixels, size, 0.50f, 0.25f, 0.085f, 25f, 155f, 0.028f, darkBrown);
                    break;

                case EmojiFaceType.T11_LaughSquint:
                    // Big > < squinting eyes + large open shouting mouth with tongue
                    DrawLine(pixels, size, 0.21f, 0.56f, 0.35f, 0.49f, 0.028f, darkBrown);
                    DrawLine(pixels, size, 0.35f, 0.49f, 0.21f, 0.42f, 0.028f, darkBrown);
                    DrawLine(pixels, size, 0.79f, 0.56f, 0.65f, 0.49f, 0.028f, darkBrown);
                    DrawLine(pixels, size, 0.65f, 0.49f, 0.79f, 0.42f, 0.028f, darkBrown);
                    DrawOpenMouth(pixels, size, 0.50f, 0.34f, 0.085f, 0.105f, darkBrown, tonguePink, false);
                    break;

                case EmojiFaceType.T12_CuteWinkGoal:
                    // Target Purple Goal: Big left shiny eye + right wink '<' + cute ':3' cat mouth
                    DrawEyeWithHighlight(pixels, size, 0.30f, 0.48f, 0.062f, darkBrown);
                    DrawLine(pixels, size, 0.76f, 0.55f, 0.62f, 0.48f, 0.030f, darkBrown);
                    DrawLine(pixels, size, 0.62f, 0.48f, 0.76f, 0.41f, 0.030f, darkBrown);
                    DrawCatMouth(pixels, size, 0.50f, 0.38f, 0.075f, 0.028f, darkBrown);
                    break;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            FaceTexCache[faceType] = tex;
            return tex;
        }

        public static Sprite GetOrCreateTierIconSprite(int tier)
        {
            if (TierIconSpriteCache.TryGetValue(tier, out Sprite cached) && cached != null)
            {
                return cached;
            }

            TierConfigData cfg = TierDatabase.GetTier(tier);
            const int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"TierIcon_{tier}",
                filterMode = FilterMode.Bilinear
            };

            Texture2D faceTex = GetOrCreateFaceTexture(cfg.emojiFace);
            Color[] pixels = new Color[size * size];

            float aspectW = cfg.gridWidth > cfg.gridHeight ? 0.92f : 0.78f;
            float aspectH = cfg.gridHeight > cfg.gridWidth ? 0.92f : (cfg.gridWidth > cfg.gridHeight ? 0.68f : 0.84f);

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / (size - 1);
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / (size - 1);
                    float dx = Mathf.Abs(u - 0.5f) / (aspectW * 0.5f);
                    float dy = Mathf.Abs(v - 0.5f) / (aspectH * 0.5f);

                    // Rounded superellipse SDF
                    float dist = Mathf.Pow(Mathf.Pow(dx, 4.5f) + Mathf.Pow(dy, 4.5f), 1f / 4.5f);
                    float alpha = 1f - SmoothStep01(0.94f, 1.0f, dist);
                    if (alpha <= 0.001f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    float rim = SmoothStep01(0.65f, 0.95f, dist);
                    Color body = Color.Lerp(cfg.bodyColor, cfg.bodyColor * 0.78f, rim);
                    float topSpec = SmoothStep01(0.65f, 0.92f, v) * (1f - rim);
                    body = Color.Lerp(body, Color.white, topSpec * 0.28f);

                    // Sample face texture inside box
                    float fu = Mathf.Clamp01((u - 0.5f) / aspectW + 0.5f);
                    float fv = Mathf.Clamp01((v - 0.5f) / aspectH + 0.5f);
                    Color faceCol = faceTex.GetPixelBilinear(fu, fv);

                    Color finalCol = Color.Lerp(body, faceCol, faceCol.a);
                    finalCol.a = alpha;
                    pixels[y * size + x] = finalCol;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            TierIconSpriteCache[tier] = sp;
            return sp;
        }

        public static Sprite GetOrCreateCheckMarkSprite()
        {
            if (_checkMarkSprite != null) return _checkMarkSprite;

            const int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "GreenCheckMark",
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            Color outline = new Color(0.12f, 0.45f, 0.08f, 1f);
            Color fill = new Color(0.32f, 0.92f, 0.22f, 1f);
            Color highlight = new Color(0.68f, 1.0f, 0.55f, 1f);

            DrawLine(pixels, size, 0.22f, 0.48f, 0.44f, 0.26f, 0.095f, outline);
            DrawLine(pixels, size, 0.44f, 0.26f, 0.80f, 0.72f, 0.095f, outline);

            DrawLine(pixels, size, 0.22f, 0.48f, 0.44f, 0.26f, 0.068f, fill);
            DrawLine(pixels, size, 0.44f, 0.26f, 0.80f, 0.72f, 0.068f, fill);

            DrawLine(pixels, size, 0.22f, 0.50f, 0.44f, 0.28f, 0.025f, highlight);
            DrawLine(pixels, size, 0.44f, 0.28f, 0.80f, 0.74f, 0.025f, highlight);

            tex.SetPixels(pixels);
            tex.Apply();
            _checkMarkSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _checkMarkSprite;
        }

        public static Sprite GetOrCreateGloveHandSprite()
        {
            if (_gloveHandSprite != null) return _gloveHandSprite;

            const int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "TutorialGloveHand",
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            Color shadow = new Color(0.55f, 0.58f, 0.65f, 0.85f);
            Color gloveWhite = new Color(0.97f, 0.98f, 1.0f, 1.0f);
            Color gloveShade = new Color(0.84f, 0.86f, 0.92f, 1.0f);

            // Index finger pointing up-left (matching the 3D white cartoon glove in the video)
            DrawLine(pixels, size, 0.48f, 0.44f, 0.26f, 0.84f, 0.115f, shadow);
            DrawLine(pixels, size, 0.48f, 0.45f, 0.26f, 0.85f, 0.095f, gloveWhite);

            // Other curled fingers & palm
            DrawSoftEllipse(pixels, size, 0.56f, 0.38f, 0.22f, 0.19f, shadow);
            DrawSoftEllipse(pixels, size, 0.55f, 0.40f, 0.20f, 0.17f, gloveWhite);
            DrawSoftEllipse(pixels, size, 0.58f, 0.55f, 0.07f, 0.09f, gloveShade);
            DrawSoftEllipse(pixels, size, 0.68f, 0.51f, 0.07f, 0.08f, gloveShade);
            DrawSoftEllipse(pixels, size, 0.76f, 0.44f, 0.06f, 0.07f, gloveShade);

            // Wrist cuff
            DrawLine(pixels, size, 0.56f, 0.20f, 0.72f, 0.26f, 0.09f, gloveShade);
            DrawLine(pixels, size, 0.56f, 0.21f, 0.72f, 0.27f, 0.07f, gloveWhite);

            tex.SetPixels(pixels);
            tex.Apply();
            // Pivot near the tip of the pointing finger (0.26, 0.85)
            _gloveHandSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.26f, 0.85f), 100f);
            return _gloveHandSprite;
        }

        private static void DrawEyeWithHighlight(Color[] pixels, int size, float cx, float cy, float radius, Color pupilCol)
        {
            DrawCircle(pixels, size, cx, cy, radius, pupilCol);
            DrawCircle(pixels, size, cx - radius * 0.28f, cy + radius * 0.30f, radius * 0.34f, Color.white);
            DrawCircle(pixels, size, cx + radius * 0.25f, cy - radius * 0.28f, radius * 0.16f, new Color(1f, 1f, 1f, 0.8f));
        }

        private static void DrawSurprisedEye(Color[] pixels, int size, float cx, float cy, float radius, Color pupilCol)
        {
            DrawCircle(pixels, size, cx, cy, radius * 1.08f, new Color(0.22f, 0.12f, 0.06f, 0.65f));
            DrawCircle(pixels, size, cx, cy, radius, Color.white);
            DrawCircle(pixels, size, cx, cy, radius * 0.52f, pupilCol);
            DrawCircle(pixels, size, cx - radius * 0.20f, cy + radius * 0.22f, radius * 0.20f, Color.white);
        }

        private static void DrawOpenMouth(
            Color[] pixels,
            int size,
            float cx,
            float cy,
            float rx,
            float ry,
            Color innerColor,
            Color tongueColor,
            bool flatTopDShape)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt((cx - rx - 0.02f) * size));
            int maxX = Mathf.Min(size - 1, Mathf.CeilToInt((cx + rx + 0.02f) * size));
            int minY = Mathf.Max(0, Mathf.FloorToInt((cy - ry - 0.02f) * size));
            int maxY = Mathf.Min(size - 1, Mathf.CeilToInt((cy + ry + 0.02f) * size));

            for (int y = minY; y <= maxY; y++)
            {
                float v = (float)y / size;
                for (int x = minX; x <= maxX; x++)
                {
                    float u = (float)x / size;
                    float dx = (u - cx) / rx;
                    float dy = (v - cy) / ry;

                    if (flatTopDShape && dy > 0.35f) continue;

                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = 1f - SmoothStep01(0.90f, 1.0f, d);
                    if (a <= 0f) continue;

                    // Tongue at the bottom of the mouth
                    float tdx = (u - cx) / (rx * 0.75f);
                    float tdy = (v - (cy - ry * 0.55f)) / (ry * 0.55f);
                    float td = Mathf.Sqrt(tdx * tdx + tdy * tdy);
                    Color col = td < 1.0f ? Color.Lerp(innerColor, tongueColor, 1f - SmoothStep01(0.85f, 1.0f, td)) : innerColor;

                    // Optional tiny white teeth line at top for D-shape smile
                    if (flatTopDShape && dy > 0.12f && Mathf.Abs(dx) < 0.72f)
                    {
                        col = Color.white;
                    }

                    BlendPixel(pixels, y * size + x, col, a);
                }
            }
        }

        private static void DrawCatMouth(Color[] pixels, int size, float cx, float cy, float lobeRadius, float thickness, Color col)
        {
            DrawArc(pixels, size, cx - lobeRadius * 0.88f, cy, lobeRadius, 200f, 350f, thickness, col);
            DrawArc(pixels, size, cx + lobeRadius * 0.88f, cy, lobeRadius, 190f, 340f, thickness, col);
        }

        private static void DrawZigZagMouth(
            Color[] pixels,
            int size,
            float xStart,
            float xEnd,
            float yCenter,
            float amp,
            int teeth,
            float thickness,
            Color col)
        {
            int steps = teeth * 2;
            float stepW = (xEnd - xStart) / steps;
            for (int i = 0; i < steps; i++)
            {
                float x0 = xStart + i * stepW;
                float x1 = x0 + stepW;
                float y0 = yCenter + ((i % 2 == 0) ? -amp : amp);
                float y1 = yCenter + ((i % 2 == 0) ? amp : -amp);
                DrawLine(pixels, size, x0, y0, x1, y1, thickness, col);
            }
        }

        private static void DrawCircle(Color[] pixels, int size, float cx, float cy, float radius, Color col)
        {
            DrawSoftEllipse(pixels, size, cx, cy, radius, radius, col, 0.92f);
        }

        private static void DrawSoftEllipse(
            Color[] pixels,
            int size,
            float cx,
            float cy,
            float rx,
            float ry,
            Color col,
            float featherStart = 0.65f)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt((cx - rx - 0.02f) * size));
            int maxX = Mathf.Min(size - 1, Mathf.CeilToInt((cx + rx + 0.02f) * size));
            int minY = Mathf.Max(0, Mathf.FloorToInt((cy - ry - 0.02f) * size));
            int maxY = Mathf.Min(size - 1, Mathf.CeilToInt((cy + ry + 0.02f) * size));

            for (int y = minY; y <= maxY; y++)
            {
                float v = (float)y / size;
                for (int x = minX; x <= maxX; x++)
                {
                    float u = (float)x / size;
                    float dx = (u - cx) / rx;
                    float dy = (v - cy) / ry;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > 1f) continue;

                    float a = 1f - SmoothStep01(featherStart, 1.0f, d);
                    BlendPixel(pixels, y * size + x, col, a * col.a);
                }
            }
        }

        private static void DrawLine(
            Color[] pixels,
            int size,
            float x0,
            float y0,
            float x1,
            float y1,
            float radius,
            Color col)
        {
            float minX = Mathf.Min(x0, x1) - radius - 0.01f;
            float maxX = Mathf.Max(x0, x1) + radius + 0.01f;
            float minY = Mathf.Min(y0, y1) - radius - 0.01f;
            float maxY = Mathf.Max(y0, y1) + radius + 0.01f;

            int ix0 = Mathf.Max(0, Mathf.FloorToInt(minX * size));
            int ix1 = Mathf.Min(size - 1, Mathf.CeilToInt(maxX * size));
            int iy0 = Mathf.Max(0, Mathf.FloorToInt(minY * size));
            int iy1 = Mathf.Min(size - 1, Mathf.CeilToInt(maxY * size));

            Vector2 a = new Vector2(x0, y0);
            Vector2 b = new Vector2(x1, y1);
            Vector2 ab = b - a;
            float abLenSq = Mathf.Max(0.000001f, Vector2.Dot(ab, ab));

            for (int y = iy0; y <= iy1; y++)
            {
                float v = (float)y / size;
                for (int x = ix0; x <= ix1; x++)
                {
                    float u = (float)x / size;
                    Vector2 p = new Vector2(u, v);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / abLenSq);
                    Vector2 closest = a + ab * t;
                    float dist = Vector2.Distance(p, closest);
                    if (dist > radius) continue;

                    float alpha = 1f - SmoothStep01(radius * 0.72f, radius, dist);
                    BlendPixel(pixels, y * size + x, col, alpha * col.a);
                }
            }
        }

        private static void DrawArc(
            Color[] pixels,
            int size,
            float cx,
            float cy,
            float radius,
            float startDeg,
            float endDeg,
            float thickness,
            Color col)
        {
            int segments = 24;
            float prevRad = startDeg * Mathf.Deg2Rad;
            Vector2 prev = new Vector2(cx + Mathf.Cos(prevRad) * radius, cy + Mathf.Sin(prevRad) * radius);

            for (int i = 1; i <= segments; i++)
            {
                float deg = Mathf.Lerp(startDeg, endDeg, (float)i / segments);
                float rad = deg * Mathf.Deg2Rad;
                Vector2 next = new Vector2(cx + Mathf.Cos(rad) * radius, cy + Mathf.Sin(rad) * radius);
                DrawLine(pixels, size, prev.x, prev.y, next.x, next.y, thickness * 0.5f, col);
                prev = next;
            }
        }

        private static void BlendPixel(Color[] pixels, int idx, Color src, float srcAlpha)
        {
            if (srcAlpha <= 0.001f) return;
            Color dst = pixels[idx];
            float outA = srcAlpha + dst.a * (1f - srcAlpha);
            if (outA <= 0.0001f) return;

            Color rgb = (src * srcAlpha + dst * dst.a * (1f - srcAlpha)) / outA;
            rgb.a = outA;
            pixels[idx] = rgb;
        }
    }
}
