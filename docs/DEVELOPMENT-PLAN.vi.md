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
| Build và bộ test preview 21 | 1015 đạt, 0 lỗi | Logic/offline và một số điểm nối Harmony; không phải xác nhận toàn bộ GUI |
| `coverage` | Người dùng chạy trong game: 8/8 đạt | Luật nguồn, lời dẫn, constructor/copy/reparse thật và giữ/xóa khóa nguồn |
| Câu hỏi song ngữ | Hai ảnh người dùng ở preview 17 | Mở được mẫu, đảo trên/dưới, đáp án dài xuống dòng, highlight bao hai ngôn ngữ |
| Các quy tắc thoại trước đó | Log PASS đã gửi: gender/weekly/state/query/response/pairs/fallback/layout | Giữ làm kiểm tra hồi quy tự động; chỉ thử lại khi thay đổi đường xử lý tương ứng |
| Nguồn thoại mở rộng | Preview 18–19 đã có code và kiểm tra dữ liệu cài đặt | Nguồn đọc được không đồng nghĩa mọi nơi sử dụng nguồn đó đã hỗ trợ song ngữ |
| Cutscene/dịch chuyển sau `coverage` | Người dùng xác nhận cầm Return Scepter; snapshot trước/sau lệnh `unchanged=True` | Đóng M0 theo xác nhận người dùng; không quy lỗi warp cho command |

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
| M0 | Xác minh cutscene/dịch chuyển sau `coverage`; bảo đảm lệnh thử không thay trạng thái chơi | Có nguyên nhân hoặc phép tái hiện đủ phân biệt quan hệ nhân quả; nếu sửa, có hồi quy phù hợp và xác nhận hành vi | Đã đóng: người dùng xác nhận Return Scepter; trạng thái tức thời không đổi |
| M1 | Chốt hội thoại NPC: lời dẫn, phản hồi, `$q` đã trả lời/fallback, nguồn và phân trang còn thiếu | Ghép đúng khóa/chỉ số đoạn; không chạy lại tác dụng phụ; trường hợp chưa hỗ trợ được ghi và fallback rõ | Preview 30 thêm khung thoại dài và nguồn gợi ý quà. Chờ nghiệm thu trong game |
| M2 | Hộp thông tin không chân dung và thoại event/lễ hội: `speak`, `message`, câu hỏi | Adapter giữ asset/event/command và ngữ cảnh; câu hỏi dùng đúng ID; event tiếp tục và kết thúc đúng | Preview 30 thêm câu hỏi event, nhánh script có nguồn, thoại lễ hội và câu hỏi Wizard Shrine. Chờ nghiệm thu trong game |
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

## Nhật ký triển khai — preview 21

- **M0 đóng:** người dùng xác nhận nguyên nhân là cầm Return Scepter khi thao tác console. Snapshot `before`/`immediate after` không đổi. Không cần yêu cầu thử lại M0.
- **M2, phần hộp thông tin:** theo dõi chính instance chuỗi trả về từ `Game1.content.LoadStringReturnNullIfNotFound(path, bool)`. Không dò ngược từ chữ đang hiện. Instance chuỗi có nhiều khóa nguồn bị coi là mơ hồ và giữ vanilla; đổi locale/content làm mất hiệu lực nguồn cũ.
- `DialogueBox(string)` được gắn các trang bản phụ sau khi đối chiếu nguyên chuỗi chính với dictionary. Mỗi trang `#` có vị trí riêng, kể cả trang trống. `^` trong loại khung này là xuống dòng hiển thị; không được diễn giải như dấu phân cách giới tính cũ của thoại NPC.
- Khung thông tin tăng chiều cao theo tổng hai bản văn, giữ đáy khung, sử dụng font chính/phụ đúng vai trò và `SecondaryFirst`. Click chỉ hiện hết chữ không làm nhảy bản dịch sang trang sau. Nếu thiếu glyph, thiếu bản dịch, đổi nội dung hoặc không vừa vùng màn hình thì giữ vanilla.
- **M2, NPC thiếu ảnh:** bỏ điều kiện vô tình chỉ cho phép NPC có portrait khi tùy chọn portrait đang bật. Với literal `Dialogue` không có key nhưng giữ đúng instance chuỗi vừa nạp, có thể khôi phục nguồn đã theo dõi; các lần reparse xóa key vẫn giữ ngữ nghĩa riêng.
- Đã kiểm tra hồi quy: identity khác nhưng cùng chữ không ghép; nguồn chia sẻ/mơ hồ; nguồn không hỗ trợ; invalidation; page count; xuống dòng; giới tính; template, item/command; chữ chính bị sửa; trang trống; chữ ký Harmony thật. **1015/1015 đạt**, build thành công.
- Antigravity rà soát thiết kế; các rủi ro template, chuỗi chia sẻ, click chỉ reveal và hình học được đối chiếu với code. Chưa nghiệm thu thị giác đường khung mới. Event/câu hỏi địa điểm/shop, chuỗi format/thao tác nối làm mất instance nguồn và `DialogueBox(List<string>)` vẫn chưa có adapter.
- **Phiếu nghiệm thu gộp M2:** trong một lượt chơi bình thường, kiểm tra hộp thông tin không chân dung (ưu tiên câu trong rạp phim vừa báo), chuyển tới trang sau nếu có và đóng khung. Quan sát hai ngôn ngữ, khung chứa đủ chữ và việc tiếp/thoát vẫn đúng. Không yêu cầu chạy lại log PASS từng token hoặc dựng lại ảnh mẫu câu hỏi cũ. Chỉ thực hiện khi nghiệm thu bản giao diện này; không phải điều kiện để chuẩn bị phần event độc lập.

