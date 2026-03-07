# Logic Structure Map: VietnameseVINAPhonemizer

This is a structural breakdown of the deeply nested `if` / `else` blocks from `dem == 1` to `dem == 4` in the VINA Phonemizer output logic.

## Common Variables
- `dem`: Length of the string (1 to 4)
- `loi`: Normalized lyric string
- `NoNext`: True if there is no next note (or next lyric is "R")
- `_C`: Starts with a specific set of consonants
- `_Cw`: Starts with a kw-like sound
- `_CV` / `VV_` / `wV`: Other specific combinations
- `tontaiC`, `tontaiCcuoi`, `tontaiVVC`: Presence of specific start/end consonants/vowels.
- `fry`: Skip condition for most blocks (treats as empty if true).
- `loi.StartsWith(".")`: Special handling for lyrics starting with a dot (affects onset logic).
- `note.lyric != "qua"`: Special exclusion for the word "qua" in tone-shift logic.

---

## Raw Phoneme Shortcut "?" (dòng 115 & 186)
- Condition: `note.lyric.StartsWith("?")`
- Action: `phoneme = note.lyric.Substring(1)` — **bypass toàn bộ logic**, gán thẳng raw phoneme.
- Xảy ra ở **hai nơi**: L115 (trước `prevNeighbour` check) và L186 (trong nhánh `else if prevNeighbour == null`).

---

## ⚠️ CẤU TRÚC SCOPE DỊ THƯỜNG (Quan Trọng!)
Đây là "cái bẫy" lớn nhất trong toàn bộ file. Scope bắt đầu từ `else if (prevNeighbour == null)` ở dòng 188 **KHÔNG nhất quán**:

```
else if (prevNeighbour == null) {          // L188
    if (note.lyric == "qua") {             // L189 — XỬ LÝ ĐẶC BIỆT "qua"
        ...
    } else {
        // 1 âm (dem == 1)                  // L199–217 — bên trong scope else của "qua"
        // 2 âm CV (dem == 2 && tontaiC)    // L219–270 — bên trong scope else của "qua"
    }                                       // L271 — ĐÓNG scope "else" của "qua"
    // 3 âm CVV/CVC — BÊN NGOÀI scope "else"!   // L273 trở đi
    // 4 âm VVVC — BÊN NGOÀI
    // 5 âm... — BÊN NGOÀI
    // BR — BÊN NGOÀI
    // y — BÊN NGOÀI
    // else { // ko phải y
    //   2 âm VV...        — BÊN NGOÀI
    // }
}                                           // L1122 — ĐÓNG scope prevNeighbour == null
```

**Hệ quả**: `dem == 1` và `dem == 2 && tontaiC` **sẽ không chạy** nếu `note.lyric == "qua"`, nhưng `dem == 3..5` **vẫn chạy** kể cả với "qua". Đây gần chắc là **một bug** của code gốc, không phải ý định.

---

## Xử Lý Đặc Biệt "qua" (dòng 189)
- Condition: `prevNeighbour == null && note.lyric == "qua"`
- Branches:
  - If `NoNext`: Add `kwa`, Add `a -` (End)
  - Else: Set `phoneme = "kwa"` (single phoneme shortcut)

---

## 1 Âm (dem == 1)
- Condition: `dem == 1`
- Transformation: Clean up N
- Branches:
  - If `NoNext`:
    - Add `- N`
    - Add `N -` (End)
  - Else:
    - Add `- N`

---

## 2 Âm (CV)
- Condition: `dem == 2 && tontaiC`
- Transformation: Extract N1, N2. Handle `_Cw` and `_CV`.
- Branches:
  - If `NoNext`:
    - If `_C`: Add `- N1` (VCP), Add `N`, Add `N2 -` (End)
    - Else: Add `N`, Add `N2 -` (End)
  - Else:
    - If `_C`: Add `- N1` (VCP), Add `N`
    - Else: Add `N`

---

