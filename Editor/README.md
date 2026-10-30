# Inspector

[← RiseOn.Transitions](../README.md)

Mọi drawer là của Odin, áp cho mọi field `Transition` và `TransitionUnboundTarget<T>`, ở
MonoBehaviour, ScriptableObject hay lồng trong class khác.

Một field `Transition` hiện thành một khung. Dòng đầu gồm tên field, thanh preview trải trên
cột giá trị (chỉ hiện khi có target đã có object), rồi nút +. Dưới đó là các target, mỗi target
một dòng, các node của nó (property, nhánh) nằm ngay dưới dòng đó. Các ô chọn (target, property, đổi kiểu, khối,
preset) là cửa sổ tìm kiếm của Utils dạng drop-down: gắn dưới nút, gõ để tìm, mục không dùng
được thì mờ kèm lý do. Bấm lại đúng nút đã mở cửa sổ thì chỉ đóng nó, không thêm gì. Alt+click
một foldout bất kỳ thì mở hay gập cả nhánh bên dưới.

## List

List target và list node là list riêng của pack, theo đúng số đo của list Odin (khung, đệm
phần tử, tay nắm, nút X):

- Không có thanh tiêu đề riêng: dòng của chủ list (khung `Transition`, dòng target) là tiêu đề,
  nút + nằm cuối dòng đó. Dòng đầu khung chia cột như một field: tên ở cột nhãn, thanh preview và
  nút + ở cột giá trị.
- Tay nắm và nút X ngang dòng đầu của phần tử, kể cả khi phần tử đang mở cao. Chỉ dòng đầu chừa
  chỗ cho chúng; các dòng bên dưới bắt đầu dưới giữa tay nắm (list lồng trong phần tử khác thì
  thẳng cột với dòng đầu, để foldout con nằm ngay dưới foldout cha) và cách đều mép trên, dưới,
  phải một khoảng đệm. Cột giá trị thẳng với field thường ở ngoài list, dù phần tử đóng hay mở.
- Drawer của phần tử gọi `ItemList.MarkFirstRow` với dòng đầu của nó, hay
  `ItemList.MarkFirstRow(row, out prefix)` khi dòng chia cột nhãn và cột giá trị; list đặt dòng
  đó theo khung phần tử, không theo layout.
- Nền phần tử, nền list và viền khớp lưới pixel của màn hình, nên ở tỉ lệ màn hình lẻ (125%,
  150%…) không có khe hay viền tràn giữa hai phần tử.
- Kéo tay nắm thì phần tử bám theo chuột, nổi trên các phần tử khác và không ra khỏi list; các
  phần tử khác trôi mượt để mở khe đúng chỗ nó sẽ nằm. Thả ra thì phần tử vào khe đó.
- Chuột phải tay nắm mở menu phần tử của Odin (chuyển, nhân bản, xóa phần tử), có vạch sáng bên
  trái phần tử như list của Odin.
- List rỗng hiện một dòng gợi ý.

## Target

- **Thêm bằng nút +**: mở drop-down chọn target. Tầng đầu là các
  GameObject cùng scene với field (hay cùng prefab, khi field nằm trong prefab); bấm một
  GameObject thì trượt vào các kiểu nó làm target được: chính nó, mỗi component một thư mục,
  trong đó là kiểu của component, lớp cha có khai property ghi được hay đi tiếp được, interface
  có property ghi được hay đi tiếp được. Mỗi thư mục mang icon của GameObject hay component của nó. Gõ tìm thì ra thẳng kiểu kèm
  đường dẫn, như `Counter › CanvasGroup`. Dòng Null đầu danh sách thêm một target None, để chọn
  object sau; đóng cửa sổ (bấm ra ngoài, Esc) thì không thêm gì. Dòng None trong list là ô
  reference của Odin, bấm vào để chọn lại. Nút + của target thêm node cũng vậy.