## Khảo sát font sau preview 21

- Người dùng gửi ảnh ba hộp thoại trong rạp ở cả chiều Việt–Anh và Anh–Việt: đã thấy bản phụ và khung chứa chữ. Đây là nghiệm thu thị giác các ca đã gửi, không phải toàn bộ event/GUI.
- Đã hoàn tất [bảng font theo locale/vai trò](FONT-MATRIX-1.6.15.vi.md) và inventory asset. M11 có thêm căn cứ về SpriteText/font_bold/BMFont, SpriteFont1, SmallFont, tinyFont, Nga alternate, Trung smooth và pack Việt Font1/Font2.
- Khoảng trống fidelity: dòng phụ hội thoại hiện ưu tiên SpriteFont1 ở scale 0.75 thay vì renderer native SpriteText của locale phụ. Cần dùng bảng vai trò làm căn cứ trước khi thay renderer; chưa đổi font trong lượt nghiên cứu. Không yêu cầu người dùng chạy lại toàn bộ ca cũ.

## Nhật ký triển khai — preview 22

- **M11, font hội thoại:** Antigravity triển khai provider/renderer native; Codex tích hợp và rà soát. Dòng phụ dùng font hội thoại của chính locale phụ, scale 0.75: Latin sprite sheet, BMFont Nga alternate/Nhật/Hàn/Trung, tùy chọn Trung smooth/scale và custom language khai báo UseGameLatinFont hoặc BitmapFontAsset/BitmapFontZoom. Không đổi locale hay trạng thái SpriteText toàn cục. Cache làm mất hiệu lực khi đổi nguồn/content; texture do content manager quản lý không bị dispose.
- Icon của SpriteText dùng sheet native riêng, kể cả khi chữ dùng BMFont. Font không có/thiếu glyph giữ vanilla; không âm thầm đổi sang SpriteFont1. Thai không có asset vanilla trong bản cài, custom chỉ khai báo font UI chưa đủ để suy ra font hội thoại.
- **M1/M2, xuống dòng:** NPC, hộp thông tin và câu hỏi dùng chung layout xen hàng. Mỗi khối bản dịch được wrap theo font/chiều rộng thật; hàng phụ xuất hiện khi hàng chính tương ứng hiện xong. SecondaryFirst đảo vị trí từng cặp hàng. Số hàng hai bản có thể khác nhau; hàng dư vẫn thuộc khối gốc, không suy đoán dịch từng từ. Nếu không vừa khung thì fallback nguyên trang.
- Đã thêm 65 kiểm tra font/policy/glyph/icon/BMFont và 12 kiểm tra xen hàng, source offset, reveal, overflow, câu hỏi. Tổng **1092/1092 đạt**, build thành công. Nhóm console layout dùng layout mới. Chưa chạy renderer GPU trong game; không coi số PASS là nghiệm thu hình ảnh mọi locale.
- Phạm vi chưa mở rộng sang mọi hội thoại: event, nguồn không nhận diện, chuỗi format/mất provenance, DDFC và các trang NPC vanilla đã chia nhỏ vẫn có thể fallback. Bước tiếp là adapter event dựa trên tài liệu khảo sát, sau khi xác nhận bố cục native trên một lượt chơi gộp. Không cần chạy lại từng nhóm token.

