# Lịch sử thay đổi

Mọi thay đổi đáng kể của `com.riseon.transitions` được ghi ở đây. Định dạng theo
[Keep a Changelog](https://keepachangelog.com/vi/1.1.0/), đánh số theo
[Semantic Versioning](https://semver.org/lang/vi/).

## [0.1.0] - 2026-11-03

### Thêm

- `Transition` → `TransitionTarget<T>` → `ITransitionNode<T>`: chuyển cảnh là danh sách
  target, mỗi target một object kiểu `T` và các node: `TransitionProperty<T, V>` ghi một
  property public của `T` với một giá trị lúc nghỉ và một modifier; `TransitionBranch<T, U>`
  đi theo một hay nhiều property của `T` tới object kiểu `U` rồi chạy node của nó.
  `Apply(t)` là hàm thuần của `t`. Dựng bằng code qua `Add(object)`,
  `Add(x => x.member, modifier)` và `Branch(x => x.a.b)`.
- `TransitionUnboundTarget<T>`: node của một target không giữ object, áp lên object truyền
  vào `Apply(object, t)`, như mọi element bị một page che. Struct bị box thì log rồi bỏ qua.
- Giá trị lúc nghỉ theo object: giá trị member có trước lần ghi đầu tiên của bất kỳ chuyển
  cảnh nào, một giá trị cho mỗi cặp (object, member), giữ tới khi object bị hủy; một vòng
  UniTask trên luồng chính dọn object đã hủy, mỗi frame một object; lớp C# thường được giữ bằng
  tham chiếu yếu, bỏ khi GC thu nó. Hoặc nhập tay (*Custom Rest*).
- `ITransitionModifier<in T, V>` và các modifier `Override`, `Addition`,
  `Multiplication`, `Blend`, `Chain`, `Axis`.
- `ITransitionSource<in T, out V>` và các source `Constant`, `Tween`, `Curve`, `Offset`,
  `Parent Size`, `Self Size`, `Sum`, `Product`, `Retiming`, `Axis`, `Vector`.
- Operation `IAddition<V>`, `IMultiplication<V>`, `IInterpolation<V>`, lấy qua
  `Operation<T>.Instance`, có sẵn cho số, vector, màu, rect, bounds, quaternion. Assembly
  tham chiếu assembly khác thì bản của nó thắng.
- Bộ sinh code đăng ký mọi lớp cài operation của từng assembly, và analyzer RTR001–RTR004.
  Mã nguồn ở `Generators~`.
- `RequiresOperationAttribute`: khối khai operation nó cần, ô chọn làm xám khi thiếu.
- `ValueAxes`: trục của `Vector2/3/4`, `Vector2Int/3Int`, `Color`, `Rect`, đăng ký thêm
  cho struct khác.
- Inspector: chuyển cảnh là một khung mang tên field, các target và property là list riêng của
  pack (kéo đổi thứ tự, xóa, không thanh tiêu đề thừa); thêm target bằng thả object hay nút +
  rồi chọn GameObject và kiểu; dòng target có ô tên, ô object và nút + thêm property theo member
  của `T`; ô modifier, source là một dòng mang tên ngắn của khối; *Change Type*, cảnh báo member
  thiếu hay trùng, preview giữ chuột (thả ra hay bị ngắt giữa chừng thì mọi member đặt lại đúng giá trị cũ),
  preset `TransitionPreset` và `TransitionTargetPreset` trong menu chuột phải của Odin; khối một
  field và ease tùy chỉnh gọn thành một dòng chia đôi; *Replace Type* của target chỉ cho chọn
  kiểu mà mọi node vẫn chạy, giữ nguyên chúng, thay cho *Replace Type* chung. Cửa sổ chọn node
  thêm property hay nhánh nhiều tầng; nút *Capture* điền giá trị lúc nghỉ từ object; field target
  không object có ô object preview nhớ theo phiên editor; lưu file thì preview kết thúc trước.
  Target, property None là ô reference của Odin; list lồng trong target hẹp lề, nút xóa thẳng cột
  nút xóa của target; cửa sổ chọn GameObject hiện icon của GameObject và component.
- Preset có sẵn trong `Presets/`: Fade, Graphic Fade, Pop, Zoom In, Slide In từ bốn hướng, Rise In,
  Drop In, và chuyển cảnh Popup, Popup Rise, Popup Drop, Page, Toast.
- link.xml sinh khi build cho member và đường đi của nhánh dựng trong Inspector, và cho `System.Type.GetType`
  mà IL2CPP High cắt mất làm hỏng `[SerializeReference]` generic.
- Ease dùng `EaseExt` của `com.riseon.utils.litmotion`, hướng dùng `DirectionExt` của `com.riseon.utils`.
- Cấu hình hỏng (target trống, member thiếu, operation thiếu…) log lỗi một lần rồi bỏ
  phần đó, không ném exception.