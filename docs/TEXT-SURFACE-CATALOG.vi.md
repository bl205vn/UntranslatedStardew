# Danh mục Bề mặt Văn bản & Kiểm kê Song ngữ Stardew Valley 1.6.15

Cập nhật: 07/11/2026.  
Mã nguồn đối chiếu: `D:\Downloads\Stardew_decompilers` (Stardew Valley 1.6.15, build 24356).  
Repo đối chiếu code (không thay nghiệm thu game): `E:\Sao lưu\UntranslatedStardew` (baseline preview 26, 1162 checks, nhánh `Full-song-ngữ-cả-game`).  
Công cụ quét tái lập: [`scripts/audit_text_surfaces.py`](<E:/Sao lưu/UntranslatedStardew/scripts/audit_text_surfaces.py>).  
Dữ liệu thô JSON: [`docs/research/text-call-sites-1.6.15.json`](<E:/Sao lưu/UntranslatedStardew/docs/research/text-call-sites-1.6.15.json>).

---

## 1. Tóm tắt Kiểm kê Từ vựng (Lexical Inventory Summary)

Qua việc quét toàn bộ 948 tệp mã nguồn C# dịch ngược của Stardew Valley 1.6.15 bằng bộ phân tích cú pháp từ vựng không phụ thuộc (`audit_text_surfaces.py`), bộ quét ghi **1.807 kết quả khớp ứng viên** thuộc 13 nhóm. Số này gồm tham chiếu font/texture và cả khai báo có thể khớp regex, không phải 1.807 vị trí cần patch hoặc số nơi text đã được bao phủ:

| Họ phương thức / Điểm nối (Call Family) | Số kết quả khớp | Tệp đại diện & Vai trò hiển thị chính |
|---|:---:|---|
| `Game1.dialogue` | 474 | `Game1.drawObjectDialogue`, `multipleDialogues`, `drawDialogueBox` |
| `Font_family_reference` | 487 | Tham chiếu font hệ thống (`dialogueFont`, `smallFont`, `tinyFont`) |
| `SpriteBatch.DrawString` | 193 | Vẽ chuỗi thô qua `b.DrawString` / `spriteBatch.DrawString` |
| `GameLocation.createQuestionDialogue` | 141 | Khởi tạo hộp hỏi đáp địa điểm / sự kiện / hành động |
| `SpriteText.draw` | 126 | Bộ dựng chữ pixel của ConcernedApe (`drawString`, cuộn, bong bóng) |
| `Utility.drawText` | 122 | Vẽ bóng và chữ đậm (`drawTextWithShadow`, `drawBoldText`) |
| `Constructor.Dialogue` | 104 | Khởi tạo đối tượng thoại NPC (`new Dialogue(...)`) |
| `IClickableMenu.hover_tooltip` | 61 | `drawHoverText`, `drawToolTip` trong menu chuột |
| `Character.showTextAboveHead` | 34 | Bong bóng chữ trên đầu NPC / người chơi (`textAboveHead`) |
| `Texture_text_candidate` | 28 | Texture tĩnh chứa chữ (Junimo Kart, Prairie King, Billboard) |
| `Constructor.HUDMessage` | 27 | Khởi tạo thông báo toast góc màn hình (`new HUDMessage(...)`) |
| `Constructor.DialogueBox` | 8 | Khởi tạo menu khung thoại độc lập (`new DialogueBox(...)`) |
| `HUDMessage.draw` | 2 | Khởi tạo và vẽ hàng đợi toast ([HUDMessage.cs](<D:/Downloads/Stardew_decompilers/StardewValley/HUDMessage.cs:138>), [Game1.cs](<D:/Downloads/Stardew_decompilers/StardewValley/Game1.cs:13816>)) |
| **Tổng cộng** | **1.807** | **Đã quét 948 tệp C#; chưa chứng minh mọi text runtime** |

> [!NOTE]
> Bảng kiểm kê trên là **danh mục ứng viên từ vựng (candidate inventory)** ở cấp tĩnh, không đại diện cho tần suất chạy runtime. Dữ liệu này chứng minh cấu trúc phân bố các ngõ xuất chữ của vanilla Stardew 1.6.15 để thiết kế các adapter bao phủ có định hướng, tuyệt đối không vá bừa (blind patch) vào từng lệnh `DrawString`.

---

Đơn vị đếm là một kết quả khớp **mỗi nhóm trên mỗi dòng**: 1539 dòng nguồn duy nhất trong 203 tệp có ứng viên. Font/texture không phải lời gọi text; một dòng có thể được đếm ở nhiều nhóm. Context class/method và literal là gợi ý regex, cần đọc mã trước khi hook; vị trí file/dòng được kiểm tra trực tiếp với nguồn. Không dùng số lượng này làm phần trăm hoàn thành mod.

## 2. Danh mục 50 mục Bề mặt / Phân hệ Màn hình (Text-Surface Feature Catalog)

Dưới đây là khảo sát đối chiếu chi tiết 50 mục phân hệ văn bản có nghĩa của game, ánh xạ trực tiếp giữa mã vanilla 1.6.15, nguồn asset, vai trò font, trạng thái điểm nối hiện tại của `UntranslatedStardew` (preview 26), bằng chứng quan sát được và ràng buộc kỹ thuật. Nhãn có hook/PASS cũ chỉ cho biết phạm vi ghi trong hàng, không đồng nghĩa toàn bộ phân hệ đã nghiệm thu.

### Nhóm 1: Thoại NPC, Lựa chọn & Sự kiện (Dialogue, Questions & Events)