## Nhật ký triển khai — preview 23

- **Nghiệm thu preview 22 từ ảnh người dùng:** font native phụ và xen hàng đã xuất hiện ở NPC có chân dung (Vincent/Kent), hộp trong rạp (người phụ nữ/Simon/quầy đồ ăn) và bảng chỉ đường. Khi hai bản khác số hàng, hàng cuối có thể chỉ có một ngôn ngữ; đây là bố cục xen theo hàng wrap, không phải căn chỉnh nghĩa từng từ. Ảnh cũng chỉ ra Caroline, câu hỏi Alex và câu khuyên tặng quà của Gus chưa có bản phụ.
- **M1, ngắt thoại `$e`:** Antigravity đối chiếu dữ liệu game và pack Việt hóa cùng parser 1.6.15. Caroline `Wed6` dùng `#$e#`; Alex `Wed` có lời dẫn `#$e#` trước `$q`. Resolver cũ chỉ tách `#$b#`, hoặc đếm `$e` thành lời dẫn nên từ chối. Đã nhận cả hai loại ở tách trang/nhánh/preview an toàn và lọc đúng lời dẫn câu hỏi. Cấu trúc lời dẫn câu hỏi vẫn phân biệt `$e`/`$b`; không đổi ngắt thoại hoặc chạy lại lệnh ở vanilla.
- Hồi quy dùng nguyên chuỗi Caroline, Alex (prompt nam/nữ, ba response key, lời dẫn), Demetrius có cả `$e`/`$b`, và fallback tham số. Không thêm lệnh kiểm tra riêng cho từng token; nhóm `pairs`/`question` có thêm tình huống `$e` trong ca sẵn có.
- **M1, nguồn động còn thiếu:** Gus/Pam/Beer được `NPC.loadCurrentDialogue` tạo từ `Strings/StringsFromCSFiles:NPC.cs.4097` bằng tên NPC/vật phẩm, nối `%revealtaste:Pam:(O)346`, rồi tạo Dialogue không khóa. `string.Format`/nối chuỗi làm mất instance nguồn đã theo dõi; không dò ngược chữ. Giữ fallback đến khi có adapter bắt đúng nguồn và tham số runtime. Không tuyên bố câu này đã được sửa.
- Cache bố cục NPC theo nội dung hai bản, kích thước khung, thứ tự dòng, font phụ và tùy chọn/font locale chính. Cache cả overflow, không đo lại trang chỉ vì thêm ký tự reveal; không giữ DialogueBox sống sau khi đóng.
- Antigravity hoàn tất công việc, Codex rà soát diff và điểm parser vanilla. Build thành công; **1099/1099 kiểm tra tự động đạt**. Chưa nghiệm thu trong game các ca vừa sửa. Bước tiếp: adapter nguồn động có tham số và các nguồn event còn thiếu; không yêu cầu chạy lại log PASS cũ.

## Nhật ký triển khai — preview 24: chuyển sang khối ngôn ngữ

