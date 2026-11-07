# Lệnh kiểm tra hội thoại — preview 19

Xem danh sách bất kỳ lúc nào: `untranslated_dialogue_help`.

## Kiểm tra thay đổi từ preview 18

Nạp save, rồi chạy một lệnh:

```text
untranslated_dialogue_check coverage
```

Gồm 8 ca: 2 luật nguồn từ điển, 3 luật ghép lời dẫn và 3 ca dùng constructor/copy/reparse `Dialogue` thực với Harmony đang cài. Các đối tượng thử riêng không được đưa vào hội thoại NPC hoặc lưu vào save; lệnh không thay đáp án hay tình bạn. Lệnh này kiểm tra logic và điểm nối runtime, không chứng minh bố cục thị giác hay độ phủ mọi khóa dịch.

Có thể chạy riêng `sources` (2 ca), `narration` (3 ca), `copy` (3 ca; cần save). `all` có 37 ca thuần và thêm 3 ca copy khi save đã nạp. Các nhóm cũ `branch`, `state`, `query`, `response`, `question`, `pairs`, `fallback`, `layout` vẫn giữ nguyên.

## Mẫu hiển thị có sẵn

```text
untranslated_dialogue_test [above|below] [wrap|pairs|authored|gender|weekly|state|query|question]
untranslated_dialogue_test [above|below] key <NPC> <key>
```

Lệnh `key` hiện chỉ chọn khóa trong `Characters/Dialogue/<NPC>` và vẫn chặn lệnh không phù hợp để không chạy tác dụng phụ trong preview. Không phải mọi nguồn mới đều có mẫu thị giác riêng. Chọn mẫu `question` để kiểm tra câu hỏi có hai đáp án; chọn đáp án để đóng mẫu.

## Danh sách và lịch sử

Các lệnh của mod được đăng ký lại mỗi lần khởi động SMAPI. Danh sách lệnh không bị xóa qua lần chạy; dùng `untranslated_dialogue_help` để xem lại. Lịch sử ↑/↓ thuộc phiên console và có thể mất khi mở tiến trình mới. File này được kèm trong ZIP dưới tên `UntranslatedStardew/COMMANDS.vi.md` để tra cứu độc lập với console.