| STT | Phân hệ / Bề mặt | Mã nguồn Vanilla 1.6.15 (`file:line+method`) | Nguồn Asset / Kiểu dữ liệu | Vai trò Font / Renderer | Điểm nối Mod hiện tại (`Patches/`) | Trạng thái & Bằng chứng thực nghiệm | Điểm mở rộng an toàn nhất | Ràng buộc Hình học / Input / Nguồn |
|:---:|---|---|---|---|---|---|---|---|
| 1 | **Hội thoại NPC thường (Visual Novel)** | [Dialogue.cs.parseDialogueString](<D:/Downloads/Stardew_decompilers/StardewValley/Dialogue.cs:559>) `parseDialogueString` · [DialogueBox.cs](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/DialogueBox.cs:887>) `draw` | `Characters/Dialogue/*`, schedule dialogue | `SpriteText` (native secondary font scale 0.75) | `DialogueBoxPatch.cs` · `DialogueSourcePatch.cs` | **Đã hỗ trợ (PASS)** · Đã nghiệm thu ảnh: Caroline, Abigail, Vincent, Kent hiển thị 2 khối. | Giữ nguồn tại parseDialogueString và vẽ tại DialogueBoxPatch | SecondaryFirst đảo nguyên khối; reveal sau khi primary hiện hết. |
| 2 | **Phản hồi sau lựa chọn câu hỏi NPC** | [Dialogue.cs.chooseResponse](<D:/Downloads/Stardew_decompilers/StardewValley/Dialogue.cs:1553>) `chooseResponse` | `Characters/Dialogue/*` (response reaction key) | `SpriteText` native 75% | `DialogueSourcePatch.cs` (BaseIndex offset) | **Đã hỗ trợ (PASS)** · Đã nghiệm thu ảnh: Alex phản hồi 2 trang đầy đủ bản dịch sau chọn. | `DialogueSourcePatch.parseDialogueString` Prefix lưu BaseIndex | Vanilla gộp các trang mới vào đuôi `dialogues` cũ; phải tra relative index. |
| 3 | **Câu hỏi thoại NPC ($q / $r)** | [DialogueBox.cs.setUpQuestions](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/DialogueBox.cs:618>) `setUpQuestions` và `draw` | `Characters/Dialogue/*` ($q/$r blocks) | `SpriteText` prompt + response choices | `DialogueQuestionPatch.cs` · `DialogueQuestionLayout.cs` | **Đã hỗ trợ (PASS)** · Nghiệm thu ảnh: Alex $q/prompt + 3 đáp án có song ngữ, highlight bao 2 dòng. | `DialogueQuestionPatch` (Transpiler đo `getHeightOfString`) | Hitbox từng đáp án độc lập; gamepad navigation cần giữ đúng `responseCC`. |
| 4 | **Câu hỏi địa điểm & Bàn thờ (Location Questions)** | [GameLocation.cs.createQuestionDialogue](<D:/Downloads/Stardew_decompilers/StardewValley/GameLocation.cs:4430>) `createQuestionDialogue` · [DialogueBox.cs](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/DialogueBox.cs:115>) | `Strings/Locations`, `Strings/Lexicon` (Yes/No) | `SpriteText` | LocationQuestionPatch + DialogueQuestionPatch (preview 27) | **Đã code/kiểm tra nguồn tĩnh, chưa nghiệm thu game** · Preview 26 thiếu Dark Shrine Yes/No; preview 27 bắt nguồn prompt và từng nhãn, giữ nguyên response key. | `DialogueBox(string, Response[])` constructor prefix/postfix | `characterDialogue` bằng null; không dùng được NPC resolver. Phải bắt qua `LoadedDialogueSource`. |
| 5 | **Thoại sự kiện / Cutscene (`speak`)** | [Event.cs.Speak](<D:/Downloads/Stardew_decompilers/StardewValley/Event.cs:155>) `DefaultCommands.Speak` | `Data/Events/*` script command payload | `SpriteText` via `new Dialogue` | Chưa tích hợp adapter event riêng (Fallback vanilla) | **Chưa có (GAP)** · Event dùng `speak actorName "textOrKey"`. Nếu là literal text thì mất asset key. | `DefaultCommands.Speak` Harmony Prefix | Event script là chuỗi lệnh phân cách dấu gạch chéo `/`. Phải tách payload tại runtime. |
| 6 | **Thông báo sự kiện (`message`)** | [Event.cs.Message](<D:/Downloads/Stardew_decompilers/StardewValley/Event.cs:1027>) `DefaultCommands.Message` | `Data/Events/*` payload | `SpriteText` via `Game1.drawDialogueNoTyping` | `ObjectDialoguePatch.cs` (chỉ khi có key trong cache) | **Chưa có adapter event (GAP)** · Các patch thoại hiện từ chối khi Game1.eventUp; không coi key tĩnh trong event là đã phủ. | `DefaultCommands.Message` Prefix | Mở `DialogueBox(List<string>)`. Không có typing effect. |
| 7 | **Câu hỏi trong sự kiện (`question` / `quickQuestion`)** | [Event.cs.Question](<D:/Downloads/Stardew_decompilers/StardewValley/Event.cs:2950>) `Question` · [Event.cs](<D:/Downloads/Stardew_decompilers/StardewValley/Event.cs:2971>) `QuickQuestion` | `Data/Events/*` payload (phân cách bằng `#`) | `SpriteText` qua `createQuestionDialogue` | Phụ thuộc adapter LocationQuestion | **Chưa có (GAP)** · Lệnh tạo `createQuestionDialogue(..., "quickQuestion")`. | Bắt tại `DefaultCommands.Question` & `QuickQuestion` | Đáp án phân tách bằng `#` ngay trong command script. Cần tra script song song trong asset event phụ. |
| 8 | **Bong bóng chữ trên đầu NPC (`textAboveHead`)** | [NPC.cs.showTextAboveHead](<D:/Downloads/Stardew_decompilers/StardewValley/NPC.cs:1372>) `showTextAboveHead` · [NPC.cs](<D:/Downloads/Stardew_decompilers/StardewValley/NPC.cs:5223>) `draw` | `Strings/SpeechBubbles`, event script | `SpriteText.drawStringWithScrollCenteredAt` | Chưa có hook | **Chưa có (GAP)** · Mới chỉ có tài liệu khảo sát, chưa cài patch. | `NPC.showTextAboveHead` Prefix hoặc wrap string | Bong bóng nằm lơ lửng trên đầu nhân vật, có giới hạn chiều cao trần nhà/màn hình và hiệu ứng mờ dần (alpha). |

---

### Nhóm 2: Tương tác Thế giới & Đồ vật (World Objects, Shrines & Museum)

| STT | Phân hệ / Bề mặt | Mã nguồn Vanilla 1.6.15 (`file:line+method`) | Nguồn Asset / Kiểu dữ liệu | Vai trò Font / Renderer | Điểm nối Mod hiện tại (`Patches/`) | Trạng thái & Bằng chứng thực nghiệm | Điểm mở rộng an toàn nhất | Ràng buộc Hình học / Input / Nguồn |
|:---:|---|---|---|---|---|---|---|---|
| 9 | **Hộp thông tin đồ vật tĩnh (Biển báo, TV, Tủ sách)** | [Game1.cs.drawObjectDialogue](<D:/Downloads/Stardew_decompilers/StardewValley/Game1.cs:9656>) `drawObjectDialogue` · [DialogueBox.cs](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/DialogueBox.cs:101>) | `Strings/Locations`, `Strings/StringsFromCSFiles` | `SpriteText` (1 khối chữ, không chân dung) | `ObjectDialoguePatch.cs` · `LoadedDialogueSourcePatch.cs` | **Đã hỗ trợ (PASS)** · Nghiệm thu ảnh: Biển chỉ đường, tương tác đồ vật hiển thị 2 khối chuẩn. | `DialogueBox(string)` Postfix kết hợp `LoadedDialogueSource` | Chỉ khớp khi chuỗi truyền vào trùng khớp tuyệt đối với kết quả của `LoadStringReturnNullIfNotFound`. |
| 10 | **Bàn thờ Witch Hut / Dark Shrines (Thông báo)** | [GameLocation.cs.performAction](<D:/Downloads/Stardew_decompilers/StardewValley/GameLocation.cs:8694>) `performAction` | `Strings/Locations:WitchHut_EvilShrine*` | `SpriteText` qua `drawObjectDialogue` | `ObjectDialoguePatch.cs` | **Đã hỗ trợ phần thông báo (PASS)** · Shrine of Memory và thông báo thiếu vật phẩm hiến tế đã có song ngữ. | Đã bắt qua `ObjectDialoguePatch` | Thông báo đơn trang không lựa chọn thì đạt; câu hỏi Yes/No chuyển sang mục 4. |
| 11 | **Bảo tàng: Tương tác cổ vật đã quyên góp (Dwarf Scroll...)** | [LibraryMuseum.cs.checkAction](<D:/Downloads/Stardew_decompilers/StardewValley.Locations/LibraryMuseum.cs:456>) `checkAction` | `Data/Objects` DisplayName + Description | `SpriteText` qua `drawObjectDialogue` | `MuseumArtifactPatch.cs` + `ObjectDialoguePatch.cs` | **Preview 28: đã code/kiểm tra offline, chờ nghiệm thu UI** · Item 96/106 ghép nguồn XNB và pack Việt hóa thật; thiếu trường giữ vanilla. | Giữ itemId ở `LibraryMuseum.checkAction`, kiểm tra chuỗi chính khớp trước khi gắn bản phụ | Game ghép chuỗi động: `" - " + data.DisplayName + " - " + "^" + data.Description`. Mất khóa `LoadedDialogueSource`. |
| 12 | **Bảo tàng: Sách thất lạc đã tìm thấy (Lost Books)** | [GameLocation.cs.readNote](<D:/Downloads/Stardew_decompilers/StardewValley/GameLocation.cs:10531>) `readNote` · [LetterViewerMenu.cs](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/LetterViewerMenu.cs:89>) | `Strings/Notes:1` .. `21` | `SpriteText` qua `Game1.drawLetterMessage` | Chưa có hook | **Chưa có (GAP)** · Sách thất lạc mở ra `LetterViewerMenu(message)`. | `GameLocation.readNote` hoặc `LetterViewerMenu(string)` constructor | Nội dung chia trang bằng ký tự `^`. Cần chia trang song ngữ đồng bộ. |

---

### Nhóm 3: Nhà Gấu Mèo, Nhà Văn Hóa & Joja (Raccoons & Bundles)