- **Quyết định mới của người dùng:** bỏ xen từng hàng. Ảnh cho thấy chính/phụ khác số hàng khiến một số hàng trông như thiếu dịch. Quyết định này thay thế yêu cầu xen hàng ở preview 22/23; không tiếp tục sửa để ép hai bản có cùng số hàng.
- **Bố cục:** toàn bộ chữ chính của trang là một khối, toàn bộ bản phụ là một khối. `SecondaryFirst` đặt nguyên khối phụ ở trên hoặc dưới. Bản phụ chỉ xuất hiện khi chữ chính của trang hiện xong, kể cả khi nằm phía trên; giữ ngưỡng hiện ký tự cuối của vanilla. Các khối do metadata ghép cặp vẫn dùng kiểm tra nguồn, không chia màn hình thành từng cặp con.
- Primary giữ nguyên chuỗi và wrapping SpriteText của game. Secondary vẫn dùng font native của locale phụ, scale 0.75 và wrap theo chiều rộng thật. Dùng cùng layout ở NPC/hộp thông tin/câu hỏi; câu hỏi giữ khối prompt và từng đáp án riêng để vùng chọn không gộp sai. Cache NPC và các fallback thiếu nguồn/font/không vừa vẫn giữ.
- Đã thay 12 kiểm tra xen hàng bằng 12 kiểm tra hành vi theo khối: độ dài lệch nhau, hai chiều trên/dưới, hiện chữ cuối trang, xuống dòng, chuỗi dài không dấu cách, ghép nhiều cặp nguồn, overflow, nhãn trùng và hình học câu hỏi. **1099/1099 đạt**, build thành công. Chưa nghiệm thu thị giác preview 24 trong game.
- **Bằng chứng ảnh preview 23:** câu hỏi Alex, lời dẫn Alex và trang Caroline đã có bản phụ sau sửa `$e`; chưa coi phản ứng sau chọn đáp án là hoàn tất vì ảnh còn thiếu bản phụ. Câu xác nhận rời rạp cũng còn thiếu adapter nguồn câu hỏi địa điểm. Gus dạng format runtime vẫn ngoài phạm vi. Các khoảng trống này được giữ cho mốc nguồn động/event/câu hỏi địa điểm tiếp theo.

- Antigravity đã hoàn tất rà soát độc lập thay đổi theo khối, không tìm thấy hồi quy đáng kể trong phạm vi đã xem. Thông báo plugin về docs thay đổi trong lúc review là do Codex cập nhật các tài liệu trên cùng worktree; không phải kết luận kiểm thử game.

## Nhật ký triển khai — preview 25: phản hồi sau lựa chọn

- **Nghiệm thu preview 24 từ ảnh:** bố cục nguyên khối đã xuất hiện ở NPC, Caroline cả hai trang, Abigail và bảng chỉ đường. Prompt/ba đáp án Alex có bản phụ; hai trang phản hồi Alex sau chọn đáp án vẫn thiếu. Ảnh lịch, bảng Help Wanted và Special Orders chưa có song ngữ; không coi các GUI đó đã tích hợp.
- **M1, phản hồi thực:** vanilla `Dialogue.chooseResponse` gọi `parseDialogueString` với khóa reaction riêng, nhưng nối các trang mới vào `dialogues` cũ. Resolver đã theo đúng khóa mới nhưng vẫn dùng `currentDialogueIndex` tuyệt đối, khiến trang đầu reaction bị tra ở index 2 thay vì 0. `DialogueSourcePatch` nay lưu thêm số trang trước parse (BaseIndex), và resolver dùng index tương đối. Copy giữ cả khóa và BaseIndex; nguồn bị xóa rõ vẫn giữ null. Không đổi danh sách/trang/chỉ số game hay chạy lại câu lệnh thứ cấp.
- Antigravity để lại bản sửa và fixture phản hồi nhưng đạt giới hạn 15 phút trước báo cáo cuối; Codex tiếp quản, rà diff, sửa các phụ thuộc headless của fixture và kiểm tra. Không tự chạy lại CLI sau timeout.
- Hồi quy dùng constructor/parse/`chooseResponse` thật với đối tượng thử riêng, đi qua hai trang reaction và copy sau reparse. Boundary friendship được chặn trong fixture để tránh achievements/content/save; xác nhận vanilla gọi boundary một lần với +15, không coi đó là kiểm thử tăng tình bạn trong game. Game/player và Harmony được khôi phục khi thoát fixture. Có thêm ca thuần reaction một/hai trang và kiểm tra source offset trong nhóm copy runtime cũ; không tăng danh sách command.
- **Cảnh báo custom source:** `RawContentProvider.Rebind` trước đây luôn xóa cờ đã cảnh báo ở mỗi thông báo registry. Nay giữ cảnh báo cùng locale/lý do trong một đợt không khả dụng, vẫn cảnh báo nếu nguyên nhân đổi hoặc nguồn từng khả dụng rồi mất. Đăng ký nguồn muộn vẫn khôi phục đọc bản dịch ngay. Đây là chống log lặp theo vòng đời provider, không che lỗi nguồn thật hoặc tự nhận vi đã đăng ký chỉ từ một dòng log. Cảnh báo ban đầu không chứng minh trạng thái nguồn về sau; ảnh có tiếng Việt cho thấy đã có nguồn usable tại những thời điểm hiển thị đó.
- Build thành công, **1114/1114 đạt**: tăng 8 ca phản hồi và 7 ca vòng đời cảnh báo so với preview 24. Chưa nghiệm thu hiển thị phản hồi mới trong game. Bố cục block/font native75% giữ nguyên. Gus dạng format, câu hỏi địa điểm, event, lịch và bảng nhiệm vụ vẫn là các adapter còn lại.

