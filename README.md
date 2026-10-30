# RiseOn.Transitions

Chuyển cảnh dựng trong Inspector (hoặc bằng code), chạy theo một bộ đếm `t` từ 0 (ẩn)
tới 1 (hiện). Mỗi chuyển cảnh là hàm thuần của `t`: cùng một `t` luôn ra cùng một hình,
nên bộ đếm chạy tới, chạy lui hay quay đầu giữa chừng đều đúng. Pack không tự chạy theo
thời gian; ai giữ bộ đếm thì người đó gọi `Apply(t)`.

Package `com.riseon.transitions`, namespace `RiseOn.Transitions`.

## Mục lục

- [Yêu cầu](#yêu-cầu)
- [Cài đặt](#cài-đặt)
- [Tổng quan](#tổng-quan)
- [Hướng dẫn nhanh](#hướng-dẫn-nhanh)
- [Thành phần](#thành-phần)
- [Lịch sử thay đổi](#lịch-sử-thay-đổi)
- [Giấy phép](#giấy-phép)

## Yêu cầu

| Phụ thuộc | Cách có | Dùng cho |
|---|---|---|
| Unity 6000.3 | | Bản đang dùng để phát triển. Bộ sinh code chạy trên Roslyn 4.3, bản Unity này mang theo |
| [`com.riseon.serializables`](https://github.com/riseongamestudio/Serializables#readme) 1.0.1 | Tự cài theo `package.json` | `ListSerRef`, `SerObject` |
| [`com.riseon.utils`](https://github.com/riseongamestudio/Utils/tree/main/Core#readme) 1.0.13 | Tự cài theo `package.json` | `AllowGenerics`: ô chọn modifier và source tự đóng generic theo kiểu của ô; `DirectionExt` cho hướng của `Offset`; `RectFieldUtils` cho các dòng chia đôi |
| [`com.riseon.utils.litmotion`](https://github.com/riseongamestudio/Utils/tree/main/LitMotion#readme) 1.0.0 | Tự cài theo `package.json` | `EaseExt` cho ease của `Tween`, `Retiming` |
| [LitMotion](https://github.com/annulusgames/LitMotion) 2.0.2 | Tự cài theo `package.json` | Các hàm ease |
| [UniTask](https://github.com/Cysharp/UniTask) (`com.cysharp.unitask`) 2.5.11 | Tự cài theo `package.json`, cần scope `com.cysharp` | Vòng dọn giá trị nghỉ của object đã hủy |
| uGUI (`com.unity.ugui`) 2.0.0 | Tự cài theo `package.json` | Đo `RectTransform` |
| [Odin Inspector](https://odininspector.com) | Cài tay từ Asset Store | Toàn bộ Inspector của pack |

"Tự cài" là khi cài qua OpenUPM; cài bằng git URL thì phải cài các package kia
trước. Thiếu Odin thì project báo một lỗi từ `RiseOn.Transitions.Requirements`.

## Cài đặt

**OpenUPM** (khuyên dùng): thêm registry OpenUPM với scope `com.riseon`,
`com.annulusgames` và `com.cysharp` vào `Packages/manifest.json`, rồi thêm package:

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": ["com.riseon", "com.annulusgames", "com.cysharp"]
    }
  ],
  "dependencies": {
    "com.riseon.transitions": "0.1.0"
  }
}
```

**Git URL**: cài các phụ thuộc trước, rồi *Package Manager → + → Add package from git URL*:

```
https://github.com/riseongamestudio/Transitions.git
```

**Thư mục local**: `"com.riseon.transitions": "file:D:/path/to/Transitions"`.

## Tổng quan

Một chuyển cảnh là các tầng lồng nhau, cộng các khối tính giá trị:

| Tầng | Kiểu | Giữ |
|---|---|---|
| Chuyển cảnh | `Transition` | Danh sách target |
| Target | `TransitionTarget<T>` | Một object kiểu `T` (hay kế thừa `T`), tên gọi, danh sách node |
| Target không object | `TransitionUnboundTarget<T>` | Danh sách node, áp lên object truyền vào lúc gọi `Apply(object, t)` |
| Node: property | `TransitionProperty<T, V>` | Một property public của `T`, giá trị lúc nghỉ, một modifier |
| Node: nhánh | `TransitionBranch<T, U>` | Đường đi qua các property public của `T` tới một object kiểu `U`, danh sách node của `U` |
| Modifier | `ITransitionModifier<T, V>` | Biến giá trị lúc nghỉ thành giá trị tại `t`: cộng, nhân, ghi đè, pha trộn… |
| Source | `ITransitionSource<T, V>` | Một giá trị theo `t`, là toán hạng của modifier: hằng, tween, curve, đo kích thước… |

Lúc chạy, mỗi property đưa giá trị lúc nghỉ cho modifier rồi ghi kết quả vào object;
property là chỗ duy nhất ghi. Giá trị lúc nghỉ là trạng thái hiện xong (`t` = 1): mặc định
là giá trị member có trước lần ghi đầu tiên của bất kỳ chuyển cảnh nào, giữ tới khi object
bị hủy; hoặc nhập tay.

Modifier và source cần cộng, nhân hay nội suy giá trị `V` thì lấy phép đó qua
[operation](Runtime/Operations/README.md): pack có sẵn cho số, vector, màu, rect,
quaternion; kiểu khác thì cài thêm một lớp, bộ sinh code tự đăng ký.

## Hướng dẫn nhanh

**Trong Inspector**

1. Khai field:

   ```csharp
   using RiseOn.Transitions;

   [SerializeField] private Transition show = new();
   ```

2. Kéo object thả vào khung của field, hoặc bấm + rồi chọn GameObject, chọn kiểu sẽ làm target: chính nó, lớp cha,
   interface, hay một component của GameObject.
3. Bấm + cuối dòng target, chọn property, rồi chọn modifier. Ví dụ trượt từ dưới lên:
   `anchoredPosition` với `Addition`, source `Tween` từ `Offset` (Down) về `Constant` (0).
4. Giữ và kéo thanh preview trên dòng đầu của khung để xem; thả ra thì mọi object về như cũ.

**Bằng code**

```csharp
var show = new Transition();
show.Add(group).Add(g => g.alpha, new MultiplicationModifier<CanvasGroup, float>(
    new TweenSource<CanvasGroup, float>(new ConstantSource<float>(0f), new ConstantSource<float>(1f))));
```

**Chạy**: mỗi lần bộ đếm đổi:

```csharp
show.Apply(t);   // t từ 0 (ẩn) tới 1 (hiện)
```

Lần ghi đầu vào một member đọc giá trị đang có làm giá trị lúc nghỉ, nên object phải đang ở
trạng thái hiện xong lúc chuyển cảnh đầu tiên chạm vào nó (vừa sinh từ prefab chẳng hạn).

**Áp lên object chỉ biết lúc chạy**: khai `TransitionUnboundTarget<T>`, `T` thường là interface
nêu những gì cần chạm tới, rồi gọi cho từng object:

```csharp
[SerializeField] private TransitionUnboundTarget<ICoverable> cover = new();

for (var i = 0; i < covered.Count; i++) cover.Apply(covered[i], t);
```

## Thành phần

| Thành phần | Việc | Chi tiết |
|---|---|---|
| Lớp nền | `Transition`, target, target không object, property, nhánh; giá trị lúc nghỉ; mở rộng; luật | [Runtime](Runtime/README.md) |
| Modifier | Các modifier có sẵn, viết modifier mới | [Runtime/Modifiers](Runtime/Modifiers/README.md) |
| Source | Các source có sẵn, viết source mới | [Runtime/Sources](Runtime/Sources/README.md) |
| Operation | Cộng, nhân, nội suy theo kiểu; cài cho kiểu mới; chọn bản khi trùng; luật analyzer | [Runtime/Operations](Runtime/Operations/README.md) |
| Giá trị | `ValueAxes` | [Runtime/Values](Runtime/Values/README.md) |
| Inspector | Kéo thả target, đổi kiểu, thêm property và nhánh, nút Capture, preview, preset, link.xml khi build | [Editor](Editor/README.md) |
| Bộ sinh code | Mã nguồn, cách build lại DLL | [Generators~](Generators~/README.md) |

## Lịch sử thay đổi

Xem [CHANGELOG.md](CHANGELOG.md).

## Giấy phép

MIT, xem [LICENSE.md](LICENSE.md).