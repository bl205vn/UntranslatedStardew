# Kế hoạch phát triển và nghiệm thu song ngữ Stardew Valley

Cập nhật: 07/11/2026. Repo: `UntranslatedStardew`. Nhánh: `Full-song-ngữ-cả-game`.

Đây là kế hoạch làm việc chính cho các lượt phát triển tiếp theo. Cập nhật trạng thái và bằng chứng ngay trong file này; không tạo lại kế hoạch mỗi lượt. Không tự công bố release hoặc gộp vào main.

## Mục tiêu và cách giữ trải nghiệm đã duyệt

Mở rộng song ngữ tới gần như toàn bộ chữ cung cấp nội dung/thông tin trong game. Ngôn ngữ chính giữ vai trò chính; bản phụ nhỏ hơn, `SecondaryFirst` quyết định trên/dưới. Mỗi cặp câu/khối giữ cùng nhau khi xuống dòng. Không suy ra căn từng từ giữa hai bản dịch nếu thiếu dữ liệu ghép rõ.

Bản chính sở hữu nhánh, ngẫu nhiên, lựa chọn, hành động, phần thưởng và tiến độ. Bản phụ chỉ lấy văn bản từ cùng nguồn/ngữ cảnh. Khi chưa xác định được nguồn hoặc cấu trúc không tương ứng, giữ cách hiển thị vanilla cho đoạn đó. Đo chữ bằng đúng font dùng để vẽ; vùng bấm, highlight, gamepad và vùng cuộn phải theo cùng bố cục.

Không yêu cầu mọi bề mặt phải chen thêm hai dòng cố định: vùng hẹp có thể dùng chi tiết/tooltip khi hover hoặc focus, nhưng phải ghi rõ hành vi và nghiệm thu riêng.

## Bằng chứng hiện có — tránh kiểm tra lại vô ích

| Phần | Bằng chứng | Phạm vi kết luận |
|---|---|---|
| Build và bộ test preview 20 | 1005 đạt, 0 lỗi | Logic/offline và một số điểm nối Harmony; không phải xác nhận toàn bộ GUI |
| `coverage` | Người dùng chạy trong game: 8/8 đạt | Luật nguồn, lời dẫn, constructor/copy/reparse thật và giữ/xóa khóa nguồn |
| Câu hỏi song ngữ | Hai ảnh người dùng ở preview 17 | Mở được mẫu, đảo trên/dưới, đáp án dài xuống dòng, highlight bao hai ngôn ngữ |
| Các quy tắc thoại trước đó | Log PASS đã gửi: gender/weekly/state/query/response/pairs/fallback/layout | Giữ làm kiểm tra hồi quy tự động; chỉ thử lại khi thay đổi đường xử lý tương ứng |
| Nguồn thoại mở rộng | Preview 18–19 đã có code và kiểm tra dữ liệu cài đặt | Nguồn đọc được không đồng nghĩa mọi nơi sử dụng nguồn đó đã hỗ trợ song ngữ |
| Cutscene/dịch chuyển sau `coverage` | Người dùng báo; log có `Warping to Farm` sau lệnh | Chưa biết nguyên nhân; chưa đóng vấn đề |

Chưa xác nhận đầy đủ: chọn đáp án bằng gamepad, nhánh `$q` đã trả lời chèn fallback, event UI, bong bóng, tên NPC song ngữ, các GUI chuyên biệt phía dưới, tương thích DDFC.

## Quy trình một đợt triển khai

1. Chọn một mốc có đầu ra rõ; đọc phần nghiên cứu liên quan và code hiện tại.
2. Giao Antigravity phần việc cụ thể gồm sửa code, kiểm tra và báo cáo. Codex giữ quyết định thiết kế, tích hợp, rà soát và bàn giao. Việc nhỏ, gắn chặt ngữ cảnh được xử lý trực tiếp.
3. Chỗ chưa chắc: đối chiếu wiki/modding docs với mã 1.6.15. Dẫn nguồn cho kết luận mới; không khảo sát lại toàn bộ game. Không dùng kết luận AGY chưa kiểm chứng để thay hành vi đã duyệt.
4. Kiểm tra logic/điểm nối cần thiết, sửa lỗi rồi đóng gói một bản cho cả mốc. Không phát hành một preview cho mỗi token.
5. Chỉ nhờ người dùng thử những hành vi cần quan sát trong game. Mỗi bản có một danh sách ngắn, nêu cách mở, kết quả mong đợi và cách thoát. Không yêu cầu gửi lại hàng loạt PASS cũ.
6. Ghi kết quả vào bảng tiến độ. Chỉ chuyển sang “nghiệm thu” khi có bằng chứng tương ứng; nếu bị chặn, ghi điều kiện cụ thể và chuyển sang việc độc lập có thể làm.