## Nhật ký triển khai — preview 26: bảng phim hiện tại/sắp chiếu

- **Nghiệm thu preview 25 từ ảnh:** Alex đã có bản phụ ở cả hai trang phản hồi sau lựa chọn. Các ảnh Caroline, Abigail, Willy, Pierre, Vincent và hộp quầy đồ ăn tiếp tục xác nhận bố cục nguyên khối. Lịch/bảng nhiệm vụ vẫn là bề mặt chưa tích hợp; không suy rộng từ ảnh thoại.
- **M2, poster rạp:** `GameLocation.performAction` với `Theater_Poster` lấy `MovieTheater.GetMovieToday` rồi tạo hai trang qua `Game1.multipleDialogues`: `Strings/Locations:Theater_Poster_0` chèn Title và `Theater_Poster_1` chèn Description. `Theater_PosterComingSoon` dùng GetUpcomingMovie và template Coming_Soon. Đây là `DialogueBox(List<string>)`, không phải hộp chuỗi literal một khóa; trước đây mất nguồn sau format và chưa có adapter cho list.
- Hook chỉ trong scope action poster, giữ đúng instance MovieData game trả về. Không tự chọn lại theo ngày, tìm từ tiêu đề đang hiện hoặc đổi lịch. Secondary đọc `Strings/Movies` qua locale nghiêm ngặt và field Title/Description từ metadata custom pack. Chỉ giải `[LocalizedText asset:key]` được hỗ trợ hoặc field dịch thuần, không chạy lại token ngẫu nhiên/query/action. Kiểm tra khớp toàn bộ trang primary trước khi attach; thiếu bản dịch/không khớp/thiếu font/không vừa thì giữ vanilla.
- Tái sử dụng ObjectDialoguePatch cho list trang có nguồn đã xác thực: font native75%, SecondaryFirst, reveal sau chữ chính, chiều cao và index trang. Scope poster được phục hồi bằng Harmony finalizer khi có exception hoặc action lồng nhau; không giữ MovieData sang thao tác tiếp theo. Custom reader chiếu riêng Title/Description trong Entries/Fields của Data/Movies, giữ field chưa bị sửa và bỏ field khi patch xóa rõ.
- **Khởi động custom locale:** hai warning tạm đến từ tạo provider ở Entry và GameLaunched trước quét nguồn. SecondarySourceDiagnostics giữ warning đến sau ReloadLanguageSources/DiscoverAdditionalLanguages; nếu nguồn cuối cùng usable thì bỏ warning tạm, nếu còn thiếu thì báo lý do mới nhất một lần. Mất nguồn sau startup vẫn báo ngay. Không thay đăng ký nguồn, nội dung pack hoặc tắt mọi cảnh báo.
- Antigravity hoàn tất triển khai poster và kiểm tra. Codex rà soát, bổ sung phục hồi context khi exception/nested action, giải token của metadata custom, xóa field metadata và ca sử dụng bộ phim thật từ asset 1.6.15 cùng pack Việt hóa đang cài. `Data/Movies` được đọc đúng List<MovieData>, không coi là dictionary.
- Build thành công; lượt kiểm tra cuối **1162/1162 đạt**, gồm ca poster dùng record `spring_movie_0` thật và pack Việt hóa đang cài. Một lượt harness trước đó dừng với lỗi nội bộ CLR tại MonoMod CompileMethodHook; chạy lại cùng DLL bằng lệnh thường đạt toàn bộ, chưa quy được nguyên nhân cho code mod. Chưa nghiệm thu renderer poster trong game. Các giới hạn còn lại: Gus format động, câu hỏi địa điểm/event, lịch, nhiệm vụ và các GUI khác; không coi poster là đã tích hợp toàn bộ rạp phim hoặc toàn game.

## Nhật ký triển khai — preview 27: kiểm kê nguồn và câu hỏi địa điểm

