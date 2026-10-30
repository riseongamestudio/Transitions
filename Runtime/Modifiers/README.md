# Modifier

[← Lớp nền](../README.md)

Modifier nhận giá trị lúc nghỉ của property (`input`), `t` và target, trả giá trị để
ghi. Mỗi property có đúng một modifier; nhiều phép thì gộp bằng `Chain`.

```csharp
public interface ITransitionModifier<in T, V> {
    V Apply(V input, float t, T target);
}
```

## Có sẵn

| Modifier | Công thức | Chứa | Cần |
|---|---|---|---|
| `Override` | `x' = source` | Một source | |
| `Addition` | `x' = x + source` | Một source | `IAddition<V>` |
| `Multiplication` | `x' = x · source` | Một source | `IMultiplication<V>` |
| `Blend` | `x' = (1 − w) · x + w · source` | Source đích, source trọng số `w` (float) | `IInterpolation<V>` |
| `Chain` | Chạy lần lượt, mỗi cái nhận kết quả cái trước | Danh sách modifier | |
| `Axis` | Chạy một modifier `float` trên một trục, giữ nguyên các trục khác | Tên trục, một modifier `float` | Trục đăng ký trong `ValueAxes` |

Ví dụ:

| Muốn | Dựng |
|---|---|
| Trượt từ dưới lên | `anchoredPosition`: `Addition` ← `Tween` (`Offset` Down → `Constant` 0) |
| Mờ từ 0 lên giá trị gốc | `alpha`: `Multiplication` ← `Tween` (0 → 1) |
| Nảy dọc 10% | `localScale`: `Axis` y ← `Multiplication` ← `Sum` (`Constant` 1, `Curve` 0 → 0.1 → 0) |
| Lóe trắng rồi về màu gốc | `color`: `Blend` ← đích `Constant` trắng, trọng số `Curve` 0 → 1 → 0 |
| Chỉ trục y | `anchoredPosition`: `Axis` y ← `Addition` ← … |

Trong ô chọn, modifier thiếu operation cho `V` hiện màu xám kèm lý do, ví dụ
`Addition (needs IAddition<Boolean>)`.

## Viết modifier mới

Cài `ITransitionModifier<T, V>`, gắn `[Serializable]`, constructor không tham số:

```csharp
// Snaps the value to a grid: a step that is not linear in the input, so no built-in block makes it.
[Serializable, TypeRegistryItem("Snap")]
public sealed class SnapModifier<T> : ITransitionModifier<T, Vector2> where T : class {
    [SerializeField] private float cell = 10f;

    public Vector2 Apply(Vector2 input, float t, T target) {
        return t >= 1f ? input : new Vector2(Mathf.Round(input.x / cell), Mathf.Round(input.y / cell)) * cell;
    }
}
```

- `T` khai rộng nhất có thể; không đọc target thì để generic hoặc `object`.
- Cần phép trên `V` thì lấy `Operation<IAddition<V>>.Instance` (null khi chưa có ai cài),
  và gắn `[RequiresOperation(typeof(IAddition<>))]` để ô chọn làm xám khi thiếu.
- Lỗi cấu hình thì log một lần rồi trả `input`, không ném exception.
- Theo các [luật](../README.md#luật): thuần, và trả đúng `input` tại `t` = 1.