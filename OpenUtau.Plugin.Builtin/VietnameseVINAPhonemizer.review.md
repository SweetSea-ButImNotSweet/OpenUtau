# VietnameseVINAPhonemizer: refactored vs original (quick review)

Nguồn so sánh:
- **Current**: `OpenUtau.Plugin.Builtin/VietnameseVINAPhonemizer.cs` (HEAD)
- **Original baseline**: `OpenUtau.Plugin.Builtin/VietnameseVINAPhonemizer.cs` tại commit `6a4b4b1`

Command đã dùng:
- `git show 6a4b4b1:OpenUtau.Plugin.Builtin/VietnameseVINAPhonemizer.cs > /tmp/vina_original.cs`
- `cp OpenUtau.Plugin.Builtin/VietnameseVINAPhonemizer.cs /tmp/vina_current.cs`
- `git diff --no-index -- /tmp/vina_original.cs /tmp/vina_current.cs`

## Khác biệt lớn (đã fix bug rõ ràng)

1. **Fix lỗi normalize tone bị ghi đè chuỗi nhiều lần**
   - Bản cũ có nhiều dòng `loi = note.lyric.Replace(...)` lặp lại, khiến thay thế trước đó bị mất.
   - Bản mới gom thành `RemoveTones(rawLyric)` nên không còn bug ghi đè này.

2. **Tách helper + list constant**
   - Các chuỗi điều kiện `Contains/StartsWith/EndsWith` được gom vào mảng static + `.Any(...)`, dễ đọc và ít lỗi copy/paste hơn.

## Khác biệt hành vi có rủi ro cần kiểm tra lại

### A) Mapping phụ âm đầu bị thiếu so với original (`x`, `r`, `ngh`)

Trong bản refactored hiện tại, đoạn chuẩn hóa `loi` đang là:

```csharp
loi = loi.Replace("ch", "C").Replace("d", "z").Replace("đ", "d").Replace("ph", "f")
         .Replace("gi", "z").Replace("gh", "g").Replace("c", "k").Replace("kh", "K").Replace("ng", "N")
         .Replace("nh", "J").Replace("tr", "Z").Replace("th", "T").Replace("qu", "kw").Replace("q", "k");
```

Không thấy mapping `x -> s`, `r -> z`, và `ngh -> N` trong nhánh chính này. Trong original, các mapping đó có tồn tại ở nhánh xử lý tương đương.

**Hệ quả có thể gặp**:
- Từ bắt đầu bằng `x` có thể không rơi vào nhóm consonant như trước.
- Từ bắt đầu bằng `r` có thể hành xử khác với `d/gi` (vốn thường map về `z` trong thiết kế cũ).
- Các âm đầu `ngh...` có thể lệch so với logic cũ.

### B) Nhánh đặc biệt `gi*` đổi từ “map riêng” sang “không map”

Refactored dùng `specialGiEndings` và nếu match thì **không chạy block replace**.
Original thì có một nhánh else riêng vẫn map một phần (ví dụ `gi -> zi`, `ng -> N`, `nh -> J`, `ch -> C`, `c -> k`).

**Hệ quả có thể gặp**:
- Các lyric `gi`, `gin`, `gim`, ... có thể đi qua nhánh logic khác hẳn so với original và tạo alias khác.

### C) Mapping `qu` đổi từ hướng cũ sang `kw`

Trong nhánh chuẩn hóa chính hiện tại có `Replace("qu", "kw")`.
Original nhánh chính thiên về `q -> k` (và ở context khác có xử lý `qu -> w`).

**Hệ quả có thể gặp**:
- Một số âm `qu...` có thể tốt hơn (vì đây có vẻ là fix chủ đích), nhưng cũng có khả năng lệch ở vài case biên nếu downstream rule chưa đồng bộ hoàn toàn.

## Kết luận ngắn

- Refactor đã sửa được bug lớn về chuỗi replace bị ghi đè.
- Tuy nhiên, so với original thì hiện tại có 3 cụm khác biệt hành vi đáng test regression:
  1) thiếu mapping `x/r/ngh` ở normalize chính,
  2) xử lý `gi*` đổi semantics,
  3) đổi chuẩn hóa `qu`.

Đề xuất test nhanh A/B bằng tập lyric tối thiểu: `xa, xe, ra, ri, nghe, nghi, gi, gin, qua, quên, quốc`.
