# Độ phủ song ngữ theo thư mục content

Mốc đối chiếu: preview 28 (`8e8c7c4`) và preview 29 đã kiểm tra tự động. Nguồn khảo sát là `D:/Downloads/Stardew_decompilers/content` và mã C# Stardew 1.6.15.24356; runtime lấy dữ liệu từ game cài đặt/nguồn locale đã đăng ký. Không cần thư mục dịch ngược trên máy người dùng.

**Đọc được dictionary/key không đồng nghĩa đã hỗ trợ mọi nơi sử dụng nó.** Độ phủ cần cả nguồn đúng, trường dịch đúng, renderer và hình học/tương tác đúng. Không có tỷ lệ phần trăm toàn game đã được xác minh.

| Thư mục người dùng hỏi | Trạng thái tới preview 29 | Đã dùng ở đâu | Chỗ còn thiếu |
|---|---|---|---|
| `Characters/Dialogue` | Một phần đã code, nhiều ảnh nghiệm thu | NPC thường, phản hồi sau lựa chọn; nhánh gender/weekly/state/query/response và câu hỏi có cấu trúc tương ứng | Source mất/format động, cú pháp chưa hỗ trợ, phân trang vật lý nhiều trang và overflow fallback. Không bao gồm event chỉ vì event có NPC |
| `Data/Events` | Preview 29: một phần đã code/kiểm tra tự động, chưa nghiệm thu game | `speak` và `message` có đúng asset/ID/biến thể script/chỉ số lệnh; chỉ những hộp đã xác minh mới được bật trong event | Event question/fork, script chứa token động, script bản dịch khác cấu trúc và hộp tràn vẫn fallback; event Emily trong pack đang cài lệch 26/19 lệnh nên chưa hiển thị song ngữ |
| `Data/Festivals` | Chưa có adapter riêng | Chưa xác minh hiển thị trực tiếp từ dữ liệu lễ hội | Phân biệt script của lễ hội, thoại NPC, menu mua đồ và thông báo; không lấy trạng thái thoại thường áp cho lễ hội |
| `Strings` | Một phần, chọn key/đường gọi | Hộp thông tin tĩnh, biển chỉ đường, nhiều câu hỏi Yes/No, poster phim; tên vật phẩm và một số tiêu đề GUI qua pipeline riêng | Không phải toàn bộ Strings; các chuỗi format động, thông báo/event, HUD và GUI chuyên biệt cần adapter riêng |
| `Strings/schedules` | Parser nhận dạng đường nguồn; độ phủ runtime chưa nghiệm thu riêng | Có thể ghép khi Dialogue còn giữ đúng key/locale và renderer đã hỗ trợ | Không gồm `Characters/schedules` là lệnh lịch trình. Lời trên đầu NPC có timer/fade và renderer khác, chưa hỗ trợ |
| `Data` (ngoài Events/Festivals) | Một phần theo schema/trường | `ExtraDialogue`, `EngagementDialogue`; Title/Description của Movies cho poster; DisplayName/Description của Objects cho hiện vật; tên item và một số tiêu đề từ dữ liệu typed | Bảng nhiệm vụ, QuestLog, bundles/raccoon, xây dựng, shop đầy đủ, thư/body ghi chú, TV... chưa hoàn tất |

## Dictionary được parser hội thoại chấp nhận

- `Characters/Dialogue/<NPC>` và `Strings/schedules/<NPC>`.
- `Data/ExtraDialogue`, `Data/EngagementDialogue`.
- `Strings/Characters`, `Events`, `StringsFromCSFiles`, `Locations`, `1_6_Strings`, `StringsFromMaps`, `SimpleNonVillagerDialogues`, `animationDescriptions`, `SpeechBubbles`, `Movies`, `Lexicon`, `Objects`.

Danh sách này chỉ mô tả allowlist trong `Core/DialogueTranslation.cs`. Ví dụ: `Strings/SpeechBubbles` đọc được nhưng không tự làm cho `NPC.drawAboveAlwaysFrontLayer` hay bong bóng chào người chơi có hai ngôn ngữ. Tương tự, `Strings/Objects` không tự làm cho mô tả trong mọi shop/inventory thành song ngữ; tooltip hiện có chủ yếu xử lý tên.

## Những ví dụ trong ảnh mới

