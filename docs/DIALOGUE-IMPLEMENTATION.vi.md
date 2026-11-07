# Trạng thái triển khai hội thoại song ngữ

Kế hoạch làm việc và nghiệm thu: [Kế hoạch phát triển](DEVELOPMENT-PLAN.vi.md).

Mốc: dialogue preview 26, Stardew Valley 1.6.15. Đây là trạng thái code; chưa thay thế nghiệm thu trong game.

**Quyết định hiện tại (preview 24):** hiển thị theo hai khối nguyên trang, bỏ xen từng hàng của preview 22/23. Toàn bộ bản phụ đợi chữ chính hiện xong; `SecondaryFirst` chỉ đổi vị trí trên/dưới của nguyên khối. Font native nhỏ 75% và các sửa `$e` vẫn giữ. Các mục preview 22/23 bên dưới là lịch sử, không mô tả bố cục đang dùng.

## Đã nối vào code

- Thoại NPC có khóa nguồn `asset:key` thuộc danh mục từ điển hội thoại vanilla được `RawContentProvider` hỗ trợ (`Characters/Dialogue/*`, `Strings/schedules/*`, `Data/ExtraDialogue`, `Data/EngagementDialogue`, `Strings/Characters`, `Strings/Events`, `Strings/StringsFromCSFiles`, `Strings/Locations`, `Strings/1_6_Strings`, `Strings/StringsFromMaps`, `Strings/SimpleNonVillagerDialogues`, `Strings/animationDescriptions`, `Strings/SpeechBubbles`): bản phụ nhỏ, `SecondaryFirst`, ghép khối do tác giả định nghĩa, xuống dòng, hiệu ứng hiện chữ và fallback khi không vừa.
- Từ chối đường dẫn tùy ý, path traversal và các asset `Data/*` có kiểu dữ liệu riêng hoặc dạng bảng/script (`Data/IncomingPhoneCalls`, `Data/Objects`, `Data/Quests`, `Data/Events/*`, `Data/Festivals/*`, `Data/mail`). Tiếp tục yêu cầu đối chiếu khớp chính xác câu chính đang hiển thị.
- Giới tính `${...^...}$`, dấu phân cách cũ, biến `@`, luân phiên tuần `||`; nhánh `$d`, `$query` được đối chiếu với câu chính đang hiện. `$p` dùng lịch sử đáp án của chính đối tượng người chơi rồi kiểm tra lại câu chính.
- Câu hỏi `$q` với các đáp án `$r` dạng phẳng, có thể có lời mở đầu thuần trước câu hỏi. Kiểm tra cả header, thứ tự và nhãn đáp án đang sống. Ghép bằng khóa câu phản hồi `Response.responseKey`; ID ghi nhớ có thể trùng giữa các đáp án.
- Một phép đo chiều cao cho khung, highlight, hover chuột và vùng chọn gamepad. Mỗi đáp án gồm hai bản văn và giữ một vùng chọn vanilla. Khi toàn bộ câu hỏi không vừa chiều cao, giữ nguyên câu hỏi vanilla.
- Theo dõi khóa nguồn hiện hành khi vanilla gọi lại `Dialogue.parseDialogueString` sau một lựa chọn và khi sao chép `new Dialogue(Dialogue)`. Bản sao giữ nguyên khóa phản hồi mới hoặc trạng thái xóa khóa `null` chủ đích, không quay về `TranslationKey` câu hỏi ban đầu.

Bản phụ được đọc như dữ liệu. Mod không chạy parser hội thoại thứ hai, không tự chọn đáp án, không áp lại điểm tình bạn hoặc lệnh hành động. Mẫu console dùng callback trả lời riêng để đóng khung trước các tác dụng phụ vanilla.

## Khung thông tin không chân dung (M2, preview 21)

Đã có adapter cho `DialogueBox(string)` khi đầu vào giữ đúng instance chuỗi từ `Game1.content.LoadStringReturnNullIfNotFound(path, bool)`, thuộc danh mục nguồn từ điển hỗ trợ và khớp chuỗi chính hiện tại. Khung tự đo/tăng chiều cao cho cả hai vai trò, giữ thứ tự `SecondaryFirst`, trang `#`, dòng `^` và click reveal/tiếp trang của vanilla. NPC có `Dialogue` nhưng không có portrait cũng được đưa qua đường vẽ đã có. Đã kiểm tra tự động; chưa nghiệm thu hình ảnh trong game.

Không dùng text bằng nhau để suy ra khóa. Shared instance khác khóa bị loại; primary bị nối/sửa, chuỗi format, template, lệnh, item token, trang không tương ứng, nguồn thiếu hoặc tràn khung đều có thể giữ vanilla. Chưa hỗ trợ `DialogueBox(List<string>)` và câu hỏi địa điểm/shop bằng adapter mới.

## Giới hạn hiện tại

