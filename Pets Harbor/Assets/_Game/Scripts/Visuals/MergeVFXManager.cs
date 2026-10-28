using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace PetsHarbor.SlideMerge
{
    public class MergeVFXManager : MonoBehaviour
    {
        public static MergeVFXManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void PlayMergeSplash(Vector3 worldPos, Color jellyColor, float scaleMultiplier = 1f)
        {
            StartCoroutine(SpawnSplashRoutine(worldPos, jellyColor, scaleMultiplier));
        }

        public void SpawnVictoryCheckmark(Transform parentBlock, Vector3 localOffset)
        {
            GameObject checkGo = new GameObject("VictoryCheckMark");
            checkGo.transform.SetParent(parentBlock, false);
            checkGo.transform.localPosition = localOffset;
            checkGo.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);

            SpriteRenderer sr = checkGo.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralEmojiDrawer.GetOrCreateCheckMarkSprite();
            sr.sortingOrder = 50;

            checkGo.transform.localScale = Vector3.zero;
            checkGo.transform.DOScale(Vector3.one * 1.15f, 0.45f).SetEase(Ease.OutBack);
        }

        private IEnumerator SpawnSplashRoutine(Vector3 center, Color jellyColor, float scaleMult)
        {
            int count = 12;
            Transform[] droplets = new Transform[count];
            Vector3[] velocities = new Vector3[count];
            Material mat = new Material(ProceduralMeshGenerator.FindBestLitShader());
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.Lerp(jellyColor, Color.white, 0.3f));
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.Lerp(jellyColor, Color.white, 0.3f));

            for (int i = 0; i < count; i++)
            {
                GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(drop.GetComponent<Collider>());
                drop.name = "JellyDropVFX";
                drop.transform.position = center + new Vector3(0f, 0.45f, 0f);
                float baseScale = Random.Range(0.14f, 0.26f) * scaleMult;
                drop.transform.localScale = Vector3.one * baseScale;

                MeshRenderer mr = drop.GetComponent<MeshRenderer>();
                mr.sharedMaterial = mat;

                float angle = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                float speed = Random.Range(2.2f, 4.5f) * scaleMult;
                velocities[i] = new Vector3(Mathf.Cos(angle) * speed, Random.Range(2.5f, 4.8f), Mathf.Sin(angle) * speed);
                droplets[i] = drop.transform;
            }

            float elapsed = 0f;
            const float life = 0.38f;
            while (elapsed < life)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                float norm = elapsed / life;

                for (int i = 0; i < count; i++)
                {
                    if (droplets[i] == null) continue;
                    velocities[i] += Vector3.down * (14f * dt);
                    droplets[i].position += velocities[i] * dt;
                    droplets[i].localScale *= (1f - dt * 2.4f);
                }

                yield return null;
            }

            for (int i = 0; i < count; i++)
            {
                if (droplets[i] != null) Destroy(droplets[i].gameObject);
            }

            Destroy(mat);
        }
    }
}
