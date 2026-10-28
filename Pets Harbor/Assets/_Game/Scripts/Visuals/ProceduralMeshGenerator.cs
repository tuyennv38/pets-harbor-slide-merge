using System.Collections.Generic;
using UnityEngine;

namespace PetsHarbor.SlideMerge
{
    public static class ProceduralMeshGenerator
    {
        private static readonly Dictionary<string, Mesh> MeshCache = new Dictionary<string, Mesh>();
        private static Texture2D _cachedWoodFloorTex;
        private static Texture2D _cachedWoodRimTex;
        private static Texture2D _cachedJellyGlossTex;
        private static Texture2D _cachedShadowTex;

        /// <summary>
        /// Generates a plump 3D rounded/beveled jelly pillow box mesh centered at (0, height/2, 0).
        /// Top face has clean [0..1] UVs for the facial expression decal or shader.
        /// </summary>
        public static Mesh GetOrCreateJellyBoxMesh(float width, float height, float depth, float cornerRadius = 0.22f)
        {
            string key = $"Jelly_{width:F2}_{height:F2}_{depth:F2}_{cornerRadius:F2}";
            if (MeshCache.TryGetValue(key, out Mesh cached) && cached != null)
            {
                return cached;
            }

            int segsX = 20;
            int segsZ = 20;
            int segsY = 10;

            float halfW = width * 0.5f;
            float halfD = depth * 0.5f;
            float rXZ = Mathf.Min(cornerRadius, Mathf.Min(halfW, halfD) * 0.48f);
            float rY = Mathf.Min(height * 0.38f, rXZ);

            // Build a subdivided box and project points onto a rounded super-ellipsoid / beveled pillow box
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> norms = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            void AddGridFace(Vector3 origin, Vector3 axisU, Vector3 axisV, int su, int sv)
            {
                int startIdx = verts.Count;
                for (int iv = 0; iv <= sv; iv++)
                {
                    float tv = (float)iv / sv;
                    for (int iu = 0; iu <= su; iu++)
                    {
                        float tu = (float)iu / su;
                        Vector3 raw = origin + axisU * tu + axisV * tv;
                        Vector3 deformed = DeformToRoundedJelly(raw, halfW, height, halfD, rXZ, rY, out Vector3 normal);
                        verts.Add(deformed);
                        norms.Add(normal);
                        uvs.Add(new Vector2((deformed.x + halfW) / width, (deformed.z + halfD) / depth));
                    }
                }

                int stride = su + 1;
                for (int iv = 0; iv < sv; iv++)
                {
                    for (int iu = 0; iu < su; iu++)
                    {
                        int i0 = startIdx + iv * stride + iu;
                        int i1 = i0 + 1;
                        int i2 = i0 + stride;
                        int i3 = i2 + 1;

                        tris.Add(i0);
                        tris.Add(i2);
                        tris.Add(i1);

                        tris.Add(i1);
                        tris.Add(i2);
                        tris.Add(i3);
                    }
                }
            }

            // Top (+Y)
            AddGridFace(new Vector3(-halfW, height, -halfD), new Vector3(width, 0, 0), new Vector3(0, 0, depth), segsX, segsZ);
            // Bottom (-Y)
            AddGridFace(new Vector3(-halfW, 0, halfD), new Vector3(width, 0, 0), new Vector3(0, 0, -depth), segsX, segsZ);
            // Front (-Z)
            AddGridFace(new Vector3(-halfW, 0, -halfD), new Vector3(width, 0, 0), new Vector3(0, height, 0), segsX, segsY);
            // Back (+Z)
            AddGridFace(new Vector3(halfW, 0, halfD), new Vector3(-width, 0, 0), new Vector3(0, height, 0), segsX, segsY);
            // Left (-X)
            AddGridFace(new Vector3(-halfW, 0, halfD), new Vector3(0, 0, -depth), new Vector3(0, height, 0), segsZ, segsY);
            // Right (+X)
            AddGridFace(new Vector3(halfW, 0, -halfD), new Vector3(0, 0, depth), new Vector3(0, height, 0), segsZ, segsY);

            Mesh mesh = new Mesh
            {
                name = key
            };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            MeshCache[key] = mesh;
            return mesh;
        }

        private static Vector3 DeformToRoundedJelly(
            Vector3 p,
            float halfW,
            float height,
            float halfD,
            float rXZ,
            float rY,
            out Vector3 normal)
        {
            float halfH = height * 0.5f;
            Vector3 local = new Vector3(p.x, p.y - halfH, p.z);

            float innerX = Mathf.Max(0.001f, halfW - rXZ);
            float innerY = Mathf.Max(0.001f, halfH - rY);
            float innerZ = Mathf.Max(0.001f, halfD - rXZ);

            Vector3 core = new Vector3(
                Mathf.Clamp(local.x, -innerX, innerX),
                Mathf.Clamp(local.y, -innerY, innerY),
                Mathf.Clamp(local.z, -innerZ, innerZ));

            Vector3 delta = local - core;
            Vector3 scaledDelta = new Vector3(
                delta.x / Mathf.Max(0.001f, rXZ),
                delta.y / Mathf.Max(0.001f, rY),
                delta.z / Mathf.Max(0.001f, rXZ));

            float len = scaledDelta.magnitude;
            Vector3 dir = len > 0.0001f ? (scaledDelta / len) : Vector3.up;

            Vector3 rounded = core + new Vector3(dir.x * rXZ, dir.y * rY, dir.z * rXZ);

            // Subtle jelly pillow dome on top surface
            float nx = Mathf.Clamp01(1f - Mathf.Abs(rounded.x) / halfW);
            float nz = Mathf.Clamp01(1f - Mathf.Abs(rounded.z) / halfD);
            if (rounded.y > 0f)
            {
                float dome = Mathf.Sin(nx * Mathf.PI * 0.5f) * Mathf.Sin(nz * Mathf.PI * 0.5f);
                rounded.y += dome * 0.004f;
            }

            normal = new Vector3(dir.x / rXZ, dir.y / rY, dir.z / rXZ).normalized;
            rounded.y += halfH;
            return rounded;
        }