- **Đã làm kiểm kê có hệ thống:** Antigravity tạo [bản đồ text](TEXT-SURFACE-CATALOG.vi.md), dữ liệu JSON và script `scripts/audit_text_surfaces.py` từ bản dịch ngược 1.6.15.24356. Codex chạy lại script, đối chiếu 1.807 kết quả với dòng mã thật, chuẩn hóa link file/hàm và sửa các mô tả quá rộng (font/texture không phải lời gọi; hook tiêu đề không chứng minh mô tả song ngữ). Có 948 tệp C# được quét, 203 tệp chứa ứng viên, 1.539 dòng nguồn duy nhất; 50 mục phân hệ gồm mục không áp dụng. Đây không phải phần trăm hoàn thành hoặc bằng chứng mọi đường runtime đã có trong danh sách.
- Dùng lại nghiên cứu cũ; bản đồ ghi riêng source/ID, renderer/font, code hiện có, bằng chứng ảnh, adapter còn thiếu và ràng buộc. Mốc ưu tiên: câu hỏi địa điểm → hover/mô tả item/raccoon → dữ liệu ghép động bảo tàng/Gus → event → menu riêng lịch/quest/shop/xây dựng → HUD → chữ baked trong texture. Không chèn bản dịch tự động vào mọi drawString.
- **M2, nhóm đầu đã code:** `DialogueBox(string, Response[], int)` được giữ nguồn của chính instance prompt và từng label trước constructor. Resolver đối chiếu lại dictionary primary và bản dịch strict-locale; giữ đúng response key/order/object. Bổ sung theo dõi `Strings/Lexicon` cho Yes/No. Tái sử dụng DialogueQuestionPatch/Layout để đo/vẽ/hover/gamepad; SecondaryFirst và font native75% giữ nguyên. Câu hỏi không chân dung được dùng ngân sách chiều cao viewport-144; NPC giữ viewport-192. Không gọi lại createQuestionDialogue hoặc action handler.
- Phạm vi là câu hỏi tĩnh có source cho tất cả thành phần, ví dụ điện thờ và các xác nhận theo cùng constructor. Thiếu một label, key trùng, đổi chữ/thứ tự, nguồn hết hiệu lực, token không an toàn, format mất provenance, nhiều trang, thiếu glyph hoặc overflow đều fallback cả câu hỏi. Không coi mọi câu hỏi địa điểm/event đã được hỗ trợ.
- Hồi quy kiểm tra prompt/label nhiều dòng, thiếu/đổi nguồn, same-text khác instance, thứ tự/key, chuỗi unsafe, duplicate translation, invalidate, hình học block và chữ ký Harmony constructor. Có ca dùng đúng dữ liệu `WitchHut_EvilShrineLeft` và hai key Lexicon trong game cùng pack Việt hóa đang cài. **1178/1178 đạt**, build thành công. Chưa nghiệm thu UI địa điểm trong game.
- **Sửa môi trường harness:** runtimeconfig của Stardew và SMAPI đang cài đều đặt `System.Runtime.TieredCompilation=false`; test harness trước đó chưa đặt, có lỗi CLR/MonoMod CompileMethodHook. Đã đặt cùng cấu hình trong csproj test và xác nhận JSON sinh ra đúng. Hai lượt chạy sau thay đổi (1177 và 1178 ca) đạt; không thay cấu hình game, không hạ tiêu chí kiểm tra, chưa khẳng định đã loại trừ mọi lỗi runtime.
- Bằng chứng ảnh mới: hai trang poster preview 26 đã song ngữ; Gunther, sách/biển chữ tĩnh, thông báo điện thờ và lời gấu mèo có bản phụ. Mô tả cổ vật Dwarf Scroll, hover yêu cầu gấu mèo và câu hỏi điện thờ còn thiếu ở ảnh preview 26, là các mục cụ thể trong bản đồ.


## Nhật ký triển khai — preview 28: đo chiều cao chính và hiện vật bảo tàng

