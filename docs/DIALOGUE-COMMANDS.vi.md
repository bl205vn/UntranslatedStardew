# Lệnh kiểm tra hội thoại — preview 29

Preview 24 giữ lệnh cũ nhưng chuyển về bố cục nguyên khối: chữ chính hiện hết trang rồi bản phụ mới hiện. `SecondaryFirst` đặt nguyên khối phụ trên/dưới. Font native vẫn nhỏ 75%. Có thể dùng `untranslated_dialogue_test above wrap` hoặc `untranslated_dialogue_test below wrap` để mở mẫu dài nếu cần; không cần chạy lại từng nhóm PASS.

Preview 23 bổ sung ngắt thoại `$e`, lời dẫn/câu hỏi có `$e` và cache NPC. Các nhóm kiểm tra giữ nguyên số ca và lệnh; không cần nhập lại từng nhóm. Khi chơi bình thường có thể gặp Caroline/Alex để nghiệm thu các ca vừa sửa; câu khuyên tặng quà động của Gus vẫn chưa hỗ trợ.

Preview 25 sửa chỉ số trang sau chọn đáp án (reaction một/nhiều trang) và cảnh báo custom source lặp khi registry refresh. Không thêm command; không cần chạy lại từng nhóm PASS. Nghiệm thu giao diện còn lại là một lượt chọn đáp án rồi đọc hết các trang NPC phản hồi như chơi bình thường.

Preview 26 thêm poster phim đang chiếu/sắp chiếu. Không cần command mới: tương tác poster trong rạp như bình thường, đọc trang tên rồi trang mô tả, nhấn tiếp để đóng. Phim lấy đúng dữ liệu game đã chọn hôm đó. Lượt nghiệm thu gộp này không cần chạy lại các nhóm PASS cũ. Warning nguồn custom chỉ được kết luận sau bước quét nguồn khởi động.

Preview 27 thêm câu hỏi địa điểm có nguồn prompt/label tĩnh (như các điện thờ). Không có command mới và không cần chạy lại từng nhóm PASS. Nếu nghiệm thu trong game, mở một câu hỏi Yes/No, quan sát hai bản và vùng chọn, rồi chọn No hoặc nhấn Escape để đóng; không cần thực hiện hành động có thay đổi save.

Preview 28 sửa phép đo chiều cao Gunther và thêm tên/mô tả hiện vật bảo tàng. Không thêm command: xem hiện vật đã hiến tặng, đọc/đóng hộp rồi đảo SecondaryFirst để thử chung. Không cần chạy lại từng nhóm PASS; không sửa save chỉ để mở câu Gunther. Câu hỏi đổi ngoại hình WizardShrine vẫn chưa hỗ trợ.

Preview 29 sửa mô tả hiện vật dài và lookahead font, thêm adapter event `speak`/`message` khi hai script tương ứng. Không thêm command, không cần chạy lại từng nhóm PASS. Xem Ancient Seed/Ornamental Fan và thử SecondaryFirst; các event thử lúc gặp tự nhiên. Event may đồ Emily trong pack VI đang cài có 19 lệnh so với 26 của EN nên vẫn primary-only; không reset save để thử lại. Xem CONTENT-COVERAGE.vi.md trong gói để biết giới hạn theo thư mục.

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


## Nghiệm thu gộp preview 30

Vào save, đóng menu và gõ `untranslated_dialogue_suite` một lần trong console SMAPI. Bộ mẫu tự mở lần lượt thoại dài và câu hỏi với bản phụ ở trên rồi ở dưới. Đọc hết các trang và chọn đáp án để chuyển ca. Dùng `untranslated_dialogue_suite stop` để dừng chuỗi, sau đó đóng mẫu hiện tại như bình thường.

Trong cùng lượt chơi, nếu gặp các ca sau thì kiểm tra thêm:

- Thoại dài Shane Wed8. Đọc qua hai trang và kiểm tra bản phụ, chân dung, biểu tượng chuyển trang.
- Gợi ý quà của Gus. Kiểm tra hai bản nhắc cùng NPC và vật phẩm.
- Wizard Shrine. Kiểm tra prompt nhiều dòng và chọn No.
- Một event hoặc lễ hội có bản dịch tương ứng. Đọc hoặc chọn đáp án rồi kiểm tra game tiếp tục đúng. Event may đồ Emily 992559 trong pack Việt hiện tại được giữ bản chính do khác script.

Chỉ cần báo ca bị lỗi và ảnh hoặc log kèm theo. Các mẫu console kiểm tra bố cục và thao tác. Các ca gặp tự nhiên kiểm tra đường nguồn thật.


## Chẩn đoán câu thiếu bản phụ

Giữ hộp thoại bị lỗi đang mở, chuyển tới console SMAPI rồi chạy `untranslated_dialogue_status`. Gửi đoạn log nguồn/trang cùng ảnh của câu đó. Lệnh in chẩn đoán, không mở mẫu thử.
