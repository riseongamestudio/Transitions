# Giá trị

[← RiseOn.Transitions](../../README.md)

| Kiểu | Việc |
|---|---|
| `ValueAxes` | Tên và cách đọc ghi từng trục `float` của struct, cho `Axis` và `Vector` |

Ease dùng `EaseExt` của [`com.riseon.utils.litmotion`](https://github.com/riseongamestudio/Utils/tree/main/LitMotion#readme),
hướng dùng `DirectionExt` của [`com.riseon.utils`](https://github.com/riseongamestudio/Utils/tree/main/Core#readme).

## ValueAxes

`Vector2/3/4`, `Vector2Int/3Int` (làm tròn khi ghi), `Color` và `Rect` có sẵn. Struct
khác thì đăng ký tên trục, cách đọc một trục và cách trả bản sao đã đổi một trục:

```csharp
public static class GameAxes {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    private static void Register() {
        ValueAxes.Register<Range>(new[] { "min", "max" },
            static (range, i) => i is 0 ? range.min : range.max,
            static (range, i, value) => i is 0 ? new Range(value, range.max) : new Range(range.min, value));
    }
}
```

Đăng ký lúc chạy để `Axis`, `Vector` dùng được, và trong Editor để ô chọn liệt kê trục.
Ô chọn chỉ đưa ra struct đã đăng ký cho tham số trục; kiểu chưa đăng ký mà Odin tự suy ra
thì hiện xám. Đọc ghi qua delegate có kiểu, không cấp phát.
