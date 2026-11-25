# Quy tắc tạo level TrafficJam

Dùng tài liệu này khi tiếp tục tạo hoặc chỉnh level trong repo Unity trên macOS hay Windows. Đường dẫn bên dưới tính từ thư mục gốc repo.

## 1. Đọc dữ liệu và luồng nạp màn

- Đọc [LevelData.cs](Assets/_Game/Scripts/Tools/LevelData.cs), [RuntimeLevelBuilder.cs](Assets/_Game/Scripts/Tools/RuntimeLevelBuilder.cs), [PrefabLibrary.asset](Assets/_Game/SO/PrefabLibrary/PrefabLibrary.asset) và ít nhất hai asset mẫu trong [SO/LevelData](Assets/_Game/SO/LevelData).
- [Level1](Assets/_Game/SO/LevelData/Level1.asset) là mẫu schema tối thiểu. [Level2](Assets/_Game/SO/LevelData/Level2.asset) là mẫu 24 xe có cone và garage. Xem các màn gần nhất để xác định mức mật độ, kích thước xe và độ khó.
- Mỗi xe lưu `key`, `position`, `facing` và `ObjectColor_car`. Ba key hiện dùng là `carsmall`, `carmedium`, `carbig`, tương ứng 4, 6 và 10 ghế theo `CarControl.Capacity`. Cone dùng key `cone` cùng `position`, `count` và `coneColor`. Đối chiếu hướng và mã màu trong [Enum.cs](Assets/_Game/Scripts/Core/Enum.cs).
- Builder đặt xe bằng **tọa độ world**; asset hiện dùng `z: 0`. `facing` điều khiển sprite, mũi tên và hướng chạy. Với hướng chéo, code chạy theo vectơ `(±1, ±1).normalized`. Các trục `(±1.25, 1.05)` trong [IsometricLevelGenerator.cs](Assets/_Game/Scripts/Tools/IsometricLevelGenerator.cs) là mốc bố cục của tool cũ. Kiểm tra raycast theo hướng thực tế của `CarControl` khi dùng các trục đó.
- [Gameplay.unity](Assets/_Game/Scenes/Gameplay.unity) gán danh sách `Runtime Level Builder > Level`. `DataManager.Level` là chỉ số từ 0 vào danh sách; UI hiển thị chỉ số cộng 1. Tên asset có thể khác số màn trên UI vì Inspector cho phép đổi thứ tự. Đọc danh sách hiện tại và giữ thứ tự người dùng đã thiết lập. Khi thêm asset, giữ GUID cũ, tạo GUID riêng cho asset mới và gán đúng mục theo yêu cầu. `NextLevel` đã lấy số màn từ danh sách, nên thêm màn thông thường chỉ cần dữ liệu và tham chiếu scene.

## 2. Thiết kế hình bao, số xe và cự ly

1. Chọn hình bao trước khi đặt xe. Luân phiên dải hình thoi, hàng so le, hai cánh chéo, eo hẹp, lõi rỗng hoặc hai cụm tách nhau. Tạo ít nhất một thay đổi rõ về hướng, hình bao hay vùng trống giữa hai màn liền kề. Chuỗi cột dọc có thể dùng cho một màn, nhưng cần xen các kiểu bố cục khác khi tạo cả lô.
2. Dùng xe dài để định hình dải chính hoặc điểm giao giữa các nhóm. Xe ngắn lấp kẽ và tạo được đường bao bậc thang. Dải chéo nên có vài xe cùng trục để người chơi nhìn ra hình. Có thể xen thân xe ngang hoặc dọc để tạo điểm chặn.
3. Dùng collider theo đúng sprite, màu, cỡ và hướng. Với hai xe thẳng hàng, đặt bước tâm theo tổng khoảng từ tâm tới hai đầu collider đang đối diện, cộng khe hở. Với hai hướng khác nhau, kiểm tra polygon thật vì ảnh chiếu và bounding box có thể tạo cảm giác chạm dù collider vẫn hở.
4. Mốc tham khảo từ bốn màn đã chỉnh chéo là khe collider nhỏ nhất khoảng **0,04–0,10 Unity unit**. Các màn xếp theo cột trước đó khoảng **0,12 unit**. Đây là kết quả đo trên asset và prefab lúc kiểm tra, có thể thay đổi nếu sửa sprite, scale hoặc vị trí. Chừa khoảng trống giữa cụm lớn hơn khe trong hàng để hình bao dễ đọc. Xem Game view dọc và kiểm tra xe nằm trong vùng bãi, cách phần đường chạy, mép màn và nút UI.
5. Tăng số xe theo từng đoạn, đồng thời xem số xe mở được lúc đầu, số làn bị chặn và số màu đòi hỏi giữ xe đỗ cùng lúc. Một màn thưa vẫn có thể khó nếu vài xe ngang cắt đúng đường thoát. Dùng walkthrough để học nguyên tắc hình học và tạo bố cục riêng cho TrafficJam.

