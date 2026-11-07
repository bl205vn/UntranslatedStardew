# Khảo sát font theo ngôn ngữ và vai trò — Stardew Valley 1.6.15

Ngày: 07/11/2026. Phạm vi: bản Windows 1.6.15 đang cài, mã game dịch ngược, wiki modding và pack Việt hóa `Kou.ViethoaSV` 1.6.1. Đây là nghiên cứu; chưa đổi renderer/font trong mod.

## Kết luận cho dòng phụ đang thấy trong ảnh

Ở preview 21, cả `DialogueBoxPatch`, `DialogueQuestionPatch` và `ObjectDialoguePatch` đều gọi `GetSecondaryFont(Game1.dialogueFont)` và vẽ `SpriteBatch.DrawString` với `DialoguePairLayout.SecondaryScale = 0.75`.

`Game1.dialogueFont` là một **SpriteFont** nạp từ **`Fonts/SpriteFont1`**, không phải `Fonts/SmallFont`. Tên biến dễ gây nhầm: font này được dùng cho tiêu đề tooltip và nhiều nhãn GUI, trong khi thân khung hội thoại chính thường đi qua `SpriteText` với renderer/font khác.

Nếu ngôn ngữ phụ là English, font hiện được chọn là `Fonts/SpriteFont1.xnb` gốc. Nếu là Việt và dùng pack đang cài, font phụ ưu tiên asset CP thay thế `Fonts/SpriteFont1`, hiện trỏ đến `SpriteFont1_vi1.xnb`. Nó khác `NewFont.fnt` dùng cho dòng chính khi Việt là ngôn ngữ game. Vì vậy hai dòng có thể khác nét ngoài khác biệt kích thước.

Các ảnh mới xác nhận ba loại thoại trong rạp đã xuất hiện hai ngôn ngữ ở cả chiều Việt–Anh và Anh–Việt. Ảnh không chứng minh mọi font/locale hoặc mọi GUI đã được kiểm tra.

## 1. Tên vai trò khác với tên asset và tên họ font

| Vai trò/điểm vẽ vanilla | API/field | Tài nguyên được dùng |
|---|---|---|
| Thân thoại NPC, hộp thông tin, câu hỏi | `SpriteText.drawString` | Sprite sheet `LooseSprites/font_bold`, hoặc BMFont theo locale/tùy chọn |
| Tên NPC dưới chân dung | Các hàm `SpriteText` căn giữa | Cùng hệ SpriteText |
| Tên hàng trong các hàng của ShopMenu | `SpriteText.drawString` | Cùng hệ SpriteText; không đồng nhất với tiêu đề tooltip |
| Tiêu đề/tên vật phẩm ở tooltip tiêu chuẩn | `Game1.dialogueFont` / `SpriteBatch.DrawString` | `Fonts/SpriteFont1` được localize |
| Thân mô tả tooltip tiêu chuẩn, tên loại, nội dung UI thường | Tham số font của `drawHoverText`, thường `Game1.smallFont` | `Fonts/SmallFont` được localize |
| Câu dưới chân dung chủ shop | `drawHoverText(..., Game1.dialogueFont, ...)` | SpriteFont1, không phải đường khung thoại NPC |
| Nội dung thư thông thường | `SpriteText` | SpriteText; nút nhận việc trong cùng menu lại có thể dùng dialogueFont |
| Nhãn/số rất nhỏ ở các call site dùng tinyFont | `Game1.tinyFont` | `Fonts/tinyFont`; không phải font dự phòng chung cho mọi nội dung |
| Dòng phụ hội thoại của mod hiện tại | `GetSecondaryFont(Game1.dialogueFont)` | SpriteFont1 của locale phụ, thu nhỏ 0.75 |

Đây là các call site tiêu biểu đã đọc, không phải khẳng định mọi tên item, mọi tiêu đề hay mọi mô tả đều dùng một API duy nhất. Một menu có thể dùng cả hai hệ. `SpriteFont` là lớp runtime; `SmallFont` và `SpriteFont1` là hai tên asset thuộc lớp đó. BMFont và sprite sheet cũng là font bitmap theo nghĩa đồ họa, nhưng khác hợp đồng dữ liệu/vẽ với SpriteFont XNB.

## 2. Font native của 12 ngôn ngữ trong menu vanilla

Các đường dẫn dưới đây tương đối với `Content/`, bỏ đuôi `.xnb` cho asset. Cột UI ghi **asset thực sự tồn tại trong thư mục cài đặt**, không suy ra họ TTF gốc.