## 3 Âm (CVV / CVC)
- Condition: `dem == 3 && tontaiC` (Skipped if `fry`)
- Transformation: Extract C, V1, V2.
- Branches:
  - If `tontaiCcuoi`: // Kết thúc bằng phụ âm
    - If `_C`: Add `- Cw` (VCP), Add `C V1`, Add `V1_1 V2_2` (ViTri)
    - Else: Add `C V1`, Add `V1_1 V2_2` (ViTri)
  - Else If `kAn`: // "cân", "kân"
    - If `NoNext`: Add `kAn`, Add `n -` (End)
    - Else: Add `kAn`
  - Else If `NoNext`:
    - If `_C`:
      - If `VV_`: Add `- Cw`(VCP), Add `C V1`, Add `V1_1 V2 -` (End)
      - Else If `wV`: Add `- Cw`(VCP), Add `C V1 V2_2`, Add `V2 -` (End)
      - Else: Add `- Cw`(VCP), Add `C V1`, Add `V1_1 V2_2` (ViTri), Add `N -` (End)
    - Else (ko _C):
      - If `VV_`: Add `C V1`, Add `V1_1 V2 -` (End)
      - Else If `wV`: Add `C V1 V2_2`, Add `V2 -` (End)
      - Else: Add `C V1`, Add `V1_1 V2_2` (ViTri), Add `N -` (End)
  - Else (has Next):
    - If `_C`:
      - If `VV_`: Add `- Cw` (VCP), Add `C V1`, Add `V1_1 V2_2` (ViTri)
      - Else If `wV`: Add `- Cw` (VCP), Add `C V1 V2_2`
      - Else: Add `- Cw` (VCP), Add `C V1`, Add `V1_1 V2_2` (ViTri)
    - Else (ko _C):
      - If `VV_`: Add `C V1`, Add `V1_1 V2_2` (ViTri)
      - Else If `wV`: Add `C V1 V2_2`
      - Else: Add `C V1`, Add `V1_1 V2_2` (ViTri)

---

## 4 Âm (VVVC / VVC liền)
- Condition: `dem == 4 && kocoC && tontaiVVC`
- Transformation: Extract V1, V2, VVC, C
- Branches:
  - If `NoNext && tontaiCcuoi`: Add `- V1 V2`, Add `VVC` (ViTri)
  - Else If `NoNext`: Add `- V1 V2`, Add `VVC` (ViTri), Add `C -` (End)
  - Else: Add `- V1 V2`, Add `VVC` (ViTri)

---

## 4 Âm (CVVC / CVVV)
- Condition: `dem == 4 && tontaiC` (Skipped if `tontaiVVC`)
- Transformation: Extract C, V1, V2, VC, N
- Branches:
  - If `tontaiCcuoi`:
    - If `_C`: Add `- Cw` (VCP), Add `C V1 V2`, Add `VC` (ViTri)
    - Else: Add `C V1 V2`, Add `VC` (ViTri)
  - Else If `loi` ends with "uân" or "uâng":
    - Trùng lặp cực gắt, chia nhánh `wAn == false` vs. `wAn == true` (khuân, luân).
  - Else If `NoNext`:
    - If `VV_`:
      - If `_C`: Add `- Cw`(VCP), Add `C V1 V2`, Add `V2 N_ -`(End)
      - Else: Add `C V1 V2`, Add `V2 N_ -`(End)
    - Else:
      - If `_C`: Add `- Cw`(VCP), Add `C V1 V2`, Add `V2_2 N`(ViTri), Add `N_ -`(End)
      - Else: Add `C V1 V2`, Add `V2_2 N`(ViTri), Add `N_ -`(End)
  - Else (has Next):
    - If `VV_`:
      - If `_C`: Add `- Cw`(VCP), Add `C V1 V2`, Add `V2 N`(ViTri)
      - Else: Add `C V1 V2`, Add `V2 N`(ViTri)
    - Else:
      - If `_C`: Add `- Cw`(VCP), Add `C V1 V2`, Add `V2_2 N`(ViTri)
      - Else: Add `C V1 V2`, Add `V2_2 N`(ViTri)

---

