# Nghiên cứu mở rộng song ngữ — chưa triển khai

Trạng thái code mới nhất: [Theo dõi triển khai hội thoại](DIALOGUE-IMPLEMENTATION.vi.md). Các mô tả “chưa triển khai” dưới đây thuộc thời điểm khảo sát.

Ngày: 05/11/2026. Codex tổng hợp ba khảo sát AGY chỉ đọc chạy song song, rồi đối chiếu các điểm quan trọng với mã nguồn cục bộ. Phần sửa font của phiên bản hiện tại là công việc triển khai riêng. Tài liệu này không cấp phép triển khai hội thoại, ruby hay song ngữ toàn game.

## 1. Kết luận thiết kế

- Có thể nghiên cứu hiển thị hai bản văn trong hội thoại; cần một bộ điều hợp riêng cho vanilla và cho Dialogue Display Framework Continued (DDFC).
- Cách thực tế để bắt đầu là hai lớp văn bản theo câu/đoạn: bản chính kích thước bình thường, bản phụ nhỏ hơn ở trên hoặc dưới. Đây là kiểu hiển thị liên dòng lấy cảm hứng từ ruby; không phải tự động căn từng từ giữa hai bản dịch.
- Ruby căn từng từ/cụm từ cần dữ liệu gắn cặp do người dịch/tác giả cung cấp. Hai câu dịch thường khác thứ tự và số từ; không thể lấy vị trí ký tự tương ứng để suy ra căn chỉnh đúng.
- Không phải mọi vùng game đều thích hợp thêm dòng cố định. Cần điều hợp theo từng loại khung: tooltip, trang có cuộn/phân trang, danh sách cố định, lớp phủ nhỏ.
- Không có bằng chứng FontSettings hiện tại hỗ trợ hai vai trò font độc lập cho bản chính/phụ. Muốn giữ chính sách không thêm cấu hình cỡ/màu trong UntranslatedStardew, cần thiết kế cầu nối hoặc đề xuất API cho FontSettings trước khi triển khai tùy biến này.