Khi AGY chạy lâu, thu kết quả bằng lệnh wait; không liên tục đọc log trung gian. Nếu giao việc quá rộng và không tiến đến đầu ra, điều chỉnh phạm vi và giữ lại phần đã làm. Không biến một lượt sửa nhỏ thành nhiều vòng bàn giao qua lại. Chỉ chạy lại kiểm tra khi có thay đổi, lỗi hoặc nghi vấn mới.

## Thứ tự thực hiện

Trạng thái dùng: **Chưa làm → Đang làm → Đã code → Đã kiểm tra tự động → Chờ nghiệm thu → Đã nghiệm thu**. Một mốc có thể chỉ hoàn thành một phần; ghi rõ phần còn lại.

| Mốc | Phạm vi và đầu ra | Điều kiện hoàn tất | Trạng thái |
|---|---|---|---|
| M0 | Xác minh cutscene/dịch chuyển sau `coverage`; bảo đảm lệnh thử không thay trạng thái chơi | Có nguyên nhân hoặc phép tái hiện đủ phân biệt quan hệ nhân quả; nếu sửa, có hồi quy phù hợp và xác nhận hành vi | Đã code/đã kiểm tra phần chẩn đoán preview 20; nguyên nhân warp chưa xác định |
| M1 | Chốt hội thoại NPC: lời dẫn, phản hồi, `$q` đã trả lời/fallback, nguồn và phân trang còn thiếu | Ghép đúng khóa/chỉ số đoạn; không chạy lại tác dụng phụ; trường hợp chưa hỗ trợ được ghi và fallback rõ | Đã code/đã kiểm tra fallback thuần và $p phẳng ở preview 20; chưa nghiệm thu runtime nhánh mới |
| M2 | Hộp thông tin không chân dung và thoại event/lễ hội: `speak`, `message`, câu hỏi | Adapter giữ asset/event/command và ngữ cảnh; câu hỏi dùng đúng ID; event tiếp tục và kết thúc đúng | Chưa làm; dùng lại khảo sát có sẵn |
| M3 | Tên NPC trong biển tên, danh sách xã hội và hồ sơ; bong bóng trên đầu NPC | Không lộ tên `???`; không trùng tên; bong bóng dùng chung timer/fade và nằm trong màn hình | Chưa làm |
| M4 | Thư, ghi chú và các trang đọc | Đọc được hai ngôn ngữ; phân trang/điều hướng đúng; nhận quà và nút nhận việc chỉ thực hiện một lần | Chưa làm |
| M5 | QuestLog, bảng giao việc thường và Special Orders/Qi | Tiêu đề/mô tả/mục tiêu/phần thưởng đúng nguồn; vùng cuộn, nút nhận/hủy và trạng thái tiến độ đúng | Chưa làm |
| M6 | Shop: tên/mô tả món, tiêu đề và câu dưới chân dung người bán | Giữ đúng câu/item vanilla đã chọn; vùng giá/số lượng/nút mua không bị che; hover và gamepad dùng được | Một phần tooltip nền đã có; adapter shop chưa nghiệm thu |
| M7 | Bundle Nhà Văn Hóa/Joja và yêu cầu raccoon, gồm shop của vợ raccoon | Kiểm tra riêng từng đường GUI; yêu cầu, vật phẩm, hover và nhận thưởng không đổi tiến độ ngoài thao tác người chơi | Chưa làm |
| M8 | Xây dựng/nâng cấp công trình và rạp phim | Chuỗi từ dữ liệu có schema rõ; chi phí/điều kiện/nút mua-xây và cảnh chiếu không bị ảnh hưởng | Chưa làm |
| M9 | GUI/HUD phụ: kỹ năng, bộ sưu tập, crafting/cooking, shipping, bản đồ, powers, toast, TV/điện thoại và thông báo hệ thống | Kiểm kê theo renderer; ghi nơi tái dùng tooltip và nơi cần adapter; hàng đợi/icon/số lượng, focus và hitbox đúng | Có nghiên cứu; độ phủ triển khai cần kiểm kê |
| M10 | Minigame và màn phụ: Junimo Kart, Journey of the Prairie King, menu vào/thoát/điểm số và các màn còn lại trong khảo sát | Phân biệt chữ runtime với chữ nằm trong texture; kiểm tra bắt đầu/kết thúc/thử lại; gameplay không bị đổi | Có ảnh/thông tin nghiên cứu; chưa nghiệm thu song ngữ |
| M11 | Tương thích và bản tích hợp | Kiểm tra font hai vai trò, Content Patcher reload, DDFC adapter, UI scale/zoom và các cấu hình mod thực tế cần hỗ trợ | Thực hiện dần; tổng hợp sau các mốc |