- **Thêm bằng kéo thả**: thả object vào list thì mở ngay danh sách kiểu của nó tại chỗ thả.
  Thả nhiều object cùng lúc thì mỗi object một thư mục, mỗi lần chọn thêm một target.
- **Một dòng** mỗi target, chia cột như mọi field: mũi tên gập mở và ô tên ở cột nhãn, ô
  object ở cột giá trị; cả hai có vạch override prefab như field thường. Ô tên để trống thì
  target mang tên object, hiện mờ làm gợi ý; chỉ gõ khi muốn đặt tên vai, ví dụ Background,
  Body trong preset.
- **Kiểu `T` cố định** từ lúc thêm. Ô object nhận `T` hay lớp kế thừa `T`, kể cả object
  khác cùng kiểu; ô để trống vẫn là trạng thái hợp lệ (vai chờ object, như từ preset).
- **Đổi kiểu**: chuột phải dòng target (vùng mũi tên gập mở):
  - *Change Type* chọn kiểu mới từ object đang giữ hoặc từ component khác cùng GameObject, rồi
    làm lại target từ đầu với kiểu đó: không giữ tên gọi, không giữ property.
  - *Replace Type* đổi sang một kiểu khác của GameObject mà vẫn giữ nguyên tên gọi và mọi property
    (modifier, giá trị lúc nghỉ). Chỉ chọn được kiểu mà mọi property vẫn chạy: có member cùng tên,
    cùng kiểu giá trị, và modifier dùng được cho kiểu đó; kiểu khác hiện mờ kèm các member sẽ mất.
    Kiểu con của `T` luôn chọn được (modifier khai `in T`), nên đây là cách thêm property chỉ kiểu
    con có (target `Graphic` trên một Image thì sang `Image`) mà không mất các effect đang có.

  *Replace Type* chung của Utils, vốn chép field theo đường dẫn, bị ẩn ở target (`[HideReplaceType]`
  trên `ITransitionTarget`): chép như thế sang `T` khác sẽ để object và property sai kiểu.

## Target không object

Field `TransitionUnboundTarget<T>` hiện thành một khung như `Transition`. Dòng đầu gồm tên
field, ô object để preview, thanh preview, rồi nút + thêm node. Ô object nhận `T` hay thứ giữ
`T` (GameObject, component có component `T` bên cạnh) và ghi tên theo `T`, như ô object của
target. Ô này không được lưu vào asset: nó là trạng thái của Inspector, nhớ theo object đang
xem và đường dẫn field cho tới khi đóng editor, qua cả lần biên dịch lại. Chưa có object thì
không có thanh preview.

Chuột phải dòng đầu có *Save as Preset* (lưu thành `TransitionTargetPreset`) và *Apply Preset*
(áp preset target có `T` là `T` của field hay lớp cha của nó).

## Node

Nút + cuối dòng target (hay dòng nhánh, dòng đầu của target không object) mở cửa sổ chọn node:

- **Property**: các property public có get, set public của `T`, gom theo kiểu khai báo, kèm
  kiểu giá trị. Property của `Object` và `Component` (`name`, `hideFlags`, `tag`) không có, vì
  không chuyển cảnh nào ghi chúng. Property đã có thì mờ, không thêm trùng được; trùng có sẵn
  (sửa tay, hay dữ liệu cũ) thì hiện cảnh báo.
- **Nhánh**, trong thư mục Branch: các property public có get của `T` trả về object (không
  tính chuỗi, mảng, delegate). Chọn một thì cửa sổ sang bước tiếp, tiêu đề là đường đi tới đó
  (`IUIElement.RectTF`): dòng đầu *Branch Here* thêm nhánh dừng ở đó, các dòng sau đi tiếp
  một tầng nữa. Gõ tìm được ở mọi bước.

Dòng nhánh mang tiêu đề là đường đi (`RectTF.parent`), kiểu `U` mờ ở cuối dòng, rồi nút +
thêm node trên object nhánh tới. Đường đi hỏng (đổi tên, xóa member, đổi kiểu) thì tiêu đề đỏ
kèm lỗi bên dưới, như property.

