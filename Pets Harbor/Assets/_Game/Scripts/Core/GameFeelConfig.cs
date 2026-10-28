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

        [Header("1. Độ Trơn Trượt & Bám Chuột (Drag & Tracking)")]
        [Tooltip("Tốc độ khối thạch bám theo con trỏ chuột (Cao = nhạy tức thì, Thấp = trễ mềm như lò xo)")]
        [Range(10f, 100f)]
        public float dragFollowSharpness = 55f;

        [Tooltip("Độ dài dây dẫn tối đa khi vướng tường (Nhỏ = đổi hướng thoát tường cực nhạy không bị rít)")]
        [Range(0.05f, 1.20f)]
        public float maxWallLeashCells = 0.30f;

        [Tooltip("Quãng đường trượt tối đa mỗi frame (ô) khi vẩy chuột nhanh")]
        [Range(1.0f, 6.0f)]
        public float maxSlideStepPerFrame = 3.5f;

        [Header("2. Lực Hút Grid & Lách Khe Chữ L (Magnetic Grid & Corner Assist)")]
        [Tooltip("Lực hút tự căn giữa rãnh ngang/dọc khi đang trượt (0 = trượt tự do, 1 = khóa chặt vào rãnh Grid)")]
        [Range(0f, 1f)]
        public float laneCenteringStrength = 0.60f;

        [Tooltip("Lực hút nam châm kéo khối về đúng tâm ô Grid ngay cả khi đang giữ chuột và di chuyển nhẹ/chậm")]
        [Range(0f, 35f)]
        public float idleMagneticPullStrength = 15f;

        [Tooltip("Ngưỡng tốc độ chuột (ô/giây) bắt đầu kích hoạt lực hút nam châm về tâm ô Grid")]
        [Range(0.2f, 6.0f)]
        public float idleMagneticSpeedThreshold = 2.4f;

        [Tooltip("Phạm vi hỗ trợ tự trượt bo góc chữ L khi vướng mép khối bên cạnh (0.48 = gần nửa ô vẫn tự lách qua)")]
        [Range(0f, 0.49f)]
        public float cornerChamferRange = 0.46f;

        [Tooltip("Tốc độ đẩy khối lách qua góc vuông chữ L")]
        [Range(0.5f, 4.0f)]
        public float cornerChamferSpeed = 1.90f;

        [Tooltip("Độ hở va chạm giữa các khối khi trượt (Tăng lên giúp lướt qua khe hẹp cực trơn không bị cọ cạnh)")]
        [Range(0.02f, 0.18f)]
        public float obstacleClearancePad = 0.095f;

        [Header("3. Quán Tính Vẩy Tay & Hút Ô Khi Thả Chuột (Flick & Snap)")]
        [Tooltip("Hệ số quán tính trượt tiếp khi vẩy chuột rồi thả tay (0 = dừng tại chỗ, Tăng = vẩy nhẹ trượt băng sang ô kế)")]
        [Range(0f, 0.20f)]
        public float releaseFlickMomentum = 0.08f;

        [Tooltip("Quãng đường trượt quán tính tối đa khi vẩy thả tay (ô)")]
        [Range(0f, 1.5f)]
        public float maxFlickDistanceCells = 0.70f;

        [Tooltip("Thời gian hút êm về tâm ô Grid khi thả chuột (giây)")]
        [Range(0.03f, 0.25f)]
        public float releaseSnapDuration = 0.10f;

        [Header("4. Độ Nhạy Hút Merge Liên Hoàn (Merge & Combo Feel)")]
        [Tooltip("Độ thẳng hàng tối thiểu giữa 2 khối cùng cấp để cho phép hút Merge (Thấp = dễ bắt cặp hơn)")]
        [Range(0.15f, 0.85f)]
        public float mergeRequiredAlignment = 0.34f;

        [Tooltip("Độ chạm/lún vào khối cùng cấp để kích hoạt hút Merge (Thấp = vừa chạm nhẹ là hút vào Merge ngay)")]
        [Range(0.02f, 0.55f)]
        public float mergePenetrationThreshold = 0.14f;

        [Tooltip("Thời gian 2 khối hút chập vào nhau trước khi biến hình (giây)")]
        [Range(0.03f, 0.22f)]
        public float mergeSuckInDuration = 0.085f;

        [Tooltip("Thời gian nghỉ giữa 2 lần Merge liên hoàn khi giữ chuột lướt tiếp (giây)")]
        [Range(0f, 0.35f)]
        public float mergeCooldown = 0.08f;

        [Header("5. Hiệu Ứng Đàn Hồi Khối Thạch (Jelly Squash & Tilt)")]
        [Tooltip("Độ kéo giãn thân viên thạch theo vận tốc trượt")]
        [Range(0f, 0.25f)]
        public float jellyVelocityStretch = 0.11f;

        [Tooltip("Góc nghiêng thân thạch tối đa khi trượt nhanh (độ)")]
        [Range(0f, 14f)]
        public float jellyVelocityTiltAngle = 5.5f;

        [Tooltip("Thời gian hiệu ứng nảy phồng khi tiến hóa lên cấp mới (giây)")]
        [Range(0.12f, 0.55f)]
        public float jellyMergePopDuration = 0.30f;

        public void ApplyBalancedDefaultPreset()
        {
            dragFollowSharpness = 55f;
            maxWallLeashCells = 0.30f;
            maxSlideStepPerFrame = 3.5f;

            laneCenteringStrength = 0.60f;
            idleMagneticPullStrength = 15f;
            idleMagneticSpeedThreshold = 2.4f;
            cornerChamferRange = 0.46f;
            cornerChamferSpeed = 1.90f;
            obstacleClearancePad = 0.095f;

            releaseFlickMomentum = 0.08f;
            maxFlickDistanceCells = 0.70f;
            releaseSnapDuration = 0.10f;

            mergeRequiredAlignment = 0.34f;
            mergePenetrationThreshold = 0.14f;
            mergeSuckInDuration = 0.085f;
            mergeCooldown = 0.08f;

            jellyVelocityStretch = 0.11f;
            jellyVelocityTiltAngle = 5.5f;
            jellyMergePopDuration = 0.30f;
        }

        public void ApplyIceGlidePreset()
        {
            dragFollowSharpness = 75f;
            maxWallLeashCells = 0.18f;
            maxSlideStepPerFrame = 4.5f;

            laneCenteringStrength = 0.72f;
            idleMagneticPullStrength = 10f;
            idleMagneticSpeedThreshold = 1.8f;
            cornerChamferRange = 0.48f;
            cornerChamferSpeed = 2.60f;
            obstacleClearancePad = 0.12f;

            releaseFlickMomentum = 0.12f;
            maxFlickDistanceCells = 0.95f;
            releaseSnapDuration = 0.08f;

            mergeRequiredAlignment = 0.26f;
            mergePenetrationThreshold = 0.08f;
            mergeSuckInDuration = 0.07f;
            mergeCooldown = 0.04f;

            jellyVelocityStretch = 0.14f;
            jellyVelocityTiltAngle = 7.0f;
            jellyMergePopDuration = 0.26f;
        }

        public void ApplyStrongMagneticGridPreset()
        {
            dragFollowSharpness = 60f;
            maxWallLeashCells = 0.25f;
            maxSlideStepPerFrame = 3.2f;

            laneCenteringStrength = 0.88f;
            idleMagneticPullStrength = 26f;
            idleMagneticSpeedThreshold = 3.5f;
            cornerChamferRange = 0.47f;
            cornerChamferSpeed = 2.20f;
            obstacleClearancePad = 0.105f;

            releaseFlickMomentum = 0.06f;
            maxFlickDistanceCells = 0.55f;
            releaseSnapDuration = 0.07f;

            mergeRequiredAlignment = 0.35f;
            mergePenetrationThreshold = 0.12f;
            mergeSuckInDuration = 0.08f;
            mergeCooldown = 0.07f;

            jellyVelocityStretch = 0.10f;
            jellyVelocityTiltAngle = 4.5f;
            jellyMergePopDuration = 0.28f;
        }
    }
}