| Ngôn ngữ | Mã/hậu tố | SpriteText dùng trong hội thoại | SpriteFont1 cho tooltip title/GUI | SmallFont cho mô tả/UI |
|---|---|---|---|---|
| Anh | en, không hậu tố | `LooseSprites/font_bold`, zoom nền 3 | `Fonts/SpriteFont1` | `Fonts/SmallFont` |
| Đức | de / de-DE | `font_bold`, zoom nền 3 | `Fonts/SpriteFont1.de-DE` | `Fonts/SmallFont.de-DE` |
| Tây Ban Nha | es / es-ES | `font_bold`, zoom nền 3 | `Fonts/SpriteFont1.es-ES` | `Fonts/SmallFont.es-ES` |
| Pháp | fr / fr-FR | `font_bold`, zoom nền 3 | `Fonts/SpriteFont1.fr-FR` | `Fonts/SmallFont.fr-FR` |
| Bồ Đào Nha Brazil | pt / pt-BR | `font_bold`, zoom nền 3 | `Fonts/SpriteFont1.pt-BR` | `Fonts/SmallFont.pt-BR` |
| Ý | it / it-IT | `font_bold`, zoom nền 3 | `Fonts/SpriteFont1` — không thấy bản it-IT | `Fonts/SmallFont` — không thấy bản it-IT |
| Thổ Nhĩ Kỳ | tr / tr-TR | `font_bold`, zoom nền 3 | `Fonts/SpriteFont1.tr-TR` | `Fonts/SmallFont.tr-TR` |
| Hungary | hu / hu-HU | `font_bold`, zoom nền 3 | `Fonts/SpriteFont1.hu-HU` | `Fonts/SmallFont.hu-HU` |
| Nga | ru / ru-RU | Mặc định `font_bold` có ánh xạ Cyrillic; bật `useAlternateFont` thì `Fonts/Russian` BMFont, zoom 3 | `Fonts/SpriteFont1.ru-RU` | `Fonts/SmallFont.ru-RU` |
| Nhật | ja / ja-JP | `Fonts/Japanese` BMFont, zoom 1.75 | `Fonts/SpriteFont1.ja-JP` | `Fonts/SmallFont.ja-JP` |
| Hàn | ko / ko-KR | `Fonts/Korean` BMFont, zoom 1.5 | `Fonts/SpriteFont1.ko-KR` | `Fonts/SmallFont.ko-KR` |
| Trung giản thể | zh / zh-CN | Thường: `Fonts/Chinese` BMFont, zoom 1.5; tùy chọn smooth: `Fonts/Chinese_round/Chinese`, zoom 1 | Thường: `Fonts/SpriteFont1.zh-CN`; smooth: `Fonts/Chinese_round/SpriteFont1.zh-CN` | Thường: `Fonts/SmallFont.zh-CN`; smooth: `Fonts/Chinese_round/SmallFont.zh-CN` |

Lưu ý:

- `LocalizedContentManager` thử asset có hậu tố locale, rồi `_international`, rồi asset gốc. Inventory font đang cài không có bản `_international` tương ứng; tiếng Ý và tinyFont dùng asset gốc trong phạm vi này.
- `useAlternateFont` mặc định false trong `Options` đang đọc. SpriteText Nga vì vậy không phải lúc nào cũng dùng Russian.fnt dù file tồn tại.
- Font Trung có thêm `dialogueFontScale`: `FontPixelZoom` được hiệu chỉnh theo tùy chọn, nên zoom ghi trong bảng là nền. `Options.loadChineseFonts` đổi cả hai UI SpriteFont khi chọn smooth.
- SpriteText vẫn dùng sprite sheet cho ký hiệu đặc biệt, chữ Junimo hoặc khi `forceEnglishFont`; một dòng có thể đi qua hơn một đường glyph.
- Mã có nhánh `th` / `Fonts/Thai` và hậu tố th-TH, nhưng menu vanilla đang đọc chỉ có 12 ngôn ngữ, không có Thai, và không tìm thấy asset Thai trong thư mục này. Không coi Thai là một bộ font vanilla đã được cài/hỗ trợ đầy đủ.
- `tinyFont.xnb` chỉ có bản không hậu tố trong inventory. Không suy ra từ đó nó có đủ glyph của mọi ngôn ngữ.

### Họ font được khai báo trong BMFont XML

| File descriptor | `info.face` | `info.size` | `common.lineHeight` | Số trang |
|---|---|---:|---:|---:|
| `Fonts/Chinese.fnt` | Noto Sans CJK SC Bold | 32 | 33 | 4 |
| `Fonts/Chinese_round/Chinese.fnt` | 荆南麦圆体 | 42 | 54 | 1 |
| `Fonts/Japanese.fnt` | SetoFont-SP | 24 | 24 | 2 |
| `Fonts/Korean.fnt` | Sandoll MiSaeng | 48 | 49 | 12 |
| `Fonts/Russian.fnt` | BasicSmaller4Ext | 15 | 15 | 1 |

