# Tài liệu Kỹ thuật: Tối ưu Game Feel, Độ Mượt Di Chuyển & Hiệu Ứng Khối Thạch 3D (Jelly Block)
**Tham chiếu Video gốc:** [YouTube Shorts `eoOOBFUVPog` - Pets Harbor](https://www.youtube.com/shorts/eoOOBFUVPog)  
**Engine mục tiêu:** Unity 6 (`6000.0.x` - URP / Built-in)  
**Liên kết Codebase hiện tại:**
- [`JellyBlock.cs`](file:///d:/Company/Cscmobi/Projects/G23/REA/Demo%20Game%20From%20Creative/Pets%20Harbor/Assets/_Game/Scripts/Gameplay/JellyBlock.cs)
- [`BlockDragController.cs`](file:///d:/Company/Cscmobi/Projects/G23/REA/Demo%20Game%20From%20Creative/Pets%20Harbor/Assets/_Game/Scripts/Gameplay/BlockDragController.cs)
- [`BoardGridManager.cs`](file:///d:/Company/Cscmobi/Projects/G23/REA/Demo%20Game%20From%20Creative/Pets%20Harbor/Assets/_Game/Scripts/Core/BoardGridManager.cs)

---

## PHẦN 1: PHÂN TÍCH GAME FEEL & ĐỘ MƯỢT TRONG VIDEO GỐC (`eoOOBFUVPog`)

Sự "đã tay" (tactile satisfaction) và mướt mắt trong video quảng cáo gốc đến từ sự phối hợp đồng bộ giữa **Điều khiển (Input)**, **Va chạm vật lý giả lập (Sub-grid Collision)**, **Biến dạng đàn hồi (Soft-body / Squash & Stretch)** và **Chất liệu ánh sáng (Translucent Shader)**.

### 1.1. Cảm giác Kéo Trượt (Movement & Drag Feel)
1. **Di chuyển liên tục dưới cấp độ ô lưới (Continuous Sub-Grid Sliding):**
   - Bàn cờ chia lưới $7 \times 8$, nhưng khối thạch **không bao giờ nhảy từng ô** trong lúc kéo. Vị trí của khối là số thực `(float GridX, float GridY)` bám theo ngón tay với độ trễ lò xo cực nhỏ ($\sim 16\text{ms} - 25\text{ms}$), tạo cảm giác khối có khối lượng vật lý thật nhưng vẫn cực kỳ nhạy.
2. **Trượt bo góc & Lách khe hẹp (Corner Chamfer / Edge-Slide Assist):**
   - *Quan sát tại `00:05 – 00:13`:* Khi bàn tay kéo các khối `1x1` và `2x1` đi theo đường chữ L (kéo ngang rồi bẻ lên dọc), dù quỹ đạo ngón tay đi vòng cung và quệt vào góc của khối bên cạnh, khối thạch vẫn **tự động trượt bo qua góc** để chui tọt vào khe trống mà không bị khựng lại.
3. **Hút nam châm êm ái vào rãnh lưới (Soft Magnetic Lane & Cell Snap):**
   - Khi thả tay hoặc khi dừng kéo, khối không giật cục về tọa độ nguyên `(int col, int row)` mà trượt êm về tâm ô bằng đường cong giảm tốc (`Cubic Ease-Out` trong `0.08s – 0.10s`).

### 1.2. Cảm giác Va Chạm & Hợp Nhất 3 Giai Đoạn (3-Stage Merge Choreography)
Quan sát các pha ghép nối tiếp ở `00:01 – 00:04`, `00:15 – 00:16`, `00:23 – 00:24`:

```mermaid
flowchart LR
    A["Giai đoạn 1: Contact Squish\n(Ép lún tại mặt tiếp xúc)"] --> B["Giai đoạn 2: Magnetic Suck-In\n(Hút chập tâm ~0.09s - 0.11s)"]
    B --> C["Giai đoạn 3: Elastic Overshoot Pop\n(Phóng to 0.6x -> 1.22x -> 0.92x -> 1.0x)"]
```

1. **Giai đoạn 1 – Ép lún tiếp xúc (Pre-Merge Contact Squish):**
   - Khi kéo khối $A$ chạm vào khối $B$ cùng cấp (ví dụ 2 khối Đỏ `2x2` tại `00:23`), chúng không biến mất ngay lập tức ở khoảnh khắc vừa chạm mép ngoài. Khối $A$ lún nhẹ vào khối $B$, hai bề mặt tiếp xúc bị nén dẹt lại theo trục va chạm và phình nhẹ ra trục vuông góc.
2. **Giai đoạn 2 – Hút chập tâm (Magnetic Fusion / Suck-In):**
   - Khi độ lún vượt ngưỡng ($\sim 35\% - 45\%$ ô), lực hút nam châm kích hoạt: cả hai khối cùng co rút nhanh về tâm của khối mới trong khoảng `0.09s – 0.11s`.
3. **Giai đoạn 3 – Nảy phồng Tiến hóa (Volume-Preserving Jelly Pop):**
   - Khối cấp mới ($k+1$) bung ra từ lõi nén ($0.58\times$), vọt qua kích thước chuẩn lên $1.22\times$ (Overshoot), thu về $0.90\times$ (Undershoot), rồi nảy nhẹ về $1.00\times$.

### 1.3. Biến Dạng Quán Tính & Chất Liệu Thạch (Visual & Material Polish)
1. **Biến dạng nghiêng và kéo giãn theo vận tốc (Velocity Shear & Stretch):**
   - Ở các pha vẩy tay nhanh (`00:12`, `00:14`, `00:26`), mặt trên của khối thạch hơi **nghiêng trễ pha** so với mặt đáy và giãn dài khoảng $5\% - 8\%$ theo hướng kéo.
2. **Khuôn mặt biểu cảm nằm "bên dưới" lớp thạch bóng (Sub-Coat Facial Expression):**
   - Mắt, miệng, má hồng không giống một tờ giấy dán đè lên trên mà nằm **bên dưới lớp phản xạ bóng kính (Specular / Clear Coat)**. Vệt sáng trắng ở gờ khối chạy xuyên qua cả biểu cảm khuôn mặt, và khi khối thạch phình/lún thì mắt miệng cũng co giãn theo.
3. **Độ trong mờ dưới da & Bóng đổ tiếp xúc (Subsurface Scattering & Contact Shadow):**
   - Ánh sáng xuyên qua rìa khối tạo viền sáng màu ấm (Fresnel / SSS), trong khi đáy khối tiếp giáp mặt khay gỗ có viền bóng tối mềm (Ambient Occlusion / Drop Shadow) giúp khối không bị cảm giác "bay lơ lửng".

---

## PHẦN 2: CÁC THƯ VIỆN UNITY KHUYÊN DÙNG

Để tiết kiệm thời gian và đạt chuẩn đồ họa / chuyển động của các studio lớn, bạn có thể sử dụng kết hợp các thư viện sau:

| Nhóm Nhu Cầu | Thư Viện Khuyên Dùng | Vai Trò Cụ Thể Trong Game Này |
| :--- | :--- | :--- |
| **1. Tweening & Chuỗi Chuyển Động** | **[PrimeTween](https://assetstore.unity.com/packages/tools/animation/primetween-high-performance-animations-and-sequences-252960)** *(Khuyên dùng #1)* hoặc **[DOTween Pro](https://assetstore.unity.com/packages/tools/visual-scripting/dotween-pro-32416)** | - Chạy chuỗi `Sequence` khi Merge (*Suck-In* $\rightarrow$ *Pop*).<br>- Chạy `Tween.PunchScale` / `DOShakeScale` không tạo rác bộ nhớ (Zero GC Allocation).<br>- Hỗ trợ `AnimationCurve` tùy chỉnh độ nảy. |
| **2. Hiệu Ứng Đàn Hồi Đỉnh Mesh (Soft-body Jiggle)** | **[Boing Kit](https://assetstore.unity.com/packages/tools/particles-effects/boing-kit-135594)** | - Tạo hiệu ứng lắc lư vật lý (Spring-based Vertex/Transform Boing Behavior) cho phần thân trên của viên thạch khi tăng tốc hoặc dừng đột ngột mà không cần dùng `MeshCollider` hay `Softbody Physics` nặng nề. |
| **3. Game Feel & Juice Tổng Hợp** | **[Feel (More Mountains)](https://assetstore.unity.com/packages/tools/particles-effects/feel-183370)** | - Cung cấp sẵn component `MMF_SquashAndStretch`, `MMF_PositionSpring`, `MMF_Haptics` (rung phản hồi trên điện thoại) và quản lý Sound Pitch tăng dần khi Combo Merge. |
| **4. Shader & Stylize Lighting** | **Unity URP Shader Graph** *(Có sẵn)* hoặc **[Flat Kit](https://assetstore.unity.com/packages/vfx/shaders/flat-kit-toon-shading-and-water-143368)** | - Tạo Shader Jelly có *Clear Coat* (lớp phủ bóng trong suốt), *Fresnel Rim*, *Fake Subsurface Scattering* và *Vertex Shear Deformation*. |

---

## PHẦN 3: 6 KỸ THUẬT LẬP TRÌNH CỐT LÕI (KÈM CÔNG THỨC & CODE MẪU)

Ngay cả khi tự code hoàn toàn trong Unity mà không mua Asset trả phí, bạn có thể đạt **100% độ mượt của video** bằng 6 kỹ thuật dưới đây:

---

### Kỹ Thuật 1: Hệ Lò Xo Giảm Chấn Bậc 2 (Second-Order Spring Damper)

Thay vì dùng `Vector3.Lerp` đơn thuần (vốn chỉ có vận tốc giảm dần, thiếu quán tính văng nhẹ khi dừng), hãy dùng bộ giải **Second-Order Dynamics** (phổ biến từ bài toán *Procedural Animation* của *t3ssel8r*) cho cả **Vị trí khối** và **Vận tốc nghiêng**:

$$\ddot{y} + \frac{2\zeta}{\omega} \dot{y} + y = x + \frac{r \zeta}{\omega} \dot{x}$$

Trong đó:
- $f$ (`frequency` $\approx 8.5\text{ Hz}$): Tốc độ phản hồi khi kéo.
- $\zeta$ (`damping` $\approx 0.68$): Hệ số cản ($\zeta < 1$ giúp khối hơi nhún nhẹ qua vị trí dừng rồi mới đứng yên).
- $r$ (`response` $\approx 1.2$): Độ nhạy tức thời ngay khi bắt đầu kéo.

```csharp
[System.Serializable]
public struct SpringVector3
{
    private Vector3 _xp; // Input frame trước
    private Vector3 _y;  // Giá trị đầu ra hiện tại
    private Vector3 _yd; // Vận tốc đầu ra hiện tại
    private float _k1, _k2, _k3;

    public void Configure(float frequency, float damping, float response, Vector3 initialValue)
    {
        _k1 = damping / (Mathf.PI * frequency);
        _k2 = 1f / ((2f * Mathf.PI * frequency) * (2f * Mathf.PI * frequency));
        _k3 = response * damping / (2f * Mathf.PI * frequency);
        _xp = _y = initialValue;
        _yd = Vector3.zero;
    }

    public Vector3 Step(float dt, Vector3 x)
    {
        if (dt <= 0.0001f) return _y;
        Vector3 xd = (x - _xp) / dt;
        _xp = x;
        float k2Stable = Mathf.Max(_k2, 1.1f * (dt * dt * 0.25f + dt * _k1 * 0.5f));
        _y += dt * _yd;
        _yd += dt * (x + _k3 * xd - _y - _k1 * _yd) / k2Stable;
        return _y;
    }
}
```

---

### Kỹ Thuật 2: Bảo Toàn Thể Tích khi Squash & Stretch (Volume-Preserving Deformation)

Trong tự nhiên, thạch rau câu là vật chất không nén được (incompressible) — thể tích $V = S_x \times S_y \times S_z$ luôn bằng hằng số $1.0$:
$$S_y = \frac{1.0}{S_x \cdot S_z}$$

- Khi kéo nhanh theo trục $X$ với độ giãn $S_x = 1 + \epsilon$, hai trục còn lại phải tự động thu nhỏ:
  $$S_z = \frac{1}{\sqrt{1 + \epsilon}}, \quad S_y = \frac{1}{\sqrt{1 + \epsilon}}$$
- Khi 2 khối ép vào nhau theo trục $X$ ($S_x = 1 - c$), trục $Z$ và $Y$ tự động phồng ra $\frac{1}{\sqrt{1 - c}}$. Quy tắc này giúp viên thạch nhìn "mọng nước" và chân thực hơn hẳn phép cộng/trừ tuyến tính.

---

### Kỹ Thuật 3: Biến Dạng Nghiêng Thân Theo Quán Tính (Velocity Tilt & Top-Surface Shear)

Khi người chơi vẩy khối thạch sang ngang/dọc, mặt đáy của viên thạch ma sát với khay gỗ còn mặt trên bị văng theo quán tính. Có 2 cách cài đặt trong [`JellyBlock.cs`](file:///d:/Company/Cscmobi/Projects/G23/REA/Demo%20Game%20From%20Creative/Pets%20Harbor/Assets/_Game/Scripts/Gameplay/JellyBlock.cs):

1. **Cách 1 – Nghiêng & Trượt nhẹ `FaceDecal` + `VisualRoot` bằng C#:**
   - Khi có vận tốc lò xo $\vec{v} = (v_x, v_z)$:
     - Nghiêng nhẹ `_visualRoot.localRotation` một góc `Quaternion.Euler(vz * 1.8f, 0f, -vx * 1.8f)`.
     - Dịch chuyển nhẹ tâm biểu cảm `FaceDecal` lệch về hướng di chuyển một khoảng $\Delta = \text{Clamp}(\vec{v} \times 0.012, -0.06, 0.06)$ để tạo hiệu ứng thị sai 3D (Parallax) bên trong khối thạch.

2. **Cách 2 – Biến dạng trượt (Shear) trực tiếp trong Vertex Shader (HLSL):**
   - Truyền vector `_JellyShearXZ` (được tính từ lò xo vận tốc) vào `MaterialPropertyBlock`.
   - Trong Vertex Shader, dịch chuyển đỉnh dựa theo chiều cao chuẩn hóa $h = \text{saturate}(v.\text{positionOS}.y / \text{BlockThickness})$:
     ```hlsl
     float heightFactor = h * h; // Đáy (h=0) cố định, Đỉnh (h=1) nghiêng mạnh nhất
     v.positionOS.xz += _JellyShearXZ.xy * heightFactor;
     ```

---

### Kỹ Thuật 4: Thuật Toán Trượt Bo Góc & Trợ Lực Lách Khe (Corner Chamfer & Lane Assist)

Đây là bí quyết quan trọng nhất trong [`BoardGridManager.cs`](file:///d:/Company/Cscmobi/Projects/G23/REA/Demo%20Game%20From%20Creative/Pets%20Harbor/Assets/_Game/Scripts/Core/BoardGridManager.cs) để khi người chơi kéo chữ L qua khe hẹp mà **không bao giờ bị vấp góc**:

```mermaid
flowchart TD
    MoveX["Kéo khối theo trục X bị chặn bởi vật cản"] --> CheckMisalign["Kiểm tra độ lệch trục vuông góc Y:\nmisalignY = |current.y - Round(current.y)|"]
    CheckMisalign -->|Nếu misalignY <= 0.32 ô| CheckLaneFree["Kiểm tra nếu làm tròn Y = Round(current.y)\nthì đường trượt trục X có thông thoáng không?"]
    CheckLaneFree -->|Thông thoáng| AutoSlideY["Tự động trượt nhẹ current.y về Round(current.y)\n(Corner Chamfer Assist) rồi tiếp tục tiến theo trục X!"]
```

- **Nguyên lý hoạt động:**
  - Giả sử người chơi đang kéo khối sang Phải ($\Delta x > 0$) để chui vào một hẻm ngang ở hàng `row = 2`, nhưng ngón tay người chơi đang giữ lệch ở `current.y = 2.22` nên cạnh dưới của khối bị vướng nhẹ ($0.22$ ô) vào khối ở hàng `row = 3`.
  - Khi bước di chuyển theo trục $X$ bị chặn, kiểm tra ô nguyên gần nhất `snappedY = Mathf.Round(current.y)`. Nếu `Mathf.Abs(current.y - snappedY) <= 0.32f` và tại `(nextX, snappedY)` **không có vật cản**, hệ thống tự động kéo mượt `current.y` về `snappedY` với tốc độ phụ trợ rồi cho phép khối trượt tiếp sang `nextX`.
  - Kết quả: Khối thạch tự "lách" qua góc vuông mượt như bơ giống hệt giây `00:08 – 00:10` trong video gốc!

---

### Kỹ Thuật 5: Hòa Trộn Biểu Cảm Emoji Nằm Dưới Lớp Bóng Kính (Under-Coat Facial Blending)

Trong [`JellyBlock.cs`](file:///d:/Company/Cscmobi/Projects/G23/REA/Demo%20Game%20From%20Creative/Pets%20Harbor/Assets/_Game/Scripts/Gameplay/JellyBlock.cs#L86-L95) hiện tại, khuôn mặt Emoji đang là một `Quad` (`FaceDecal`) dùng shader `Unlit/Transparent` đặt cao hơn mặt thạch `0.018f`. Cách này dễ làm nhưng có 2 nhược điểm:
1. Mặt `Quad` phẳng không có độ phồng vòm (Pillow Top) như đỉnh viên thạch.
2. Ánh sáng bóng kính (Specular highlight) của viên thạch bị `Quad` che khuất.

**Giải pháp nâng cấp chuẩn Studio:**
- **Cách A (Gộp vào cùng Texture/Shader của khối thạch):**
  - Khi khởi tạo chất liệu cho mỗi Tier, gộp (composite) trực tiếp Texture `EmojiFace` lên vùng tâm của `JellyBodyTexture` (hoặc dùng 2 kênh texture `_BaseMap` + `_FaceDecalMap` trong cùng 1 Shader `Lit`).
  - Nhờ đó, toàn bộ mắt, miệng, má hồng sẽ nằm **trên chính lưới 3D vòm cong của đỉnh thạch** và nằm **dưới lớp Specular (`Smoothness = 0.90`) + Rim Light**, giúp ánh sáng phản chiếu bóng loáng phủ lên trên cả khuôn mặt!
- **Cách B (Nếu giữ `FaceDecal` riêng):**
  - Dùng lưới vòm cong (Curved Top Mesh) thay cho `Quad` phẳng và đổi Shader của `FaceDecal` sang `Lit Transparent` (hoặc nhân thêm lớp Rim Highlight giả lập) để đồng bộ ánh sáng.

---

### Kỹ Thuật 6: Bóng Đổ Tiếp Xúc Đáy Khay (Soft Contact Drop Shadow) & Âm Thanh Leo Thang

1. **Bóng tiếp xúc (Contact Ambient Occlusion Blob):**
   - Thêm một tấm `ShadowQuad` mỏng màu nâu đen bán trong suốt (`Alpha = 0.28`, viền mờ dần bằng Radial Gradient) nằm sát mặt sàn khay gỗ (`y = 0.005f`) ngay dưới bụng mỗi khối thạch.
   - Khi khối thạch được nhấc nhẹ lên lúc kéo (`y = 0.05f`), bóng đổ hơi mở rộng và nhạt đi một chút; khi khối thạch nảy xuống, bóng đổ đậm lại. Hiệu ứng nhỏ này tăng cảm giác chiều sâu 3D lên gấp đôi.
2. **Âm thanh thạch & Tăng cao độ theo cấp (Pitch-Scaled Jelly Audio):**
   - Khi bắt đầu chạm khối: Phát âm thanh *Pop/Squish* trầm nhẹ (`pitch = 0.95`).
   - Khi Merge lên cấp `Tier k`: Tăng dần `pitch = 0.85f + k * 0.06f` để chuỗi Merge liên hoàn tạo cảm giác hưng phấn (Dopamine rush).

---

## PHẦN 4: BẢNG SO SÁNH HIỆN TRẠNG PROJECT & ĐỀ XUẤT NÂNG CẤP

| Hạng Mục | Hiện Trạng Trong Project (`Pets Harbor`) | Đề Xuất Nâng Cấp Để Đạt 100% Như Video |
| :--- | :--- | :--- |
| **Theo dõi vị trí kéo** | `Vector3.Lerp` hàm mũ (`Exp(-38 * dt)`) trong [`JellyBlock.cs`](file:///d:/Company/Cscmobi/Projects/G23/REA/Demo%20Game%20From%20Creative/Pets%20Harbor/Assets/_Game/Scripts/Gameplay/JellyBlock.cs#L300). | Thêm **Second-Order Spring** + **Velocity Tilt** (nghiêng nhẹ thân khối `1.5° - 3°` theo vận tốc kéo). |
| **Lách góc hẹp (Corner Slide)** | Swept AABB tách trục $X, Y$ trong [`BoardGridManager.cs`](file:///d:/Company/Cscmobi/Projects/G23/REA/Demo%20Game%20From%20Creative/Pets%20Harbor/Assets/_Game/Scripts/Core/BoardGridManager.cs#L291-L335). | Bổ sung **Corner Chamfer Assist**: tự động căn chỉnh trục vuông góc khi vướng mép $\le 0.30$ ô để lách khe chữ L mượt mà. |
| **Hiệu ứng Squash & Stretch** | Cộng/trừ tuyến tính `sx, sy, sz` trong [`JellyBlock.cs`](file:///d:/Company/Cscmobi/Projects/G23/REA/Demo%20Game%20From%20Creative/Pets%20Harbor/Assets/_Game/Scripts/Gameplay/JellyBlock.cs#L353-L390). | Áp dụng công thức **Bảo toàn thể tích** ($S_y = \frac{1}{S_x \cdot S_z}$) để khối thạch phồng/xẹp tự nhiên hơn. |
| **Hiển thị Mặt Emoji** | `Quad` phẳng `Unlit/Transparent` đặt trên đỉnh khối trong [`JellyBlock.cs`](file:///d:/Company/Cscmobi/Projects/G23/REA/Demo%20Game%20From%20Creative/Pets%20Harbor/Assets/_Game/Scripts/Gameplay/JellyBlock.cs#L87-L95). | Hòa trộn trực tiếp `FaceTexture` vào Texture đỉnh của `JellyBoxMesh` (dưới lớp `Lit Smoothness = 0.90`) để mặt cong theo vòm thạch và bắt sáng bóng kính. |
| **Bóng tiếp xúc đáy khay** | Chỉ dùng Directional Light Shadow mặc định. | Thêm **Soft Contact Shadow Blob** dưới đáy mỗi viên thạch để tách biệt rõ khối 3D với mặt khay gỗ. |