Đồng hồ/thứ/ngày vẫn ưu tiên thấp, tùy chọn riêng nếu làm; không tự bỏ khỏi mục tiêu song ngữ và không chen vào trước các mốc nội dung. Chữ trong logo/texture cần phương án asset riêng nếu đưa vào phạm vi, không đánh dấu hoàn thành nhờ renderer chữ.

## Kế hoạch kiểm tra dùng chung

### Phần Codex/Antigravity tự làm

- Nguồn: đúng asset/key/field/ID; locale phụ không âm thầm lấy nhầm ngôn ngữ; dữ liệu thiếu, sai schema, stale hoặc bị CP sửa thì phản ứng rõ.
- Parser: nhánh đúng, placeholder, ranh giới đoạn, header/ID khớp; không chạy parser/lệnh/hành động của bản phụ; kiểm tra nhánh không hoạt động khi có thể gây ghép sai.
- Bố cục: chuỗi ngắn, dài và từ dài không có dấu cách; tiếng Việt có dấu, xuống dòng, overflow và hai giá trị `SecondaryFirst`; đo và vẽ cùng font.
- Điểm nối runtime: constructor/copy, đổi màn/đoạn, invalidation/reload, đường Harmony và vùng chọn/scroll. Có kiểm tra hồi quy cho lỗi thực tế đã sửa.
- Chạy build và bộ kiểm tra liên quan một lần sau khi hoàn tất nhóm thay đổi; mở rộng kiểm tra khi có rủi ro cụ thể. Chứng minh rõ phần nào dùng dữ liệu/font thật, phần nào chỉ dùng fixture.

### Phần cần quan sát trong game

Mỗi renderer mới chọn một ca đại diện có nội dung dài và tương tác thật. Thử cấu hình đang dùng của người dùng trước; thêm độ phân giải/UI scale khác khi hình học vừa thay đổi. Không yêu cầu tích số mọi token × mọi GUI × mọi độ phân giải.

Một phiếu thử cần có:

- Bản ZIP và mốc; chỉ dẫn ngắn hoặc một lệnh mở chuỗi ca.
- 3–5 hành vi quan sát cụ thể, gồm thao tác tiếp/chọn/thoát và kết quả mong đợi.
- Các trạng thái cần đọc trước/sau nếu có tác dụng phụ: vị trí nhân vật, event, tình bạn, lịch sử đáp án, nhiệm vụ, tiền/vật phẩm hoặc nhận quà.
- Kết quả: đạt, lỗi, chưa thử. Ảnh chứng minh bố cục; log chứng minh đường chạy; không dùng một loại để thay cho loại kia.

Không dùng command có chủ đích warp, đặt cờ, tặng vật phẩm hoặc thay tiến độ chỉ để mở mẫu nếu chưa được người dùng cho phép. Mẫu thử phải kết thúc/đóng được và không tiêu thụ hàng đợi hội thoại NPC thật.

## M0 — nhật ký điều tra ban đầu

- Người dùng đã chạy `coverage` và nhận 8/8 PASS.
- Log phiên đã đọc: save nạp lúc **14:32:43**, `coverage` lúc **14:32:45**, `Warping to Farm` lúc **14:32:48**, `untranslated_dialogue_help` lúc **14:33:03** (giờ ghi trong log).
- `DialogueRuntimeChecks.RunCopies` hiện chỉ tạo NPC thử, dựng/copy/reparse các câu thuần `Hello.`, `Reaction.`, `Unkeyed.` rồi đọc khóa. Chưa thấy lệnh warp hoặc gọi event trực tiếp trong hàm này.
- Các dữ kiện trên chỉ xác nhận thứ tự thời gian; chưa chứng minh command gây warp hoặc warp độc lập với command. Không đóng vấn đề dựa trên 8/8 PASS.
- Bước tiếp theo: lần theo constructor/parser và các hook đang cài; kiểm tra command có đi qua event/queue hay trạng thái đang chờ từ lúc nạp save. Nếu chưa tách được, bổ sung chẩn đoán trước/sau command không sửa save, rồi thực hiện một phép thử gộp có kiểm soát khi thật sự cần.

## Tiết kiệm hạn mức và quy tắc bàn giao