- Gunther: khoảng cách bản phụ đã gọn, là nghiệm thu bố cục cụ thể của preview 28.
- Marnie và trang hai Shane, Kent: có bản phụ. Trang đầu Shane/Leah còn thiếu; ảnh chưa đủ để kết luận là thiếu bản dịch hay overflow/parser fallback, cần truy đúng key và log nguồn trước khi thay chính sách.
- Ancient Seed bảo tàng: còn thiếu trong ảnh preview 28; preview 29 đã sửa và kiểm tra tự động chuẩn hóa `Game1.parseText`/`DialogueBox.getCurrentString`/phân trang để không loại nhầm mô tả dài.
- Emily event + thông báo mở máy may: còn thiếu ở preview 28. Preview 29 có adapter event, nhưng script may đồ EN cài đặt có 26 lệnh còn VI có 19, lời thoại khác; bộ ghép từ chối để tránh ghép nhầm. Không đánh dấu event này hoàn thành.
- Bong bóng `Hi, Tofu`: chưa có adapter trên đầu NPC.

## Truy nguồn tiêu biểu và nơi mở rộng

| Dữ liệu | Nơi game chọn/ghép | Renderer/adapter mod |
|---|---|---|
| `Data/ExtraDialogue:Gunther_MuseumComplete` | `LibraryMuseum.OpenGuntherDialogueMenu`, `Game1.parseText` | `DialogueBoxPatch` + `PrimaryDialogueMetrics` |
| `Data/Objects:<itemId>` -> `Strings/Objects:*_Name`, `*_Description` | `ObjectDataDefinition.GetData`, `LibraryMuseum.checkAction` giữ `museumPieces` ID | `MuseumArtifactPatch` -> `MuseumArtifactTranslation` -> `ObjectDialoguePatch` |
| `Data/Movies` Title/Description -> `Strings/Movies` + template `Strings/Locations` | `MovieTheater.GetMovieToday/GetUpcomingMovie`, action poster | `MoviePosterPatch` + `MoviePosterAdapter` |
| Prompt/label từ `Strings/Locations` và `Strings/Lexicon` | `GameLocation.createQuestionDialogue` -> constructor question | `LoadedDialogueSource` + `LocationQuestionPatch` + `DialogueQuestionPatch` |
| `Data/Events/<location>:<eventId/conditions>` | `Event` chọn script/command; lệnh `speak`, `message` | Preview 29: `EventDialogueTranslation` + `EventDialoguePatch`; chưa nghiệm thu UI |

Theo dõi tiến độ và nghiệm thu tại [DEVELOPMENT-PLAN.vi.md](DEVELOPMENT-PLAN.vi.md), kiểm kê renderer tại [TEXT-SURFACE-CATALOG.vi.md](TEXT-SURFACE-CATALOG.vi.md). Thư mục JSON EN không chứa đủ mọi typed asset của game; tra thêm XNB cài đặt theo schema thực tế khi thiếu.


## Bằng chứng preview 29

- Build thành công; 1218 kiểm tra đạt. Kiểm tra nguồn thật đọc XNB EN và pack Việt hóa đang cài, không dùng thư mục JSON dịch ngược làm nguồn runtime.
- Corpus nông trại, nhánh giới tính nữ: 56 lệnh text trong 14 event có nguồn/cấu trúc ghép được. Đây là kiểm tra nguồn và ghép, không phải số câu đã nghiệm thu renderer hay tỷ lệ hoàn thành game.
- Có kiểm tra cài Harmony ở cả `Event.DefaultCommands.Speak`/`Message`; đổi lệnh, skip và reload loại bỏ quyền dùng bản phụ của hộp cũ. Không chạy event thật, không kiểm chứng thời điểm/hiệu ứng đồ họa trong game.
- Mô tả dài dùng `Game1.parseText` và `DialogueBox.getCurrentString` thật trong fixture; item 96/106/114 xác nhận nguồn hai trường và pack VI. Phép đo có ca Latin zoom2 + icon, BMFont zoom1.5 với ngưỡng width−4 và xuống dòng tường minh.

Thư mục khảo sát hiện chứa 52 JSON `Characters/Dialogue`, 42 JSON `Data/Events`, 9 JSON `Data/Festivals`, 30 JSON `Strings/schedules`, 31 JSON Strings khác, 13 JSON Data khác. Các số này chỉ là số file đầu vào, không phải số file đã hỗ trợ và không đại diện tất cả asset game.
