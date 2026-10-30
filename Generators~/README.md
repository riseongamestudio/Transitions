# Bộ sinh code

[← RiseOn.Transitions](../README.md)

Mã nguồn của `Runtime/Generators/RiseOn.Transitions.Generators.dll`: bộ sinh registry
operation (`OperationRegistryGenerator`) và analyzer (`OperationAnalyzer`). Xem việc chúng
làm ở [Operation](../Runtime/Operations/README.md).

Thư mục có `~` ở cuối nên Unity không nạp; Unity chỉ nạp DLL đã build. DLL nằm trong
`Runtime/` và mang nhãn `RoslynAnalyzer`, nên Unity chạy nó trên assembly `RiseOn.Transitions`
và mọi assembly tham chiếu tới.

## Build lại

Cần .NET SDK 6 trở lên.

```bash
dotnet build "Generators~" -c Release
```

DLL ghi thẳng đè lên `Runtime/Generators/`, rồi Unity biên dịch lại các assembly liên
quan. File `.meta` của DLL giữ nguyên, nên nhãn và GUID không đổi.

## Ràng buộc

- Tham chiếu `Microsoft.CodeAnalysis.CSharp` **4.3.0**, bản Roslyn mà Unity 6000.3 dùng.
  Tham chiếu bản mới hơn thì Unity không nạp được DLL.
- Target `netstandard2.0`, không phụ thuộc DLL nào khác.
- `.meta` của DLL: nhãn `RoslynAnalyzer`, bỏ mọi nền tảng (kể cả Editor), không kiểm tham
  chiếu. Tạo lại `.meta` thì đặt lại đủ ba điều này.