        /// <summary>
        /// Generates a warm procedural wood grain texture matching the wooden board in the video.
        /// </summary>
        public static Texture2D GetOrCreateWoodTexture(bool darkerRim = false)
        {
            if (!darkerRim && _cachedWoodFloorTex != null) return _cachedWoodFloorTex;
            if (darkerRim && _cachedWoodRimTex != null) return _cachedWoodRimTex;

            int width = 512;
            int height = 512;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = darkerRim ? "WoodRimTex" : "WoodFloorTex",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            Color baseLight = darkerRim
                ? new Color(0.62f, 0.38f, 0.21f, 1f)
                : new Color(0.73f, 0.47f, 0.27f, 1f);
            Color baseDark = darkerRim
                ? new Color(0.42f, 0.23f, 0.11f, 1f)
                : new Color(0.54f, 0.31f, 0.16f, 1f);

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;

                    // Vertical wood planks & organic grain rings
                    float noise1 = Mathf.PerlinNoise(u * 14f + 1.3f, v * 3.5f + 2.1f);
                    float noise2 = Mathf.PerlinNoise(u * 42f + 7.7f, v * 8f + 4.3f) * 0.35f;
                    float wave = Mathf.Sin((u * 18f + noise1 * 3.2f) * Mathf.PI);
                    float grain = Mathf.Clamp01(0.5f + 0.35f * wave + (noise1 - 0.5f) * 0.4f + (noise2 - 0.17f));

                    // Subtle vertical plank grooves
                    float plank = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 4f));
                    float groove = Mathf.SmoothStep(0f, 0.06f, plank);

                    Color col = Color.Lerp(baseDark, baseLight, grain);
                    col = Color.Lerp(baseDark * 0.78f, col, groove);
                    pixels[y * width + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            if (darkerRim) _cachedWoodRimTex = tex;
            else _cachedWoodFloorTex = tex;
            return tex;
        }

        /// <summary>
        /// Generates a soft 3D glossy rim/specular overlay texture for the jelly blocks so they look like translucent 3D jelly even from top-down camera angles.
        /// </summary>
        public static Texture2D GetOrCreateJellyBodyTexture()
        {
            if (_cachedJellyGlossTex != null) return _cachedJellyGlossTex;

            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "JellyBodyGlossTex",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float v = (float)y / (size - 1);
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / (size - 1);

                    // Distance to rounded border
                    float dx = Mathf.Abs(u - 0.5f) * 2f;
                    float dy = Mathf.Abs(v - 0.5f) * 2f;
                    float edgeDist = Mathf.Max(dx, dy);

                    // Slight darkening near outer rim, bright inner subsurface glow
                    float shade = Mathf.Lerp(1.04f, 0.84f, Mathf.SmoothStep(0.65f, 1.0f, edgeDist));

                    // Glossy highlight arc near top-left & top edge
                    float topHighlight = Mathf.SmoothStep(0.76f, 0.93f, v) * (1f - Mathf.SmoothStep(0.75f, 0.96f, dx));
                    float cornerSpec = Mathf.Exp(-((u - 0.18f) * (u - 0.18f) * 45f + (v - 0.82f) * (v - 0.82f) * 45f));

                    float brightness = shade + topHighlight * 0.16f + cornerSpec * 0.24f;
                    pixels[y * size + x] = new Color(brightness, brightness, brightness, 1f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _cachedJellyGlossTex = tex;
            return tex;
        }

        /// <summary>
        /// Generates a smooth radial drop-shadow texture for soft contact shadows under jelly blocks.
        /// </summary>
        public static Texture2D GetOrCreateSoftShadowTexture()
        {
            if (_cachedShadowTex != null) return _cachedShadowTex;

            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "SoftContactShadowTex",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float v = (y - size * 0.5f) / (size * 0.5f);
                for (int x = 0; x < size; x++)
                {
                    float u = (x - size * 0.5f) / (size * 0.5f);
                    // Rounded box distance function
                    float dx = Mathf.Max(0f, Mathf.Abs(u) - 0.45f);
                    float dy = Mathf.Max(0f, Mathf.Abs(v) - 0.45f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Soft exponential falloff from center to edge
                    float alpha = Mathf.Clamp01(1f - dist / 0.55f);
                    alpha = alpha * alpha * (3f - 2f * alpha); // Smoothstep

                    // Warm dark brown-black shadow matching wooden tray
                    pixels[y * size + x] = new Color(0.12f, 0.07f, 0.03f, alpha * 0.42f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _cachedShadowTex = tex;
            return tex;
        }

        public static Shader FindBestLitShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s != null) return s;
            s = Shader.Find("Standard");
            if (s != null) return s;
            return Shader.Find("Sprites/Default");
        }

        public static Shader FindBestUnlitTransparentShader()
        {
            Shader s = Shader.Find("Sprites/Default");
            if (s != null) return s;
            s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s != null) return s;
            return Shader.Find("Unlit/Transparent");
        }
    }
}