Đây là tên khai báo trong descriptor của **font SpriteText**. Không gán các tên này cho `SmallFont.*.xnb` hoặc `SpriteFont1.*.xnb` chỉ vì cùng locale. Asset XNB SpriteFont chứa atlas/glyph/metric, không cung cấp căn cứ đủ để xác nhận họ TTF gốc bằng tên file.

### Line spacing không chỉ nằm trong atlas

`Game1` đặt `smallFont.LineSpacing = 28`; khi dịch lại field, Hàn thêm 16, Thổ thêm 4, custom dùng `SmallFontLineSpacing`. Ở luồng khởi tạo cũng có lệnh đặt `dialogueFont.LineSpacing = 42`. Font BMFont có lineHeight, advance và zoom riêng; không lấy một hằng số của SmallFont để đo mọi vai trò. Cần phân biệt metadata trong file với giá trị runtime đã được game/mod điều chỉnh.

## 3. Pack Việt hóa đang cài

Đã đọc `content.json`, `config.json`, descriptor `.fnt` và kiểm tra các file XNB có tồn tại. Cấu hình ghi nhận: **Font1=NewFont, Font2=Style1, DeactiveFonts=false**.

| Vai trò | Cấu hình/asset | Kết luận |
|---|---|---|
| SpriteText của locale vi | `UseLatinFont=false`, `FontFile=Fonts/NewFont`, `FontPixelZoom=1`, `FontApplyYOffset=true` | Dùng BMFont do pack cung cấp cho khung thoại và các call site SpriteText |
| Descriptor active | `assets/Content/Fonts/Font-Vi/NewFont.fnt` | face khai báo `StardewValley_K`, size 48, lineHeight 48, một trang |
| UI body | Target `Fonts/SmallFont` → `assets/Content/Fonts/SmallFont_vi1.xnb` | Theo Font2=Style1 |
| UI title | Target `Fonts/SpriteFont1` → `assets/Content/Fonts/SpriteFont1_vi1.xnb` | Theo Font2=Style1; font phụ hội thoại vi hiện ưu tiên đường này |
| Khi tắt font pack | `DeactiveFonts=true` đổi sang `UseLatinFont=true` và không áp các patch font trên | Cần font khác có glyph Việt; không thể mặc định font Latin gốc đủ mọi dấu |

Khi `Font2=Default`, pack dùng các file `_vi.xnb` thay vì `_vi1.xnb`. Hai mục Font1/Font2 không phải hai tên cho cùng một font: chúng điều khiển hai hệ font khác nhau. Pack không thay `tinyFont` ở các patch đã khảo sát.

## 4. Custom language khác có bắt buộc giống Việt không?

