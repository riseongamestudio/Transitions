# Operation

[← Lớp nền](../README.md)

C# không có phép cộng, nhân chung cho mọi kiểu, nên khối cần cộng, nhân hay nội suy
một `V` lấy phép đó từ một lớp cài operation của `V`.

| Contract | Hàm | Dùng bởi |
|---|---|---|
| `IAddition<V>` | `V Apply(V a, V b)` | `Addition`, `Sum` |
| `IMultiplication<V>` | `V Apply(V a, V b)` | `Multiplication`, `Product` |
| `IInterpolation<V>` | `V Apply(V a, V b, float t)`, không kẹp `t` | `Tween`, `Blend` |

Tất cả kế thừa `IOperation`. Khối lấy bản đang dùng qua
`Operation<IAddition<V>>.Instance`, `null` khi chưa ai cài.

## Có sẵn

`float`, `double`, `int`, `long`, `Vector2/3/4`, `Vector2Int/3Int`, `Color`, `Color32`,
`Rect`, `RectInt`, `Bounds`: đủ ba phép, nhân theo từng thành phần. Kiểu nguyên làm tròn
khi nội suy. `Quaternion`: không có cộng; nhân là ghép xoay, nội suy là `SlerpUnclamped`.

## Cài cho kiểu mới

Viết một lớp cài contract, đặt ở assembly nào cũng được:

```csharp
public sealed class BoolOperations : IInterpolation<bool> {
    public bool Apply(bool a, bool b, float t) {
        return t < .5f ? a : b;
    }
}
```

Không cần đăng ký, không cần `[Preserve]`. Bộ sinh code của pack chạy trên mọi assembly
tham chiếu pack, viết sẵn một registry tạo mọi lớp cài operation của assembly đó, và gắn
`[assembly: OperationRegistry]` để lúc chạy không phải quét. `IOperation` mang
`[RequireImplementors]`, nên trình cắt code giữ mọi lớp cài ở mọi mức.

Assembly build ngoài Unity (DLL có sẵn) không qua bộ sinh code: lúc chạy pack quét
reflection riêng assembly đó, và nó phải tự giữ lớp cài khỏi trình cắt code.

## Hai bản cùng một phép

Hai assembly cùng cài `IAddition<Vector2>`:

- Assembly nào **tham chiếu** assembly kia (trực tiếp hay gián tiếp) thì thắng. Game
  muốn thay bản của pack thì cài cùng phép trong assembly của game.
- Không bên nào tham chiếu bên kia: dùng bản đứng đầu theo tên assembly rồi tên lớp,
  Editor cảnh báo. Muốn chọn thì cài phép đó ở một assembly tham chiếu cả hai, chuyển
  tiếp sang bản muốn dùng:

  ```csharp
  public sealed class PreferredVector2Addition : IAddition<Vector2> {
      private readonly IAddition<Vector2> chosen = new PackA.Vector2Operations();

      public Vector2 Apply(Vector2 a, Vector2 b) {
          return chosen.Apply(a, b);
      }
  }
  ```

## Luật analyzer

Analyzer đi kèm bộ sinh code, báo ngay trong code:

| Mã | Mức | Khi |
|---|---|---|
| RTR001 | Error | Hai lớp cùng cài một phép trong một assembly |
| RTR002 | Error | Lớp cài phép mà không tạo được: còn generic, không có constructor không tham số, hay lồng trong lớp private |
| RTR003 | Warning | Lớp cài phép không public: assembly khác muốn chọn nó chỉ chuyển tiếp được bằng reflection |
| RTR004 | Warning | Hai assembly không biết nhau cùng cài một phép mà assembly này thấy cả hai. Báo trên file registry sinh ra, vì Unity bỏ cảnh báo không có file |

RTR004 chỉ thấy lớp public của assembly khác; lớp không public trùng phép thì Editor
cảnh báo lúc chạy.