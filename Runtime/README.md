# Lớp nền và luật khi mở rộng

[← RiseOn.Transitions](../README.md)

## Lớp nền

| Lớp | Việc |
|---|---|
| `Transition` | Danh sách target. `Apply(float t)` chạy mọi target. `Add(object, label)` thêm target bằng code |
| `TransitionTarget<T>` | Một object, tên gọi và danh sách node. `T` cố định từ lúc tạo; object có thể là `T` hay lớp kế thừa `T`. `Add(x => x.member, modifier)` thêm property, `Branch(x => x.a.b)` thêm nhánh bằng code |
| `TransitionUnboundTarget<T>` | Danh sách node như target nhưng không giữ object: `Apply(object, t)` áp lên object truyền vào, mỗi object giữ giá trị lúc nghỉ riêng. `Add`, `Branch` như target |
| `ITransitionNode<in T>` | Một mục trong danh sách node: `Apply(T target, float t)` |
| `TransitionProperty<T, V>` | Node ghi một property public có get, set public của `T`; giá trị lúc nghỉ; một modifier |
| `TransitionBranch<T, U>` | Node đi theo một hay nhiều property public có get của `T` (`Root.Group`) tới object kiểu `U`, là kiểu khai báo của property cuối, rồi chạy danh sách node của `U` trên object đó. Đường đi đọc lại mỗi lần `Apply` |
| `ITransitionModifier<in T, V>` | `V Apply(V input, float t, T target)`. Xem [Modifier](Modifiers/README.md) |
| `ITransitionSource<in T, out V>` | `V Evaluate(float t, T target)`. Xem [Source](Sources/README.md) |

Field giữ một chuyển cảnh: `[SerializeField] Transition`. Chuyển cảnh không cần
`[SerializeReference]` ở chỗ dùng; bên trong nó tự giữ target, property và khối bằng
`[SerializeReference]`.

`T` của modifier và source là thứ khối **đọc** từ target, khai rộng nhất có thể:
`OffsetSource` đọc `Component`, nên cắm được vào target `RectTransform` hay `Image`;
khối không đọc gì khai `object`. Nhờ `in T`, khối khai `T` rộng vào được ô có `T` hẹp.

## Giá trị lúc nghỉ

Giá trị lúc nghỉ là giá trị của property khi đã hiện xong, `t` = 1. Mỗi property chọn
một trong hai:

- **Theo object** (mặc định): giá trị member có trước lần ghi đầu tiên của bất kỳ chuyển
  cảnh nào. Mỗi cặp (object, member) chỉ có một giá trị lúc nghỉ, dùng chung cho mọi chuyển
  cảnh ghi member đó, giữ tới khi object bị hủy. Code ngoài ghi vào member sau đó không đổi
  giá trị lúc nghỉ.
- Bật **Custom Rest**: dùng giá trị nhập tay ở ô `Rest`.

