# Source

[← Lớp nền](../README.md)

Source là một giá trị theo `t`, đọc được target nhưng không nhận giá trị lúc nghỉ. Nó
là toán hạng của modifier, hay của một source khác.

```csharp
public interface ITransitionSource<in T, out V> {
    V Evaluate(float t, T target);
}
```

## Có sẵn

| Nhóm | Source | Việc | Cần |
|---|---|---|---|
| Value | `Constant` | Một giá trị cố định | |
| Value | `Tween` | Từ source `from` tới source `to` trong một đoạn `[a, b]` của `t`, có ease; trước đoạn giữ `from`, sau đoạn giữ `to` | `IInterpolation<V>` |
| Value | `Curve` | Một số theo `AnimationCurve` trong đoạn `[a, b]` | |
| Measure | `Offset` | Quãng dời theo một hướng cho `anchoredPosition`: bằng kích thước chính nó, của cha, đủ ra khỏi cha, hay số đơn vị | Target nằm trên `RectTransform` |
| Measure | `Parent Size`, `Self Size` | Kích thước rect của cha, của chính nó | |
| Combine | `Sum`, `Product` | Tổng, tích nhiều source | `IAddition<V>`, `IMultiplication<V>` |
| Combine | `Retiming` | Chạy một source theo đồng hồ riêng: lấy đoạn `[a, b]`, lặp, đi rồi về, ease, đảo chiều | |
| Vector | `Axis` | Một trục của source struct | Trục đăng ký trong `ValueAxes` |
| Vector | `Vector` | Ghép struct từ một source `float` mỗi trục | Trục đăng ký trong `ValueAxes` |

`Offset` với **Out Of Parent** dời bằng kích thước cha cộng kích thước chính nó: đủ ra
hẳn khỏi cha từ bất cứ chỗ nào bên trong, kể cả khi vị trí lúc nghỉ không ở giữa. Source
không biết vị trí lúc nghỉ nên không tính quãng vừa khít.

Source đo (`Offset`, `Parent Size`, `Self Size`) đo lúc chạy, mỗi lần `Evaluate`; layout
đổi giữa chừng thì quãng dời đổi theo.

## Viết source mới

Cài `ITransitionSource<T, V>`, gắn `[Serializable]`, constructor không tham số:

```csharp
// The size of the canvas the target is on, for a slide that leaves the whole screen.
[Serializable, TypeRegistryItem("Canvas Size", "Measure")]
public sealed class CanvasSizeSource : ITransitionSource<Component, Vector2> {
    public Vector2 Evaluate(float t, Component target) {
        var canvas = target.GetComponentInParent<Canvas>();
        return canvas ? ((RectTransform)canvas.rootCanvas.transform).rect.size : Vector2.zero;
    }
}
```

Source mới cắm được ngay vào mọi modifier và source có sẵn. Các điểm còn lại như
[modifier](../Modifiers/README.md#viết-modifier-mới): `T` khai rộng nhất, phép trên `V`
qua `Operation`, lỗi cấu hình log một lần, và hàm thuần.