- Chỉ thoại/câu hỏi NPC hoặc hộp thông tin có khóa nguồn từ điển hợp lệ và khớp nguyên văn câu chính đang hiển thị (hoặc mẫu console) mới gắn bản phụ. Thoại mất `TranslationKey` và không giữ đúng instance chuỗi đã nạp, hoặc đã thay thế tham số `{0}`/`%endearment` trước khi tạo `Dialogue` giữ nguyên vanilla.
- Dù `Strings/Events` nằm trong danh mục từ điển đọc được khi thoại thường dùng khóa này, renderer vẫn chặn cứng `!Game1.eventUp`; hội thoại trong event UI, câu hỏi địa điểm, shop và câu hỏi do mod tạo động vẫn chưa hỗ trợ.
- Nhánh điều kiện bao quanh câu hỏi, câu hỏi lồng nhau, lệnh trong nhãn và cấu trúc không tương ứng giữ vanilla. Lời dẫn thuần trước `$q` đã được ghép theo chỉ số đoạn và cấu trúc câu hỏi/đáp án tương ứng; câu đang hiện vẫn phải khớp nguyên văn.
- Đường `$q` đã trả lời có ánh xạ fallback theo lịch sử chính và đúng khóa NPC. Hỗ trợ câu thuần nhiều trang và `$p` phẳng, có tính offset lời dẫn. Fallback chứa query/weekly riêng, câu hỏi lồng hoặc lệnh chưa hỗ trợ vẫn dùng vanilla. Phần mới đã kiểm tra tự động, chưa nghiệm thu runtime.
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

## Preview 22: font native và xen hàng

Ba đường vẽ hiện có (NPC, hộp thông tin, câu hỏi) dùng DialogueNativeFontProvider/DialogueNativeFont thay SpriteFont1 cho bản phụ, scale 0.75. Provider đọc font native của locale phụ: Latin font_bold/font_colored, BMFont theo locale và metadata custom language. Icon đặc biệt giữ sheet SpriteText. Không đổi font/ngôn ngữ toàn cục; font thiếu hoặc thiếu glyph giữ fallback. Font UI của tooltip và các GUI khác không thuộc thay đổi này.

DialogueInterleavedLayout wrap riêng hai bản trong từng khối có nguồn đối chiếu, xen hàng phụ với hàng chính và đảo từng cặp qua SecondaryFirst. Offset chữ chính giữ theo chuỗi vanilla để reveal không chạy sang hàng sau. Hàng phụ xuất hiện khi hàng chính tương ứng hoàn tất. Nếu hai bản có số hàng khác nhau, phần dư giữ trong cùng khối; đây là bố cục theo hàng, không bảo đảm tương đương nghĩa từng hàng. Câu hỏi dùng cùng hình học cho vẽ và hitbox. Hộp thông tin điều chỉnh chiều cao; NPC chưa tăng khung vượt vùng vanilla và fallback khi không vừa.

Hỗ trợ policy font của 12 ngôn ngữ vanilla cùng custom khai báo font hội thoại native; không đồng nghĩa đã tích hợp mọi nguồn hội thoại hay nghiệm thu mọi ngôn ngữ trong game. Thai thiếu asset vanilla và custom chỉ khai báo SpriteFont UI không có font native để suy ra. Custom renderer như DDFC vẫn ngoài phạm vi. Kiểm tra tự động: 1092/1092; kiểm tra GPU/thị giác preview 22 còn chờ.

## Preview 23: ngắt thoại `$e` và các ca thiếu dịch

Đã bổ sung `#$e#` cạnh `#$b#` trong resolver trang/nhánh và preview an toàn. `$e` được bỏ khỏi danh sách lời dẫn thực khi tính index trước câu hỏi; shape lời dẫn câu hỏi vẫn giữ riêng loại ngắt để từ chối cấu trúc không khớp. Các token vẫn do vanilla xử lý, renderer chỉ đọc kết quả và bản phụ thuần. Ca dữ liệu thật: Caroline `Wed6`, Alex `Wed` (giới tính/prompt/ba đáp án/lời dẫn), Demetrius Introduction có hai loại ngắt. Kiểm tra tự động 1099/1099; chưa xác nhận hiển thị của các ca mới trong game.

Ảnh preview 22 đã xác nhận font native/xen hàng cho Vincent, Kent và các hộp thông tin trong rạp cùng bảng chỉ đường. Chưa thể suy ra mọi NPC/locale/event đều hoạt động.

Gus khuyên tặng Pam Beer vẫn giữ vanilla: câu format runtime từ `NPC.cs.4097` rồi gắn `%revealtaste`, không có TranslationKey và mất instance nguồn. Cần adapter lấy đúng tham số tên NPC/vật phẩm từ luồng game; resolver hiện tại không tự dựng bản dịch bằng cách phân tích ngược chữ đang hiện.

Cache NPC tránh wrap/đo lại mỗi frame nếu chỉ tăng số chữ hiện. Thay nội dung/kích thước/thứ tự/ngôn ngữ/font/tùy chọn native làm cache tính lại, kể cả kết quả không vừa.

## Preview 24: toàn bộ trang chính rồi mới hiện bản phụ