| Asset mẫu | Xe | Màu | Cone | Bố cục |
|---|---:|---:|---:|---|
| Level13 | 30 | 5 | 2 | Hình thoi, các dải chéo dài ở giữa |
| Level15 | 34 | 5 | 3 | Dải nghiêng trái, đầu dải so le |
| Level17 | 40 | 6 | 4 | Lõi dọc, hai cánh chéo hướng ra ngoài |
| Level20 | 46 | 6 | 5 | Cánh chéo mở rộng về dưới, cone so le phía trên |
| [Level21](Assets/_Game/SO/LevelData/Level21.asset) | 48 | 7 | 6 | Hai cánh thoát chéo và sáu làn giữa có cone. Sáu hướng chạy |
| [Level22](Assets/_Game/SO/LevelData/Level22.asset) | 54 | 7 | 6 | Hàng ngang khóa các làn dọc, một số làn có cả xe chắn và cone. Sáu hướng chạy |
| [Level23](Assets/_Game/SO/LevelData/Level23.asset) | 40 + 2 trong garage | 7 | 4 | Bãi so le, chừa hành lang chéo ở góc dưới phải cho garage. Năm hướng chạy |

Trong danh sách `Gameplay` kiểm tra ngày 25/11/2026, thứ tự đầu danh sách là `Level23`, `Level22`, `Level21`, tương ứng màn 1, 2, 3 trên UI. Khi chạy thử, đối chiếu lại thứ tự hiện tại.

### Bài học từ 50 màn tham chiếu