## 4 Âm (Tiên, Tiết)
- Condition: `dem == 4 && tontaiVVC && tontaiC`
- Transformation: Extract C, V1, VVC, N
- Branches:
  - If `tontaiCcuoi`:
    - If `_C`: Add `- Cw` (VCP), Add `C V1`, Add `VVC` (ViTri)
    - Else: Add `C V1`, Add `VVC` (ViTri)
  - Else If `NoNext`:
    - If `_C`: Add `- Cw` (VCP), Add `C V1`, Add `VVC` (ViTri), Add `N -` (End)
    - Else: Add `C V1`, Add `VVC` (ViTri), Add `N -` (End)
  - Else (has Next):
    - If `_C`: Add `- Cw` (VCP), Add `C V1`, Add `VVC` (ViTri)
    - Else: Add `C V1`, Add `VVC` (ViTri)

---

## 5 Âm (CVVVC, ví dụ "thuyết")
- Condition: `dem == 5 && tontaiVVC && tontaiC` (Skipped if `fry`)
- Transformation: Extract C, Cw, V1, V2, VVC, N. Handle `wV`, `_Cw`, `_CV`.
- Branches: **Cấu trúc giống y hệt 4 Âm (Tiên/Tiết)** nhưng nucleus là `C V1 V2` thay vì `C V1`.
  - If `tontaiCcuoi`:
    - If `_C`: Add `- Cw` (VCP), Add `C V1 V2`, Add `VVC` (ViTri)
    - Else: Add `C V1 V2`, Add `VVC` (ViTri)
  - Else If `NoNext`:
    - If `_C`: Add `- Cw` (VCP), Add `C V1 V2`, Add `VVC` (ViTri), Add `N -` (End)
    - Else: Add `C V1 V2`, Add `VVC` (ViTri), Add `N -` (End)
  - Else (has Next):
    - If `_C`: Add `- Cw` (VCP), Add `C V1 V2`, Add `VVC` (ViTri)
    - Else: Add `C V1 V2`, Add `VVC` (ViTri)

---

## Sub-branch Patterns & Nuances
- **Dot Handling (`.StartsWith(".")`)**: Within `uân/uâng` and `dem == 3` blocks, lyrics starting with a dot override the onset to use `C w @` or `vow C w @` instead of the standard leading consonant.
- **Tone Shift (`a = (...)`)**: For lyrics ending in `ia`, `ua`, `ưa`, `ya`, the phonetic nucleus is shifted to `@` (unless the word is "qua").
- **Special Nucleus Combinations**: `Ong`, `ung`, `ong` always use `ng0` as the coda. `Ai` and `ay` use `y/i`.
- **Skip Logic (`if (fry) {} else`)**: If `fry` is true, the entire phonetic extraction block is skipped for that note.

---

## BR (breath)
- Condition: `BR` (lyric starts with "breath")
- Logic đơn giản: nếu `prevNeighbour == null`, add `breath{num}` trực tiếp.

---

## Phụ Âm "y" (dem == 2 hoặc 3, bắt đầu bằng "y")
- Condition: `note.lyric.StartsWith("y") && koVVCchia`
- dem == 2 (ví dụ "ya"):
  - If `NoNext`: Add `- yV`, Add `V -` (End)
  - Else: Add `- yV`
- dem == 3 (ví dụ "yên" nhưng ko VVC):
  - If `tontaiCcuoi`: Add `- yV1`, Add `V1 V2` (ViTri)
  - Else If `NoNext`:
    - If `VV_`: Add `- yV1`, Add `V1 V2 -` (End)
    - Else If `wV`: Add `- yV1 V2`, Add `V2 -` (End)
    - Else: Add `- yV1`, Add `V1 V2` (ViTri), Add `V2 -` (End)
  - Else (has Next):
    - If `wV`: Add `- yV1 V2`
    - Else: Add `- yV1`, Add `V1 V2` (ViTri)

---

## 2 Âm VV (không có phụ âm đầu, ví dụ "oa", "an")
- Condition: `dem == 2 && kocoC` (Skipped if `fry`)
- Transformation: Extract V1, V1_, V2, N. Handle `wV`, `VV_`.
- Branches:
  - If `tontaiCcuoi`: Add `- V1`, Add `V1 V2` (ViTri)
  - Else If `NoNext`:
    - If `wV`: Add `- V1 V2`, Add `N -` (End)
    - Else If `VV_`: Add `- V1`, Add `V1 N -` (End)
    - Else: Add `- V1`, Add `V1_ V2` (ViTri), Add `N -` (End)
  - Else (has Next):
    - If `wV`: Add `- V1 V2`
    - Else If `VV_`: Add `- V1`, Add `V1 N` (ViTri)
    - Else: Add `- V1`, Add `V1_ V2` (ViTri)