## Property

Member chọn lúc thêm và là tiêu đề của property, kèm kiểu giá trị ở cuối dòng; không có ô
để đổi member, vì member khác thường khác kiểu: muốn đổi thì xóa rồi thêm lại. Tiêu đề đỏ,
kèm lỗi bên dưới, khi `T` không còn member đó (đổi tên hay xóa) hay khi member đổi kiểu.
Lúc chạy, property đó log lỗi một lần rồi bỏ qua.

Ô **Custom Rest** tắt thì giá trị lúc nghỉ lấy theo object. Bật thì hiện ô `Rest`, thụt vào
dưới ô bật tắt, kèm nút **Capture** cuối dòng: bấm thì điền giá trị member đang có trên object
mà property ghi lúc này (object của target, object preview của target không object, hay object
nhánh tới). Trong play mode, member đã được chuyển cảnh ghi thì Capture lấy giá trị lúc nghỉ pack
đang giữ cho nó, không lấy giá trị đang chạy giữa chừng. Không có object thì không có nút.

Mỗi ô modifier và source là ô `[SerializeReference]` của Odin như mọi field, nhưng chỉ ghi tên
ngắn của khối (`Multiplication`, `Tween`…) theo `[TypeRegistryItem]`: kiểu target và kiểu giá
trị không lặp lại vì ô đã định sẵn, kiểu đầy đủ nằm trong tooltip. Bấm ô mở bộ chọn khối, tiêu
đề là tên field (From, Modifier…); các ô con của khối gập dưới nó.

Khối, source hay modifier, chỉ có một field Unity vẽ trên một dòng (số, chuỗi, vector, màu,
curve, object…; `Constant` chẳng hạn) gọn thành một dòng: cột giá trị chia đôi, nửa trái là ô
khối như mọi ô khối, nửa phải là field đó. Ease để *Custom Animation Curve* cũng chia đôi y như
vậy, ô ease bên trái, curve bên phải. Hai nửa vẽ theo khung tính sẵn nên thẳng hàng từ dòng này
sang dòng khác, cách nhau đủ chỗ cho vạch override prefab. Ô enum trong các dòng này (ease chẳng
hạn) mở cửa sổ tìm kiếm drop-down như mọi ô chọn khác, mở ra cuộn tới giá trị đang chọn; enum
`[Flags]` thì mỗi lần chọn bật tắt một cờ, có dấu tích, cửa sổ vẫn mở để chọn tiếp.

Ô chọn modifier và source tự đóng generic theo kiểu của ô: ô `ITransitionModifier<RectTransform, Vector2>`
hiện sẵn `Addition`, `Blend`… cho đúng `<RectTransform, Vector2>`, và `Constant` dù
`Constant` khai `object` (qua variance). Khối thiếu operation cho kiểu giá trị, hay khối
trục với kiểu không có trục, hiện xám kèm lý do. Đổi kiểu một khối mà giữ dữ liệu: chuột
phải, *Replace Type* (Utils).

## Preview

Thanh trượt trên dòng đầu của khung (của `Transition` hay của target không object, trên object
preview) chỉ chạy khi đang giữ chuột:

1. Nhấn: đọc giá trị của mọi member sẽ bị ghi, kể cả member trên object các nhánh tới.
2. Kéo: áp `t` theo vị trí chuột, 0 ở mép trái. Giá trị lúc nghỉ theo object là giá trị đã đọc.
3. Thả: đặt lại đúng các giá trị đã đọc, dù `t` = 1 cho ra gì: chuyển cảnh không kết thúc ở
   chỗ bắt đầu, hay giá trị lúc nghỉ nhập tay, cũng không để lại gì trên object. Ngoài play
   mode, giá trị lúc nghỉ đọc trong lúc preview cũng bị bỏ.

