# Lệnh kiểm tra hội thoại — preview 23

Preview 22 giữ các lệnh cũ. Font native và xen hàng tự áp dụng khi bật song ngữ; dùng `untranslated_dialogue_test above wrap` hoặc `untranslated_dialogue_test below wrap` nếu cần mở mẫu dài. Không cần chạy lại từng nhóm PASS. Font/độ vừa khung vẫn cần xem trong game; các ca logic đã được chạy gộp khi build.

Preview 23 bổ sung ngắt thoại `$e`, lời dẫn/câu hỏi có `$e` và cache NPC. Các nhóm kiểm tra giữ nguyên số ca và lệnh; không cần nhập lại từng nhóm. Khi chơi bình thường có thể gặp Caroline/Alex để nghiệm thu các ca vừa sửa; câu khuyên tặng quà động của Gus vẫn chưa hỗ trợ.

Xem danh sách bất kỳ lúc nào: `untranslated_dialogue_help`.

## Kiểm tra thay đổi từ preview 18

Nạp save và chờ nhân vật có thể di chuyển bình thường, đóng menu/sự kiện, rồi chạy một lệnh:

```text
untranslated_dialogue_check coverage
```

Gồm 8 ca: 2 luật nguồn từ điển, 3 luật ghép lời dẫn và 3 ca dùng constructor/copy/reparse `Dialogue` thực với Harmony đang cài. Các đối tượng thử riêng không được đưa vào hội thoại NPC hoặc lưu vào save; lệnh không thay đáp án hay tình bạn. Lệnh này kiểm tra logic và điểm nối runtime, không chứng minh bố cục thị giác hay độ phủ mọi khóa dịch.

Có thể chạy riêng `sources` (2 ca), `narration` (3 ca), `copy` (3 ca; cần save). `qfallback` có 4 ca thuần kiểm tra nhánh fallback câu hỏi. `all` có 41 ca thuần và thêm 3 ca copy khi runtime ổn định. Nếu game chưa ổn định, copy được báo hoãn; `coverage` vẫn in 5 ca thuần, không tính 3 ca chưa chạy là PASS. Trạng thái trước/sau copy và chuyển cảnh trong 5 giây được ghi vào SMAPI log, không thay save hoặc vị trí nhân vật. Các nhóm cũ `branch`, `state`, `query`, `response`, `question`, `pairs`, `fallback`, `layout` vẫn giữ nguyên.

## Mẫu hiển thị có sẵn

```text
untranslated_dialogue_test [above|below] [wrap|pairs|authored|gender|weekly|state|query|question]
untranslated_dialogue_test [above|below] key <NPC> <key>
```

Lệnh `key` hiện chỉ chọn khóa trong `Characters/Dialogue/<NPC>` và vẫn chặn lệnh không phù hợp để không chạy tác dụng phụ trong preview. Không phải mọi nguồn mới đều có mẫu thị giác riêng. Chọn mẫu `question` để kiểm tra câu hỏi có hai đáp án; chọn đáp án để đóng mẫu.

## Danh sách và lịch sử

Các lệnh của mod được đăng ký lại mỗi lần khởi động SMAPI. Danh sách lệnh không bị xóa qua lần chạy; dùng `untranslated_dialogue_help` để xem lại. Lịch sử ↑/↓ thuộc phiên console và có thể mất khi mở tiến trình mới. File này được kèm trong ZIP dưới tên `UntranslatedStardew/COMMANDS.vi.md` để tra cứu độc lập với console.

## Bản 21 và các hộp thông tin thực tế

Không thêm lệnh mẫu nhỏ. Adapter tự áp dụng cho hộp thông tin có nguồn xác định trong lúc chơi. `coverage` là kiểm tra logic/runtime, không mở hộp thoại mẫu và không xác nhận tất cả GUI đã có bản phụ. Đợt nghiệm thu M2 được gom trong kế hoạch phát triển; các mẫu câu hỏi cũ không cần chạy lại.
