# Untranslated Stardew

Untranslated Stardew là bản mod SMAPI cho Stardew Valley 1.6 giúp hiển thị tên vật phẩm và giao diện song ngữ trong bảng thông tin (tooltip), menu kỹ năng, menu thăng cấp nghề, tab quyền hạn, bộ sưu tập (bao gồm tiêu đề thư từ, ghi chú bí ẩn và tab bên lề), hỗ trợ font theo vai trò cố định (Fixed Role Fonts) cho mọi ngôn ngữ game và ngôn ngữ tùy chỉnh, xử lý đầy đủ các loại vật phẩm bao gồm giấy dán tường và sàn nhà, đồng thời hỗ trợ nạp nguồn ngôn ngữ và tên song ngữ từ các mod Content Patcher / C# bên thứ ba, cung cấp API tra cứu tên đa ngôn ngữ và tìm kiếm không dấu (chuẩn hóa tiếng Việt `đ`/`Đ`).

Chi tiết hợp đồng custom language, giới hạn font và cách kiểm tra: [Font theo vai trò 1.3.0](docs/LOCALE-ROLE-FONTS-1.3.0.md). Custom pack khác Việt hóa cần khai báo nguồn; hỗ trợ không đồng nghĩa tự nhận mọi định dạng pack.

---

## 1. Tính năng chính (Features)

- **Font Theo Vai Trò Cố Định (Fixed Role Fonts):**
  - **Phân định vai trò cứng (Strict Role Binding):** Đoạn chữ thuộc ngôn ngữ chính luôn dùng font chính (`Game1.dialogueFont`, `Game1.smallFont`); đoạn chữ thuộc ngôn ngữ phụ luôn dùng font phụ (nạp từ XNB của locale tương ứng hoặc nguồn tùy chỉnh), kể cả đối với các từ thuần ASCII hoặc không dấu. Không còn chọn font chính chỉ vì font chính cũng chứa ký tự của tên phụ. Nếu font vai trò thiếu glyph/tài nguyên, vẫn có dự phòng và cảnh báo.
  - **Bất biến vai trò khi đổi vị trí (`SecondaryFirst` Invariance):** Khi bật tùy chọn `SecondaryFirst = true`, mod hoán đổi vị trí hiển thị (ngôn ngữ phụ ở trên, ngôn ngữ chính ở dưới) nhưng giữ liên kết font - vai trò; đoạn phụ vẫn dùng font phụ, đoạn chính vẫn dùng font chính.
  - **Cấu trúc thành phần văn bản (`StructuredTextComposition`):** Mọi chuỗi song ngữ đều được gắn kèm siêu dữ liệu vai trò rõ ràng, duy trì qua quá trình ngắt dòng (`line wrapping`), tính toán kích thước (`MeasureString`), và vẽ (`DrawString`).
  - **Hỗ trợ đầy đủ 12 ngôn ngữ game:** `en` (default), `es`, `de`, `fr`, `pt`, `ru`, `ja`, `zh`, `ko` (+16px line spacing), `it` (fallback font gốc), `tr` (+4px line spacing), `hu`.

- **Hiển thị Song ngữ Giấy dán tường & Sàn nhà (`Wallpaper & Flooring`):**
  - Khắc phục lỗi thiếu tên song ngữ cho các vật phẩm Giấy dán tường `(WP)` và Sàn nhà `(FL)` do game vanilla thiết kế `WallpaperDataDefinition` và `FlooringDataDefinition` trả về `RawData = null`.
  - Phân giải chuẩn xác qua các khóa chuỗi chia sẻ `Wallpaper.cs.13204` (Wallpaper) và `Wallpaper.cs.13203` (Flooring) trong `Strings/StringsFromCSFiles` cho mọi ngôn ngữ hỗ trợ.
  - Hỗ trợ đầy đủ **toàn bộ 13 tiền tố loại định danh vật phẩm**: `(O)`, `(BC)`, `(B)`, `(FL)`, `(F)`, `(H)`, `(M)`, `(P)`, `(S)`, `(T)`, `(TR)`, `(WP)`, `(W)`.
  - Lưu trữ cache độc lập theo Qualified Item ID, bảo đảm các mẫu khác ID nhưng cùng tên hiển thị (như các loại giấy dán tường) không bị ghi đè hoặc đánh rơi bản ghi.

- **Cơ chế Nguồn Ngôn ngữ Tùy chỉnh Tổng quát (Generic Language Sources):**
  - Đọc descriptor tường minh từ CP/API hoặc CustomFields của `Data/AdditionalLanguages`; không tự suy luận font/asset của mọi pack.
  - Cung cấp asset từ điển SMAPI mở `Mods/GiaHung.UntranslatedStardew/LanguageSources` để các tác giả gói Content Patcher khai báo nguồn chuỗi (`Strings`), tệp font (`SmallFont`, `DialogueFont`), mẫu tài nguyên (`AssetPattern`) và thư mục gốc (`ContentRoot`).
  - Cung cấp C# API `RegisterLanguageSource` và `UnregisterLanguageSource` trong `IUntranslatedStardewApi` cho các mod C#.
  - Kiểm tra asset không dùng `..` thoát ContentRoot; không xem đây là sandbox chống junction/symlink.
  - Khai báo bộ nạp tương thích ngược (bundled descriptor) cho bản dịch tiếng Việt `Kou.ViethoaSV` mà không cần người dùng sửa gói pack cũ.
  - Cảnh báo khi thiếu nguồn custom/font/glyph; tên hai ngôn ngữ giống nhau chỉ hiển thị một lần.