- Dùng `content/Strings/Objects.json` làm đầu mối key; đối chiếu `LibraryMuseum.checkAction` (456–462), `ObjectDataDefinition.GetData` và `SpriteText.drawString`. Dữ liệu `Data/Objects` có schema typed được kiểm tra trực tiếp từ XNB cài đặt; thư mục content dịch ngược không có file JSON typed này. Không đưa thư mục dịch ngược thành phụ thuộc runtime.
- **Gunther:** tại chiều rộng 716 px, câu `Gunther_MuseumComplete` được cách đo cũ với width 712 tính thành 156 px, nhưng quy tắc vẽ Latin chỉ cần 102 px. Thay phép đo bằng `PrimaryDialogueMetrics`, phân biệt Latin với BMFont (BMFont vẫn có ngưỡng width−4); giữ khoảng cách block 12 px. Bộ đo độc lập với bộ xếp hai ngôn ngữ; dùng chung cho NPC, object và question. Có hồi quy đúng câu, chiều rộng biên và dấu xuống dòng. Chưa có ảnh nghiệm thu bản sửa trong game.
- **Hiện vật:** giữ ID vật phẩm từ ô `museumPieces` được vanilla chọn (cả ô phía trên), chỉ đính kèm khi vanilla đã mở hộp thoại mới. Tra DisplayName/Description trong `Data/Objects` rồi key `Strings/Objects`; kiểm tra chuỗi chính ghép lại khớp hộp hiện tại, lấy phụ bằng lookup strict, không fallback key thiếu sang EN. Bản phụ là một khối đầy đủ sau/trước khối chính. Thiếu một trường, token chưa giải quyết, thêm trang hoặc text stale thì giữ vanilla. Không sửa danh sách hiến tặng, phần thưởng hay thao tác nhận vật phẩm.
- Giới hạn hiện vật: hỗ trợ trường LocalizedText có nguồn nhận diện; metadata custom ghi tên/mô tả literal trực tiếp chưa có adapter riêng. Không suy độ phủ tất cả mod từ hai hiện vật mẫu.
- Antigravity CLI đã chạy và sửa một phần, đạt giới hạn 20 phút trước khi hoàn tất. Codex tiếp quản diff, bỏ mã dò tạm, hoàn thiện kiểm tra nguồn/locale, tách phép đo và adapter rồi xác minh. Không tự tiếp tục/restart AGY sau timeout.
- Build thành công, **1187/1187 đạt**. Có kiểm tra cài Harmony vào `LibraryMuseum.checkAction`, đọc key hiện vật thật từ XNB và ghép cả hai trường của item 96/106 với pack Việt hóa đang cài. Đây là kiểm tra offline/điểm nối; chưa phải nghiệm thu vẽ hoặc tương tác trong game.
- Ảnh người dùng preview 27 xác nhận câu hỏi vé rạp và hai điện thờ có bản phụ; **WizardShrine đổi ngoại hình còn thiếu** do `.Replace('\n', '^')` làm mất nguồn chuỗi. Đợt này chưa sửa đường đó. Gus format động, overflow NPC, event và GUI chuyên biệt vẫn tiếp tục theo các mốc.
- Bản bàn giao: `bin/UntranslatedStardew-1.3.2-dialogue-preview28.zip`, không cài đè mod, không commit/release/main merge.
- Phiếu nghiệm thu gộp: (1) xem câu hoàn thành bảo tàng của Gunther nếu save đang có; phụ sát sau khối chính, không dư một dòng lớn; (2) xem Dwarf Scroll I và Ornamental Fan đã hiến tặng, đọc đủ tên/mô tả hai ngôn ngữ rồi đóng; (3) đảo SecondaryFirst với một hiện vật, hai khối đảo vị trí và không tràn khung. Không cần chạy lại từng nhóm PASS hoặc thay tiến độ save để tạo ca.


## Nhật ký triển khai — preview 29: bảo tàng dài, phép đo font và event có nguồn

