# Untranslated Stardew

Untranslated Stardew là bản mod SMAPI cho Stardew Valley 1.6 giúp hiển thị tên vật phẩm song ngữ trong bảng thông tin (tooltip), đồng thời cung cấp API tìm kiếm hỗ trợ tra cứu tên đa ngôn ngữ và tìm kiếm không dấu (chuẩn hóa tiếng Việt `đ`/`Đ`).

---

## 1. Tính năng chính (Features)

- **Tooltip Song Ngữ (Bilingual Tooltip):**
  - Hiển thị tên vật phẩm ở ngôn ngữ hiện hành của game (ngôn ngữ chính, bao gồm cả bản dịch tiếng Việt từ mod `Kou.ViethoaSV`) và ngôn ngữ phụ được cấu hình (mặc định là tiếng Anh `en`, hoặc bất kỳ ngôn ngữ nào được hỗ trợ).
  - Tự động phát hiện và bỏ qua hiển thị trùng lặp khi tên chính và tên phụ giống nhau (ví dụ: khi đang chơi game bằng tiếng Anh và tên phụ là tiếng Anh).
  - Giữ nguyên các hậu tố số lượng (ví dụ: ` x5`), nhãn công thức chế tạo (ví dụ: `(Công thức)`), và tên động của người dùng.
  - Thứ tự hiển thị linh hoạt: ngôn ngữ chính trước hoặc phụ trước (`SecondaryFirst`).

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

- **API Tìm Kiếm Công Khai (Public Mod API):**
  - Cung cấp interface `IUntranslatedStardewApi` cho các mod tìm kiếm/menu khác qua `helper.ModRegistry.GetApi<IUntranslatedStardewApi>("GiaHung.UntranslatedStardew")`.
  - Thực thể API ổn định (stable instance): Các mod bên thứ ba lưu tham chiếu API vẫn luôn nhận được cấu hình và resolver cập nhật mới nhất mà không bị giữ provider đã giải phóng.
  - Cung cấp các phương thức:
    - `Matches(item, query, bool? ignoreDiacritics = null)`: Khớp từ khóa tìm kiếm trên cả tên chính và tên phụ. Tùy chọn `ignoreDiacritics` cho phép ghi đè cấu hình mod khi người gọi cần tìm kiếm nhạy dấu.
    - `GetNames(item)`: Trả về cặp `(primaryName, secondaryName)`.
    - `GetSearchTerms(item)`: Trả về danh sách các từ khóa tìm kiếm (tên chính, tên phụ, các biến thể đã chuẩn hóa bỏ dấu).
  - Chuẩn hóa bỏ dấu tiếng Việt chuẩn Unicode FormD kết hợp ánh xạ `đ`/`Đ` -> `d`/`D`.

---

## 2. Cài đặt & Phân phối (Installation & Artifacts)

### Môi trường đã kiểm chứng:
- **Phân tích mã nguồn:** Stardew Valley 1.6.15.24356 decompiled source.
- **Biên dịch & Liên kết:** SMAPI 4.5.2 & Stardew Valley 1.6.15.24356 (.NET 6.0 Runtime).
- **Kiểm thử Offline:** Xác thực nạp tệp XNB thực tế của game (English, French) và gói Việt hóa `Kou.ViethoaSV` (không khởi động runtime game hay SMAPI).

### Cài đặt:
1. Tải bản build đóng gói tại:  
   `bin/Release/net6.0/UntranslatedStardew 1.0.1.zip`
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

## 3. Quy trình Kiểm thử với CJB Item Spawner & Lệnh Console (Testing & Validation Workflow)

### A. Kiểm tra nhanh với CJB Item Spawner:
1. **Chuẩn bị:** Mở game và vào một file lưu tạm thời (test save).
2. **Mở menu Spawn:** Bấm phím mở CJB Item Spawner (mặc định phím `i`).
3. **Kiểm tra Tooltip Trực tiếp:**
   - Rà chuột (hover) trực tiếp lên các ô vật phẩm trong bảng Spawner: tooltip hiển thị tên chính và tên phụ (ví dụ: `Trứng Ếch` / `Frog Egg`, `Mật ong rừng` / `Wild Honey`).
   - Spawn thử một vài món vào túi đồ (inventory) và rà chuột trong túi đồ để xác nhận tính đồng nhất.
4. **Kiểm tra Phụ kiện (Trinkets) & Biến thể Động:**
   - Chọn tab Phụ kiện / gõ tìm kiếm phụ kiện.
   - Thử các loại phụ kiện: `Basilisk Paw`, `Fairy Box`, `Ice Rod`, `Golden Spur`, `Frog Egg`, `Magic Quiver`, `Magic Hair Gel`, `Parrot Egg`.
   - Với các biến thể có màu sắc hoặc cấp độ:
     - `Trứng Ếch Xanh lục` (`Green Frog Egg`), `Trứng Ếch Đỏ` (`Red Frog Egg`), `Trứng Ếch Ngũ Sắc` (`Prismatic Frog Egg`).
     - `Pháp trượng Băng Toàn năng` (`Perfect Ice Rod`).
     - `Pháp tiễn Siêu thanh` (`Rapid Magic Quiver`), `Pháp tiễn Hạng nặng` (`Heavy Magic Quiver`).
   - Xác nhận tên phụ hiển thị đúng từng biến thể, không bị đè nhầm tên mặc định.