Bị ngắt giữa chừng (biên dịch lại, đổi play mode, thanh trượt mất chuột) cũng đặt lại như khi
thả. Lưu bất kỳ thứ gì (scene, prefab trong Prefab Mode kể cả auto save, asset) cũng kết thúc
preview trước khi ghi, nên file không bao giờ chứa giá trị preview. Đặt lại từng member riêng
rẽ: một member lỗi (setter ném exception) thì log lỗi, các member khác vẫn về đúng. Mỗi lúc
chỉ một preview chạy. Không ghi Undo, không đánh dấu thay đổi, không lưu `t`.

## Preset

Hai mục *Save as Preset*, *Apply Preset* nằm trong menu chuột phải của Odin: chuột phải dòng
đầu của khung (preset cả chuyển cảnh) hay dòng một target (preset target):

| Preset | Lưu từ | Áp vào | Áp thì |
|---|---|---|---|
| `TransitionPreset` | Cả field `Transition` | Field `Transition` | Mọi target bị thay bằng các vai của preset (tên gọi, `T`, property, ô object trống); kéo object vào từng vai |
| `TransitionTargetPreset` | Một target, hay một target không object | Target, target không object, có `T` là `T` của preset hay lớp kế thừa nó | Node bị thay; object và tên gọi giữ lại |

*Save as Preset* hỏi chỗ lưu asset; object không được lưu vào preset. *Apply Preset* mở cửa
sổ liệt kê mọi preset hợp trong project và trong các package, trống khi chưa có. Áp ghi Undo.


### Preset có sẵn

Pack mang theo một bộ preset trong `Presets/`, hiện trong *Apply Preset* của mọi project cài pack:

| Preset | Loại | Làm gì |
|---|---|---|
| Fade | Target `CanvasGroup` | `alpha` từ 0 lên giá trị lúc nghỉ, OutQuad |
| Graphic Fade | Target `Graphic` | Trục `a` của `color` từ 0 lên, OutQuad; cho Image, Text không có CanvasGroup |
| Pop | Target `Transform` | `localScale` từ 0 bật lên, OutBack |
| Zoom In | Target `Transform` | `localScale` từ 0.8 lên, OutCubic |
| Slide In From Left, Right, Top, Bottom | Target `RectTransform` | `anchoredPosition` trượt vào từ ngoài cha, OutCubic |
| Rise In | Target `RectTransform` | `anchoredPosition` trồi lên từ dưới ngoài cha, vượt quá chút rồi về, OutBack |
| Drop In | Target `RectTransform` | `anchoredPosition` rơi xuống từ trên ngoài cha, nảy khi chạm chỗ, OutBounce |
| Popup | Chuyển cảnh | Vai Backdrop (Fade) và Content (Pop) |
| Popup Rise | Chuyển cảnh | Vai Backdrop (Fade) và Content (Rise In) |
| Popup Drop | Chuyển cảnh | Vai Backdrop (Fade) và Content (Drop In) |
| Page | Chuyển cảnh | Vai Content (Slide In From Right) |
| Toast | Chuyển cảnh | Vai Body Group (Fade) và Body (Slide In From Top) |

Preset của package cài từ registry là chỉ đọc; áp preset chỉ sao chép nội dung của nó.

## link.xml khi build

Mỗi lần build, pack sinh một link.xml (`Library/RiseOn.Transitions/link.xml`) cho trình
cắt code, gồm:

- Mọi member mà property dựng trong Inspector ghi, và mọi member trên đường đi của nhánh, đọc từ mọi scene, prefab và asset
  dạng text trong project, kể cả cái không có trong build. Preset bỏ qua.
- `System.Type.GetType`: engine gọi hàm này từ native để nạp kiểu generic lưu bằng
  `[SerializeReference]`, và IL2CPP ở mức High cắt nó đi, làm game văng khi nạp. Đây là lỗi
  của Unity đã báo; dòng này không làm build to thêm đáng kể.

Asset phải lưu dạng text (*Asset Serialization: Force Text*, mặc định).