- Ảnh preview 28 của người dùng xác nhận Gunther không còn dư khoảng trống lớn, Marnie/hai trang và Kent có bản phụ. Ancient Seed vẫn thiếu, event Emily/bong bóng vẫn thiếu; trang đầu Shane/Leah thiếu chưa đủ để gán nguyên nhân duy nhất.
- Đã thêm [CONTENT-COVERAGE.vi.md](CONTENT-COVERAGE.vi.md) trả lời theo sáu nhóm thư mục người dùng hỏi. Không coi allowlist/đọc được source là hiển thị toàn thư mục.
- Sửa mismatch `Environment.NewLine`: chuẩn hóa cùng `DialogueBox.getCurrentString` tại museum match và chọn trang object. Fixture gọi `Game1.parseText` + getter thật với mô tả dài; stale text vẫn bị từ chối. Source XNB và VI được kiểm tra cho item 96, 106, 114 (Ancient Seed).
- Sửa lookahead icon ở Latin zoom khác 3 bằng truyền zoom hiệu dụng vào phép đo, không sửa font static. Ca zoom2 + icon tái hiện ngắt dòng; có golden BMFont zoom1.5, ngưỡng width−4 và caret.
- Đường event mới: `Data/Events/<location>:<id/conditions>` -> `Event.DefaultCommands.Speak`/`Message` -> hộp vừa mở -> renderer hiện có. Giữ event game đã chọn, xác minh asset/ID và toàn bộ lệnh chính đang chạy để phân biệt biến thể; cả hai script có cùng cấu trúc/actor và chỉ text speak/message được khác. Không chạy TokenParser hay lệnh từ bản phụ. Script token động, fork/command bị sửa, thiếu nguồn, không tương ứng hoặc mơ hồ giữ vanilla. Chỉ những hộp được gắn nguồn đã kiểm tra mới vượt guard eventUp; không bật song ngữ đại trà cho mọi event.
- Capture gắn đúng event/chỉ số lệnh/generation; advance, skip và reload làm bản phụ cũ hết hiệu lực. Bố cục và reveal giữ nguyên, NPC tràn khung vẫn fallback. Message giữ phân trang/nội dung chính vanilla và sử dụng chuẩn hóa mới.
- **Giới hạn dữ liệu thật:** ID may đồ Emily 992559 có hai biến thể điều kiện trong EN. Biến thể trong ảnh có 26 lệnh, bản VI đang cài có 19 lệnh và nội dung khác; giữ primary-only. Không sửa dữ liệu dịch để làm kiểm tra xanh. Kiểm tra đã đổi giả định ban đầu “pack có cùng script” thành kiểm tra sự khác biệt thật và fallback theo hợp đồng đã đặt trước.
- Corpus Farm (giới tính nữ) có 56 lệnh text ghép được thuộc 14 event với pack VI. Không coi đây là nghiệm thu game. Event questions/forks, festivals, bubbles và token động chưa hỗ trợ.
- Gemini Flash3.8High được gọi ở worktree riêng nhưng worker lỗi khởi động socket Windows (`listen tcp 127.0.0.1:0 ... forbidden`), không thay file. Codex tiếp quản, triển khai và xác minh; không ghi công CLI cho code chưa chạy.
- Build thành công, **1218/1218 đạt**; có source/corpus thật, fixture bất tương ứng, Harmony hai command và vòng đời capture. Chưa chạy event/UI thật, không đổi save, không deploy/commit/release.
- Bản ZIP: `bin/UntranslatedStardew-1.3.2-dialogue-preview29.zip`. Phiếu thử gộp: xem Ancient Seed/Ornamental Fan đã hiến tặng rồi đóng; đảo SecondaryFirst ở một hiện vật; khi tự gặp event có script tương ứng thì đọc/tiếp/đóng như bình thường. Không reset cờ/sửa save để xem lại Emily; bản hiện tại của pack đó chưa tương ứng nên không yêu cầu thử lại event này.


## Preview 30 — nghiệm thu gộp M1–M2

- Khung NPC có thể cao thêm khi vừa màn hình, giữ kích thước chân dung và chừa chỗ cho biểu tượng chuyển trang. Hai ngôn ngữ vẫn tách thành hai khối. Trang quá dài so với màn hình dùng cách phân trang của game.
- Gợi ý quà ghép từ mẫu câu, NPC và vật phẩm vanilla thực sự đã chọn. Câu hỏi event giữ thứ tự và ID đáp án gốc. Nhánh fork/switch chỉ dịch khi truy được script vừa nạp và đối chiếu được danh sách lệnh đang chạy.
- Lễ hội dùng đúng khóa biến thể năm. Wizard Shrine xử lý riêng biến đổi xuống dòng của prompt. Event may đồ Emily 992559 vẫn chỉ hiện bản chính vì script Việt đang cài có nội dung khác bản EN.
- Phiếu thử gộp nằm trong DIALOGUE-COMMANDS.vi.md. Các đường dẫn mới cần nghiệm thu hiển thị và chuyển tiếp trong game.