| STT | Phân hệ / Bề mặt | Mã nguồn Vanilla 1.6.15 (`file:line+method`) | Nguồn Asset / Kiểu dữ liệu | Vai trò Font / Renderer | Điểm nối Mod hiện tại (`Patches/`) | Trạng thái & Bằng chứng thực nghiệm | Điểm mở rộng an toàn nhất | Ràng buộc Hình học / Input / Nguồn |
|:---:|---|---|---|---|---|---|---|---|
| 13 | **Thoại gấu mèo hoang dã (Plain Dialogue)** | [Raccoon.cs.checkAction](<D:/Downloads/Stardew_decompilers/StardewValley.Characters/Raccoon.cs:415>) `checkAction` | `Strings/1_6_Strings:Raccoon_intro` / `interim` | `SpriteText` qua `drawObjectDialogue` | `ObjectDialoguePatch.cs` | **Đã hỗ trợ (PASS)** · Xác nhận: Ảnh xác nhận câu gấu mèo nhờ giúp đỡ đã song ngữ; chưa nghiệm thu toàn bộ nhánh thưởng. | Đã bắt qua `ObjectDialoguePatch` | Chuỗi tĩnh nạp trực tiếp qua `Game1.content.LoadString`. |
| 14 | **Hover yêu cầu giao nộp Gấu mèo (Dried Mushrooms...)** | [JunimoNoteMenu.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/JunimoNoteMenu.cs:1321>) `draw` · `c.hoverText` | `Data/Objects` (Flavored Item DisplayName) | `Game1.smallFont` qua `IClickableMenu.drawHoverText` | `TooltipPatch.cs` (bị miss vì không gọi `drawToolTip`) | **Bị thiếu (GAP)** · Bằng chứng: Hover Dried Purple Mushrooms chỉ hiện 1 thứ tiếng Anh. | Postfix `JunimoNoteMenu.performHoverAction` hoặc Prefix `drawHoverText` | Menu gấu mèo dùng `JunimoNoteMenu` chế độ `singleBundleMenu`, gán `item.DisplayName` vào `c.hoverText` và vẽ bằng `drawHoverText`. |
| 15 | **Cửa hàng vợ gấu mèo (Raccoon Shop)** | [Raccoon.cs.TryOpenShopMenu](<D:/Downloads/Stardew_decompilers/StardewValley.Characters/Raccoon.cs:70>) `Utility.TryOpenShopMenu` | `Data/Shops:Raccoon` | `SpriteBatch.DrawString` + Tooltip | `TooltipPatch.cs` | **Tiêu đề song ngữ (PARTIAL)** · Tiêu đề tooltip item song ngữ; mô tả item và thoại chủ shop chưa có. | `ShopMenu` adapter | Cần hỗ trợ mô tả item và câu thoại bên chân dung chủ shop. |
| 16 | **Bundle Nhà Văn Hóa (Standard + Remixed)** | [JunimoNoteMenu.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/JunimoNoteMenu.cs:1321>) `draw` | `netWorldState.BundleData`, `Data/Bundles` | `Game1.dialogueFont` (`b.DrawString`) | Chưa có hook | **Chưa có (GAP)** · Tên bundle và mô tả phần thưởng vẽ bằng DrawString. | `JunimoNoteMenu.draw` Prefix/Transpiler | Phải bảo toàn cờ kiểm tra `canReadJunimoText` (chưa đọc được thì hiện `???`). |
| 17 | **Dự án phát triển Joja (Joja CD Projects)** | [JojaCDMenu.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/JojaCDMenu.cs:220>) `draw` | `Strings/UI:JojaCDMenu_*` | `SpriteText.drawString` + `smallFont` | Chưa có hook | **Chưa có (GAP)** · Bảng ký hợp đồng Joja chưa có adapter. | `JojaCDMenu.draw` Prefix | Layout cố định dạng chứng từ ngân hàng; chữ dài dễ tràn khung tiền. |

---

### Nhóm 4: Bảng Tin, Nhiệm Vụ & Lịch (Bulletin Board, Quests & Calendar)