5. **Kiểm tra Tìm kiếm Không Dấu & Song Ngữ trong Spawner:**
   - Gõ tìm kiếm tiếng Việt không dấu: ví dụ gõ `ca noc` -> xuất hiện `Cá nóc`; gõ `trung ech` -> xuất hiện `Trứng Ếch`; gõ `phap truong` -> xuất hiện các loại `Pháp trượng Băng`.
   - Gõ tìm kiếm bằng tên tiếng Anh: ví dụ gõ `pufferfish` -> xuất hiện `Cá nóc`; gõ `frog egg` -> xuất hiện `Trứng Ếch`; gõ `ice rod` -> xuất hiện `Pháp trượng Băng`.
   - Đổi các tab danh mục (`All`, `Tools`, `Weapons`, v.v.) và thứ tự sắp xếp (`Name`, `Price`, `Type`, `ID`): xác nhận các bộ lọc danh mục và sắp xếp của CJB hoạt động chính xác 100%.

### B. Lệnh Console Chẩn đoán Độ Phủ Danh Mục (`untranslated_audit`):
Trong cửa sổ SMAPI console khi đang trong game, người dùng có thể gõ:
- `untranslated_audit`: Hiển thị bảng tổng hợp thống kê số lượng vật phẩm song ngữ, trùng tên, và thiếu dữ liệu theo từng danh mục registry (`(O)`, `(BC)`, `(B)`, `(H)`, `(T)`, `(W)`, `(TR)`, `(P)`, `(S)`, `(F)`, `(M)`).
- `untranslated_audit details`: Liệt kê chi tiết ID và tên của các vật phẩm khuyết bản dịch phụ để tiện kiểm tra gói ngôn ngữ.

---

## 4. Cấu hình (Configuration)

File cấu hình `config.json` sẽ được tự động tạo khi chạy mod lần đầu, hoặc chỉnh sửa trực tiếp qua menu **Generic Mod Config Menu (GMCM)**:

| Tùy chọn | Kiểu | Mặc định | Ý nghĩa |
| :--- | :--- | :--- | :--- |
| `SecondaryLanguage` | string | `"en"` | Mã ngôn ngữ phụ hiển thị trên tooltip (ví dụ: `en`, `vi`, `es`, `fr`, `de`, `ja`, `ko`, `zh`). |
| `ShowSecondaryInTooltip` | bool | `true` | Bật/tắt hiển thị tên ngôn ngữ phụ trên tooltip. |
| `SecondaryFirst` | bool | `false` | `false`: Tên ngôn ngữ chính ở trên, tên phụ ở dưới.<br>`true`: Tên ngôn ngữ phụ ở trên, tên chính ở dưới. |
| `DiacriticInsensitiveSearch`| bool | `true` | Bật/tắt mặc định chuẩn hóa bỏ dấu tiếng Việt và ánh xạ `đ`/`Đ` khi tìm kiếm qua API. |

---

## 4. Ngôn ngữ hỗ trợ & Giới hạn (Supported Languages & Scope Limits)

### Ngôn ngữ có sẵn trong game (Vanilla Built-in):
Các ngôn ngữ sau có sẵn tệp XNB trong thư mục `Content/` của game:
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
- Khi chơi game bằng ngôn ngữ khác và chọn `SecondaryLanguage: "vi"`, mod sẽ phát hiện thư mục mod `Kou.ViethoaSV` qua `helper.ModRegistry.Get("Kou.ViethoaSV")` hoặc đường dẫn quy ước và đọc các tệp `_vi.xnb`. Nếu không tìm thấy, mod sẽ ghi cảnh báo 1 lần qua mã hóa ngôn ngữ `I18n.Log_SecondaryFallback` và chuyển về tiếng Anh an toàn.

### Phạm vi và Giới hạn (Boundaries):
- Mod chỉ can thiệp vào bảng thông tin tooltip của vật phẩm và tra cứu qua API; **không** thay đổi tên NPC, quái vật, hội thoại hay ghi đè logic game của thuộc tính `Item.Name`/`Item.DisplayName`.
- Không tự ý thêm ô tìm kiếm mới vào giao diện game (cung cấp API tiêu chuẩn để các mod giao diện/tìm kiếm khác tích hợp).
- Đối với các vật phẩm tùy biến từ mod khác không có tệp dữ liệu dịch, mod sẽ giữ an toàn hoặc ẩn tiêu đề phụ thay vì hiển thị tên suy đoán không chính xác.

---

## 6. Hướng dẫn Lệnh Build & Kiểm thử (Build & Verification)

