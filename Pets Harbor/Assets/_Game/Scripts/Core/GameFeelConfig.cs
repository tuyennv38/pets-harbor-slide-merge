using UnityEngine;

namespace PetsHarbor.SlideMerge
{
    [CreateAssetMenu(fileName = "GameFeelConfig", menuName = "PetsHarbor/Game Feel Config")]
    public class GameFeelConfig : ScriptableObject
    {
        private static GameFeelConfig _instance;
        public static GameFeelConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GameFeelConfig>("GameFeelConfig");
                    if (_instance == null)
                    {
                        _instance = CreateInstance<GameFeelConfig>();
                        _instance.name = "GameFeelConfig_Runtime";
                    }
                }
                return _instance;
            }
            set => _instance = value;
        }

        [Header("🎛️ 6 THÔNG SỐ CỐT LÕI ĐIỀU KHIỂN GAME FEEL")]

        [Tooltip("1. LỰC ĐẨY CHUỘT (Push Power): Khuếch đại tốc độ vẩy chuột thành lực đẩy khi thả tay. Đẩy nhẹ đi ngắn (1 ô), đẩy mạnh phóng vọt đi xa nhiều ô.")]
        [Range(0f, 3.0f)]
        public float pushForcePower = 0.50f;

        [Tooltip("2. MA SÁT MẶT KHAY (Slide Friction): Lực hãm khi khối đang trượt theo đà đẩy. Giảm thấp (3-6) = trơn như băng trượt rất dài; Tăng cao (14-22) = dừng nhanh.")]
        [Range(2.0f, 25.0f)]
        public float slideFriction = 7.5f;

        [Tooltip("3. ĐỘ NHẠY BÁM CHUỘT (Drag Responsiveness): Tốc độ khối thạch bám 1:1 theo chuột khi giữ kéo.")]
        [Range(15f, 90f)]
        public float dragResponsiveness = 75f;

        [Tooltip("4. LỰC HÚT RÃNH & Ô GRID (Grid Magnetism): Giữ khối trượt thẳng theo rãnh ngang/dọc và tự hút chặt về tâm ô Grid khi di chuyển nhẹ hoặc khi dừng trượt.")]
        [Range(0f, 1.0f)]
        public float gridMagnetStrength = 0.72f;

        [Tooltip("5. TRỢ LỰC LÁCH GÓC CHỮ L (Corner Assist): Tự động trượt bo qua góc vuông để chui vào khe hẹp mà không bị vấp cạnh.")]
        [Range(0f, 1.0f)]
        public float cornerSlideAssist = 0.85f;

        [Tooltip("6. ĐỘ NHẠY HÚT MERGE & ĐÀN HỒI THẠCH (Merge & Jelly Feel): Độ nhạy tự hút chập khi chạm khối cùng cấp và độ mềm dẻo của viên thạch.")]
        [Range(0.2f, 1.0f)]
        public float mergeSensitivity = 0.82f;

        [Header("🎯 HỖ TRỢ GIÓNG HÀNG GHÉP ĐÔI (Smart Partner Alignment)")]
        [Tooltip("7. TỰ ĐỘNG HÃM DỪNG KHI GIÓNG THẲNG KHỐI CÙNG CẤP: Khi trượt ngang/dọc, nếu lướt qua vị trí thẳng hàng với khối cùng cấp (đang có đường thoáng để hợp nhất), khối tự hãm dừng lại đúng ô gióng để người chơi đẩy thẳng vào ăn điểm mà không bị trượt lố.")]
        public bool enableSmartPartnerAlignmentStop = true;

        [Tooltip("Độ nhạy lực hút gióng hàng (Partner Alignment Magnet Strength): Giữ khối dính nhẹ vào ô thẳng hàng khi kéo chậm hoặc vẩy lướt.")]
        [Range(0.2f, 1.0f)]
        public float partnerAlignmentMagnetStrength = 0.85f;

        // --- Các thông số nội suy tự động từ 6 thanh trượt cốt lõi ---
        public float DragFollowSharpness => dragResponsiveness;
        public float MaxWallLeashCells => Mathf.Lerp(0.45f, 0.18f, cornerSlideAssist);
        public float MaxSlideStepPerFrame => 4.2f;

        public float LaneCenteringStrength => Mathf.Lerp(0f, 0.88f, gridMagnetStrength);
        public float IdleMagneticPullStrength => Mathf.Lerp(0f, 24f, gridMagnetStrength);
        public float IdleMagneticSpeedThreshold => Mathf.Lerp(0.8f, 3.0f, gridMagnetStrength);

        public float CornerChamferRange => Mathf.Lerp(0.10f, 0.48f, cornerSlideAssist);
        public float CornerChamferSpeed => Mathf.Lerp(0.8f, 2.5f, cornerSlideAssist);
        public float ObstacleClearancePad => Mathf.Lerp(0.05f, 0.115f, cornerSlideAssist);

        public float ReleaseSnapDuration => Mathf.Lerp(0.14f, 0.07f, gridMagnetStrength);

        public float MergeRequiredAlignment => Mathf.Lerp(0.60f, 0.26f, mergeSensitivity);
        public float MergePenetrationThreshold => Mathf.Lerp(0.32f, 0.08f, mergeSensitivity);
        public float MergeSuckInDuration => Mathf.Lerp(0.13f, 0.07f, mergeSensitivity);
        public float MergeCooldown => Mathf.Lerp(0.18f, 0.04f, mergeSensitivity);

        public float JellyVelocityStretch => Mathf.Lerp(0.05f, 0.14f, mergeSensitivity);
        public float JellyVelocityTiltAngle => Mathf.Lerp(2.5f, 6.8f, mergeSensitivity);
        public float JellyMergePopDuration => Mathf.Lerp(0.36f, 0.27f, mergeSensitivity);

        public void ApplyBalancedDefaultPreset()
        {
            pushForcePower = 0.50f;
            slideFriction = 7.5f;
            dragResponsiveness = 75f;
            gridMagnetStrength = 0.72f;
            cornerSlideAssist = 0.85f;
            mergeSensitivity = 0.82f;
        }

        public void ApplyIceGlidePreset()
        {
            pushForcePower = 0.85f;
            slideFriction = 4.2f;
            dragResponsiveness = 85f;
            gridMagnetStrength = 0.65f;
            cornerSlideAssist = 0.95f;
            mergeSensitivity = 0.92f;
        }

        public void ApplyStrongMagneticGridPreset()
        {
            pushForcePower = 0.40f;
            slideFriction = 11.5f;
            dragResponsiveness = 75f;
            gridMagnetStrength = 0.92f;
            cornerSlideAssist = 0.88f;
            mergeSensitivity = 0.80f;
        }
    }
}