- **Mở rộng Giao diện Song ngữ (UI Expansion):**
  - **Bảng Kỹ năng (`SkillsPage`)**: Hiển thị tên kỹ năng (Trồng trọt, Câu cá, Hái lượm, Khai khoáng, Chiến đấu, May mắn) và tên nghề nghiệp cấp 5 / 10 ở dạng song ngữ khi rà chuột (hover). Giữ nguyên mô tả tăng chỉ số theo ngôn ngữ chính.
  - **Menu Thăng cấp Nghề (`LevelUpMenu`)**: Hiển thị tiêu đề nghề nghiệp song ngữ trên hai thẻ lựa chọn khi người chơi đạt cấp 5 hoặc cấp 10 với bố cục đo đạc an toàn: tiêu đề chính vẽ bằng `dialogueFont` 1 dòng chuẩn vanilla tại `top+160`, tiêu đề phụ vẽ bằng `smallFont` tại khoảng trống đo đạc `top+208` (cách dòng đạn đầu tiên 32px), giới hạn chiều rộng cột trước icon (336px/352px) và tự động cắt ngắn thêm dấu `...` nếu vượt quá. Sử dụng trạng thái hiển thị scoped (`LevelUpMenuState`) khôi phục nguyên vẹn danh sách sau khi vẽ và bảo đảm khôi phục ngay cả khi có ngoại lệ (`Finalizer`), không làm rò rỉ chuỗi song ngữ sang logic click/hover hay lưu trạng thái cấu hình cũ.
  - **Vật phẩm & Quyền hạn (`PowersTab`)**: Hiển thị tên song ngữ cho các quyền hạn và vật phẩm đặc biệt đã mở khóa (Chìa khóa Đầu lâu, Bùa phép của Rừng, Tri thức của Gấu, v.v.). Giữ nguyên ẩn danh `???` và không làm lộ nội dung đối với các quyền hạn chưa đạt được.
  - **Bảng Bộ sưu tập (`CollectionsPage`)**:
    - **Vật phẩm & Thành tựu**: Hiển thị tên song ngữ cho nông sản, cá, cổ vật, khoáng sản, món ăn và danh hiệu thành tựu (`Achievements`). Bảo toàn hoàn toàn phần mô tả chi tiết, số lượng đã vận chuyển, giá bán, trạng thái công thức và trạng thái chưa mở khóa (`???`).
    - **Tiêu đề Thư từ (`Letters` - Tab 7)**: Hiển thị tiêu đề thư song ngữ khi rà chuột (hover) trên các bức thư đã đọc (ví dụ: `Bức thư từ Krobus (A Letter From Krobus)`). Trích xuất chính xác trường tiêu đề sau ký tự phân cách `[#]` từ từ điển `Data/mail` của ngôn ngữ phụ; hoàn toàn không can thiệp hay dịch nội dung thân thư. Các thư không có trường tiêu đề hoặc chưa mở khóa (`???`) giữ nguyên theo ngôn ngữ chính.
    - **Tiêu đề Mật thư & Mảnh nhật ký vụn (`Secret Notes & Journal Scraps` - Tab 6)**: Hiển thị tiêu đề song ngữ trên đầu ghi chú (`Ghi chú bí ẩn #X (Secret Note #X)` hoặc `Mảnh nhật ký vụn #X (Journal Scrap #X)`) kết hợp với nội dung thân ghi chú. Đối với các ghi chú chỉ chứa ảnh (image-only notes như #10, #13, #15, #19, #20, #21), tiêu đề song ngữ hiển thị chuẩn xác mà không bị ghép nội dung văn bản thừa. Thân ghi chú được giữ nguyên 100%.
    - **Nhãn Tab Bên lề (`Side Tabs`)**: Hiển thị tên song ngữ khi rà chuột lên các icon chuyển tab bên trái của bảng Bộ sưu tập (ví dụ: `Thư từ (Letters)`, `Ghi chú bí ẩn (Secret Notes)`), không làm lệch bố cục nhãn cố định của game.

- **Hỗ trợ Nội dung Mod (Content Patcher & C# Modded Content):**
  - **Asset Từ điển Tùy chỉnh (`Mods/GiaHung.UntranslatedStardew/Names`)**: Cung cấp asset từ điển SMAPI mở để các gói Content Patcher hoặc mod khác có thể đăng ký tên song ngữ cho vật phẩm, thư từ, quyền hạn, thành tựu, mật thư, và tab thông qua lệnh `EditData`.
  - **Fallback Ngôn ngữ Chuẩn SMAPI**: Tự động thử nghiệm theo thứ tự ưu tiên chuẩn: mã locale chính xác (`zh-CN`) -> mã ngôn ngữ cha (`zh`) -> `default`. Ngôn ngữ `en` chỉ được trả về nếu nguồn mod có cung cấp trực tiếp, không tự tiện gán tiếng Anh bừa bãi.
  - **Độ Ưu Tiên Phân Giải Minh Bạch**: Tên từ Content Patcher / C# API (`External`) > Nguồn dữ liệu có token `[LocalizedText]` > Tệp XNB vanilla gốc > Trả về `null` (giữ nguyên tên chính). Tuyệt đối không thay đổi locale toàn cục của game hay quét đoán chuỗi không căn cứ.

- **Tooltip Song Ngữ (Bilingual Tooltip):**
  - Hiển thị tên vật phẩm ở ngôn ngữ hiện hành của game (ngôn ngữ chính, bao gồm cả bản dịch tiếng Việt từ mod `Kou.ViethoaSV`) và ngôn ngữ phụ được cấu hình (mặc định là `default` theo chuẩn SMAPI, `en`, hoặc bất kỳ ngôn ngữ nào được hỗ trợ).
  - Tự động phát hiện và bỏ qua hiển thị trùng lặp khi tên chính và tên phụ giống nhau (ví dụ: khi đang chơi game bằng tiếng Anh và tên phụ là tiếng Anh).
  - Giữ nguyên các hậu tố số lượng (ví dụ: ` x5`), nhãn công thức chế tạo (ví dụ: `(Công thức)`), và tên động của người dùng.
  - Thứ tự hiển thị linh hoạt: ngôn ngữ chính trước hoặc phụ trước (`SecondaryFirst`).

- **Ngôn ngữ `default` Chuẩn SMAPI (SMAPI Native Default Semantics):**
  - Hỗ trợ lựa chọn ngôn ngữ phụ là `default` song song với `en`.
  - Theo cơ chế SMAPI `TranslationHelper`, `default` đại diện cho nguồn ngôn ngữ gốc / dự phòng không gắn hậu tố mã ngôn ngữ (với game gốc Stardew Valley, tệp gốc không hậu tố là tiếng Anh; với giao diện của mod, `default` trỏ về `i18n/default.json`). Không áp dụng suy đoán dịch cho các mod bên thứ ba không có nguồn tài nguyên được hỗ trợ.
  - Không phát sinh cảnh báo lỗi fallback unsupported khi người dùng chọn `default`. Các cấu hình cũ với `en` vẫn tiếp tục hoạt động hoàn hảo.

- **Phạm vi Phân giải Tên Vật phẩm (Item Category Support):**
  - Vật phẩm thông thường (`Objects`): hoa quả, rau củ, cá, khoáng sản, tài nguyên, hạt giống, đồ thủ công.
  - Vật phẩm chế biến/ngâm ủ động (`Flavored Objects`): Rượu (`Wine`), Mứt (`Jelly`), Dưa muối (`Pickles`), Mật ong (`Honey` / `Wild Honey`), Nước ép (`Juice`), Trứng cá (`Roe`), Trứng cá muối (`Aged Roe`), Mồi câu (`Bait`), Hoa quả khô (`Dried Fruit`), Nấm khô (`Dried Mushroom`), Cá hun khói (`Smoked Fish`). Tự động phân giải tên nguyên liệu trong ngôn ngữ phụ.
  - Tuân thủ thứ tự ưu tiên của game: `displayNameFormat` được ưu tiên hơn `preserveType`, phân giải an toàn các token `[LocalizedText ...]`.
  - Quy tắc số nhiều hoa quả khô: Tôn trọng mẫu ghi đè đặc thù của game (ví dụ: mẫu riêng cho Dried Blueberries không bị thêm số nhiều lần hai); các mẫu chung sử dụng chính xác bộ quy tắc của `Lexicon.makePlural`.
  - Dụng cụ nâng cấp (`Tools`): Nhận diện cấp độ nâng cấp (`UpgradeLevel`) để ghép tiền tố cấp bậc chuẩn (Đồng, Thép, Vàng, Iridium).
  - Máy móc / Công trình thủ công lớn (`BigCraftables`).
  - Vũ khí (`Weapons`).
  - Giày (`Boots`) & Mũ (`Hats`): Trích xuất chính xác trường hiển thị bản địa (trường 6 của Data/Boots và trường 5 của Data/hats) cho ngôn ngữ phụ; không gán nhãn tiếng Anh nội bộ sai lệch khi ngôn ngữ phụ là tiếng khác.
  - Phụ kiện (`Trinkets`) & Quần áo (`Clothing` - Áo, Quần).
  - Nội thất (`Furniture` - phân giải trường token 7).

- **Phụ kiện & Biến thể Động (Trinkets & Dynamic Variants):**
  - Hỗ trợ toàn bộ 8 phụ kiện chuẩn của game 1.6 (`BasiliskPaw`, `FairyBox`, `IceRod`, `IridiumSpur`, `FrogEgg`, `MagicQuiver`, `MagicHairDye`, `ParrotEgg`).
  - Khắc phục triệt để lỗi thiết kế của game khi `TrinketDataDefinition.GetData` trả về `RawData = null`, phân giải chính xác qua nguồn dữ liệu `Data/Trinkets` và chuỗi dịch `Strings/1_6_Strings`.
  - Hỗ trợ các biến thể động nâng cao qua mẫu token `displayNameOverrideTemplate`:
    - Đổi màu Trứng Ếch (`frog_variant_0` -> `frog_variant_7`: Green, Brown, Blue-Green, Blue, Red, Yellow, Void, Prismatic Frog Egg).
    - Cấp bậc gậy băng toàn năng (`PerfectIceRod`).
    - Các biến thể ống tên ma pháp (`PerfectMagicQuiver`, `RapidMagicQuiver`, `HeavyMagicQuiver`).
  - Cache thông minh theo từng biến thể màu sắc và cấp độ, không bị hiển thị nhầm lẫn giữa các biến thể của cùng một loại phụ kiện.

- **Tích Hợp Tìm Kiếm Song Ngữ CJB Item Spawner (CJB Integration):**
  - Tự động phát hiện và tích hợp nếu mod `CJBok.ItemSpawner` được cài đặt; không phụ thuộc cứng mã nguồn/assembly lúc biên dịch.
  - Cho phép gõ tìm kiếm tiếng Việt không dấu (ví dụ: gõ "ca noc" ra "Cá nóc", "trung ech" ra "Trứng Ếch").
  - Cho phép gõ tên tiếng Anh phụ (ví dụ: gõ "pufferfish" tìm thấy "Cá nóc", gõ "frog egg" tìm thấy "Trứng Ếch").
  - Bảo toàn 100% logic lọc danh mục tab (`All`, `Weapons`, `Fish`, v.v.), các bộ lọc quy tắc tùy biến, và thứ tự sắp xếp của CJB (`By Name`, `By Price`, `By Type`, `By ID`).

- **Nạp Asset Độc Lập An Toàn (Isolated Content Loading):**
  - Sử dụng instance `Microsoft.Xna.Framework.Content.ContentManager` riêng biệt để đọc trực tiếp file `.xnb` từ đĩa.
  - Không bao giờ thay đổi `CurrentLanguageCode` toàn cục của game.
  - Không gây ô nhiễm cache tĩnh `LocalizedContentManager.localizedAssetNames` của game.
  - Khởi tạo ServiceProvider theo cơ chế lười (lazy initialization) đảm bảo an toàn khi `Game1.content` chưa sẵn sàng ở pha `Entry`.
  - Cơ chế dự phòng theo từng khóa (per-key fallback): Khi từ điển ngôn ngữ phụ thiếu một khóa cụ thể, tự động lấy khóa tương ứng từ tệp gốc tiếng Anh.
  - Tự động dọn dẹp bộ nhớ (`Dispose`) và làm mới cache khi xảy ra sự kiện `LocaleChanged` hoặc `AssetsInvalidated`.

- **API Công Khai (Public Mod API):**
  - Cung cấp interface `IUntranslatedStardewApi` cho các mod tìm kiếm/menu hoặc mod nội dung khác qua `helper.ModRegistry.GetApi<IUntranslatedStardewApi>("GiaHung.UntranslatedStardew")`.
  - Thực thể API ổn định (stable instance): Các mod bên thứ ba lưu tham chiếu API vẫn luôn nhận được cấu hình và resolver cập nhật mới nhất mà không bị giữ provider đã giải phóng.
  - Cung cấp các phương thức:
    - `Matches(item, query, bool? ignoreDiacritics = null)`: Khớp từ khóa tìm kiếm trên cả tên chính và tên phụ. Tùy chọn `ignoreDiacritics` cho phép ghi đè cấu hình mod khi người gọi cần tìm kiếm nhạy dấu.
    - `GetNames(item)`: Trả về cặp `(primaryName, secondaryName)`.
    - `GetSearchTerms(item)`: Trả về danh sách các từ khóa tìm kiếm (tên chính, tên phụ, các biến thể đã chuẩn hóa bỏ dấu).
    - `GetSecondaryName(string category, string id, string? language = null)`: Tra cứu tên song ngữ theo danh mục (`Item`, `Mail`, `Power`, `Achievement`, `SecretNote`, `SideTab`), ID và mã ngôn ngữ tùy chọn (mặc định lấy ngôn ngữ phụ đang cấu hình).
    - `RegisterTranslations(string sourceModId, IDictionary<string, string> translations)`: Đăng ký hàng loạt bản dịch từ mod C#, trả về token `IDisposable` để tự động hủy khi mod dừng hoạt động.
    - `RegisterTranslation(string sourceModId, string category, string id, string language, string translatedName)`: Đăng ký một bản dịch đơn lẻ.
    - `UnregisterTranslations(string sourceModId)`: Thu hồi toàn bộ bản dịch đã đăng ký của mod.
  - Chuẩn hóa bỏ dấu tiếng Việt chuẩn Unicode FormD kết hợp ánh xạ `đ`/`Đ` -> `d`/`D`.

---

## 2. Cài đặt & Phân phối (Installation & Artifacts)

### Môi trường đã kiểm chứng:
- **Phân tích mã nguồn:** Stardew Valley 1.6.15.24356 decompiled source.
- **Biên dịch & Liên kết:** SMAPI 4.5.2 & Stardew Valley 1.6.15.24356 (.NET 6.0 Runtime).
- **Kiểm thử Offline:** Xác thực nạp tệp XNB thực tế của game (English, French) và gói Việt hóa `Kou.ViethoaSV` (không khởi động runtime game hay SMAPI).

### Cài đặt:
1. Tải bản build đóng gói tại:  
   `bin/Release/net6.0/UntranslatedStardew 1.3.0.zip`
2. Giải nén thư mục `UntranslatedStardew` vào thư mục `Mods/` của game:  
   `Stardew Valley/Mods/UntranslatedStardew/`
3. Cấu trúc thư mục mod:
   ```text
   Mods/UntranslatedStardew/
   ├── manifest.json
   ├── UntranslatedStardew.dll
   └── i18n/
       ├── default.json
       └── vi.json
   ```

---

## 3. Hướng dẫn Tích hợp Nội dung Mod (Content Patcher & C# Integration Guide)

### A. Dành cho Tác giả Gói Content Patcher:
Mod cung cấp một từ điển SMAPI tùy chỉnh `Mods/GiaHung.UntranslatedStardew/Names` được khởi tạo rỗng khi tải game. Bất kỳ gói Content Patcher nào cũng có thể dùng hành động `EditData` để đăng ký tên song ngữ cho các nội dung tùy biến của mình mà không cần phụ thuộc mã nguồn C#.

#### Cú pháp khóa từ điển (Key Schema):
`{Category}/{Id}@{Language}` hoặc `{Category}/{Id}` (áp dụng cho ngôn ngữ `default`).

- **Category được hỗ trợ:**
  - `Item`: Vật phẩm thông thường, nguyên liệu, vũ khí, giày, mũ, phụ kiện, nội thất (ví dụ: `Item/MyMod_RubySword@vi`).
  - `Mail`: Khóa thư từ trong `Data/mail` (ví dụ: `Mail/MyMod_WelcomeLetter@en`).
  - `Power`: Quyền hạn trong `Data/Powers` (ví dụ: `Power/MyMod_ForestBlessing@vi`).
  - `Achievement`: Tên thành tựu trong `Data/Achievements` (ví dụ: `Achievement/101@default`).
  - `SecretNote`: Ghi chú bí ẩn hoặc Mảnh nhật ký vụn (ví dụ: `SecretNote/25@vi` hoặc `SecretNote/1005@en`).
  - `SideTab`: Tên nhãn các tab bên lề bộ sưu tập (`0` đến `7`).
- **Language:** Mã ngôn ngữ mục tiêu (`vi`, `en`, `fr`, `zh-CN`, v.v.) hoặc bỏ trống phần `@Language` để đặt cho `default`.

#### Quy tắc Phân giải Locale (SMAPI Semantics):
Khi người chơi chọn ngôn ngữ phụ (ví dụ: `zh-CN`), Untranslated Stardew sẽ tra cứu theo thứ tự:
1. Locale chính xác (`zh-CN`)
2. Locale cha (`zh`)
3. Locale mặc định (`default`)
*Lưu ý: Tên tiếng Anh `en` chỉ được hiển thị nếu gói mod có khai báo rõ ràng `@en`; mod không tự động coi `default` là tiếng Anh khi ngôn ngữ được yêu cầu khác.*

#### Ví dụ `content.json` của Content Patcher:
```json
{
  "Format": "2.0.0",
  "Changes": [
    {
      "Action": "EditData",
      "Target": "Mods/GiaHung.UntranslatedStardew/Names",
      "Entries": {
        "Item/MyMod_AncientFruit@vi": "{{i18n:fruit.name}}",
        "Item/MyMod_AncientFruit@en": "Ancient Sunfruit",
        "Mail/MyMod_QuestNotice@vi": "{{i18n:mail.title}}",
        "Power/MyMod_MagicSight@vi": "{{i18n:power.title}}"
      }
    }
  ]
}
```
*(Gói mẫu hoàn chỉnh có thể tham khảo tại thư mục [examples/ContentPatcherPack](file:///examples/ContentPatcherPack/content.json)).*

### B. Dành cho Lập trình viên Mod C#:
Các mod C# khác có thể lấy API công khai thông qua cơ chế SMAPI Mod Registry:

```csharp
IUntranslatedStardewApi? api = helper.ModRegistry.GetApi<IUntranslatedStardewApi>("GiaHung.UntranslatedStardew");
if (api != null)
{
    // Đăng ký hàng loạt bản dịch, trả về token IDisposable để tự động dọn dẹp khi mod unload
    IDisposable token = api.RegisterTranslations("MyMod.UniqueId", new Dictionary<string, string>
    {
        ["Item/MyMod_Wand@en"] = "Star Wand",
        ["Item/MyMod_Wand@vi"] = "Đũa Phép Ngôi Sao",
        ["Mail/MyMod_Welcome@vi"] = "Thư Chào Mừng"
    });

    // Hoặc đăng ký đơn lẻ
    api.RegisterTranslation("MyMod.UniqueId", "Item", "MyMod_Gem", "vi", "Đá Quý Bí Ẩn");

    // Tra cứu tên song ngữ theo danh mục
    string? secondary = api.GetSecondaryName("Item", "MyMod_Gem");
}
```

---

## 4. Quy trình Kiểm thử & Danh sách Kiểm tra Chụp màn hình (Manual Verification Checklist)

### A. Kiểm tra Nhanh với CJB Item Spawner & Lệnh Console:
1. **Kiểm tra Tooltip & Phụ kiện:** Mở bảng CJB Spawner (phím `i`), rà chuột lên vật phẩm và phụ kiện (Trứng Ếch 8 màu, gậy băng Perfect Ice Rod, ống tên Quiver) để xác nhận tooltip song ngữ.
2. **Tìm kiếm Không Dấu & Song Ngữ:** Gõ `ca noc` -> ra `Cá nóc`; gõ `pufferfish` -> ra `Cá nóc`.
3. **Lệnh Console SMAPI:**
   - `untranslated_audit`: Tổng hợp số lượng vật phẩm song ngữ, trùng tên, và thiếu dữ liệu theo từng danh mục registry.
   - `untranslated_audit details`: Liệt kê chi tiết ID và tên của các vật phẩm khuyết bản dịch phụ.

### B. Danh sách Kiểm tra Chụp Màn hình (Manual Screenshot Verification Checklist):
Để xác nhận tính ổn định của giao diện trước khi xuất xưởng, hãy kiểm tra các màn hình sau:
1. **Bảng Bộ sưu tập - Tab Thư từ (`Letters` - Tab 7):**
   - Rà chuột lên một bức thư bất kỳ đã đọc (ví dụ: `Robin` hoặc thư từ `Krobus`).
   - *Kết quả mong đợi:* Tooltip hiển thị tiêu đề song ngữ (ví dụ: `Bức thư từ Krobus (A Letter From Krobus)` hoặc theo định dạng `SecondaryFirst`), ngắt dòng chuẩn xác ở độ rộng 256px.
2. **Bảng Bộ sưu tập - Tab Ghi chú bí ẩn (`Secret Notes` - Tab 6):**
   - Mở Ghi chú bí ẩn có văn bản (ví dụ: Mật thư #23): tiêu đề hiển thị song ngữ (`Ghi chú bí ẩn #23 (Secret Note #23)`), thân ghi chú giữ nguyên văn bản gốc.
   - Mở Ghi chú chỉ có ảnh (ví dụ: Mật thư #10, #13, #15, #19, #20, #21): tiêu đề hiển thị song ngữ, bên dưới hiển thị ảnh mà không có chữ thừa hay lỗi khoảng trống.
3. **Bảng Bộ sưu tập - Mảnh nhật ký vụn (`Journal Scraps`):**
   - Mở Mảnh nhật ký vụn (chỉ số index >= 1000): tiêu đề hiển thị `Mảnh nhật ký vụn #1 (Journal Scrap #1)`.
4. **Bảng Bộ sưu tập - Tab Bên lề (`Side Tabs`):**
   - Rà chuột lên tab 6: hiển thị `Ghi chú bí ẩn (Secret Notes)`.
   - Rà chuột lên tab 7: hiển thị `Thư từ (Letters)`.
   - *Kết quả mong đợi:* Nhãn cố định của game không bị biến dạng, chỉ có tooltip hoverText hiển thị tên song ngữ.
5. **Bảo mật Mục Chưa Mở khóa (Locked State & Spoiler Protection):**
   - Kiểm tra các ô thư từ hoặc mật thư chưa tìm thấy: hiển thị `???`, hoàn toàn không làm lộ tiêu đề hay nội dung.
6. **Menu Thăng cấp Nghề (`LevelUpMenu`):**
   - Màn hình chọn nghề cấp 5 hoặc cấp 10: tiêu đề chính `dialogueFont` 1 dòng tại `top+160`, tiêu đề phụ `smallFont` tại `top+208`, không bị chồng lấn lên dòng đạn đầu tiên và cắt ngắn an toàn nếu vượt quá 336px/352px.
7. **Bảng Quyền hạn (`PowersTab`):**
   - Rà chuột lên các quyền hạn đã mở khóa: tên song ngữ hiển thị chuẩn xác, không bị lỗi xuống dòng.

---

## 5. Cấu hình (Configuration)

File cấu hình `config.json` sẽ được tự động tạo khi chạy mod lần đầu, hoặc chỉnh sửa trực tiếp qua menu **Generic Mod Config Menu (GMCM)**:

| Tùy chọn | Kiểu | Mặc định | Ý nghĩa |
| :--- | :--- | :--- | :--- |
| `SecondaryLanguage` | string | `"default"` | Mã ngôn ngữ phụ hiển thị trên tooltip và giao diện (`default`, `en`, `vi`, `es`, `fr`, `de`, `ja`, `ko`, `zh`). `default` đại diện cho ngôn ngữ gốc/fallback của SMAPI. |
| `ShowSecondaryInTooltip` | bool | `true` | Bật/tắt hiển thị tên ngôn ngữ phụ trên tooltip và các menu giao diện hỗ trợ. |
| `SecondaryFirst` | bool | `false` | `false`: Tên ngôn ngữ chính ở trước/trên, tên phụ ở sau/dưới.<br>`true`: Tên ngôn ngữ phụ ở trước/trên, tên chính ở sau/dưới. |
| `DiacriticInsensitiveSearch`| bool | `true` | Bật/tắt mặc định chuẩn hóa bỏ dấu tiếng Việt và ánh xạ `đ`/`Đ` khi tìm kiếm qua API. |

---

## 6. Ngôn ngữ hỗ trợ & Giới hạn (Supported Languages & Scope Limits)

### Ngôn ngữ có sẵn trong game (Vanilla Built-in):
Các ngôn ngữ sau có sẵn tệp XNB trong thư mục `Content/` của game:
- `default` (Nguồn dữ liệu gốc / fallback không hậu tố của game hoặc mod)
- `en` (English)
- `es` / `es-ES` (Spanish)
- `de` / `de-DE` (German)
- `fr` / `fr-FR` (French)
- `pt` / `pt-BR` (Portuguese)
- `ru` / `ru-RU` (Russian)
- `ja` / `ja-JP` (Japanese)
- `zh` / `zh-CN` (Chinese)
- `ko` / `ko-KR` (Korean)
- `it` / `it-IT` (Italian)
- `tr` / `tr-TR` (Turkish)
- `hu` / `hu-HU` (Hungarian)

### Ngôn ngữ Mod Tiếng Việt (`vi`):
- Khi chơi game bằng tiếng Việt (`Kou.ViethoaSV`), tên chính của vật phẩm hiển thị tiếng Việt đầy đủ.
- Khi chơi game bằng ngôn ngữ khác và chọn `SecondaryLanguage: "vi"`, mod sẽ phát hiện thư mục mod `Kou.ViethoaSV` qua `helper.ModRegistry.Get("Kou.ViethoaSV")` hoặc đường dẫn quy ước và đọc các tệp `_vi.xnb`. Nếu không tìm thấy, mod sẽ ghi cảnh báo 1 lần qua mã hóa ngôn ngữ `I18n.Log_SecondaryFallback` và chuyển về `default` an toàn.

### Phạm vi và Giới hạn (Boundaries & Limits):
- Mod chỉ can thiệp vào tiêu đề tên hiển thị của vật phẩm, kỹ năng, nghề nghiệp, quyền hạn, tiêu đề thư từ, tiêu đề mật thư/mảnh nhật ký và tên nhãn tab bên lề.
- **Tuyệt đối không dịch:** Thân thư, nội dung mật thư, tên NPC, quái vật, hội thoại hay ghi đè logic game của thuộc tính `Item.Name`/`Item.DisplayName`.
- **Không tự động đoán dịch cho các mod ảo:** Tệp XNB của game gốc không thể thấy các tài nguyên ảo của Content Patcher/C#. Để modded item/mail hiển thị song ngữ, tác giả mod cần khai báo qua `Mods/GiaHung.UntranslatedStardew/Names`, C# API hoặc sử dụng token `[LocalizedText ...]`. Nếu không có bản dịch phụ, mod sẽ hiển thị tên chính nguyên bản một cách sạch sẽ.
- Tuyệt đối không thay đổi mã ngôn ngữ toàn cục `LocalizedContentManager.CurrentLanguageCode` của game.
- Chi tiết báo cáo kiểm tra lỗi và ranh giới kỹ thuật: xem thêm tại [`docs/DEBUG-MANUAL-1.2.1.md`](file:///docs/DEBUG-MANUAL-1.2.1.md), [`docs/AUDIT-1.2.0.md`](file:///docs/AUDIT-1.2.0.md) và [`docs/UI-COVERAGE.md`](file:///docs/UI-COVERAGE.md).

---

## 7. Hướng dẫn Lệnh Build & Kiểm thử (Build & Verification)

### Build mod (Offline Release, tạo gói Zip):
```pwsh
dotnet build -c Release /p:EnableModDeploy=false /p:EnableModZip=true
```
- Kết quả thư mục mod: `bin/Release/net6.0/`
- Tệp nén Release zip: `bin/Release/net6.0/UntranslatedStardew 1.2.2.zip` (chỉ chứa `manifest.json`, `UntranslatedStardew.dll` và thư mục `i18n/`, loại trừ toàn bộ thư mục `examples/` và mã kiểm thử).

### Chạy bộ kiểm thử hồi quy (Regression Test Suite & Real XNB Smoke Check):
```pwsh
dotnet run --project tests/UntranslatedStardew.Tests/UntranslatedStardew.Tests.csproj
```
Bộ kiểm thử gồm **668 ca kiểm tra** đã qua toàn diện, gồm kiểm tra biên Harmony CJB, UI Expansion, bố cục LevelUpMenu scoped & dynamic fonts, nạp XNB thật, phân giải phông song ngữ và tích hợp modded content:
1. `LanguageHelper`: Phân giải mã chuẩn ISO, hỗ trợ `default` và vanilla built-in vs Tiếng Việt, ánh xạ hậu tố tệp XNB.
2. `SearchMatcher Accent & 'đ'`: Chuẩn hóa Unicode FormD, loại bỏ dấu thanh, ánh xạ `đ`/`Đ` -> `d`/`D`.
3. `SearchMatcher Query Matching`: Khớp chuỗi không phân biệt hoa thường, chuỗi con, cờ phân biệt dấu, phát sinh từ khóa tìm kiếm.
4. `TooltipFormatter`: Định dạng tiêu đề song ngữ, đảo vị trí theo cấu hình, loại bỏ tiêu đề trùng lặp, bảo toàn số lượng ` x5` và nhãn `(Công thức)`.
5. `LexiconPluralize`: Kiểm tra quy tắc số nhiều chuẩn vanilla cho trái cây sấy và các ngoại lệ không biến đổi (`Hops`, `Pickles`, `Dried Cranberries`).
6. `ItemCacheKey`: Khả năng phân biệt khóa cache giữa các loại ngâm ủ, nguyên liệu, cấp độ dụng cụ và các biến thể động của phụ kiện.
7. `Trinket Resolution (Bug Reproduction)`: Khắc phục triệt để lỗi `RawData = null`, phân giải toàn bộ 8 phụ kiện vanilla trong tiếng Anh, Pháp và tiếng Việt, cùng các biến thể động (`frog_variant_0..7`, `PerfectIceRod`, `RapidMagicQuiver`, `HeavyMagicQuiver`).
8. `CJB Search Filter Integration`: Kiểm thử bộ lọc CJB với truy vấn tiếng Việt không dấu, tên phụ tiếng Anh, chuyển đổi cấu hình `ignoreDiacritics`, khớp ID và bảo toàn danh mục.
9. `MockContentProvider & Fallback`: Kiểm tra nạp từ điển, cơ chế per-key fallback về ngôn ngữ mặc định khi từ điển ngôn ngữ phụ khuyết khóa, thứ tự ưu tiên `displayNameFormat` và bảo toàn mẫu sấy đặc thù.
10. `Stable API Lifecycle`: Kiểm tra API đối tượng ổn định duy trì tham chiếu đến resolver và cấu hình động sau các lần cập nhật mà không bị mất đồng bộ.
11. `Systematic Item Name Coverage Audit`: Kiểm tra tự động độ phủ phân giải trên các danh mục phụ kiện, giày, mũ.
12. `Real XNB Offline Smoke Check`: Nạp thực tế trực tiếp các tệp XNB từ đĩa (Strings/Objects English, French, Vietnamese; Data/Boots; Data/hats; Data/Trinkets; Strings/1_6_Strings) qua ContentManager độc lập mà không cần khởi chạy game hay SMAPI.
13. `SMAPI 'default' Semantics`: Kiểm thử nạp `default` không bị cảnh báo unsupported, phân biệt rõ `default` và `en`.
14. `UI Harmony Boundary Signatures & Field Binding`: Kiểm tra phản chiếu (reflection) chữ ký phương thức cho cả 6 điểm vá Harmony và các trường dữ liệu nội bộ (`leftProfessionDescription`, `rightProfessionDescription`, `professionsToChoose`, `leftProfessionColor`, `rightProfessionColor`, `hoverTitle`, `hoverText`, `descriptionText`, `powers`).
15. `LevelUpMenu Scoped State, Pure Layout & Truncation`: Kiểm tra hình học tọa độ chuẩn Stardew Valley 1.6.15 (khoảng cách 104px giữa tiêu đề và dòng đạn đầu tiên, phụ đề tại `top+208`, giới hạn chiều rộng cột 336px/352px trước icon, thuật toán cắt ngắn thêm `...`), lưu/khôi phục trạng thái scoped qua Postfix/Finalizer, bảo toàn nguyên vẹn chuỗi gốc sau 50 chu kỳ vẽ liên tiếp và khi xảy ra ngoại lệ.
16. `UI Fixture Formatting & Merged Text`: Kiểm thử cấu trúc ghép chuỗi hai dấu xuống dòng của `CollectionsPage`, bảo toàn số liệu thống kê, chống ghép lặp hai lần, bảo vệ trạng thái `???` chưa mở khóa.
17. `Real XNB UI Expansion Smoke Check`: Nạp thực tế các tên nghề (Professions 0, 1, 4), kỹ năng (Farming, Fishing, Foraging, Mining, Combat), quyền hạn (SkullKey, BearPaw, ForestMagic), thành tựu (Achievement 0) và bộ sưu tập (Item 128) qua các tệp XNB thật của English, French và tiếng Việt `Kou.ViethoaSV`.
18. `Mail Title & Secret Note Tests`: Kiểm tra trích xuất tiêu đề thư chuẩn xác từ trường `[#]` trong `Data/mail`, phân định Mật thư vs Mảnh nhật ký vụn (`index >= 1000`), ghi chú ảnh không lỗi nội dung, bảo toàn ký tự xuống dòng chéo nền tảng, định dạng độc lập tránh ngắt dòng đè mất phụ đề, và bảo vệ submenu guard khi letter viewer mở.
19. `External Translation Registry & Modded Fallbacks`: Kiểm tra cơ chế đăng ký từ điển `Mods/GiaHung.UntranslatedStardew/Names`, fallback chuẩn SMAPI (`exact -> parent -> default`), tra cứu ID định danh đầy đủ (`(W)Custom_DragonSword`), và độ ưu tiên so với dữ liệu gốc.
20. `C# Public API, Lifecycle & Thread Safety`: Kiểm tra vòng đời đăng ký C#, tính năng hủy tự động `IDisposable` kèm đồng bộ xóa cache resolver tức thì, quyền sở hữu token theo thế hệ (generation) chống ghi đè nhầm, phân xử xung đột ưu tiên đăng ký sau cùng, an toàn đa luồng (thread-safe copy-on-write), hợp đồng ngôn ngữ rõ ràng không tự động trả về tiếng Pháp khi hỏi tiếng Việt, và cô lập lỗi an toàn.
21. `Real XNB Mail & Secret Notes Smoke Check`: Nạp thực tế tiêu đề thư Robin và mật thư từ các tệp XNB thật của English, French và tiếng Việt.
22. `Note Image Preview Layout, Safe Area Clamping & Dynamic Fonts`: Kiểm tra đối chiếu mã lệnh IL gốc của game với các giá trị tọa độ cố định (`mouseY+96`, `mouseY+112`, `288x288`), xác nhận bảo toàn 100% tọa độ gốc khi tiêu đề đơn dòng, tự động dịch chuyển cả khung và ảnh theo cùng độ dời delta khi tiêu đề đa dòng (song ngữ Việt-Anh hoặc phông chữ lớn), xác thực kẹp biên vùng an toàn màn hình (`Safe Area Right/Bottom/Top/Left`), cô lập an toàn trạng thái scoped (`CollectionsNoteImageState`) và phục hồi qua `Finalizer` khi gặp ngoại lệ, kiểm tra bố cục phông chữ động `LevelUpMenu` và tính năng thu phóng an toàn (`bounded scaling`).
23. `XNB Font Metadata Parsing & Headless Glyph Coverage`: Giải mã nhị phân tệp XNB thật (uncompressed, LZX compressed 0x01/0x81, HiDef), trích xuất mảng ký tự và DefaultCharacter mà không cần thiết bị đồ họa GPU. Xác thực đầy đủ 134 ký tự có dấu và chữ `đ`/`Đ` của tiếng Việt, độ phủ Cyrillic tiếng Nga (ru-RU), Kana/Kanji tiếng Nhật (ja-JP) và Hanzi tiếng Trung (zh-CN).
24. `Bilingual Run Planner & Metrics`: Kiểm tra phân đoạn văn bản theo font runs, ưu tiên vẽ cả dòng từ cùng một phông để bảo toàn baseline dấu, kiểm tra chuẩn hóa NFC từ input phân rã NFD, tính toán chiều cao dòng cực đại (`per-line max height`), và hoán đổi thứ tự `SecondaryFirst`.
25. `Bilingual Font Manager & Diagnostic Lifecycle`: Kiểm tra cảnh báo ký tự không được hỗ trợ phát ra duy nhất 1 lần, làm mới bộ nhớ cache và giải phóng ContentManager độc lập.
26. `Harmony Interception & Recursion Guards`: Kiểm tra bảo vệ phạm vi scoped (chỉ can thiệp khi `BilingualRenderScope.IsActive == true`), chống đệ quy vô hạn khi vẽ và đo đạc, passthrough an toàn cho các chuỗi thuần ASCII.
27. `Fixed Role Fonts, Multi-Locale Surfaces & Generic Language Sources (v1.3.0)`: Kiểm tra độ phủ phân giải Giấy dán tường `(WP)` và Sàn nhà `(FL)` trên toàn bộ 12 ngôn ngữ game và gói Việt hóa, kiểm tra lưu trữ cache phân biệt giữa các item ID cùng tên hiển thị, kiểm tra phân định font theo vai trò cố định với 2 phông giả lập cùng chứa mã ASCII (đoạn phụ bắt buộc dùng phông phụ, không bị phông chính nuốt chửng), kiểm tra bất biến vai trò khi đổi vị trí `SecondaryFirst`, kiểm tra siêu dữ liệu vai trò xuyên suốt dòng ngắt và thân mô tả, kiểm tra pipeline Harmony thực tế bắt lệnh vẽ headless không cần GPU, kiểm tra an ninh chặn thoát đường dẫn `../` và vòng đời C# register/unregister của `LanguageSourceRegistry`, nạp cấu trúc Content Patcher từ điển `LanguageSources` và xóa sạch dữ liệu đệm cũ.

---

## 8. Nhật ký thay đổi (Changelog)

### v1.3.0
- **Font Theo Vai Trò Cố Định (Fixed Role Fonts):**
  - **Phân định vai trò cứng (Strict Role Binding):** Loại bỏ cơ chế chọn font phụ thuộc đơn thuần vào việc thiếu glyph. Đoạn chữ thuộc vai trò chính luôn luôn dùng font chính (`Game1.dialogueFont`, `Game1.smallFont`); đoạn chữ thuộc vai trò phụ luôn luôn dùng font phụ, kể cả khi từ đó thuần ASCII và font chính cũng có thể vẽ được.
  - **Bảo toàn liên kết font - vai trò khi `SecondaryFirst`:** Đổi thứ tự hiển thị chỉ hoán đổi vị trí dòng (dòng phụ nằm trên, dòng chính nằm dưới), tuyệt đối không đảo lộn liên kết giữa đoạn chữ và font tương ứng.
  - **Cấu trúc thành phần văn bản (`StructuredTextComposition` & `RoleSpan`):** Siêu dữ liệu vai trò được truyền xuyên suốt quá trình ngắt dòng của game, tính toán kích thước và vẽ chuỗi.
  - **Hỗ trợ đầy đủ font của 12 ngôn ngữ tích hợp trong game:** `en` (default), `es`, `de`, `fr`, `pt`, `ru`, `ja`, `zh`, `ko` (+16px line spacing), `it` (fallback font gốc), `tr` (+4px line spacing), `hu`.
- **Hiển thị Song ngữ Giấy dán tường & Sàn nhà (`Wallpaper & Flooring`):**
  - Khắc phục triệt để lỗi thiếu tên song ngữ ở Giấy dán tường `(WP)` và Sàn nhà `(FL)` do vanilla Stardew trả về `RawData = null`.
  - Phân giải trực tiếp qua các khóa chuỗi chia sẻ `Wallpaper.cs.13204` ("Wallpaper") và `Wallpaper.cs.13203` ("Flooring") trong `Strings/StringsFromCSFiles` cho mọi ngôn ngữ.
  - Hỗ trợ đầy đủ **13 tiền tố loại định danh vật phẩm**: `(O)`, `(BC)`, `(B)`, `(FL)`, `(F)`, `(H)`, `(M)`, `(P)`, `(S)`, `(T)`, `(TR)`, `(WP)`, `(W)`.
  - Phân tách độc lập các khóa cache theo Qualified Item ID, bảo đảm các mẫu khác ID nhưng trùng tên hiển thị không ghi đè lẫn nhau.
- **Cơ chế Nguồn Ngôn ngữ Tùy chỉnh Tổng quát (Generic Language Sources):**
  - Đọc nguồn custom có CustomFields tường minh trong `Data/AdditionalLanguages`, hoặc đăng ký qua CP/API.
  - Cung cấp asset từ điển SMAPI `Mods/GiaHung.UntranslatedStardew/LanguageSources` cho phép các gói Content Patcher khai báo nguồn chuỗi, font và mẫu asset.
  - Cung cấp C# API `RegisterLanguageSource` và `UnregisterLanguageSource` trên `IUntranslatedStardewApi`.
  - Kiểm tra đường dẫn asset nằm trong ContentRoot đã khai báo.
  - Tự động làm mới cache và xóa sạch dữ liệu đệm cũ khi nguồn bị hủy đăng ký.
  - Đóng gói sẵn descriptor tương thích ngược cho mod tiếng Việt `Kou.ViethoaSV`.
  - Ghi cảnh báo khi thiếu nguồn/font/glyph; không sinh bản dịch cho nội dung chưa có nguồn.
- **Bộ Kiểm thử Tự động:** Mở rộng lên **885 assertions hồi quy đạt**, gồm XNB thực, Harmony headless và vòng đời nguồn custom. Chưa xác minh trực quan trong game.

### v1.2.2
- **Khắc phục lỗi phông chữ giao diện thiếu ký tự (UI Missing Glyphs Bug Fix):**
  - **Mô tả sự cố (Bug Finding):** Phông chữ giao diện (`Game1.smallFont`, `Game1.dialogueFont`) được nạp cố định duy nhất theo ngôn ngữ chính khi khởi động game. Gói Content Patcher Việt hóa (`Kou.ViethoaSV`) đặt điều kiện nạp phông `"When": { "Language": "vi" }`, nên khi ngôn ngữ chính khác `vi` (ví dụ tiếng Anh `en`), game chỉ dùng phông tiếng Anh vanilla. MonoGame `SpriteFont` trong Stardew Valley cài đặt `DefaultCharacter` là `'*'` (mã ASCII 42), khiến mọi ký tự ngoài bảng mã tiếng Anh tự động biến thành dấu sao (ví dụ: `Cá nóc` -> `C* n*c`).
  - **Kiến trúc Phân đoạn Phông (Per-Language Font Runs):** Thay vì gộp texture atlas (Glyph-Union tốn kém VRAM và lỗi GPU trong môi trường headless), mod triển khai cơ chế phân đoạn chuỗi theo font runs. Các ký tự thuộc ngôn ngữ phụ được vẽ nối tiếp bằng phông phụ nạp riêng biệt qua instance `ContentManager` độc lập; tuyệt đối không đụng chạm đến cache tĩnh `LocalizedContentManager.localizedAssetNames` hay hoán đổi locale toàn cục.
  - **Ưu tiên Baseline Đồng nhất & Chuẩn hóa Unicode NFC:** Ưu tiên phân đoạn cả dòng theo phông phụ khi dòng đó chứa ký tự của ngôn ngữ phụ để đảm bảo đường chân chữ (baseline), kerning và khoảng cách dấu thanh hoàn toàn đồng nhất. Tự động chuẩn hóa mọi chuỗi sang Unicode Form C (NFC) trước khi đo đạc và vẽ, khắc phục lỗi dấu thanh bị vẽ lệch khi chuỗi đầu vào ở dạng NFD.
  - **Điểm Vá Toàn Cục với Cơ chế Passthrough Phạm vi Nghiêm ngặt (Process-Wide Harmony Entrypoints with Strict Active-Scope Passthrough):** Cài đặt điểm vá Harmony trên các phương thức toàn cục `SpriteFont.MeasureString` và `SpriteBatch.DrawString` trong MonoGame, nhưng áp dụng cơ chế active-scope passthrough tuyệt đối (chỉ can thiệp khi `BilingualRenderScope.IsActive == true` và chuỗi được theo dõi bởi `BilingualStringTracker`). Khi ở ngoài phạm vi menu của mod hoặc chuỗi không thuộc mod, phương thức trả về `true` ngay lập tức để chạy mã gốc MonoGame 100% byte-for-byte.
  - **Khắc phục lỗi Double-Exit Scope qua Cookie Tham chiếu Độc lập (`ScopeCookie`):** Sử dụng đối tượng cookie tham chiếu (`class`) hỗ trợ `Interlocked.Exchange` cho mỗi lời gọi render scope. Khắc phục triệt để lỗi scope bị thoát hai lần khi cả `Postfix` và `Finalizer` cùng thực thi trên một invocation của Harmony, bảo đảm scope cha vẫn duy trì nguyên vẹn khi scope con hoàn tất hoặc ném ngoại lệ.
  - **Chống Đệ quy Vô hạn & Bảo vệ Ký tự Thiếu Cả Hai Phông:** Quản lý cờ luồng `IsMeasuringActive` trong khối `try ... finally` lúc phân đoạn và đo đạc. Khi gặp ký tự không được hỗ trợ bởi cả phông chính lẫn phông phụ, bộ đo đạc không bị tái kích hoạt vô tận gây tràn ngăn xếp (stack overflow); ghi nhận cảnh báo một lần duy nhất theo bảng mã ký tự có giới hạn bộ nhớ (bounded cache).
  - **Sửa lỗi Đường tắt NFC Chuẩn hóa (NFC Fast-Path):** Khi chuỗi đầu vào ở dạng NFD (ký tự phân tách) nhưng phông chính đã hỗ trợ đầy đủ các ký tự dựng sẵn (NFC), hệ thống tự động trả về plan dựng sẵn qua phông chính thay vì trả về null khiến game vẽ trực tiếp chuỗi NFD gây lỗi dấu thanh.
  - **Bảo toàn Phép Biến đổi Toàn khối (Block Flipping Transforms & Scale):** Tính toán và phản chiếu tọa độ tương đối của từng run dựa trên `TotalSize` và `run.Size` khi sử dụng `SpriteEffects.FlipHorizontally` hoặc `SpriteEffects.FlipVertically`. Phân biệt chính xác giữa tỉ lệ bị bỏ qua (mặc định là 1) và tỉ lệ hiển thị bằng 0 (`scale: Vector2.Zero`).
  - **Bộ nhớ Đệm Asset GPU & Cảnh báo Phông Ngôn ngữ Thiếu Rõ ràng:** Lưu vết các phông phụ đã cố gắng tải từ nguồn khởi tạo để tránh việc truy vấn đĩa lặp lại mỗi khung hình khi thiếu asset. Ghi log cảnh báo chẩn đoán rõ ràng khi asset phông của một ngôn ngữ được hỗ trợ không tồn tại trên đĩa.
  - **Tương thích Phông Động & FontSettings:** Tôn trọng phông chữ hiện hành của game (nếu người dùng cài đặt mod `FontSettings`, phông chữ tùy biến của locale chính được giữ nguyên 100% và chỉ fallback cho các glyph thực sự còn thiếu).
  - **Ranh giới Tính năng Nghiêm ngặt:** Mod tuyệt đối không bổ sung tùy chỉnh kích thước/màu sắc phông chữ độc lập cho người dùng, không can thiệp hội thoại (`SpriteText`/BMFont) hay bố cục ruby.
  - **Ma trận Kiểm thử Thủ công (Manual Test Matrix):**
    1. `en` -> `vi`: Tiếng Anh chính, tiếng Việt phụ hiển thị đầy đủ dấu không bị dấu `*` hay lỗi ngắt dòng.
    2. `en` -> `ja`: Tiếng Anh chính, Kanji/Kana phụ hiển thị qua phông tiếng Nhật `SmallFont.ja-JP` / `SpriteFont1.ja-JP`.
    3. `vi` -> `ru`: Tiếng Việt chính, Cyrillic phụ hiển thị qua phông tiếng Nga `SmallFont.ru-RU` / `SpriteFont1.ru-RU`.
    4. `ja` -> `en`: Tiếng Nhật chính, tiếng Anh phụ hiển thị qua phông chính hoặc fallback.
    5. Chuyển đổi cấu hình ngôn ngữ phụ / phông chữ mà không cần khởi động lại game: làm mới cache tức thì.
- **Bộ Kiểm thử Tự động:** Mở rộng lên **706 ca kiểm thử hồi quy** (vượt qua 100% 605 bài test cũ + 101 bài test mới về giải mã XNB, phông chữ động, Harmony nested fixture, transforms và acceptance fixes).

### v1.2.1
- **Khắc phục lỗi đè ảnh xem trước Mật thư / Mảnh nhật ký (`CollectionsPage Image Preview Overlap`):**
  - **Mô tả sự cố (Bug Screenshot Case):** Khi người chơi rà chuột lên Mảnh nhật ký vụn số 4 (`Journal Scrap #4`) hoặc các ghi chú dạng ảnh khác trong Bảng Bộ sưu tập với giao diện song ngữ (ví dụ Tiếng Việt + Tiếng Anh: `"Mảnh nhật ký vụn #4\nJournal Scrap #4"`), khung ảnh và ảnh bản đồ bị vẽ đè trực tiếp lên dòng tiêu đề thứ hai.
  - **Phân tích nguyên nhân gốc:** Trong mã gốc `CollectionsPage.draw` (dòng ~871-876), game gọi `drawHoverText` rồi vẽ trực tiếp khung ảnh tại `mouseY + 64 + 32` (`mouseY + 96`) và ảnh tại `mouseY + 112`. Game giả định cố định rằng tooltip luôn chỉ có 1 dòng cao 64px. Khi hiển thị song ngữ, tooltip mở rộng lên 96px+, khiến tọa độ cố định của ảnh rơi vào giữa dòng chữ phụ.
  - **Giải pháp triệt để:** Bổ sung lớp tính toán hình học thuần túy `CollectionsNoteImageLayoutHelper` và điểm vá an toàn `CollectionsPageDrawPatch`. Đo đạc chính xác chiều cao thực tế của chuỗi `hoverText` qua phông chữ hiện hành `Game1.smallFont`, tự động dịch chuyển cả khung ảnh và ảnh xuống cùng một khoảng delta (`ExtraHeight = tooltipHeight - 64`) để nằm khít ngay dưới đáy tooltip. Khi tiêu đề không có dòng phụ, giữ nguyên 100% tọa độ gốc vanilla.
  - **Kẹp biên an toàn màn hình theo nhóm phối hợp (`Joint Coordinated Clamping`):** Tính toán và kẹp biên đồng thời cả tiêu đề tooltip và khung ảnh như một khối liên kết. Khi ở gần cạnh đáy (`safeArea.Bottom`) hoặc cạnh phải (`safeArea.Right`), cả tooltip và khung ảnh cùng dịch chuyển lên/sang trái, bảo đảm tuyệt đối đáy tooltip luôn nằm trên đỉnh khung ảnh (`headerBottom <= boxY`), không bị tái hiện lỗi đè ảnh khi kẹp độc lập. Hỗ trợ hiển thị song song (side-by-side) hoặc co giãn ảnh theo tỷ lệ khi chiều cao màn hình khả dụng bị thu hẹp.
  - **Bảo toàn phạm vi & Trạng thái Scoped:** Chỉ can thiệp ảnh xem trước trong `CollectionsPage`; không làm ảnh hưởng đến menu đọc thư (`LetterViewerMenu`), thân thư từ hay tooltip vật phẩm khác. Sử dụng mô hình `Prefix`, `Postfix`, `Finalizer` với đối tượng `CollectionsNoteImageState` tạm ẩn `secretNoteImage = -1` và `___hoverText = ""` lúc vẽ, luôn khôi phục lại giá trị gốc kể cả khi có ngoại lệ.
- **Tương thích Phông chữ Động & Thu phóng Giới hạn (`Dynamic Font Safety in LevelUpMenu`):**
  - Trong menu chọn nghề `LevelUpMenu`, thay thế hằng số giả định cứng `top+208` trước đây bằng cơ chế đo đạc thực tế chiều cao của `Game1.dialogueFont` và `Game1.smallFont` mỗi khung hình (frame-by-frame).
  - Tự động hạ thấp vị trí phụ đề nằm ngay dưới đáy tiêu đề chính đã đo đạc (`SubtitleY >= titleY + titleHeight`), ngăn chặn triệt để nguy cơ phụ đề đè lên tiêu đề chính khi dùng phông chữ Việt hóa lớn hoặc mod tinh chỉnh phông (`FontSettings`).
  - Tích hợp cơ chế thu phóng có giới hạn (`bounded scaling`) khi cả tiêu đề và phụ đề đều lớn hơn khoảng trống khả dụng trước dòng đạn đầu tiên (`bullet1Y` tại `top+264`). Nếu phông chữ quá lớn khiến không gian khả dụng dưới ngưỡng đọc được (< 12px hoặc tỷ lệ thu phóng < 0.5f), mod an toàn ẩn lớp phủ phụ đề để tránh va chạm chữ thay vì cố vẽ gây đè chữ.
- **Bộ Kiểm thử Tự động:** Mở rộng lên 605 ca kiểm thử hồi quy (vượt qua 100% 497 bài test cũ + 108 bài test hình học và ranh giới patch mới).

### v1.2.0
- **Tiêu đề Thư từ Song ngữ (`Letters` - Tab 7):** Hiển thị tên song ngữ cho các bức thư đã đọc khi rà chuột trong Bảng Bộ sưu tập. Trích xuất chính xác trường tiêu đề sau ký tự phân cách `[#]` từ `Data/mail`; không dịch thân thư. Định dạng độc lập từng phần để tránh việc ngắt dòng của game làm ẩn tiêu đề phụ. Bổ sung guard bỏ qua khi menu xem thư (`letterviewerSubMenu`) đang mở.
- **Tiêu đề Mật thư & Mảnh nhật ký vụn (`Secret Notes & Journal Scraps` - Tab 6):** Hiển thị tiêu đề song ngữ trên đầu ghi chú. Hỗ trợ đầy đủ ghi chú văn bản, ghi chú chỉ có ảnh (image-only notes), và các mảnh nhật ký vụn (`index >= 1000`). Giữ nguyên 100% nội dung thân ghi chú.
- **Tab Bên lề Song ngữ (`Side Tabs`):** Hiển thị tên ngôn ngữ phụ trên tooltip khi rà chuột lên các biểu tượng tab bên trái bảng Bộ sưu tập, không ảnh hưởng đến nhãn cố định của game.
- **Hỗ trợ Nội dung Mod từ Content Patcher:** Cung cấp asset từ điển SMAPI `Mods/GiaHung.UntranslatedStardew/Names` cho phép các mod Content Patcher đăng ký tên song ngữ cho vật phẩm (hỗ trợ ID định danh như `(W)`), thư từ, quyền hạn, thành tựu, mật thư qua hành động `EditData`. Gói ví dụ mẫu yêu cầu phụ thuộc bắt buộc (`IsRequired: true`).
- **Hỗ trợ Nội dung Mod từ C#:** Bổ sung phương thức `RegisterTranslations` (trả về `IDisposable`), `RegisterTranslation`, `UnregisterTranslations`, và `GetSecondaryName` vào interface `IUntranslatedStardewApi`.
- **Dự phòng Ngôn ngữ Chuẩn SMAPI:** Triển khai cơ chế phân giải `exact -> parent -> default` cho nội dung modded. Tiếng Anh `en` cũng dự phòng về `default` theo chuẩn SMAPI, trong khi chọn `default` chỉ tra cứu `default`. Bản dịch gốc `default` có thể là ngôn ngữ khác ngoài tiếng Anh.
- **Sửa lỗi Vòng đời & An toàn Đa luồng (Audit Fixes):**
  - Xóa cache resolver ngay khi token đăng ký bị dispose.
  - Quản lý token theo thế hệ (generation-based ownership), ngăn token cũ vô tình hủy đăng ký mới hơn.
  - Phân xử xung đột có thứ tự xác định (đăng ký sau cùng thắng).
  - Triển khai snapshot bất biến (copy-on-write) đảm bảo an toàn tuyệt đối khi đọc/ghi đồng thời trên nhiều luồng.
  - API `GetSecondaryName` với ngôn ngữ chỉ định tuân thủ nghiêm ngặt ngôn ngữ yêu cầu, không tự ý trả về tiếng Pháp khi người dùng yêu cầu tiếng Việt.
  - Khắc phục lỗi khôi phục trạng thái `LevelUpMenu`: lưu tham chiếu `__state` trước khi sửa đổi mô tả để luôn khôi phục được ngay cả khi gặp ngoại lệ lúc bắt đầu vẽ.
  - Hỗ trợ ký tự xuống dòng chéo nền tảng (`\r\n\r\n` và `\n\n`) khi phân tách tiêu đề mật thư.
  - Bổ sung theo dõi vô hiệu hóa asset cho `Data/mail`, `Data/SecretNotes`, `Strings/Locations`, và `Mods/GiaHung.UntranslatedStardew/Names`.
- **Bộ Kiểm thử Tự động:** Mở rộng lên 497 ca kiểm tra toàn diện, xác thực toàn bộ tính năng mới, an toàn luồng và dữ liệu XNB thật.

### v1.1.0
- **Hỗ trợ Ngôn ngữ `default` Chuẩn SMAPI:** Bổ sung `default` làm lựa chọn ngôn ngữ phụ chính thức song song với `en`. `default` nạp trực tiếp tài nguyên gốc không hậu tố của game/mod mà không phát cảnh báo unsupported fallback. Cấu hình mặc định cho người dùng mới được cập nhật thành `default`.
- **Mở rộng Giao diện - Bảng Kỹ năng (`SkillsPage`):** Hiển thị song ngữ tên các kỹ năng (Farming, Mining, Foraging, Fishing, Combat, Luck) và tên nghề nghiệp cấp 5 / 10 khi rà chuột.
- **Mở rộng Giao diện - Menu Thăng cấp Nghề (`LevelUpMenu`):** Hiển thị tiêu đề nghề nghiệp song ngữ trên hai thẻ chọn lựa ở cấp 5 và cấp 10 với bố cục đo đạc an toàn: tiêu đề chính `dialogueFont` 1 dòng tại `top+160`, phụ đề `smallFont` tại `top+208` trong khoảng trống 104px trước dòng đạn đầu tiên, tự động cắt ngắn giới hạn theo độ rộng cột trước icon (336px/352px). Trạng thái hiển thị scoped (`LevelUpMenuState`) khôi phục nguyên vẹn danh sách sau khi vẽ và bảo đảm an toàn khi có ngoại lệ (`Finalizer`), không làm rò rỉ chuỗi song ngữ.
- **Mở rộng Giao diện - Vật phẩm & Quyền hạn (`PowersTab`):** Hiển thị tên song ngữ cho các quyền hạn và vật phẩm đặc biệt đã mở khóa trong `Data/Powers`, tự động tính toán lại độ rộng ngắt dòng; bảo toàn trạng thái ẩn danh `???` cho các mục chưa mở khóa.
- **Mở rộng Giao diện - Bộ sưu tập (`CollectionsPage`):** Hiển thị tên song ngữ cho vật phẩm thu hoạch, cá, cổ vật, khoáng sản, món ăn nấu nướng và danh hiệu thành tựu (`Achievements`). Phân tách chuẩn xác ranh giới hai dòng xuống dòng để bảo toàn toàn bộ mô tả chính, số lượng và giá bán; giữ nguyên trạng thái ẩn `???` và không can thiệp thư từ / ghi chú bí mật.
- **Kiểm tra Chữ ký Harmony & Trường Dữ liệu Tự động:** Kiểm tra reflection toàn bộ 5 chữ ký phương thức UI và các trường đối tượng đích lúc khởi tạo, ghi log một lần qua I18n nếu chữ ký game bị thay đổi trong các bản cập nhật tương lai.
- **Bổ sung Vô hiệu hóa Cache:** Thêm theo dõi vô hiệu hóa asset cho `Strings/Furniture`, `Strings/Pants`, `Strings/Shirts` cùng với các bảng chuỗi hiện hữu.

### v1.0.1
- **Phụ kiện (Trinkets):** Khắc phục lỗi thiếu tên song ngữ cho toàn bộ 8 phụ kiện vanilla do `TrinketDataDefinition.GetData` trả về `RawData = null`.
- **Biến thể động phụ kiện:** Hỗ trợ tên song ngữ cho các biến thể đổi màu Trứng Ếch (8 màu), gậy băng Perfect Ice Rod, và ống tên Rapid/Heavy Magic Quiver qua `displayNameOverrideTemplate`.
- **Tích hợp CJB Item Spawner:** Tự động hỗ trợ tìm kiếm song ngữ và tìm kiếm không dấu trong CJB Item Spawner khi mod có mặt, bảo toàn các tab danh mục và sắp xếp của CJB.
- **Lệnh Console SMAPI:** Bổ sung lệnh `untranslated_audit` cho phép người dùng kiểm tra nhanh độ phủ tên song ngữ trên toàn bộ vật phẩm đang tải trong game.
- **Làm mới Cache:** Bổ sung vô hiệu hóa cache cho `Data/Trinkets`, `Strings/1_6_Strings`, `Data/Pants`, `Data/Shirts`, `Data/Mannequins` và giải phóng asset trong ContentManager độc lập.