Pack giữ các giá trị này trong một kho nội bộ, nhóm theo object. Mỗi frame, một vòng
UniTask trên luồng chính kiểm một object theo lượt và bỏ object đã bị hủy. Object không
phải Unity object (lớp C# thường) được kho giữ bằng tham chiếu yếu, nên kho không làm nó sống
lâu hơn: GC thu nó rồi thì giá trị của nó bị bỏ. Đọc, ghi member của object như vậy đi qua
reflection, mỗi lần ghi một giá trị struct thì box một lần.

Object phải đang ở trạng thái hiện xong lúc chuyển cảnh đầu tiên chạm vào nó: vừa sinh ra
từ prefab chẳng hạn. Hai chuyển cảnh cùng ghi một member cùng lúc thì cái ghi sau thắng.

## Mở rộng

- **Nguồn giá trị mới** (đo một thứ riêng của game): cài `ITransitionSource<T, V>`. Cắm
  được ngay vào mọi modifier có sẵn. Xem [Source](Sources/README.md#viết-source-mới).
- **Phép biến đổi mới** (phi tuyến, không ghép được từ khối có sẵn): cài
  `ITransitionModifier<T, V>`. Xem [Modifier](Modifiers/README.md#viết-modifier-mới).
- **Kiểu giá trị mới cho khối cần cộng, nhân, nội suy**: cài operation, xem
  [Operation](Operations/README.md).
- **Trục cho struct khác**: `ValueAxes.Register`, xem [Giá trị](Values/README.md#valueaxes).

Mọi khối cần `[Serializable]` trên từng lớp cụ thể, constructor không tham số, và nên có
`[TypeRegistryItem("Tên", "Nhóm")]` cho ô chọn.

## Luật

Những điều không suy ra được từ việc đọc từng file.

### Khối phải là hàm thuần

Modifier và source: cùng đối số phải ra cùng kết quả, không ghi gì, không giữ trạng thái
giữa các lần gọi, không đọc giá trị đang có của property (đó là thứ lần trước vừa ghi).
Cần giá trị lúc nghỉ thì nó đã là `input` của modifier. Vi phạm thì tua ngược và quay
đầu giữa chừng sẽ sai.

### Tại `t` = 1, modifier trả lại đúng `input`

Để chuyển cảnh kết thúc đúng ở giá trị lúc nghỉ. Với khối có sẵn, nghĩa là source phải
kết thúc ở phần tử trung hòa của phép chứa nó: `Addition` ở 0, `Multiplication` ở 1,
trọng số của `Blend` ở 0. `Override` không có phần tử trung hòa: source của nó phải tự
kết thúc ở giá trị lúc nghỉ, hoặc đặt nó trước một modifier đưa giá trị về.

### Một property chỉ một chỗ ghi

Hai property cùng ghi một member của một target thì cái sau thắng. Inspector không cho thêm
trùng, và cảnh báo nếu đã trùng sẵn. Luật này tính cả member phụ thuộc nhau
(`anchoredPosition` và `localPosition`).

### Member tìm theo `T`, không theo kiểu thật của object

Danh sách member, lúc chạy, và link.xml khi build đều tìm trên `T`. Member chỉ có ở lớp
kế thừa thì không dùng được; muốn dùng thì đổi `T` của target. Đường đi của nhánh cũng vậy:
mỗi bước tìm trên kiểu khai báo của bước trước.

### Target không object chỉ nhận object kiểu tham chiếu

Struct cài interface `T` bị box khi truyền vào, ghi lên bản box thì mất. `Apply` log lỗi
một lần rồi bỏ qua.

### Thiếu cấu hình thì báo một lần rồi bỏ qua

Target trống, member không tồn tại hay khác kiểu, đường đi của nhánh hỏng hay không tới
object nào, khối thiếu operation, trục không có:
log lỗi **một lần** cho mỗi chỗ hỏng rồi bỏ phần đó, phần còn lại vẫn chạy. Không ném
exception, vì một chuyển cảnh hỏng một chỗ không nên làm hỏng cả màn hình.

### Luật chung của `[SerializeReference]`

Theo đủ các luật ở
[SerRef](https://github.com/riseongamestudio/Serializables/blob/v1.0.0/Runtime/SerRef/README.md#luật-của-serializereference):
đổi tên lớp, namespace hay assembly là mất dữ liệu đã lưu, trừ khi gắn `[MovedFrom]`;
nhân bản một phần tử tạo hai ô chung một instance.

### Code stripping

Property và đường đi của nhánh dựng trong Inspector được gọi bằng tên, nên build có cắt code sẽ cắt mất chúng;
pack tự sinh link.xml giữ lại khi build, xem [Editor](../Editor/README.md#linkxml-khi-build).
Property thêm bằng code dùng biểu thức (`g => g.alpha`), trình cắt thấy được, không cần
gì thêm. Asset tạo sau khi build (tải về lúc chạy) nằm ngoài tầm của link.xml: tự giữ
member nó dùng.