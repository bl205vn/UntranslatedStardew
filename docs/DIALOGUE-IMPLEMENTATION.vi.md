# Trạng thái triển khai hội thoại song ngữ

Mốc: dialogue preview 19, Stardew Valley 1.6.15. Đây là trạng thái code; chưa thay thế nghiệm thu trong game.

## Đã nối vào code

- Thoại NPC có khóa nguồn `asset:key` thuộc danh mục từ điển hội thoại vanilla được `RawContentProvider` hỗ trợ (`Characters/Dialogue/*`, `Strings/schedules/*`, `Data/ExtraDialogue`, `Data/EngagementDialogue`, `Strings/Characters`, `Strings/Events`, `Strings/StringsFromCSFiles`, `Strings/Locations`, `Strings/1_6_Strings`, `Strings/StringsFromMaps`, `Strings/SimpleNonVillagerDialogues`, `Strings/animationDescriptions`, `Strings/SpeechBubbles`): bản phụ nhỏ, `SecondaryFirst`, ghép khối do tác giả định nghĩa, xuống dòng, hiệu ứng hiện chữ và fallback khi không vừa.
- Từ chối đường dẫn tùy ý, path traversal và các asset `Data/*` có kiểu dữ liệu riêng hoặc dạng bảng/script (`Data/IncomingPhoneCalls`, `Data/Objects`, `Data/Quests`, `Data/Events/*`, `Data/Festivals/*`, `Data/mail`). Tiếp tục yêu cầu đối chiếu khớp chính xác câu chính đang hiển thị.
- Giới tính `${...^...}$`, dấu phân cách cũ, biến `@`, luân phiên tuần `||`; nhánh `$d`, `$query` được đối chiếu với câu chính đang hiện. `$p` dùng lịch sử đáp án của chính đối tượng người chơi rồi kiểm tra lại câu chính.
- Câu hỏi `$q` với các đáp án `$r` dạng phẳng, có thể có lời mở đầu thuần trước câu hỏi. Kiểm tra cả header, thứ tự và nhãn đáp án đang sống. Ghép bằng khóa câu phản hồi `Response.responseKey`; ID ghi nhớ có thể trùng giữa các đáp án.
- Một phép đo chiều cao cho khung, highlight, hover chuột và vùng chọn gamepad. Mỗi đáp án gồm hai bản văn và giữ một vùng chọn vanilla. Khi toàn bộ câu hỏi không vừa chiều cao, giữ nguyên câu hỏi vanilla.
- Theo dõi khóa nguồn hiện hành khi vanilla gọi lại `Dialogue.parseDialogueString` sau một lựa chọn và khi sao chép `new Dialogue(Dialogue)`. Bản sao giữ nguyên khóa phản hồi mới hoặc trạng thái xóa khóa `null` chủ đích, không quay về `TranslationKey` câu hỏi ban đầu.

Bản phụ được đọc như dữ liệu. Mod không chạy parser hội thoại thứ hai, không tự chọn đáp án, không áp lại điểm tình bạn hoặc lệnh hành động. Mẫu console dùng callback trả lời riêng để đóng khung trước các tác dụng phụ vanilla.

## Giới hạn hiện tại

- Chỉ thoại/câu hỏi NPC có khóa nguồn từ điển hợp lệ và khớp nguyên văn câu chính đang hiển thị (hoặc mẫu console) mới gắn bản phụ. Thoại đã qua tiền xử lý làm mất `TranslationKey` hoặc đã thay thế tham số `{0}`/`%endearment` trước khi tạo `Dialogue` giữ nguyên vanilla.
- Dù `Strings/Events` nằm trong danh mục từ điển đọc được khi thoại thường dùng khóa này, renderer vẫn chặn cứng `!Game1.eventUp`; hội thoại trong event UI, câu hỏi địa điểm, shop và câu hỏi do mod tạo động vẫn chưa hỗ trợ.
- Nhánh điều kiện bao quanh câu hỏi, câu hỏi lồng nhau, lệnh trong nhãn và cấu trúc không tương ứng giữ vanilla. Lời dẫn thuần trước `$q` đã được ghép theo chỉ số đoạn và cấu trúc câu hỏi/đáp án tương ứng; câu đang hiện vẫn phải khớp nguyên văn.
- Đường `$q` đã trả lời và chèn nội dung từ khóa fallback ngay trong cùng lần parse chưa có ánh xạ riêng. Câu phản hồi được vanilla nạp qua lần parse mới thì theo dõi được khóa.
- Câu hỏi có nhãn chính giống nhau nhưng bản phụ khác nhau giữ vanilla, vì call site đo chiều cao chỉ mang chuỗi chữ.
- DDFC chưa được hỗ trợ. Chưa mở rộng thư, bong bóng, bundle, xây dựng hoặc nhiệm vụ bằng bản thay đổi này.

## Kiểm tra theo nhóm

Danh sách đầy đủ và kiểm tra coverage mới: [Lệnh console](DIALOGUE-COMMANDS.vi.md). `untranslated_dialogue_check coverage` kiểm tra gộp nguồn, lời dẫn và sao chép khóa sau khi nạp save; `untranslated_dialogue_help` in danh sách lệnh.

`untranslated_dialogue_check all` gom kiểm tra quy tắc; `question` là nhóm mới cho câu hỏi, gồm ghép nguồn, header, ID chung, tuần, lệnh không phù hợp, bố cục và overflow. Các nhóm cũ vẫn dùng được. Không cần chạy từng nhóm cũ sau mỗi bản.

Mẫu thị giác gom kiểm tra câu hỏi:

```text
untranslated_dialogue_test above question
untranslated_dialogue_test below question
```

Mẫu lấy cặp câu Evelyn Monday đã giải theo đúng ngôn ngữ chính/phụ đang cấu hình, dùng một câu ngắn cho prompt và một đáp án lặp dài hơn. Nếu không vừa, giảm số lần lặp từ 3 xuống 1; không giảm cỡ font. Mẫu không cố định tiếng Việt ở vai trò phụ. Chọn đáp án để đóng. Lỗi thiếu glyph nêu ngôn ngữ và mã Unicode; lỗi overflow nêu chiều cao cần và ngân sách khung. Cần xác nhận trong game: chữ/hộp highlight/vùng bấm cùng hàng, chọn bằng gamepad, chữ xuống dòng và cả hai vị trí trên/dưới. Build, kiểm tra logic và kiểm tra Harmony call site không chứng minh được chất lượng hình ảnh.

## Căn cứ sử dụng lại

- [Khảo sát bề mặt 1.6.15](BILINGUAL-SURFACE-RESEARCH-1.6.15.vi.md), phần hội thoại và lựa chọn.
- [Nghiên cứu mở rộng](ADVANCED-BILINGUAL-RESEARCH.vi.md), phần provenance, tác dụng phụ và bố cục.
- [Wiki Dialogue](https://stardewvalleywiki.com/Modding%3ADialogue), phần Dialogue commands, Question example và Response IDs.
- Mã cục bộ `Dialogue.parseDialogueString`, `chooseResponse`, `DialogueBox.setUpQuestions`, `draw`, `performHoverAction`, `update` của 1.6.15 được dùng để chốt hành vi. AGY rà soát thiết kế; các mô tả call site khác mã cục bộ không được áp dụng.