`DialogueBlockLayout` thay `DialogueInterleavedLayout`. Primary của các cặp đã xác thực được nối lại đúng dấu cách của trang gốc, giữ nguyên chuỗi/vị trí reveal; SpriteText của game tự wrap khối chính. Secondary nối thành một khối, wrap bằng metrics native riêng. Bản phụ chỉ được vẽ sau `RevealAt = Primary.Length`, có chuẩn hóa ngưỡng advance ký tự cuối như vanilla. Đặt phụ trên không làm bản phụ hiện sớm. Khối trùng toàn bộ chỉ vẽ một bản.

Chiều cao gồm toàn bộ hai khối cùng khoảng cách cho ascender native; không có hàng trống giả để ép số dòng bằng nhau. Câu hỏi dùng cùng Page.Height cho vẽ/hover/gamepad, nhưng vẫn giữ prompt và từng đáp án riêng. Hộp thông tin tăng chiều cao trong ngân sách màn hình; NPC giữ fallback nếu khung không đủ. Không làm tăng độ phủ nguồn dịch chỉ bằng thay layout.

Ảnh preview 23 đã xác nhận câu hỏi/lời dẫn Alex và trang Caroline có bản phụ. Phản ứng Alex sau đáp án và xác nhận rời rạp còn chưa có bản phụ trong ảnh; nguồn động Gus cũng chưa hỗ trợ. Đây là các việc còn lại, không bị che bằng ghép từ chữ nhìn thấy. Build/kiểm tra logic preview 24: 1099/1099; chưa nghiệm thu giao diện phiên bản mới trong game.

## Preview 25: chỉ số tương đối của nguồn reaction

`parseDialogueString` nối trang mới vào Dialogue hiện tại. Nguồn reaction được theo dõi bằng `(Key, BaseIndex)` tại lúc parse; dịch trang bằng `currentDialogueIndex - BaseIndex`. Dialogue mới có BaseIndex 0; copy mang theo mốc của nguồn đã reparse; explicit null source không được hồi sinh. Các câu cũ đang chờ chuyển sang reaction vẫn phải qua đối chiếu chữ chính chính xác, không được ghép từ nguồn mới nếu không khớp. Đây là sửa index/provenance, không reset hoặc đổi index vanilla.

Đã kiểm tra constructor → câu hỏi → `chooseResponse` → hai trang phản hồi → copy trên đối tượng headless có Harmony thật; source/null copy cũng được giữ trong nhóm coverage hiện có. Fixture chặn friendship boundary và khôi phục trạng thái static, không thao tác save. Kiểm tra toàn bộ 1114/1114; chưa xác nhận render trong game của reaction mới.

RawContentProvider chỉ phát lại cảnh báo cùng locale/lý do sau khi nguồn từng phục hồi rồi lại mất; registry refresh lặp không tạo lại cùng cảnh báo. Lỗi thiếu nguồn thật vẫn hiển thị và giữ primary-only. Cơ chế không sửa hay bỏ qua đăng ký nguồn.

## Preview 26: poster phim từ action có nguồn

MoviePosterPatch bắt đúng MovieData trong action Theater_Poster/ComingSoon, rồi xác thực list trang hiện có trước khi gắn MoviePosterAdapter vào ObjectDialoguePatch. Phim đang chiếu có trang tên và trang mô tả; poster sắp chiếu có một trang tên. Bộ phim vẫn do vanilla chọn từ ngày/tiến độ của game; adapter không gọi lại GetMovieForDate hoặc đổi locale. Giải bản phụ bằng strict locale lookup từ Strings/Movies, template Strings/Locations và metadata Title/Description của custom pack theo ID phim. Không chạy token động của bản phụ hoặc chạy lại token ngẫu nhiên chính. Khi không đủ nguồn/format/metrics thì fallback.

Metadata hỗ trợ chiếu Entries/Fields riêng cho Data/Movies; đây không phải bộ thực thi Content Patcher đầy đủ hay loader cho mọi dữ liệu typed. List trang chỉ được attach trong scope poster có MovieData và primary đã đối chiếu; không mở nhận diện tùy ý mọi DialogueBox(List<string>). Finalizer phục hồi scope khi exception/nested action. Giữ block layout, font native75%, SecondaryFirst và cơ chế chuyển/hiện chữ của vanilla.

SecondarySourceDiagnostics hoãn warning nguồn custom trong giai đoạn khởi động đến sau quét nguồn. Nguồn usable sau quét hủy cảnh báo tạm; lỗi còn tồn tại vẫn báo, nguồn mất sau đó vẫn dùng cảnh báo vòng đời provider. Không che lỗi thật hoặc tự gắn update key.

Ảnh preview 25 xác nhận phản hồi Alex cả hai trang đã song ngữ. Hiển thị poster preview 26 và log khởi động mới chưa nghiệm thu trực tiếp trong game.

Kiểm tra preview 26: build thành công, lượt cuối 1162/1162 đạt; có ca lấy dữ liệu phim thật và pack Việt hóa. Một lần harness có lỗi CLR/MonoMod khi cài Harmony; cùng DLL chạy lại bình thường đạt toàn bộ. Đây chưa phải bằng chứng renderer hoặc khởi động SMAPI trong game.