### Build mod (Offline Release, tạo gói Zip):
```pwsh
dotnet build -c Release /p:EnableModDeploy=false /p:EnableModZip=true
```
- Kết quả thư mục mod: `bin/Release/net6.0/`
- Tệp nén Release zip: `bin/Release/net6.0/UntranslatedStardew 1.0.1.zip`

### Chạy bộ kiểm thử hồi quy (Regression Test Suite & Real XNB Smoke Check):
```pwsh
dotnet run --project tests/UntranslatedStardew.Tests/UntranslatedStardew.Tests.csproj
```
Bộ kiểm thử gồm **168 ca kiểm tra** đã qua, gồm kiểm tra biên Harmony CJB và dữ liệu XNB thật:
1. `LanguageHelper`: Phân giải mã chuẩn ISO, phát hiện ngôn ngữ Vanilla vs Tiếng Việt, ánh xạ hậu tố tệp XNB.
2. `SearchMatcher Accent & 'đ'`: Chuẩn hóa Unicode FormD, loại bỏ dấu thanh, ánh xạ `đ`/`Đ` -> `d`/`D`.
3. `SearchMatcher Query Matching`: Khớp chuỗi không phân biệt hoa thường, chuỗi con, cờ phân biệt dấu, phát sinh từ khóa tìm kiếm.
4. `TooltipFormatter`: Định dạng tiêu đề song ngữ, đảo vị trí theo cấu hình, loại bỏ tiêu đề trùng lặp, bảo toàn số lượng ` x5` và nhãn `(Công thức)`.
5. `LexiconPluralize`: Kiểm tra quy tắc số nhiều chuẩn vanilla cho trái cây sấy và các ngoại lệ không biến đổi (`Hops`, `Pickles`, `Dried Cranberries`).
6. `ItemCacheKey`: Khả năng phân biệt khóa cache giữa các loại ngâm ủ, nguyên liệu, cấp độ dụng cụ và các biến thể động của phụ kiện.
7. `Trinket Resolution (Bug Reproduction)`: Khắc phục triệt để lỗi `RawData = null`, phân giải toàn bộ 8 phụ kiện vanilla trong tiếng Anh, Pháp và tiếng Việt, cùng các biến thể động (`frog_variant_0..7`, `PerfectIceRod`, `RapidMagicQuiver`, `HeavyMagicQuiver`).
8. `CJB Search Filter Integration`: Kiểm thử bộ lọc CJB với truy vấn tiếng Việt không dấu, tên phụ tiếng Anh, chuyển đổi cấu hình `ignoreDiacritics`, khớp ID và bảo toàn danh mục.
9. `MockContentProvider & Fallback`: Kiểm tra nạp từ điển, cơ chế per-key fallback về tiếng Anh khi từ điển ngôn ngữ phụ khuyết khóa, thứ tự ưu tiên `displayNameFormat` và bảo toàn mẫu sấy đặc thù.
10. `Stable API Lifecycle`: Kiểm tra API đối tượng ổn định duy trì tham chiếu đến resolver và cấu hình động sau các lần cập nhật mà không bị mất đồng bộ.
11. `Systematic Item Name Coverage Audit`: Kiểm tra tự động độ phủ phân giải trên các danh mục phụ kiện, giày, mũ.
12. `Real XNB Offline Smoke Check`: Nạp thực tế trực tiếp các tệp XNB từ đĩa (Strings/Objects English, French, Vietnamese; Data/Boots; Data/hats; Data/Trinkets; Strings/1_6_Strings) qua ContentManager độc lập mà không cần khởi chạy game hay SMAPI.

---

## 7. Nhật ký thay đổi (Changelog)

### v1.0.1
- **Phụ kiện (Trinkets):** Khắc phục lỗi thiếu tên song ngữ cho toàn bộ 8 phụ kiện vanilla do `TrinketDataDefinition.GetData` trả về `RawData = null`.
- **Biến thể động phụ kiện:** Hỗ trợ tên song ngữ cho các biến thể đổi màu Trứng Ếch (8 màu), gậy băng Perfect Ice Rod, và ống tên Rapid/Heavy Magic Quiver qua `displayNameOverrideTemplate`.
- **Tích hợp CJB Item Spawner:** Tự động hỗ trợ tìm kiếm song ngữ và tìm kiếm không dấu trong CJB Item Spawner khi mod có mặt, bảo toàn các tab danh mục và sắp xếp của CJB.
- **Lệnh Console SMAPI:** Bổ sung lệnh `untranslated_audit` cho phép người dùng kiểm tra nhanh độ phủ tên song ngữ trên toàn bộ vật phẩm đang tải trong game.
- **Làm mới Cache:** Bổ sung vô hiệu hóa cache cho `Data/Trinkets`, `Strings/1_6_Strings`, `Data/Pants`, `Data/Shirts`, `Data/Mannequins` và giải phóng asset trong ContentManager độc lập.