Đã khảo sát bố cục mở màn và các lượt đầu của 50 video, từ Level 240 xuống Level 191. Số xe là ước lượng theo hình. Bộ đếm `LEFT` hiển thị số khách còn lại. Phạm vi khảo sát chưa bao gồm lời giải trọn vẹn của từng màn. [Playlist Bus Escape – Traffic Jam](https://www.youtube.com/playlist?list=PLDnss7IHdMi10CpPPoU1n5WECgEVm5JQB)

- **Mật độ và hình bao.** Các màn thoáng như 236 và 199 có khoảng 30–40 xe, còn các bàn kín như 238 và 221 khoảng 70–90 xe. Màn 237 dùng viền kín và nhánh chéo hội tụ. Màn 222 tách hai khối bằng hành lang. Màn 198 dùng vòng xe dọc bao quanh lõi xe ngang. Màn 192 xòe nhiều nhánh từ lõi dọc. Dùng các nguyên tắc hình học này để dựng bố cục riêng cho TrafficJam.
- **Hướng và kích cỡ.** Gom xe cùng hướng thành dải để người chơi đọc được đường đi. Xe dài chạy ngang hoặc chéo có thể cắt đường thoát của nhiều xe phía sau. Các màn 204, 207 và 194 dùng dải chéo làm cấu trúc chính. Chừa khoảng trống ở giao điểm để thấy xe nào đang giữ làn.
- **Màu và độ khó.** Có thể đặt hai xe cùng màu ở các cụm khác nhau, một xe dễ lấy ở rìa và một xe mở được nhiều đường ở sâu trong bãi. Màn 195 có nhãn `HARD` với bố cục khá thoáng, còn 230 có nhãn `SUPER HARD` với lõi dày và nhiều xe ẩn. Khi thiết kế màn khó cho TrafficJam, xem số xe mở đầu, số lớp vật cản và áp lực lên bốn ô đỗ cùng với tổng số xe.

## 3. Phối màu và đặt cone

- Chỉ dùng màu có cấu hình sprite xe, khách và cone tương ứng. Mốc Level13–20 là 5–6 màu. Cách đã dùng cho các màn dày là chọn 2–3 màu trong nhóm xe thoát được đầu màn, ưu tiên màu mở cone; phân bổ các màu còn lại cho nhóm xe bị chặn. Có thể đổi màu giữa các xe để tạo lời giải và giữ số màu dự kiến.
- Đặt cone trước đầu một làn sao cho raycast thực sự chạm collider của cone và collider không chồng xe. Mốc Level13–20 là 2–5 cone, với `count` từ 4–8. Đây là số **khách cùng màu**, không phải số xe.
- [ConeBlocker](Assets/_Game/Scripts/GamePlay/ConeBlocker.cs) nghe `CarControl.OnCarDepartedFull`. [GuestManager](Assets/_Game/Scripts/Core/GuestManager.cs) phát sự kiện khi xe đã đầy khách và trừ theo sức chứa. Xe cần đúng màu và có đường ra trước khi cone mở. Nếu nhiều cone cùng màu, mỗi cone cùng nhận sự kiện và tự tính ngưỡng của mình.
- Tính sức chứa theo màu của các xe có thể thoát trước từng cone. Tránh trường hợp tất cả xe màu cần thiết đều ở sau cone. Với nhiều cone, kiểm tra thứ tự mở lần lượt và xác nhận xe phía sau có đường thoát tiếp.

## 4. Bố trí garage

- `LevelData.garages` lưu `key` và world `position`. Garage dùng thứ tự xe trong `GarageController.list_PresetCars` của prefab, nhả từng xe khi cửa thông. Kiểm tra prefab có xe sẵn và giữ lối đi trống từ điểm chờ tới biên. [GarageController.cs](Assets/_Game/Scripts/GamePlay/GarageController.cs)
- Luồng khởi tạo hiện tại giữ một tham chiếu `GameController.garageController` và gọi `HandleStart` trên tham chiếu đó. Với luồng này, bố trí một garage có xe mỗi màn. [GameController.cs](Assets/_Game/Scripts/Core/GameController.cs)
- `Level23` dùng `garagetopright` ở góc dưới phải. Xe hồng 4 chỗ ra trước, xe cyan 6 chỗ ra sau. Xe hồng trong garage góp vào ngưỡng 8 khách của cone hồng. Khi tính lời giải, đưa cả hai xe và thứ tự nhả vào mô hình hàng khách.

## 5. Xác minh trước khi bàn giao

- **Dữ liệu và tham chiếu.** Kiểm tra số xe, cỡ xe, số màu, số cone, key, enum, GUID và thứ tự thật trong `Gameplay`. Khi sửa màn có sẵn, giữ `.meta` và chỉ chỉnh những asset thuộc phạm vi yêu cầu.
- **Vị trí và đường chạy.** Dùng prefab thật trong Unity để rà collider chồng nhau. `CarControl.CheckBlockByRaycast` bắn từ tâm xe theo `facing`, dùng `float_GateRayCast` và `carLayer` của prefab, rồi bỏ collider của chính xe. Với prefab chuẩn từng kiểm tra, ray dài 2 Unity unit. Kiểm tra thêm đường quét của thân xe tới biên để phát hiện xe nằm lệch tâm ray. Xác nhận mỗi cone có xe bị chặn và có một thứ tự tháo hết xe sau khi cập nhật trạng thái cone.
- **Mô phỏng hàng khách nếu có thể.** `GuestManager.BuildSpawnPlanFromCars` chia khách theo sức chứa vào hai nhóm theo trạng thái xe đầu màn, trộn từng nhóm rồi ghép nhóm thông trước nhóm bị chặn. Mô phỏng theo khách đầu hàng, bốn ô đỗ mặc định, số ghế còn trống và sự kiện mở cone khi xe đầy. Tìm lời giải qua nhiều seed; các đợt trước dùng khoảng 100–150 mẫu mỗi màn. Đây là kiểm tra mẫu, chưa chứng minh mọi hàng khách đều giải được. Khi một mẫu thất bại, kiểm tra lại màu xe mở đường, sức chứa, ngưỡng cone và thứ tự gỡ xe.
- **Kết quả khi tạo Level21–23.** Mỗi màn có lời giải cho 150/150 mẫu hàng khách trong mô hình giản lược lúc bàn giao. Mô hình dùng quan hệ chặn theo làn hoặc collider xấp xỉ và có tính thứ tự xe garage cho Level23. Unity Play Mode và Android/Logcat chưa được chạy ở các lượt tạo màn đó.
- **Play Mode.** Trong `Gameplay`, xác nhận màn nạp đúng asset và đúng số xe, chọn một xe sau cone, đưa đủ khách đúng màu lên xe có đường ra, rồi xác nhận bộ đếm giảm, cone mở và xe phía sau đi được khi đường thông. Chơi qua thắng, `Replay` và `Next Level` nếu có thể; báo riêng Unity Editor, Play Mode và Android/Logcat đã thực hiện hay chưa.
- **Chỉnh tay.** Trong Play Mode, chỉnh vị trí GameObject dưới `_PARKING`, `_CONES` hoặc `_GARAGES`. Trong Inspector, chỉnh `CarControl > Car Info > Directions Car / Object Color Car` cho xe, hoặc `ConeBlocker > Initial Count / Cone Color` cho cone. Mở `Bus Frenzy > Map Layout Tool > Save Layout to LevelData` để ghi vào asset đang chạy. Nút bỏ qua xe đang chạy, đang đỗ hoặc đã rời bãi. `Initial Count` là số khách cần lúc bắt đầu màn. Chạy lại màn để kiểm tra dữ liệu đã lưu. Khi dùng tool tạo tự động hiện có, chú ý lệnh batch của `IsometricLevelGenerator` nhắm vào Level3–Level10.
- **Phạm vi nút Save.** Xe lưu `position`, `facing` và `ObjectColor_car`. Cone lưu `position`, `count` theo `Initial Count` và `coneColor`. Garage lưu `position` của GameObject gốc. Console báo số mục đã lưu và đã bỏ qua theo từng loại.

## 6. Tiếp tục trên Windows

Lấy phiên bản repo có tài liệu này, các asset `.asset` và `.meta` cùng scene tương ứng. Các script thử nghiệm từng chạy trong thư mục tạm trên Mac không phải dependency của project. Khi mở chat Codex mới, có thể dùng lời nhắc sau:

> Đọc AGENTS.md và LEVEL_GENERATION.md. Xem các LevelData mới nhất cùng thứ tự trong Gameplay. Tạo thêm các level theo phạm vi tôi yêu cầu, đổi hình bao giữa các màn, phối cự ly theo collider và kiểm tra mỗi cone có xe mở đường. Báo riêng phần đã thử trong Editor, mô phỏng và Play Mode.