Tham khảo định nghĩa ruby và cách gắn cặp base/annotation: [W3C CSS Ruby Annotation Layout](https://www.w3.org/TR/css-ruby-1/), phần Introduction và Ruby Box Model. Đây là tài liệu mô hình chữ, không phải thư viện có sẵn trong Stardew/MonoGame.

## 2. Hội thoại: phải giữ được nguồn gốc câu

Nguồn đối chiếu: `D:\Downloads\Stardew_decompilers\StardewValley\Dialogue.cs:191` khai báo `TranslationKey`, chú thích nói rõ khóa chỉ mang tính thông tin và chuỗi hiển thị có thể đã qua thế token/phân tích. Các đường khởi tạo/tra cứu nằm gần dòng346–426. Màn hình hội thoại nằm trong `StardewValley.Menus\DialogueBox.cs`, nhánh vẽ gần dòng894–921, ngắt trang gần587–597.

Luồng đề xuất (chưa code):

1. Lấy khóa bản dịch và ngữ cảnh từ đối tượng Dialogue, không đoán ngược từ câu đã vẽ.
2. Tìm bản phụ từ đúng nguồn/locale. Bản gốc vanilla trên đĩa là một nguồn; câu mới do CP/C# thêm có thể chỉ tồn tại trong pipeline đang chạy và cần provider/ánh xạ của tác giả.
3. Phân biệt nội dung câu với các lệnh điều khiển: chân dung, cảm xúc, câu hỏi, lựa chọn, hành động, ngắt đoạn và biến tên người chơi.
4. Dựng hai bản văn hiển thị mà không chạy lại tác dụng phụ, lựa chọn ngẫu nhiên, nhận vật phẩm hoặc sự kiện hoàn tất câu.
5. Tính phân trang trên khối hai lớp; đồng bộ nút tiếp tục và hiệu ứng hiện chữ. Không dùng cùng chỉ số ký tự cho hai bản dịch dài khác nhau.

Nếu `TranslationKey` không có hoặc tác giả tạo câu động, mặc định giữ một bản văn. Không tự dịch máy và không bịa khóa. Khóa có sẵn cũng chưa chứng minh template phụ tương ứng đúng câu runtime; cần kiểm tra ngữ cảnh và token.

## 3. DDFC: adapter đã trở thành cần thiết khi mở rộng tới hội thoại

Nguồn: `E:\Sao lưu\Mods_Cần_Tích_Hợp\DialogueDisplayFrameworkContinued-main`.

- `Framework\Patches\DialogueBoxPatches.cs` khoảng93–132 can thiệp vẽ chân dung/khung và `getCurrentString` để ngăn vanilla vẽ trùng.
- `Framework\DialogueBoxRenderer.cs:308` và319 phát sự kiện trước/sau khi vẽ chuỗi.
- `IDialogueDisplayApi.cs:21`–24 công bố `RenderingDialogueString`/`RenderedDialogueString`. `IBaseData` có `Alpha` và `Scale` có thể sửa; `Framework\Api\RenderEventArgs.cs` cung cấp SpriteBatch, DialogueBox và dữ liệu vẽ.

Có điểm nối API đáng khảo sát: điều khiển độ hiện của lần vẽ gốc và vẽ hai lớp trong adapter, dùng hình học thực tế của DDFC. Tuy nhiên, đặt Alpha=0 chỉ ẩn hình, không giải quyết phân trang, chiều cao khung, hiệu ứng chữ hoặc trường hợp lựa chọn. Vì vậy đây là ứng viên proof-of-concept, chưa phải bản tích hợp hoàn chỉnh.

Cần kiểm tra riêng hội thoại thường, câu hỏi/lựa chọn, chân dung trái/phải, hộp thoại không người nói, gamepad và các mod thay chân dung. Không suy từ việc tooltip item tương thích để kết luận hội thoại ruby cũng tương thích.

## 4. Ruby và vai trò SecondaryFirst

Đề xuất cấu trúc dữ liệu giữ vai trò cố định:

- `PrimaryText` + phong cách chính.
- `SecondaryText` + phong cách phụ.
- Vị trí trên/dưới được quyết định riêng bởi `SecondaryFirst`.

Đảo thứ tự không có nghĩa đổi nhầm nguồn font hoặc màu của hai vai trò. Nếu bản phụ là chữ nhỏ, nó vẫn là chữ nhỏ khi đặt lên trên. Một chế độ 'dòng trên luôn lớn' sẽ là chính sách khác và cần chốt riêng trước khi code.

Trình bố trí phải dùng đúng glyph/font khi đo, tính độ cao cả hai lớp mỗi dòng, giữ cặp đoạn khi xuống dòng và phân trang. Với hiệu ứng đánh máy, cần chọn đơn vị hiện chữ theo cụm ký tự/đoạn; Rune không tự giải quyết dấu tổ hợp hay việc căn câu dịch. Không nên chặn mọi DrawString trong toàn game để làm việc này.

## 5. FontSettings và StardewUI

Nguồn FontSettings trong `E:\Sao lưu\Mods_Cần_Tích_Hợp\FontSettings`:

- `Framework\GameFontType.cs` có ba vai trò toàn cục: SmallFont, DialogueFont, SpriteText.
- `Framework\FontPatching\MainFontPatcher.cs` xử lý asset font và các thành phần font hiện hành.
- Chưa thấy `GetApi()` công khai trong ModEntry. Các dịch vụ cấu hình/tạo font được khảo sát là nội bộ.
- `.Cli` là công cụ tạo font, `.Shared` là mã compile-time dùng chung; chúng không tự tạo thêm hai bộ cấu hình font runtime cho UntranslatedStardew.

Vì thế cần thỏa thuận API kiểu 'style/font theo role Primary/Secondary + thông báo đổi cấu hình' hoặc một companion integration được tác giả hỗ trợ. Chưa có lý do cam kết nó đã chạy chỉ nhờ cài FontSettings. Chưa sửa mã FontSettings hoặc thêm cấu hình cỡ/màu trong mod hiện tại.

StardewUI: `E:\Sao lưu\Mods_Cần_Tích_Hợp\StardewUI-master\Core\ViewMenu.cs:24` kế thừa IClickableMenu; dòng356–367 gọi drawToolTip cho nhánh TooltipData chuẩn. Điều này không làm mọi Label/StarML tự có hai bản dịch. Muốn ruby cần widget/layout model hoặc adapter cho consumer cụ thể. `Core\Widgets\Label.cs` gần489–508 xử lý Rune khi ngắt dòng; vẫn phải kiểm tra grapheme và bố trí annotation riêng.

## 6. Ma trận vùng giao diện (nghiên cứu khả năng, không phải danh sách đã hỗ trợ)

| Vùng | Nguồn cục bộ tiêu biểu | Chiến lược khả thi | Ràng buộc |
|---|---|---|---|
| Tooltip item/kỹ năng/quyền hạn/bộ sưu tập | IClickableMenu, SkillsPage, PowersTab, CollectionsPage | Hai tên, wrap theo font; nền tảng đã có | Đã có code riêng nhưng cần kiểm tra từng layout/font runtime |
| Hội thoại | DialogueBox, Dialogue | Khối câu chính/phụ + phân trang | Cần provenance, controlcodes, lựa chọn và adapter DDFC |
| Nội dung thư/ghi chú | LetterViewerMenu.cs gần105,141,926,976 | Hai lớp hoặc phân trang | Quà/nút nhận việc thường ở trang cuối; không coi số trang vô hạn là trải nghiệm tốt |
| Chi tiết nhiệm vụ | QuestLog.cs gần636–657,747–765 | Thêm đoạn trong khung cuộn | Tính lại scroll height, scissor và vị trí nút |
| Danh sách nhiệm vụ | QuestLog.cs gần593–611 | Hover/giữ phím thay vì thêm dòng cố định | Ô và hitbox có kích thước cố định |
| Shop/danh sách chế tạo | ShopMenu.cs gần1906–1927,1993 | Tên chính trong hàng + tooltip hai lớp | Số hàng, giới hạn chiều rộng, icon/giá |
| Tổng kết xuất hàng | ShippingMenu.cs gần891–900 | Hover/chi tiết phụ hoặc thiết kế lại hàng | Bước hàng cố định khoảng68px và căn số tiền |
| Bundle/Junimo | JunimoNoteMenu.cs gần1386–1393 | Hover hoặc bảng chi tiết | Dải tiêu đề thấp, không tự co giãn |
| Lịch/thông báo nhiệm vụ ngày | Billboard.cs gần418–426,463–466 | Hover cho ngày; opt-in/giữ phím cho poster | Poster có nút nhận việc, thiếu cuộn |
| Địa danh bản đồ | MapPage.cs gần283–294,364 | Tooltip co giãn | Nhãn/dải cuộn cố định không nhất thiết đủ hai dòng |
| Cài đặt/nút menu | OptionsPage.cs:48, OptionsElement.cs gần120–136 | Tooltip hoặc chế độ thay thế | Hitbox, dropdown và gamepad phải giữ đúng |
| HUD notifications | HUDMessage.cs gần138–204 | Từng loại thông báo opt-in | Một số khung cố định cao112px, nhiều thông báo xếp chồng |
| Đồng hồ | — | Loại khỏi phạm vi | Theo yêu cầu người dùng |

Các số dòng là chỉ mục để đọc mã bản1.6.15, không phải bằng chứng tương thích mọi phiên bản. 'Có chỗ' là kết luận phải dựa trên font, độ phân giải, UI scale và độ dài bản dịch thực tế; không thể tuyên bố mọi ô cố định đều không thể hoặc mọi khung cuộn đều tự hỗ trợ.

## 7. Lộ trình đề xuất, không tự triển khai

1. Hoàn thiện nền font đo/vẽ đúng glyph cho các vùng hiện có; kiểm tra ma trận locale và đổi font.
2. Proof-of-concept hội thoại vanilla có TranslationKey ổn định, kiểm tra phân trang/controlcodes trước khi hỗ trợ DDFC.
3. Thiết kế một layout hai lớp theo đoạn; chỉ gọi 'ruby căn từ' khi có dữ liệu gắn cặp tương ứng.
4. Chốt API font/style theo vai trò với FontSettings. Chưa thêm tùy biến màu/cỡ vào mod hiện tại.
5. Mở rộng từng adapter UI theo bảng trên, bắt đầu từ khung cuộn/tooltip; không can thiệp mọi chuỗi phẳng.

Tiêu chí chấp nhận: không mất glyph, không thay đổi logic hội thoại/quà/nút bấm, không lộ nội dung chưa mở khóa, hai bản dịch giữ đúng nguồn/ngữ cảnh, không vỡ layout khi SecondaryFirst/locale/font thay đổi. Giao diện của mod C# hay StarML tùy biến có thể cần tích hợp API riêng. Báo cáo này chưa kiểm chứng hình ảnh hoặc chạy hội thoại trong game.