- Ưu tiên AGY cho phần triển khai lớn có đầu ra rõ; Codex rà soát các quyết định và diff trọng yếu.
- Một mốc, một bản bàn giao khi có thay đổi đáng kể. Không tạo thêm command/preview cho mỗi trường hợp thuần logic.
- Bảo toàn các command cũ; dùng `untranslated_dialogue_help` và file hướng dẫn để tra cứu. Lệnh mới phải nêu rõ chỉ in console hay mở giao diện.
- Báo cáo ngắn: đã thay gì, bằng chứng kiểm tra, giới hạn còn lại và đúng một việc người dùng cần làm nếu có. Không yêu cầu người dùng thử để thay cho việc tự kiểm tra code.
- Chưa có lịch hoàn tất cam kết. Ưu tiên theo mốc và rủi ro; không đánh dấu phần còn lại hoàn thành để khớp thời hạn gói dịch vụ.

## Tài liệu đầu vào và nơi cập nhật

- [Trạng thái hội thoại](DIALOGUE-IMPLEMENTATION.vi.md): chi tiết phạm vi code đang chạy và giới hạn.
- [Lệnh kiểm tra](DIALOGUE-COMMANDS.vi.md): danh sách lệnh và tác động của từng nhóm.
- [Khảo sát bề mặt 1.6.15](BILINGUAL-SURFACE-RESEARCH-1.6.15.vi.md): nguồn/renderer của từng màn, gồm raccoon, event, shop và minigame.
- [Nghiên cứu mở rộng](ADVANCED-BILINGUAL-RESEARCH.vi.md): provenance, font, ruby liên dòng và DDFC.
- [UI coverage](UI-COVERAGE.md): đối chiếu độ phủ GUI hiện có; không mặc định coi mọi mục là đã nghiệm thu.
- Wiki được dẫn trong các tài liệu trên; khi bổ sung kết luận, ghi trang liên quan và phiên bản mã đã đối chiếu.

Mỗi lần hoàn thành mốc, cập nhật bảng tiến độ, bằng chứng, ca đã đạt và bước tiếp theo tại file này. Các tài liệu khảo sát là đầu vào lịch sử; trạng thái triển khai được cập nhật ở đây và tài liệu trạng thái chuyên biệt.

## Nhật ký triển khai — preview 20

- **M0:** Antigravity đã rà soát độc lập đường gọi đã cung cấp, không tìm thấy warp trực tiếp trong mẫu câu thuần; đây là rà soát có phạm vi, chưa loại trừ hook của mod khác. Lệnh copy chỉ chạy khi world đã nạp, người chơi có thể di chuyển, không có event/menu/minigame/fade/warp hoặc hội thoại chờ. Nếu chưa ổn định, báo hoãn copy và vẫn in kết quả các luật thuần; không ghi PASS cho ca runtime chưa chạy.
- Ghi trạng thái trước/sau `RunCopies` vào SMAPI log; quan sát tối đa 5 giây qua UpdateTicked, ghi thay đổi location/event/fade/menu/warp cùng tọa độ. Chuyển cảnh muộn chỉ là dữ kiện thời gian, không gán nguyên nhân cho command. Không restore vị trí, không sửa event hoặc save. Observer tự tháo khi hết thời gian, world thoát hoặc mod dispose.
- **M1:** Resolver mới đối chiếu header `$q/$r`, dùng lịch sử đáp án chính để chọn khóa fallback, đọc câu chính từ chính `NPC.Dialogue` mà vanilla dùng và bản phụ từ nguồn locale nghiêm ngặt theo `LoadedDialogueKey`. Trừ số đoạn lời dẫn khỏi chỉ số runtime. Chỉ hỗ trợ fallback câu thuần nhiều trang hoặc `$p` phẳng; không chạy lại weekly/query, không lồng câu hỏi hay thực thi lệnh ở bản phụ.
- Đã thêm hồi quy cho offset, nhiều trang, giới tính, weekly của câu cha, `-1`, `null`, chưa trả lời, header không khớp, thiếu dịch, lệnh item/action, câu hỏi lồng và `$p` cùng chữ khác nhánh; kiểm tra observer phân biệt đi bộ với chuyển location/event.
- Build thành công, **1005/1005** kiểm tra tự động đạt. `qfallback` là nhóm console mới gồm 4 ca thuần; `coverage` giữ 8 ca khi runtime đủ điều kiện. Không yêu cầu người dùng chạy lại nhóm cũ ở mốc này.
- **Chưa đóng M0**, chưa xác nhận runtime/ảnh của fallback mới. Bước tiếp: thu bằng chứng M0 khi có log từ tình huống thực; M2 (event/hộp thông tin) có thể chuẩn bị độc lập dựa trên khảo sát sẵn có. Không coi việc đọc được `Strings/Events` là đã tích hợp renderer event.