Không. `Data/AdditionalLanguages` cho phép `UseLatinFont=true` để dùng font Latin sẵn có, hoặc `false` với `FontFile`, `FontPixelZoom`, `FontApplyYOffset` cho SpriteText. Một ngôn ngữ có đủ glyph trong font sẵn có có thể không cần thêm font. Một pack cũng có thể dựa vào framework/mod font khác. [Wiki Custom languages](https://stardewvalleywiki.com/Modding%3ACustom_languages)

`FontFile` của AdditionalLanguages không tự thay `Game1.smallFont` hay `Game1.dialogueFont`. Nếu GUI cần ký tự chưa có, tác giả phải cung cấp font/patch/adapter thích hợp cho GUI nữa. Pack Việt đang làm việc đó bằng CP Load các XNB. Đây là nhu cầu theo bộ glyph và phạm vi sử dụng, không phải quy tắc “mọi custom language bắt buộc đóng gói đúng ba bộ font”.

Wiki mô tả descriptor `.fnt` dạng XML và các texture page; Content Patcher làm chúng thành asset game. Vì vậy BMFont ở đây là định dạng dữ liệu mà renderer game đọc, không phải CP tự chuyển mọi đường vẽ chữ sang BMFont. Tài liệu của tác giả FontSmasher cũng phân biệt font dialogue BMFont của CJK với các SpriteFont UI vẫn tồn tại. [Wiki](https://stardewvalleywiki.com/Modding%3ACustom_languages), [FontSmasher — Compatibility](https://github.com/ichortower/FontSmasher#compatibility)

## 5. Hệ quả đối với UntranslatedStardew hiện tại

1. **Hội thoại phụ hiện là GUI title font thu nhỏ**, chưa phải bảo toàn họ/font native của SpriteText cho ngôn ngữ phụ. English phụ dùng SpriteFont1 thay vì font_bold; Việt phụ đang dùng Font2 thay vì Font1.
2. **CJK có cùng khác biệt vai trò:** `SpriteFont1.ja-JP/ko-KR/zh-CN` có glyph của locale nhưng không đồng nghĩa cùng kiểu/metric với BMFont native của dòng chính.
3. **Custom BMFont không phải luôn bị bỏ qua:** `BilingualFontManager` đã có nhánh `CustomBitmapFont.Load` chuyển BMFont thành SpriteFont khi nguồn custom không có SpriteFont ưu tiên. Trong pack Việt hiện tại, `SpriteFontAssets` được chọn trước, nên BMP FontFile không phải đường active cho dòng phụ. Kết luận “mọi custom FontFile không bao giờ dùng được” sẽ sai với code hiện tại.
4. **Chinese smooth còn là khoảng trống:** nhánh built-in phụ hiện tìm các asset gốc có hậu tố locale, chưa thấy lựa chọn `Chinese_round` theo chính sách riêng cho vai trò phụ. Cần thiết kế rõ lựa chọn này khi làm fidelity; không đổi locale game để nạp một lần font phụ.
5. **Scale 0.75 không chứng minh bị lỗi raster:** font bitmap thu nhỏ có thể thay đổi độ đều nét tùy texture, zoom, vị trí và sampler. Cần so ảnh/render thực tế; chưa kết luận ảnh người dùng là lỗi lấy mẫu chỉ từ hệ số scale.
6. `SecondaryFirst` chỉ đổi vị trí. Font và phương pháp đo của mỗi ngôn ngữ/vai trò phải giữ ổn định ở cả hai chiều Anh–Việt và Việt–Anh.

Hướng triển khai tiếp: tách lựa chọn **locale + vai trò + renderer**. Hội thoại/Shop row/biển tên dùng nguồn SpriteText native tương ứng; title tooltip dùng SpriteFont1; mô tả dùng SmallFont. Mỗi renderer phụ phải có metric riêng, không thay các static field SpriteText hoặc ngôn ngữ toàn cục trong lúc vẽ. Có thể tái dùng các adapter dữ liệu/atlas hiện có, nhưng không suy ra rằng mọi renderer có cùng baseline, shadow, icon hay wrap. Đây là hướng thiết kế từ kết quả khảo sát, chưa phải code đã tích hợp.

## 6. Bằng chứng và giới hạn

- [Inventory JSON](research/font-assets-1.6.15.json): 12 locale, asset UI thực tế có/không hậu tố, SHA-256/size file, thông tin 5 BMFont và cấu hình/font Việt active. Không chứa atlas hoặc toàn bộ font game.
- `D:/Downloads/Stardew_decompilers/StardewValley/Game1.cs`: dòng 2360–2375 và 3103–3113 nạp các SpriteFont/spacing; dòng 3061 nạp `LooseSprites/font_bold`.
- `StardewValley.BellsAndWhistles/SpriteText.cs`: `LoadFontData` gần 739, lựa chọn glyph gần 640 và `isUsingNonSpriteSheetFont` gần 850.
- `StardewValley/LocalizedContentManager.cs`: `CurrentLanguageLatin` gần 117; localized asset fallback gần 392; `LanguageCodeString` gần 446.
- `StardewValley/Options.cs`: `loadChineseFonts` gần 1020; default font options gần 722.
- `StardewValley.Menus/IClickableMenu.cs`: `drawToolTip` gần 1021, title/body của `drawHoverText` gần 1200/1270. `ShopMenu.cs` gần 1926/1930 vẽ tên hàng bằng SpriteText, gần 2024 vẽ câu người bán bằng dialogueFont.
- Mã mod: `Core/BilingualFontManager.cs` (chọn font theo vai trò/locale và thứ tự fallback), `Core/CustomBitmapFont.cs`, `Core/DialoguePairLayout.cs` (scale), ba patch hội thoại.
- Antigravity đã đọc tài liệu FontSmasher và đưa báo cáo độc lập. Truy cập wiki bên AGY bị Cloudflare chặn; phía Codex đọc được trang wiki và đã đối chiếu trực tiếp. Những nhận định tuyệt đối về “không bao giờ dùng BMFont” hoặc “scale chắc chắn gây artifact” không được chấp nhận vì chưa khớp code/bằng chứng.
- Không sửa code, không phát hành preview mới, không yêu cầu kiểm tra lại từng locale trong game ở đợt nghiên cứu này. Chưa kiểm tra GPU/font override của mọi mod hoặc tên TTF gốc của tất cả XNB.