| STT | Phân hệ / Bề mặt | Mã nguồn Vanilla 1.6.15 (`file:line+method`) | Nguồn Asset / Kiểu dữ liệu | Vai trò Font / Renderer | Điểm nối Mod hiện tại (`Patches/`) | Trạng thái & Bằng chứng thực nghiệm | Điểm mở rộng an toàn nhất | Ràng buộc Hình học / Input / Nguồn |
|:---:|---|---|---|---|---|---|---|---|
| 18 | **Lịch thị trấn: Sinh nhật NPC & Lễ hội** | [Billboard.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/Billboard.cs:406>) `draw` | `Data/Festivals`, `Strings/UI`, `NPC.displayName` | `Game1.dialogueFont` + `drawHoverText` | Chưa có hook | **Bị thiếu (GAP)** · Bằng chứng ảnh: Hover sinh nhật NPC và lễ hội chỉ hiện tiếng Anh. | `Billboard.performHoverAction` (sửa `hoverText`) | `Billboard.calendarDayData` tạo sẵn chuỗi `HoverText`. Phải tái dựng song ngữ cho cả tên NPC và tên lễ hội. |
| 19 | **Bảng thông báo nhận việc (Help Wanted Daily)** | [Billboard.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/Billboard.cs:406>) `draw` | `Data/Quests`, `Strings/StringsFromCSFiles` | `Utility.drawTextWithShadow` (`dialogueFont`) | Chưa có hook | **Bị thiếu (GAP)** · Ảnh chụp màn hình: Poster Help Wanted nguyên bản tiếng Anh. | `Billboard.draw` Prefix / `Quest.currentDescription` | Nhiệm vụ sinh động dùng `DescriptionElement` chứa mảnh ghép NPC/vật phẩm. Phải lấy đúng mảnh theo locale phụ. |
| 20 | **Bảng nhiệm vụ đặc biệt (Special Orders Board)** | [SpecialOrdersBoard.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/SpecialOrdersBoard.cs:178>) `draw` | `Data/SpecialOrders`, `Strings/SpecialOrders` | `Utility.drawTextWithShadow` (`dialogueFont`) | Chưa có hook | **Bị thiếu (GAP)** · Ảnh chụp màn hình: Tên nhiệm vụ và yêu cầu chỉ có tiếng Anh. | `SpecialOrdersBoard.draw` Prefix/Postfix | Thẻ bài có diện tích vẽ rất hẹp; tên dài phải giới hạn 2 dòng hoặc scale font. |
| 21 | **Bảng lệnh đặc biệt của Qi (Qi's Special Orders)** | [SpecialOrdersBoard.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/SpecialOrdersBoard.cs:178>) `draw` | `Data/SpecialOrders` (Qi prefix) | `Utility.drawTextWithShadow` | Chưa có hook | **Bị thiếu (GAP)** · Chung kiến trúc với Special Orders Board. | Tái dùng adapter `SpecialOrdersBoard` | Dùng texture nền tím của Qi (`mouseCursors2`). |
| 22 | **Nhật ký nhiệm vụ người chơi (QuestLog / Journal)** | [QuestLog.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/QuestLog.cs:584>) `draw` | `Data/Quests`, `Data/SpecialOrders` | `SpriteText` (tiêu đề) + `Utility.drawTextWithShadow` | Chưa có hook | **Chưa có (GAP)** · Danh sách nhiệm vụ đang làm và chi tiết mục tiêu chưa có song ngữ. | `QuestLog.draw` Prefix/Postfix | Nút "Hủy nhiệm vụ" và "Nhận thưởng" cần tính toán lại toạ độ Y khi mô tả mục tiêu dài ra. |

---

### Nhóm 5: Cửa Hàng, Xây Dựng, Rạp Phim & Lời Khuyên (Shops, Construction & Movies)

| STT | Phân hệ / Bề mặt | Mã nguồn Vanilla 1.6.15 (`file:line+method`) | Nguồn Asset / Kiểu dữ liệu | Vai trò Font / Renderer | Điểm nối Mod hiện tại (`Patches/`) | Trạng thái & Bằng chứng thực nghiệm | Điểm mở rộng an toàn nhất | Ràng buộc Hình học / Input / Nguồn |
|:---:|---|---|---|---|---|---|---|---|
| 23 | **Thoại chủ cửa hàng bên chân dung (Shop Owner)** | [ShopMenu.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/ShopMenu.cs:1877>) `draw` · `portraitPerson` | `Data/Shops:Owner` dialogue string | `Game1.dialogueFont` (`Utility.drawTextWithShadow`) | Chưa có hook | **Chưa có (GAP)** · Câu chào của Pierre, Marnie, Gus bên cạnh avatar vẫn đơn ngữ. | `ShopMenu.setUpShopOwnerDialogue` | Chiều rộng wrap cố định 304 pixel ở mép trái menu. Không gian hẹp, cần xếp trên dưới gọn. |
| 24 | **Gus khuyên tặng quà (Dynamic Gift Advice)** | [NPC.cs.loadCurrentDialogue](<D:/Downloads/Stardew_decompilers/StardewValley/NPC.cs:3608>) `loadCurrentDialogue` | `Strings/StringsFromCSFiles:NPC.cs.4097` + `%revealtaste` | `SpriteText` qua `DialogueBox` | `DialogueSourcePatch.cs` (bị miss vì mất key) | **Bị thiếu (GAP)** · Lời khuyên tặng bia/quà của Gus chỉ hiện tiếng Anh do `new Dialogue(..., null, formatted)`. | `NPC.loadCurrentDialogue` Harmony Postfix | Chuỗi sinh bằng `string.Format` kết hợp nhiều tham số runtime và lệnh ẩn `%revealtaste`. |
| 25 | **Xây dựng & Nâng cấp công trình (Carpenter Menu)** | [CarpenterMenu.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/CarpenterMenu.cs:1294>) `draw` | `Data/Buildings` (DisplayName, Description) | `SpriteText` (tên) + `Utility.drawTextWithShadow` | Chưa có hook | **Chưa có (GAP)** · Menu xây chuồng, nhà của Robin chưa có bản dịch phụ. | `CarpenterMenu.draw` Prefix | Cần tính toán kích thước khung preview tòa nhà và danh sách nguyên liệu. |
| 26 | **Poster phim rạp chiếu (Movie Poster)** | [GameLocation.cs.performAction](<D:/Downloads/Stardew_decompilers/StardewValley/GameLocation.cs:8694>) `performAction` | `Data/Movies` (Title, Description) | `SpriteText` via `Game1.multipleDialogues` | `MoviePosterPatch.cs` (preview 26) | **Đã hỗ trợ & Nghiệm thu ảnh (PASS)** · Poster "Now Playing" hiển thị tên và mô tả song ngữ chuẩn. | `MoviePosterPatch.cs` (Scope action guard) | Dùng adapter nạp `Data/Movies` dạng `List<MovieData>`. Bố cục 2 khối song ngữ. |
| 27 | **Quầy ăn rạp phim & Lời thoại cảnh chiếu phim** | [MovieTheater.cs](<D:/Downloads/Stardew_decompilers/StardewValley.Locations/MovieTheater.cs:1698>) · [MovieTheaterScreeningEvent.cs](<D:/Downloads/Stardew_decompilers/StardewValley.Events/MovieTheaterScreeningEvent.cs:656>) | `Data/Movies`, `Strings/MoviesReactions` | `SpriteText` qua shop menu và event `/message` | Phụ thuộc ShopMenu và Event message | **Một phần (PARTIAL)** · Ảnh xác nhận hộp lời quầy đồ ăn tĩnh đã dịch qua ObjectDialoguePatch; menu Concessions và cảnh chiếu/event chưa có adapter riêng. | Tái dùng adapter Shop & Event | Đoạn phim chạy tự động, không để thời gian đọc phụ đè mất nhịp chuyển cảnh. |

---

### Nhóm 6: Thư Từ & Ghi Chú Bí Mật (Letters & Notes)

| STT | Phân hệ / Bề mặt | Mã nguồn Vanilla 1.6.15 (`file:line+method`) | Nguồn Asset / Kiểu dữ liệu | Vai trò Font / Renderer | Điểm nối Mod hiện tại (`Patches/`) | Trạng thái & Bằng chứng thực nghiệm | Điểm mở rộng an toàn nhất | Ràng buộc Hình học / Input / Nguồn |
|:---:|---|---|---|---|---|---|---|---|
| 28 | **Thư trong hòm thư (Mailbox Letters)** | [LetterViewerMenu.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/LetterViewerMenu.cs:909>) `draw` | `Data/mail` (Text, Title) | `SpriteText.drawString` (giấy thư) | Chưa có adapter LetterViewer riêng; CollectionsPageHoverPatch có tiêu đề thư | **Một phần (PARTIAL)** · Tiêu đề trong Collections có đường ghép; nội dung cửa sổ đọc thư chưa có adapter. | `LetterViewerMenu(string mail, ...)` constructor | Thư có chứa cú pháp quà tặng `%item ... %%`. Parser vanilla phải giữ quyền cấp item duy nhất 1 lần. |
| 29 | **Ghi chú bí mật & Nhật ký đảo (Secret Notes)** | [LetterViewerMenu.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/LetterViewerMenu.cs:909>) `draw` | `Data/SecretNotes` | `SpriteText` hoặc ảnh viết tay | CollectionsPagePatch/DrawPatch hỗ trợ tiêu đề/ảnh hover | **Một phần (PARTIAL)** · Collections có ghép tiêu đề; body cửa sổ đọc và các ảnh chứa chữ chưa được phủ toàn bộ. | `LetterViewerMenu(int secretNoteIndex)` constructor | Một số secret note là texture ảnh (bản đồ kho báu), không thể dịch bằng text renderer. |

---

### Nhóm 7: Túi Đồ, Chỉ Số, Kỹ Năng & Chế Tạo (Inventory, Tooltips & Skills)

| STT | Phân hệ / Bề mặt | Mã nguồn Vanilla 1.6.15 (`file:line+method`) | Nguồn Asset / Kiểu dữ liệu | Vai trò Font / Renderer | Điểm nối Mod hiện tại (`Patches/`) | Trạng thái & Bằng chứng thực nghiệm | Điểm mở rộng an toàn nhất | Ràng buộc Hình học / Input / Nguồn |
|:---:|---|---|---|---|---|---|---|---|
| 30 | **Tooltip túi đồ: Tiêu đề & Tên vật phẩm** | [IClickableMenu.cs.drawToolTip](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/IClickableMenu.cs:996>) `drawToolTip` | `Data/Objects`, `Data/Weapons`, `Data/BigCraftables` | `Game1.dialogueFont` (Bold) | `TooltipPatch.cs` (`FormatTooltipTitle`) | **Có hook trong phạm vi nêu dưới (PARTIAL)** · Ảnh đã xác nhận: Các tên tooltip qua drawToolTip có hook; ảnh xác nhận một số vật phẩm, không phải mọi item. | Đã hook tại `TooltipPatch` | `ref hoverTitle` được ghép trực tiếp và đăng ký với `BilingualStringTracker`. |
| 31 | **Tooltip túi đồ: Mô tả chi tiết vật phẩm** | [IClickableMenu.cs.drawToolTip](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/IClickableMenu.cs:996>) `drawToolTip` | `Data/Objects:Description` | `Game1.smallFont` | `TooltipPatch.cs` (chưa format `hoverText`) | **Bị thiếu (GAP)** · Tooltip hiện tại chỉ song ngữ dòng tên; dòng mô tả bên dưới vẫn đơn ngữ. | `TooltipPatch.Prefix` (sửa thêm `ref string hoverText`) | Cần kiểm soát chiều cao tooltip để không tràn ra ngoài đáy màn hình (đặc biệt ở zoom cao). |
| 32 | **Chỉ số Buff & Tác dụng tức thời (Buffs Tooltip)** | [IClickableMenu.cs.drawToolTip](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/IClickableMenu.cs:996>) `drawToolTip` | `Data/Buffs`, `Strings/UI:ItemHover_Energy` | `Game1.smallFont` | `TooltipPatch.cs` | **Một phần (PARTIAL)** · Năng lượng/Máu theo game; tên buff mod chưa format đầy đủ. | `TooltipPatch.cs` | Icon buff đi kèm số chỉ số cộng (+1 Tốc độ, +2 Câu cá). Giữ icon và số, chỉ song ngữ tên buff. |
| 33 | **Thông tin nâng cấp công cụ & Rèn ngọc (Forge)** | [IClickableMenu.cs.drawToolTip](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/IClickableMenu.cs:996>) `drawToolTip` | `Strings/UI` (Forged, Enchantment level) | `Game1.smallFont` (`Color.DarkRed`) | `TooltipPatch.cs` | **Chưa có (GAP)** · Dòng chú thích rèn ngọc (VD: "Crit (+3)") chưa song ngữ. | `TooltipPatch.cs` | Chuỗi ghép trực tiếp trong thân hàm `drawToolTip`. |
| 34 | **Tab Sức mạnh / Tinh thông (Powers Tab)** | [PowersTab.cs.performHoverAction](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/PowersTab.cs:106>) `performHoverAction` | `Data/Powers`, `Strings/UI` | `Game1.dialogueFont` + `smallFont` | `PowersTabPatch.cs` | **Có hook trong phạm vi nêu dưới (PARTIAL)** · Code ghép tên sức mạnh; descriptionText vẫn lấy c.hoverText chính và wrap lại. Chưa có bằng chứng song ngữ thân mô tả. | Đã hook tại `PowersTabPatch` | Hover cập nhật text và wrap theo khung tooltip. |
| 35 | **Trang Kỹ năng & Nghề nghiệp (Skills Page)** | [SkillsPage.cs.performHoverAction](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/SkillsPage.cs:326>) `performHoverAction` | `Strings/UI:LevelUp_ProfessionName_*` | `Game1.smallFont` qua `drawHoverText` | `SkillsPagePatch.cs` | **Có hook trong phạm vi nêu dưới (PARTIAL)** · Code ghép hoverTitle của kỹ năng/nghề nghiệp; thân mô tả không được dịch ở patch này. | Đã hook tại `SkillsPagePatch` | Dùng `SkillProfessionData` tra cứu theo ID nghề nghiệp. |
| 36 | **Màn hình lên cấp & Chọn nghề nghiệp (Level Up)** | [LevelUpMenu.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/LevelUpMenu.cs:689>) `draw` | `Strings/UI` (Level up title, choices) | `Game1.dialogueFont` + `smallFont` | `LevelUpMenuPatch.cs` · `LevelUpLayoutHelper.cs` | **Có hook trong phạm vi nêu dưới (PARTIAL)** · Kiểm tra offline: Code thêm phụ đề tên hai nghề nghiệp; mô tả/phần thưởng không được coi là đã phủ hết. | Đã hook tại `LevelUpMenuPatch` | Tính toán lại chiều cao hộp thoại lựa chọn để tránh tràn đáy menu. |
| 37 | **Trang Chế tạo & Nấu ăn (Crafting / Cooking)** | [CraftingPage.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/CraftingPage.cs:606>) `draw` | `Data/CraftingRecipes`, `Data/CookingRecipes` | `drawHoverText` & `drawRecipeDescription` | `TooltipPatch.cs` (chỉ tiêu đề) | **Một phần (PARTIAL)** · Tên món chế tạo song ngữ; danh sách nguyên liệu và mô tả món chưa song ngữ. | `CraftingRecipe.drawRecipeDescription` | Chứa danh sách tên nguyên liệu kèm số lượng sở hữu/cần. |

---

### Nhóm 8: Bộ Sưu Tập & Thành Tựu (Collections & Achievements)

| STT | Phân hệ / Bề mặt | Mã nguồn Vanilla 1.6.15 (`file:line+method`) | Nguồn Asset / Kiểu dữ liệu | Vai trò Font / Renderer | Điểm nối Mod hiện tại (`Patches/`) | Trạng thái & Bằng chứng thực nghiệm | Điểm mở rộng an toàn nhất | Ràng buộc Hình học / Input / Nguồn |
|:---:|---|---|---|---|---|---|---|---|
| 38 | **Tab Bộ sưu tập (Vật phẩm, Cá, Khoáng sản...)** | [CollectionsPage.cs.createDescription](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/CollectionsPage.cs:696>) `createDescription` · `522` `draw` | `Data/Objects`, `Data/Fish`, `Strings/UI` | `Game1.smallFont` | `CollectionsPagePatch.cs` · `CollectionsPageHoverPatch.cs` | **Có hook trong phạm vi nêu dưới (PARTIAL)** · Code ghép tên/tiêu đề, giữ phần body/stats/count gốc. Không coi mô tả và số liệu đã được dịch toàn bộ. | Đã hook tại `CollectionsPagePatch` | Layout helper xử lý xuống dòng và canh lề ảnh ghi chú. |
| 39 | **Cửa sổ / Popup Thành tựu (Achievements)** | [HUDMessage.cs](<D:/Downloads/Stardew_decompilers/StardewValley/HUDMessage.cs:138>) · [Game1.cs](<D:/Downloads/Stardew_decompilers/StardewValley/Game1.cs:3508>) | `Data/Achievements` (Name, Description) | `Game1.dialogueFont` qua `HUDMessage` | Chưa có hook HUDMessage | **Chưa có (GAP)** · Thông báo mở khóa thành tựu trên màn hình chưa song ngữ. | `HUDMessage(string, ...)` constructor | Thành tựu có kèm icon ngôi sao; cần giữ nguyên bố cục icon và thời gian hiển thị. |

---

### Nhóm 9: Hệ Thống, Lưu Game, Menu Chính & HUD (System, Saves, Title & HUD)

| STT | Phân hệ / Bề mặt | Mã nguồn Vanilla 1.6.15 (`file:line+method`) | Nguồn Asset / Kiểu dữ liệu | Vai trò Font / Renderer | Điểm nối Mod hiện tại (`Patches/`) | Trạng thái & Bằng chứng thực nghiệm | Điểm mở rộng an toàn nhất | Ràng buộc Hình học / Input / Nguồn |
|:---:|---|---|---|---|---|---|---|---|
| 40 | **Bảng tổng kết cuối ngày / Vận chuyển (Shipping)** | [ShippingMenu.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/ShippingMenu.cs:777>) `draw` | `Strings/UI:ShippingMenu_*` | `SpriteText.drawString` + `dialogueFont` | Chưa có hook | **Chưa có (GAP)** · Các hạng mục Nông sản, Nhặt nhạnh, Cá... chưa có dòng phụ. | `ShippingMenu.draw` Prefix/Postfix | Menu tính toán hoạt cảnh cộng tiền chạy từng dòng số liệu. |
| 41 | **Màn hình Tải game (Load Game Menu)** | [LoadGameMenu.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/LoadGameMenu.cs:1008>) `draw` | `Strings/StringsFromCSFiles:LoadGameMenu*` | `Game1.dialogueFont` (`b.DrawString`) | Chưa có hook | **Chưa có (GAP)** · Thông tin file save (Tên nông trại, ngày tháng, số tiền) đơn ngữ. | `LoadGameMenu.draw` Prefix | Layout theo từng ô save slot; độ rộng bị cố định theo kích thước banner. |
| 42 | **Menu Tiêu đề & Tạo nhân vật (TitleMenu)** | [TitleMenu.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/TitleMenu.cs:1830>) `draw` | `Strings/UI:TitleMenu_*` | `SpriteText` + button textures | Chưa có hook | **Chưa có (GAP)** · Các nút Chơi đơn, Chơi mạng, Thoát chưa có bản dịch phụ. | `TitleMenu.draw` Prefix | Nút bấm dùng texture sprite sheet có sẵn chữ tiếng Anh/ngôn ngữ chính. |
| 43 | **Menu Tuỳ chọn & Cài đặt (OptionsPage)** | [OptionsPage.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/OptionsPage.cs:603>) `draw` | `Strings/UI:Options_*` | `Utility.drawTextWithShadow` (`smallFont`) | Chưa có hook | **Chưa có (GAP)** · Tên các thiết lập âm thanh, đồ họa, điều khiển chưa song ngữ. | `OptionsElement.draw` | Danh sách cuộn dạng bảng; cần tránh làm vỡ vị trí các checkbox và thanh trượt slider. |
| 44 | **Thông báo góc màn hình (HUD Messages / Toasts)** | [Game1.cs.showRedMessage](<D:/Downloads/Stardew_decompilers/StardewValley/Game1.cs:3508>) `showRedMessage` · [HUDMessage.cs](<D:/Downloads/Stardew_decompilers/StardewValley/HUDMessage.cs:138>) | `Strings/StringsFromCSFiles`, `Strings/UI` | `IClickableMenu.drawHoverText` & `Utility.drawTextWithShadow` | Chưa có hook | **Chưa có (GAP)** · Thông báo "Không thể đặt vật phẩm ở đây", "Hết năng lượng" chưa song ngữ. | `Game1.showRedMessageUsingLoadString` | Hàng đợi toast xếp chồng theo trục Y; chữ 2 dòng sẽ làm tăng `heightUsed`. |
| 45 | **Đồng hồ, Ngày tháng & Hộp tiền (DayTimeMoneyBox)** | [DayTimeMoneyBox.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/DayTimeMoneyBox.cs:335>) `draw` | `Strings/StringsFromCSFiles:DayTimeMoneyBox*` | `Game1.dialogueFont` + `tinyFont` | Chưa có hook | **Chưa có (Ưu tiên thấp)** · Ngày/tháng/giờ hiển thị ở góc trên bên phải. | `DayTimeMoneyBox.draw` | Khung đồ họa cố định rất nhỏ; khuyến nghị giữ đơn ngữ hoặc chỉ hiện tooltip khi hover. |

---

### Nhóm 10: Trò Chơi Phụ & Đa Người Chơi (Minigames & Multiplayer)

| STT | Phân hệ / Bề mặt | Mã nguồn Vanilla 1.6.15 (`file:line+method`) | Nguồn Asset / Kiểu dữ liệu | Vai trò Font / Renderer | Điểm nối Mod hiện tại (`Patches/`) | Trạng thái & Bằng chứng thực nghiệm | Điểm mở rộng an toàn nhất | Ràng buộc Hình học / Input / Nguồn |
|:---:|---|---|---|---|---|---|---|---|
| 46 | **Minigame Câu cá (BobberBar / Fishing)** | [BobberBar.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/BobberBar.cs:664>) `draw` | `Strings/UI:BobberBar_*` (Perfect!, Hit!) | `SpriteText` & `b.DrawString` | Chưa có hook | **Chưa có (GAP)** · Chữ báo trạng thái kéo cần hiển thị nhanh trong vài giây. | `BobberBar.draw` | Nhịp độ gameplay thời gian thực nhanh, chữ biến mất ngay khi cá cắn câu. |
| 47 | **Minigame Arcade (Junimo Kart & Prairie King)** | [MineCart.cs](<D:/Downloads/Stardew_decompilers/StardewValley.Minigames/MineCart.cs:6114>) · [AbigailGame.cs](<D:/Downloads/Stardew_decompilers/StardewValley.Minigames/AbigailGame.cs:55>) | Textures tĩnh (`Minigames/...`) + `DrawString` | SpriteFont nội bộ & baked pixel texture | Chưa có adapter minigame | **Hỗn hợp text runtime và texture** · Logo, bảng điểm và biển báo được vẽ thẳng vào sprite texture. | Text runtime cần adapter; chữ baked mới cần thay asset texture | Renderer văn bản không can thiệp được vào các chữ đã vẽ chết vào điểm ảnh. |
| 48 | **Trò chơi Sòng bạc & Máy gắp thú (Slots & CraneGame)** | [Slots.cs](<D:/Downloads/Stardew_decompilers/StardewValley.Minigames/Slots.cs:355>) · [CraneGame.cs](<D:/Downloads/Stardew_decompilers/StardewValley.Minigames/CraneGame.cs:1925>) | `Strings/StringsFromCSFiles` + textures | `Game1.dialogueFont` | Chưa có hook | **Chưa có (GAP)** · Hướng dẫn cược và thông báo trúng thưởng đơn ngữ. | `Slots.draw` / `CraneGame.draw` | Chữ hiển thị lồng trong khung máy arcade. |
| 49 | **Khung Chat & Đặt tên Đa người chơi (Multiplayer Chat & Naming)** | [ChatBox.cs.draw](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/ChatBox.cs:690>) `draw` · [NamingMenu.cs](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/NamingMenu.cs:170>) | Người chơi nhập + `Multiplayer.cs` system msgs | `Game1.smallFont` | Chưa có hook | **Không áp dụng cho chat người (AS-DESIGNED)** | Chỉ xử lý thông báo hệ thống có key | Lời chat của người chơi nhập là chuỗi tùy ý runtime; chỉ dịch các câu lệnh hệ thống (`[Server]...`). |
| 50 | **Mã nguồn di động / Nhánh có điều kiện (Mobile/Conditional)** | [Chest.cs.Obsolete](<D:/Downloads/Stardew_decompilers/StardewValley.Objects/Chest.cs:29>) (`Obsolete` mobile attribute) | Không có tiền xử lý `#if ANDROID` | Desktop SMAPI target | Không áp dụng | **Phạm vi nguồn: bản PC 1.6.15** | Không tạo adapter mobile riêng | Thư mục này là bản PC. Không thấy #if Android không chứng minh kiến trúc bản mobile, vì decompiler đọc assembly đã biên dịch. |

---

## 3. Phân tích Các Khoảng Trống Quan Sát từ Ảnh Người Dùng (Gap Deep-Dive)

Đối chiếu trực tiếp với các triệu chứng người dùng đã ghi nhận bằng hình ảnh thực tế:

```
[Bằng chứng quan sát] ──► [Điểm nghẽn mã nguồn Vanilla 1.6.15] ──► [Nguyên nhân kỹ thuật]
1. Dwarf Scroll Bảo tàng ──► LibraryMuseum.cs:461              ──► Ghép chuỗi động làm mất key LoadedDialogueSource
2. Hover Dried Mushrooms  ──► JunimoNoteMenu.cs:1471             ──► Dùng drawHoverText thay vì drawToolTip
3. Dark Shrine Yes/No     ──► GameLocation.cs:9147 + 4430        ──► createQuestionDialogue tạo DialogueBox không có NPC
4. Poster Rạp phim        ──► MoviePosterPatch.cs                ──► ĐÃ GIẢI QUYẾT (Preview 26) qua List<MovieData> scope
5. Phản hồi Alex          ──► DialogueSourcePatch.cs             ──► ĐÃ GIẢI QUYẾT (Preview 25) qua BaseIndex offset
6. Lời khuyên quà Gus     ──► NPC.cs:3702                        ──► string.Format + new Dialogue(null, text) mất key
```

1. **Cổ vật Bảo tàng (Dwarf Scroll...)**:
   - *Mã nguồn*: [LibraryMuseum.cs](<D:/Downloads/Stardew_decompilers/StardewValley.Locations/LibraryMuseum.cs:461>): `Game1.drawObjectDialogue(Game1.parseText(" - " + data.DisplayName + " - " + "^" + data.Description));`
   - *Nguyên nhân*: Game ghép động chuỗi tiêu đề và mô tả, sau đó gọi `parseText`. Chuỗi này không đi qua `LoadString` tĩnh, khiến `LoadedDialogueSource` không có khóa. `ObjectDialoguePatch` không tìm thấy nguồn phụ và tự động fallback về vanilla.
   - *Giải pháp*: Giữ ID cổ vật tại nơi game ghép chuỗi; không suy nguồn chỉ từ tiền tố. Hook trực tiếp `LibraryMuseum.checkAction` để tra `Data/Objects` theo `itemId` đã hiến tặng.

2. **Yêu cầu Gấu mèo (Dried Purple Mushrooms...)**:
   - *Mã nguồn*: [JunimoNoteMenu.cs](<D:/Downloads/Stardew_decompilers/StardewValley.Menus/JunimoNoteMenu.cs:1471>): `IClickableMenu.drawHoverText(b, (!this.singleBundleMenu && ...) ? "???" : JunimoNoteMenu.hoverText, Game1.smallFont);`
   - *Nguyên nhân*: `TooltipPatch` hiện tại chỉ hook `IClickableMenu.drawToolTip`. Tại menu gấu mèo (`singleBundleMenu = true`), game dùng `drawHoverText` và truyền trực tiếp `c.hoverText` (vốn được gán bằng `item.DisplayName` ở dòng 1550/1630). Vì không có đối tượng `Item` truyền vào hàm vẽ, `TooltipPatch` hoàn toàn không được kích hoạt.
   - *Giải pháp*: Mở rộng adapter hoặc hook tại `JunimoNoteMenu.performHoverAction` để định danh nguyên liệu yêu cầu và nạp tên song ngữ.

3. **Câu hỏi Địa điểm & Bàn thờ (Dark Shrine Yes/No Questions)**:
   - *Mã nguồn*: [GameLocation.cs](<D:/Downloads/Stardew_decompilers/StardewValley/GameLocation.cs:9147>) & `4430`: `this.createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:WitchHut_EvilShrineLeft"), this.createYesNoResponses(), "evilShrineLeft");`
   - *Nguyên nhân*: `createQuestionDialogue` khởi tạo `new DialogueBox(dialogue, choices)`. Tại đây `box.characterDialogue` là `null`. [DialogueQuestionPatch.cs](<E:/Sao lưu/UntranslatedStardew/Patches/DialogueQuestionPatch.cs:102>) chỉ xử lý khi `box.characterDialogue != null`. Do đó câu hỏi địa điểm bị từ chối và giữ nguyên bản đơn ngữ.
   - *Giải pháp*: Preview 27 đã thêm `LocationQuestionPatch.cs` để bắt provenance qua `LoadedDialogueSource` và nối lại layout câu hỏi.

4. **Lời khuyên tặng quà của Gus (Format động)**:
   - *Mã nguồn*: [NPC.cs](<D:/Downloads/Stardew_decompilers/StardewValley/NPC.cs:3702>): `message = Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4097", nameAndTitle, itemName);` rồi nối `%revealtaste` và gọi `new Dialogue(this, null, message)`.
   - *Nguyên nhân*: Vì truyền khóa `translationKey = null`, `DialogueSourcePatch` không thể xác định asset gốc. Hơn nữa, chuỗi nạp đã qua phép thế tham số (`string.Format`) nên đối chiếu chuỗi thô sẽ thất bại.
   - *Giải pháp*: Cần một adapter chuyên biệt nạp lại mẫu `{0}/{1}` từ `StringsFromCSFiles` bằng locale phụ và thế tên NPC cùng tên vật phẩm tương ứng theo locale phụ.

---

## 4. Kiến trúc Bố cục Song ngữ Hiện hành (Approved Layout Architecture)

Hệ thống tuân thủ chặt chẽ các quyết định thiết kế đã được người dùng phê duyệt từ Preview 24 đến Preview 26:

```
┌────────────────────────────────────────────────────────┐
│ [HỘI THOẠI / HỘP THÔNG TIN]                            │
│                                                        │
│ Hello there, farmer! Welcome to the valley.            │  ◄── KHỐI CHÍNH (Primary Block)
│ How is your first day going?                           │      Font SpriteText của Game
│                                                        │
│ Xin chào nông dân! Chào mừng bạn đến thung lũng.       │  ◄── KHỐI PHỤ (Secondary Block)
│ Ngày đầu tiên của bạn diễn ra như thế nào?             │      Native Secondary Font (75%)
└────────────────────────────────────────────────────────┘
```

- **Quy tắc nguyên khối (Block Layout)**:
  - Bỏ bố cục xen kẽ từng dòng (interleaved rows). Toàn bộ văn bản ngôn ngữ chính tạo thành một khối liên tục phía trên (hoặc dưới), toàn bộ văn bản ngôn ngữ phụ tạo thành khối liên tục còn lại.
  - Cấu hình `SecondaryFirst`: Hoán đổi vị trí của cả 2 khối (`SecondaryFirst = true` thì khối phụ nằm trên, khối chính nằm dưới).
- **Quy tắc xuất hiện (Reveal Timing)**:
  - Khối phụ chỉ bắt đầu xuất hiện khi khối chính đã hoàn tất hiệu ứng gõ chữ (reveal typing). Ngay cả khi khối phụ nằm ở trên, nó vẫn chờ khối chính hiện xong mới xuất hiện, bảo đảm nhịp đọc tự nhiên và tránh phân tâm.
- **Quy tắc Font & Đo lường**:
  - Ở các khung thoại đã nối, khối chính dùng renderer/font của game (`SpriteText` hoặc `Game1.dialogueFont`).
  - Khối phụ dùng **Native Font của chính ngôn ngữ phụ** ở tỷ lệ co giãn **75%** (`DialoguePairLayout.SecondaryScale = 0.75f`). Đối với tiếng Việt, sử dụng font BMFont chuẩn có dấu đầy đủ; đối với tiếng Trung/Nhật/Hàn/Nga, dùng BMFont native tương ứng.
- **Đa ngôn ngữ mở rộng (Universal Custom Languages)**:
  - Các adapter mới không gán cứng tiếng Việt; cần kiểm chứng riêng từng vai trò font và nguồn dữ liệu. Mục tiêu là 12 ngôn ngữ vanilla và custom language có nguồn/font khai báo hợp lệ. Provider dùng `CustomLanguagePackReader`/`LanguageSourceRegistry`; reader chỉ hỗ trợ một phần ngữ nghĩa Content Patcher. Không cam kết mọi pack hay mọi surface tự hoạt động.

---

## 5. Các Đợt Adapter Ưu tiên (Prioritized Implementation Batches)

Để hoàn thiện độ phủ toàn diện mà không gây hồi quy (regression) hay làm quá tải hệ thống, quá trình triển khai được phân thành 7 đợt rõ ràng:

```
┌────────────────────────────────────────────────────────────────────────┐
│ LỘ TRÌNH TRIỂN KHAI 7 ĐỢT ADAPTER (Phân loại theo mức độ phụ thuộc)    │
└────────────────────────────────────────────────────────────────────────┘
  [Đợt 1] Location Questions (nguồn tĩnh: preview 27) ──► Giải quyết Shrines, Bed, Elevator
     │
  [Đợt 2] Tooltip & Hovers ──────────────────────► Giải quyết Raccoon Dried Mushrooms, Item Descs
     │
  [Đợt 3] Parameterized & Typed Data ────────────► Giải quyết Museum Dwarf Scroll, Gus Gift Advice
     │
  [Đợt 4] Event Payload Text ────────────────────► Giải quyết Cutscene Speak, Message, QuickQuestion
     │
  [Đợt 5] Dedicated Menus ───────────────────────► Billboard (Calendar/Quests), SpecialOrders, Joja
     │
  [Đợt 6] HUD & System Notifications ────────────► Toast Messages, Achievements, End-Day Shipping
     │
  [Đợt 7] Texture-only Text ─────────────────────► Content Patcher graphic assets (Minigames/Logos)
```

### Đợt 1: Câu hỏi Địa điểm & Bàn thờ (Bounded Location Questions)
- **Phạm vi**: `GameLocation.createQuestionDialogue`, `DialogueBox(string, Response[])`.
- **Mục tiêu**: Xử lý giường ngủ ("Đi ngủ?"), bàn thờ Dark Shrines, thang máy hầm mỏ, quầy sách, lễ hội câu mực.
- **Cách thức**: Bắt provenance chuỗi nạp qua `LoadedDialogueSource`, giải Yes/No theo `Strings/Lexicon`, áp dụng `DialogueQuestionLayout`.

### Đợt 2: Mô tả Tooltip & Hover Danh sách (Tooltip Description & List Hovers)
- **Phạm vi**: `IClickableMenu.drawToolTip`, `JunimoNoteMenu.performHoverAction`, `IClickableMenu.drawHoverText`.
- **Mục tiêu**: Bổ sung song ngữ cho mô tả vật phẩm túi đồ và hover nguyên liệu nộp gấu mèo (Dried Purple Mushrooms).
- **Cách thức**: Nối thêm `hoverText` phụ đã format vào sau `hoverText` chính; đo đạc chiều cao tooltip và tự động dịch vị trí Y nếu chạm đáy màn hình.

### Đợt 3: Dữ liệu Động có Tham số (Parameterized & Typed Runtime Data)
- **Phạm vi**: Cổ vật bảo tàng ([LibraryMuseum.cs](<D:/Downloads/Stardew_decompilers/StardewValley.Locations/LibraryMuseum.cs:461>)), Gus khuyên quà tặng ([NPC.cs](<D:/Downloads/Stardew_decompilers/StardewValley/NPC.cs:3702>)).
- **Mục tiêu**: Không phụ thuộc vào key tĩnh thô; giải mã định danh vật phẩm (`(O)itemId`) và tên NPC tại chỗ rồi ghép bản dịch phụ chuẩn.

### Đợt 4: Văn bản Sự kiện & Cắt cảnh (Event Script Payload)
- **Phạm vi**: `Event.DefaultCommands.Speak`, `Message`, `Question`, `QuickQuestion`, `TextAboveHead`.
- **Mục tiêu**: Toàn bộ thoại diễn ra trong lễ hội và cutscene chuyển cảnh.
- **Cách thức**: Bắt giữ `event.fromAssetName` và chỉ số lệnh hiện tại (`event.CurrentCommand`); tra cứu script tương ứng trong asset `Data/Events/*` của locale phụ để trích xuất payload văn bản.

### Đợt 5: Các Menu Chuyên biệt (Dedicated Menus & Boards)
- **Phạm vi**: `Billboard.cs` (Lịch & Help Wanted), `SpecialOrdersBoard.cs`, `QuestLog.cs`, `CarpenterMenu.cs`.
- **Mục tiêu**: Song ngữ hóa toàn diện bảng tin thị trấn, bảng đơn hàng đặc biệt, nhật ký phiêu lưu và xây nhà Robin.
- **Cách thức**: Adapter riêng cho từng menu nhằm xử lý ràng buộc hình học (thẻ bài chật hẹp, bảng gỗ, khung tiền).

### Đợt 6: Thông báo HUD & Thông điệp Hệ thống (HUD, Toasts & System)
- **Phạm vi**: `HUDMessage.draw`, `Game1.showRedMessageUsingLoadString`, `ShippingMenu.cs`.
- **Mục tiêu**: Thông báo nhặt đồ, thông báo lỗi đỏ, bảng tổng kết tiền cuối ngày.
- **Cách thức**: Bắt thông báo có mã khóa; tính toán lại khoảng cách hàng đợi (`ref int heightUsed`) để các toast không đè lên nhau.

### Đợt 7: Chữ Đồ họa trong Texture (Texture-only Assets)
- **Phạm vi**: Logo Junimo Kart, Prairie King, biển hiệu Billboard, nút TitleMenu.
- **Mục tiêu**: Phân biệt ranh giới rõ ràng: những chữ này vẽ chết trong tệp `.png`/`.xnb`. Không thể can thiệp bằng code renderer chữ. Cần cung cấp asset texture thay thế qua Content Patcher.

---

## 6. Tại sao Hook Toàn cục vào `DrawString` là Sai lầm Kiến trúc?

Nhiều dự án mod dịch thuật sơ khai thường cố gắng hook thẳng vào `SpriteBatch.DrawString` toàn cục để tìm chuỗi tiếng Anh và thế bằng chuỗi dịch. Trong kiến trúc phức tạp của Stardew Valley 1.6.15, phương pháp này **hoàn toàn không thể hoạt động** vì các lý do cốt tử sau:

1. **Mất hoàn toàn Ngữ cảnh & Phả hệ Nguồn (Loss of Semantic Provenance)**:
   - Tại tầng `DrawString`, chuỗi truyền vào chỉ là một chuỗi ký tự thô. Từ "Spring" có thể là Mùa Xuân (`Strings/StringsFromCSFiles:Spring`), chiếc Lò xo (`Data/Objects:Spring`), hoặc Hành động Bật nhảy (`Strings/UI:...`). Không thể suy ngược lại asset key nếu chỉ nhìn vào văn bản.
2. **Sai lệch Kích thước Khung & Cắt dòng (Measurement & Wrapping Mismatch)**:
   - Menu của game đo đạc chiều dài chuỗi bằng `font.MeasureString` hoặc `SpriteText.getWidthOfString` **trước khi vẽ** để định cỡ khung hình hộp (texture box). Nếu âm thầm thay chuỗi dài hơn ở lệnh vẽ, chữ sẽ tràn ra ngoài khung nền, đè lên nút bấm hoặc bị kẹp mất chữ.
3. **Phá vỡ Vùng bấm Chuột & Điều hướng Tay cầm (Hitbox & Gamepad Desync)**:
   - Các hộp lựa chọn câu hỏi (`DialogueBox.responseCC`) tính toán vùng chọn (ClickableComponent) dựa trên toạ độ tính trước. Nếu thay đổi layout tại lúc vẽ mà không cập nhật `responseCC`, người chơi dùng tay cầm sẽ trỏ chuột một đằng nhưng bấm trúng đáp án một nẻo.
4. **Vòng lặp Ký tự Gõ dần (Reveal Animation State Machine)**:
   - `DialogueBox` và `SpriteText` sở hữu máy trạng thái gõ chữ (`characterIndexInString`, `characterPosition`). Thay đổi chuỗi tại tầng vẽ sẽ khiến bộ đếm ký tự bị lệch, gây giật chữ hoặc treo không bao giờ gõ xong trang thoại.
5. **Đổ vỡ Hiệu năng (Performance Penalty)**:
   - Có tới 193 điểm gọi `DrawString` và hàng trăm đối tượng được vẽ mỗi khung hình (60 FPS). Việc tra cứu từ điển và phân tích chuỗi tại tầng GPU draw call sẽ làm sụt giảm tốc độ khung hình (frame drops) nghiêm trọng.

---

## 7. Những Giới hạn Không thể Xác định Tĩnh (Static Audit Limitations)

Để duy trì tính trung thực khoa học theo chuẩn Robert C. Martin và nguyên tắc SOLID:

- **Các chuỗi tạo bằng `string.Format` không có metadata**: Nếu code game nối chuỗi tự do (như `text = "A" + b + "C"`) mà không giữ khóa nguồn, không có công cụ phân tích tĩnh nào có thể biết chắc cấu trúc chuỗi phụ tương ứng nếu không có adapter runtime chuyên biệt.
- **Hành vi ngẫu nhiên và điều kiện save**: Các nhánh thoại dùng xác suất `$c`, nhánh câu hỏi theo điểm tình bạn `$d`, hoặc các bundle remixed được sinh ngẫu nhiên theo hạt giống thế giới (world seed) chỉ có thể quan sát khi game đang chạy với dữ liệu cụ thể.
- **Sự can thiệp của các Mod khác (Content Patcher / DDFC)**: Các mod như Content Patcher có thể ghi đè token hoặc thay đổi font runtime. Tệp dịch ngược chỉ phản ánh mã vanilla 1.6.15 gốc.

---

## 8. Ma trận Nghiệm thu Thủ công Tóm tắt (Compact Acceptance Matrix)

| Nhóm Kiểm thử | Ca Đại diện Quan sát trong Game | Hành động & Thao tác | Kết quả Kỳ vọng Đạt chuẩn |
|---|---|---|---|
| **NPC & Câu hỏi** | Alex tại bãi biển (`Wed`) | Nói chuyện, xem prompt, chọn đáp án 1, xem phản hồi | Khối chính hiện trước, khối phụ (native font 75%) hiện sau. Phản hồi 2 trang hiển thị đầy đủ 2 ngôn ngữ. |
| **Địa điểm & Bàn thờ** | Bàn thờ Witch Hut (Dark Shrine) | Tương tác bàn thờ xóa ký ức, chọn Có/Không | Prompt hỏi và 2 lựa chọn Có/Không hiển thị song ngữ; vùng chọn bao trọn cả 2 dòng chữ. |
| **Bảo tàng & Đồ vật** | Bàn trưng bày Dwarf Scroll | Bấm vào cuộn giấy Dwarf Scroll đã quyên góp | Hộp thoại hiện: Tên cuộn giấy song ngữ ở dòng trên, mô tả cổ vật song ngữ ở dòng dưới. |
| **Nhà Gấu Mèo** | Gấu mèo tại Cindersap Forest | Mở menu yêu cầu giao nộp thức ăn, hover vào nấm tím | Bảng yêu cầu hiện tên "Dried Purple Mushrooms / Nấm tím khô" đầy đủ trong tooltip hover. |
| **Bảng tin & Lịch** | Bảng tin trước cửa hàng Pierre | Mở xem lịch sinh nhật, sau đó mở poster Help Wanted | Sinh nhật NPC hiện tên NPC + ngày sinh song ngữ. Poster giao hàng hiện mô tả và phần thưởng song ngữ. |
| **Rạp chiếu phim** | Bảng áp phích rạp phim Pelican | Bấm kiểm tra áp phích phim đang chiếu | 2 trang hiển thị: Tên phim song ngữ (trang 1) và nội dung tóm tắt song ngữ (trang 2). |


### Cập nhật preview 29

- Hiện vật bảo tàng: đã sửa chuẩn hóa xuống dòng; item96/106/114 kiểm tra XNB+VI thật, chờ UI.
- Event `speak`/`message`: đã có adapter source/index và kiểm tra tự động; chỉ cho script tương ứng, không bao gồm event question/fork/festivals. Event Emily may đồ trong pack VI cài đặt vẫn lệch cấu trúc 26/19 lệnh và fallback.
- Bảng trạng thái theo thư mục hiện tại: [CONTENT-COVERAGE.vi.md](CONTENT-COVERAGE.vi.md). Các mục cũ nói event bị chặn mô tả baseline preview28; preview29 chỉ mở guard cho hộp được adapter xác minh.