---

## 3 Âm VVC/VVV (không có C đầu, không VVC liền, ví dụ "oát", "oan", "oai")
- Condition: `dem == 3 && koVVCchia && kocoC` (Skipped if `fry`)
- Transformation: Extract V1, V2, V2_2, V3, N. Handle `wV`.
- Branches:
  - If `tontaiCcuoi && wV`: Add `- V1 V2`, Add `V2 V3` (ViTri)
  - Else If `NoNext`:
    - If `wV && VV_`: Add `- V1 V2`, Add `V2_2 N -` (End)
    - Else If `wV`: Add `- V1 V2`, Add `V2_2 V3` (ViTri), Add `N -` (End)
  - Else (has Next):
    - If `wV`: Add `- V1 V2`, Add `V2_2 V3` (ViTri)

---

## 3 Âm VVV/VVC (không có C đầu, có VVC liền, ví dụ "yên", "ướt")
- Condition: `dem == 3 && tontaiVVC && kocoC`
- Transformation: Extract V1, VVC, C.
- Branches:
  - If `NoNext && tontaiCcuoi`: Add `- V1`, Add `VVC` (ViTri)
  - Else If `NoNext`: Add `- V1`, Add `VVC` (ViTri), Add `C -` (End)
  - Else: Add `- V1`, Add `VVC` (ViTri)

---

## Design/Refactoring Strategy
- Bóc tách theo chiều dọc (Vertical separation): Xử lý phần "Tạo Cấu Trúc Khởi Đầu" (Onset: add `- N1`, `- Cw`) riêng, phần "Phần Giữa" (Nucleus: `C V1`, `N`, `VVC`) riêng, và phần "Phần Kết Thúc" (Coda: `N2 -`, `C -`, `N -`) riêng.
- Nếu `_C` đúng và là nốt bắt đầu/ngắt quãng (`if (_C && VCP)` gì đó): luôn add list phoneme với cái `position = VCP`.
- Tính `NoNext` (Cuối câu / Dấu gạch ngang): Nếu đúng thì luôn có 1 cái `.Add` với `position = End` ở vế đuôi.
- Phần ở giữa sẽ tuỳ vào số đếm `dem` và `tontaiC`, `tontaiVVC` để chèn vào giữa Onset và Coda.

### GIÁC NGỘ LỚN (The Great Epiphany)
Sau khi đọc toàn bộ file từ dòng 1 tới dòng 2400, em phát hiện ra một sự **lặp code khổng lồ (Massive Duplication)**:
- Nửa trên (từ dòng 200 tới 1123) xử lý trường hợp `if (prevNeighbour == null)`.
- Nửa dưới (từ dòng 1124 tới 2385) xử lý trường hợp `else if (prevNeighbour != null)`.

**Sự khác biệt duy nhất** giữa hàng ngàn dòng code ở 2 nửa này là cách sinh ra nốt bắt đầu (Onset):
- Nửa trên, người dùng tách âm hoàn toàn, nên thêm dấy gạch rành mạch `- C` (ví dụ: `- b`, `- h`, `- a`).
- Nửa dưới, các âm dính liền nhau, phần code dưới tính toán một âm đệm `vow` từ nốt trước (ví dụ `a`, `i`, `@`) và thêm `vow C` (ví dụ: `a b`, `i h`, `@ a`).

### Chiến lược tóm gọn (Từ 2400 dòng xuống 200 dòng)
1. **Phần Đầu (Onset)**: Dựa vào logic `vow` (nếu có note liền trước) hoặc `-` (nếu là chữ đầu câu) ghép với phụ âm đầu.
2. **Phần Giữa (Nucleus)**: Các nguyên âm chính (ví dụ `C V1`, `V1 V2`, `VVC`).
3. **Phần Cuối (Coda)**: Phụ âm cuối hoặc ngắt luồng hơi `N2 -`, `C -`, `N -`.
Tất cả các phần trung gian chỉ là xử lý logic string. Thêm `phonemes.Add` đồng loạt một lần ở cuối hàm sau khi xử lý xong list các mảnh chữ tắt.
