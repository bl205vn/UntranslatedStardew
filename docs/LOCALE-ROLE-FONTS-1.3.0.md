# Font theo vai trò và nguồn ngôn ngữ — 1.3.0

Phạm vi: tên item, tooltip kỹ năng/nghề và menu chọn nghề cấp 5/10, Vật phẩm & Quyền hạn, tên trong Bộ sưu tập (gồm tiêu đề thư/mật thư). Không bổ sung hội thoại song ngữ, ruby text hay ô tìm kiếm.

## Hai vai trò font

- Chữ chính dùng font hiện hành của game tại điểm vẽ (`smallFont` hoặc `dialogueFont`), kế thừa font chính do FontSettings thay thế.
- Chữ phụ dùng font của locale phụ ngay cả khi toàn bộ tên là ASCII và font chính cũng có ký tự đó.
- `SecondaryFirst` đổi vị trí, không đổi vai trò font. Các font chỉ nạp khi cần, không đổi ngôn ngữ toàn cục và không nạp lại toàn bộ game thành hai bản.
- Khi font được chọn thiếu glyph, vẫn có dự phòng sang font còn lại; nếu cả hai thiếu thì game dùng ký tự mặc định và mod ghi cảnh báo có giới hạn. Vì vậy không thể bảo đảm mọi ký tự của mọi bộ chữ bằng hai file font bất kỳ.
- Đây là hai **bộ** font theo ngôn ngữ: mỗi bộ có thể gồm font nhỏ và font tiêu đề. Không triển khai lựa chọn font riêng cho `tinyFont`, hay chỉnh màu/cỡ chữ phụ qua FontSettings trong bản này.

Chuỗi được chuẩn hóa NFC trước khi tính vị trí vai trò. Phép đo và vẽ dùng cùng font. Nhãn thư/tab được ngắt dòng bằng phép đo theo vai trò, có ngắt từ dài/CJK độc lập với locale chính. Nội dung thân mô tả giữ vai trò chính.

## Ngôn ngữ tích hợp và custom

Các mã tích hợp: `en`, `es`, `de`, `fr`, `pt`, `ru`, `ja`, `zh`, `ko`, `it`, `tr`, `hu`; nhận các alias locale tương ứng. `default` dùng tài nguyên gốc không có hậu tố; trên bản game đang kiểm tra đó là tiếng Anh. `en` vẫn là mã tiếng Anh tường minh. Font locale không có file riêng dùng font gốc, giống các locale Latin của game.

Gói `Kou.ViethoaSV` có adapter cho `assets/Content/*_vi.xnb`, không cần sửa pack hiện tại. Các custom language khác dùng hợp đồng nguồn bên dưới. Không tự suy luận được thư mục/font của mọi pack chỉ từ `LanguageCode` hoặc `FontFile`: `FontFile` của `Data/AdditionalLanguages` là BMFont của SpriteText, khác SpriteFont mà các tooltip đang dùng. Pack chỉ có BMFont cần cung cấp SpriteFont hoặc adapter thích hợp.

Không có nguồn custom hợp lệ: giữ tên chính và ghi cảnh báo; không âm thầm lấy toàn bộ tiếng Anh rồi gọi đó là ngôn ngữ custom. Với nguồn hợp lệ nhưng thiếu một khóa, vẫn giữ cơ chế dự phòng từng khóa về dữ liệu gốc. Nội dung mod C#/CP không có bản dịch được khai báo không thể tự sinh bản dịch.

## Hợp đồng nguồn cho tác giả mod

CP có thể `EditData` asset `Mods/GiaHung.UntranslatedStardew/LanguageSources`, giá trị là descriptor:

```json
{
  "LanguageCode": "xx-example",
  "SourceModId": "Author.LanguagePack",
  "ContentRoot": "assets/Content",
  "AssetPattern": "{asset}_xx",
  "SmallFont": "Fonts/SmallFont_xx",
  "DialogueFont": "Fonts/SpriteFont1_xx",
  "Strings": {
    "Strings/StringsFromCSFiles:Wallpaper.cs.13204": "Example wallpaper name"
  }
}
```

`ContentRoot` tương đối với thư mục mod có UniqueID `SourceModId`, hoặc đường dẫn tuyệt đối tường minh. Tên asset tương đối với ContentRoot, không có đuôi `.xnb`; placeholder phân biệt hoa thường: `{asset}`, `{locale}`. File phải tồn tại, đúng kiểu XNB (`SpriteFont` với font, từ điển đúng schema với chuỗi). `Strings` có thể cung cấp chuỗi trực tiếp, ưu tiên khóa `asset:key`, sau đó khóa không có asset. Dùng ID item có tiền tố loại, thí dụ `(WP)0`, `(FL)0`, `(O)0`, khi đăng ký tên qua asset `Names`/API cũ.

