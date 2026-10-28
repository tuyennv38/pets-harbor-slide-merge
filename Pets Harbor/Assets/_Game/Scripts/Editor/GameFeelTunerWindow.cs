#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PetsHarbor.SlideMerge.Editor
{
    public class GameFeelTunerWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private SerializedObject _serializedConfig;

        private const string AssetPath = "Assets/_Game/Resources/GameFeelConfig.asset";

        [MenuItem("PetsHarbor/Game Feel Tuner (Sliders) %#g", false, 1)]
        public static void OpenWindow()
        {
            GameFeelTunerWindow win = GetWindow<GameFeelTunerWindow>("Game Feel Tuner");
            win.minSize = new Vector2(440f, 560f);
            win.Show();
        }

        public static GameFeelConfig GetOrCreateConfigAsset()
        {
            GameFeelConfig cfg = AssetDatabase.LoadAssetAtPath<GameFeelConfig>(AssetPath);
            if (cfg == null)
            {
                string dir = Path.GetDirectoryName(AssetPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                cfg = CreateInstance<GameFeelConfig>();
                cfg.ApplyBalancedDefaultPreset();
                AssetDatabase.CreateAsset(cfg, AssetPath);
                AssetDatabase.SaveAssets();
            }

            GameFeelConfig.Instance = cfg;
            return cfg;
        }

        private void OnEnable()
        {
            GameFeelConfig cfg = GetOrCreateConfigAsset();
            _serializedConfig = new SerializedObject(cfg);
        }

        private void OnGUI()
        {
            GameFeelConfig cfg = GetOrCreateConfigAsset();
            if (_serializedConfig == null || _serializedConfig.targetObject != cfg)
            {
                _serializedConfig = new SerializedObject(cfg);
            }

            DrawTunerGUI(cfg, _serializedConfig, ref _scrollPos, true);
        }

        public static void DrawTunerGUI(
            GameFeelConfig cfg,
            SerializedObject serializedConfig,
            ref Vector2 scrollPos,
            bool useScrollView)
        {
            serializedConfig.Update();

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(
                "Kéo các thanh trượt bên dưới ngay trong lúc đang PLAY để cảm nhận trực tiếp độ trơn trượt, lực hút Grid và độ nhạy Merge.\n" +
                "Mọi tinh chỉnh đều tự động lưu vào GameFeelConfig.asset (KHÔNG bị mất khi tắt Play Mode)!",
                MessageType.Info);

            // Preset Buttons
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("⚡ Bộ Thiết Lập Nhanh (1-Click Presets)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🧊 Siêu Trơn Trượt\n(Ice Glide)", GUILayout.Height(36f)))
            {
                Undo.RecordObject(cfg, "Apply Ice Glide Preset");
                cfg.ApplyIceGlidePreset();
                EditorUtility.SetDirty(cfg);
            }
            if (GUILayout.Button("🧲 Hút Grid Mạnh\n(Magnetic Grid)", GUILayout.Height(36f)))
            {
                Undo.RecordObject(cfg, "Apply Magnetic Grid Preset");
                cfg.ApplyStrongMagneticGridPreset();
                EditorUtility.SetDirty(cfg);
            }
            if (GUILayout.Button("⚖️ Cân Bằng Chuẩn\n(Default)", GUILayout.Height(36f)))
            {
                Undo.RecordObject(cfg, "Apply Balanced Preset");
                cfg.ApplyBalancedDefaultPreset();
                EditorUtility.SetDirty(cfg);
            }
            EditorGUILayout.EndHorizontal();

            if (Application.isPlaying && GameManager.Instance != null)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("↻ Reset Màn Chơi Ngay", GUILayout.Height(28f)))
                {
                    GameManager.Instance.RestartLevel();
                }
                if (GUILayout.Button("⚡ Auto Merge 1 Bước", GUILayout.Height(28f)))
                {
                    GameManager.Instance.TriggerAutoMergeStep();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4f);

            if (useScrollView)
            {
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            }

            // Section 1
            DrawSection("1. Độ Trơn Trượt & Bám Chuột (Drag & Tracking)", new[]
            {
                ("dragFollowSharpness", "Tốc độ bám chuột (Follow Sharpness)"),
                ("maxWallLeashCells", "Dây dẫn tối đa khi chạm tường (Wall Leash)"),
                ("maxSlideStepPerFrame", "Quãng đường trượt tối đa/frame")
            }, serializedConfig);

            // Section 2
            DrawSection("2. Lực Hút Rãnh Grid & Bo Góc Chữ L (Magnetic Grid & Corner)", new[]
            {
                ("laneCenteringStrength", "Lực hút căn giữa rãnh ngang/dọc"),
                ("idleMagneticPullStrength", "Lực hút tâm ô khi di chuyển nhẹ/chậm"),
                ("idleMagneticSpeedThreshold", "Ngưỡng tốc độ bật lực hút tâm ô"),
                ("cornerChamferRange", "Phạm vi tự trượt bo góc chữ L (ô)"),
                ("cornerChamferSpeed", "Tốc độ đẩy bo góc lách khe"),
                ("obstacleClearancePad", "Độ hở va chạm giữa các khối (Clearance)")
            }, serializedConfig);

            // Section 3
            DrawSection("3. Quán Tính Vẩy Tay & Hút Ô Khi Thả Chuột (Flick & Snap)", new[]
            {
                ("releaseFlickMomentum", "Quán tính trượt tiếp khi vẩy thả tay"),
                ("maxFlickDistanceCells", "Quãng đường vẩy quán tính tối đa (ô)"),
                ("releaseSnapDuration", "Thời gian hút êm về ô khi thả chuột (s)")
            }, serializedConfig);

            // Section 4
            DrawSection("4. Độ Nhạy Hút Merge Liên Hoàn (Merge & Combo)", new[]
            {
                ("mergeRequiredAlignment", "Độ thẳng hàng tối thiểu để Merge"),
                ("mergePenetrationThreshold", "Độ chạm/lún kích hoạt Merge (ô)"),
                ("mergeSuckInDuration", "Thời gian hút chập 2 khối (s)"),
                ("mergeCooldown", "Thời gian nghỉ giữa 2 lần Merge liên tiếp (s)")
            }, serializedConfig);

            // Section 5
            DrawSection("5. Hiệu Ứng Đàn Hồi Khối Thạch (Jelly Squash & Tilt)", new[]
            {
                ("jellyVelocityStretch", "Độ kéo giãn thạch khi trượt nhanh"),
                ("jellyVelocityTiltAngle", "Góc nghiêng thạch khi trượt (độ)"),
                ("jellyMergePopDuration", "Thời gian nảy phồng khi lên cấp (s)")
            }, serializedConfig);

            if (useScrollView)
            {
                EditorGUILayout.EndScrollView();
            }

            if (serializedConfig.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(cfg);
            }
        }

        private static void DrawSection(
            string title,
            (string propName, string label)[] fields,
            SerializedObject serializedConfig)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);

            foreach (var (propName, label) in fields)
            {
                SerializedProperty prop = serializedConfig.FindProperty(propName);
                if (prop != null)
                {
                    EditorGUILayout.PropertyField(prop, new GUIContent(label, prop.tooltip));
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }
    }

    [CustomEditor(typeof(GameManager))]
    public class GameManagerEditor : UnityEditor.Editor
    {
        private SerializedObject _serializedConfig;
        private Vector2 _scrollPos;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("🎛️ Mở Cửa Sổ Riêng: Game Feel Tuner", GUILayout.Height(30f)))
            {
                GameFeelTunerWindow.OpenWindow();
            }

            GameFeelConfig cfg = GameFeelTunerWindow.GetOrCreateConfigAsset();
            if (_serializedConfig == null || _serializedConfig.targetObject != cfg)
            {
                _serializedConfig = new SerializedObject(cfg);
            }

            GameFeelTunerWindow.DrawTunerGUI(cfg, _serializedConfig, ref _scrollPos, false);
        }
    }
}
#endif