SMAPI 4.5.2 không công khai đường dẫn mod qua `IModInfo`. Adapter đường dẫn tương đối đọc thuộc tính `DirectoryPath` của metadata SMAPI hiện tại, có kiểm tra lỗi. Nếu SMAPI thay đổi metadata này, dùng API với đường dẫn tuyệt đối hoặc cập nhật adapter. Không dựa vào thư mục làm việc của tiến trình. Kiểm tra đường dẫn có chặn `..` thoát root về mặt đường dẫn; không coi đây là sandbox bảo mật chống junction/symlink.

Ví dụ CP ở `examples/LanguageSourcePack`; đây là mẫu tích hợp, phải thay tên và cung cấp tài nguyên/font của pack thực tế, không đóng gói font game trong repository.

C# gọi trên game thread sau GameLaunched (lưu token, không `using` ngay trong handler nếu cần tồn tại lâu):

```csharp
var api = Helper.ModRegistry.GetApi<IUntranslatedStardewApi>("GiaHung.UntranslatedStardew");
languageToken = api?.RegisterLanguageSource(
    ModManifest.UniqueID, "xx-example", Path.Combine(Helper.DirectoryPath, "assets", "Content"),
    assetPattern: "{asset}_xx", smallFont: "Fonts/SmallFont_xx",
    dialogueFont: "Fonts/SpriteFont1_xx");
// Khi gỡ nguồn: languageToken?.Dispose();
```

API đã có `RegisterTranslation(s)` cho tên item/mail/power/...; `RegisterLanguageSource` cung cấp nguồn tài nguyên và font, không có tham số `strings` trong chữ ký C# hiện tại.

Cũng có thể thêm CustomFields vào mục `Data/AdditionalLanguages`: `UntranslatedStardew.SourceModId`, `.ContentRoot`, `.AssetPattern`, `.SmallFont`, `.DialogueFont` (cùng tiền tố `UntranslatedStardew`). Owner phải tường minh; ID của mục ngôn ngữ không mặc nhiên là UniqueID của pack.

Nguồn được đăng ký sau cùng có ưu tiên. Khi reload, snapshot CP và AdditionalLanguages thay thế dữ liệu cũ theo nguồn gốc riêng, không xóa nguồn API/bundled. Đăng ký muộn/gỡ nguồn cập nhật provider, cache tên và font. Descriptor được sao chép để tránh người gọi thay đổi cache ngoài ý muốn.

## Giấy dán tường và sàn

`WallpaperDataDefinition`/`FlooringDataDefinition` có `RawData = null`. Resolver trước đây thiếu nhánh này nên một số locale không tìm được tên. Bản này đọc `Strings/StringsFromCSFiles:Wallpaper.cs.13204` và `.13203`. Các ID khác nhau vẫn có cache riêng; nhiều mẫu cùng tên không phải lỗi. Nếu hai ngôn ngữ thực sự cùng tên, mod chỉ hiện một lần.

## Bằng chứng và kiểm tra trong game

Build Release offline, không deploy; 885 assertions đạt. Có kiểm tra XNB thật cho locale tích hợp và pack Việt, đo font headless, interception Harmony và bắt lệnh vẽ, NFD/NFC, font ASCII theo vai trò, CJK wrapping, cache item cùng tên/khác ID và vòng đời CP/API. Đây không phải xác nhận hình ảnh trong game.

Cần kiểm tra thủ công trước khi phát hành: Việt↔Anh, Anh↔Nhật/Trung/Hàn/Nga, hai locale Latin, custom pack dùng font riêng; cả hai thứ tự SecondaryFirst; tooltip item thường/trinket/giấy dán tường/sàn, kỹ năng/nghề cấp 5/10, quyền hạn, các tab bộ sưu tập, thư và mật thư có ảnh. Đổi ngôn ngữ khi đang chơi, reload CP, kiểm tra mép màn hình và FontSettings. Kiểm tra tìm kiếm song ngữ bằng ô tìm kiếm có sẵn của CJB, không tạo ô mới. Tương thích framework hiện mới được đối chiếu nguồn và test offline, chưa nghiệm thu trực quan.
