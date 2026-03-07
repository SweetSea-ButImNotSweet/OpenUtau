using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Vietnamese VINA Phonemizer", "VIE VINA", "Jani Tran - Hoang Phuc", language: "VI")]
    public class VietnameseVINAPhonemizer : Phonemizer {
        /// <summary>
        /// The lookup table to convert a hiragana to its tail vowel.
        /// </summary>
        static readonly string[] vowels = [
            "a=a,à,á,ả,ã,ạ,ă,ằ,ắ,ẳ,ẵ,ặ,A,À,Á,Ả,Ã,Ạ,Ă,Ằ,Ắ,Ẳ,Ẵ,Ặ",
            "A=â,ầ,ấ,ẩ,ẫ,ậ,Â,Ầ,Ấ,Ẩ,Ẫ,Ậ",
            "@=ơ,ờ,ớ,ở,ỡ,ợ,Ơ,Ờ,Ớ,Ở,Ỡ,Ợ,@",
            "i=i,y,ì,í,ỉ,ĩ,ị,ỳ,ý,ỷ,ỹ,ỵ,I,Y,Ì,Í,Ỉ,Ĩ,Ị,Ỳ,Ý,Ỷ,Ỹ,Ỵ",
            "e=e,è,é,ẻ,ẽ,ẹ,E,È,É,Ẻ,Ẽ,Ẹ",
            "E=ê,ề,ế,ể,ễ,ệ,Ê,Ề,Ế,Ể,Ễ,Ệ",
            "o=o,ò,ó,ỏ,õ,ọ,O,Ò,Ó,Ỏ,Õ,Ọ",
            "O=ô,ồ,ố,ổ,ỗ,ộ,Ô,Ồ,Ố,Ổ,Ỗ,Ộ",
            "u=u,ù,ú,ủ,ũ,ụ,U,Ù,Ú,Ủ,Ũ,Ụ",
            "U=ư,ừ,ứ,ử,ữ,ự,Ư,Ừ,Ứ,Ử,Ữ,Ự",
            "m=m,M",
            "n=n,N",
            "ng=g,G",
            "nh=h,H",
            "-=c,C,t,T,-,p,P,R,',1,2,3,4,5",
            ".=.",
        ];

        static readonly Dictionary<string, string> vowelLookup;

        static VietnameseVINAPhonemizer() {
            vowelLookup = vowels.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
        }

        private static string RemoveTones(string text) {
            if (string.IsNullOrEmpty(text)) return text;
            return text
                .Replace('à', 'a').Replace('á', 'a').Replace('ả', 'a').Replace('ã', 'a').Replace('ạ', 'a')
                .Replace('ằ', 'ă').Replace('ắ', 'ă').Replace('ẳ', 'ă').Replace('ẵ', 'ă').Replace('ặ', 'ă')
                .Replace('ầ', 'â').Replace('ấ', 'â').Replace('ẩ', 'â').Replace('ẫ', 'â').Replace('ậ', 'â')
                .Replace('ờ', 'ơ').Replace('ớ', 'ơ').Replace('ở', 'ơ').Replace('ỡ', 'ơ').Replace('ợ', 'ơ')
                .Replace('ì', 'i').Replace('í', 'i').Replace('ỉ', 'i').Replace('ĩ', 'i').Replace('ị', 'i')
                .Replace('ỳ', 'y').Replace('ý', 'y').Replace('ỷ', 'y').Replace('ỹ', 'y').Replace('ỵ', 'y')
                .Replace('è', 'e').Replace('é', 'e').Replace('ẻ', 'e').Replace('ẽ', 'e').Replace('ẹ', 'e')
                .Replace('ề', 'ê').Replace('ế', 'ê').Replace('ể', 'ê').Replace('ễ', 'ê').Replace('ệ', 'ê')
                .Replace('ò', 'o').Replace('ó', 'o').Replace('ỏ', 'o').Replace('õ', 'o').Replace('ọ', 'o')
                .Replace('ồ', 'ô').Replace('ố', 'ô').Replace('ổ', 'ô').Replace('ỗ', 'ô').Replace('ộ', 'ô')
                .Replace('ù', 'u').Replace('ú', 'u').Replace('ủ', 'u').Replace('ũ', 'u').Replace('ụ', 'u')
                .Replace('ừ', 'ư').Replace('ứ', 'ư').Replace('ử', 'ư').Replace('ữ', 'ư').Replace('ự', 'ư');
        }

        private static string NormalizePhonetics(string text) {
            if (string.IsNullOrEmpty(text)) return text;
            // Map Vietnamese vowels to VINA phonetic symbols
            text = text.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i")
                       .Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
            // Map Consonants
            return text.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                       .Replace("Z", "tr").Replace("T", "th");
        }

        private static void ApplySpecialNuances(string loi, ref string v2, ref string v2_2, ref string n) {
            bool isToneShift = IsToneShift(loi);
            if (isToneShift && !loi.Contains("qua")) {
                v2 = "@";
                v2_2 = "@";
                n = "@";
            }

            // Special Coda mappings
            if (IsNgCoda(loi)) {
                n = "ng0";
            }
            if (loi.EndsWith("Ai") || loi.EndsWith("ay")) {
                v2 = "y";
                n = "i";
            }
        }

        private static void AddPhoneme(List<Phoneme> phonemes, string phoneme, int? position = null) {
            if (position.HasValue) {
                phonemes.Add(new Phoneme { phoneme = phoneme, position = position.Value });
            } else {
                phonemes.Add(new Phoneme { phoneme = phoneme });
            }
        }


        private static bool IsToneShift(string s) => s.EndsWith("ia") || s.EndsWith("ua") || s.EndsWith("ưa") || s.EndsWith("ya");
        private static bool IsToneShiftStrict(string s) => s == "ia" || s == "ua" || s == "ưa";
        private static bool IsNgCoda(string s) => s == "Ong" || s == "ung" || s == "ong";
        private static bool IsN0Coda(string s) => s == "ôN" || s == "uN" || s == "oN";
        private static bool IsNasalLetter(string s) => s == "N" || s == "n" || s == "J" || s == "m";

        /// <summary>
        /// Tính toán âm chuyển tiếp (vowel transition) từ nốt trước đó sang nốt hiện tại.
        /// Hàm này trả về nguyên âm (vow) của nốt trước để tạo sự nối âm mượt mà (VD: từ 'ba' sang 'ca' -> lấy 'a').
        /// Đồng thời nó xác định nốt trước có tận cùng bằng phụ âm đóng không (prevHasFinalC - ví dụ: t, p, c, k),
        /// và kiểm tra xem có cần bỏ qua việc nối âm (noVCP) do trùng phụ âm hoặc trùng nguyên âm không.
        /// </summary>
        private string TinhToanAmChuyenTiep(Note prevNote, string currentLoi, bool isH, bool startsWithC, out bool prevHasFinalC, out bool noVCP) {
            var prevLyric = prevNote.phoneticHint ?? prevNote.lyric;
            var unicode = ToUnicodeElements(prevLyric);
            string vow = "-";
            if (!vowelLookup.TryGetValue(unicode.LastOrDefault() ?? string.Empty, out vow)) {
                vow = "-";
            }

            string PR = prevNote.lyric;
            if (PR.EndsWith("nh")) vow = "nh";
            if (PR.EndsWith("ng")) vow = "ng";
            if (PR.EndsWith("ch") || PR.EndsWith("t") || PR.EndsWith("k") || PR.EndsWith("p")) vow = "-";

            if (PR != "R") PR = PR.ToLower();
            if (PR == "gi") PR = "zi";

            PR = RemoveTones(PR);
            PR = NormalizePhonetics(PR);

            if (currentLoi == "R") {
                if (PR.EndsWith("ua") || PR.EndsWith("ưa") || PR.EndsWith("ia") || PR.EndsWith("uya")) vow = "@";
            } else {
                if (PR.EndsWith("ua") || PR.EndsWith("ưa") || PR.EndsWith("ia") || PR.EndsWith("uya")) vow = "@0";
                if (PR.EndsWith("breaT")) vow = "-";
                if (PR.EndsWith("ao") || PR.EndsWith("eo") || PR.EndsWith("êu") || PR.EndsWith("iu") || PR.EndsWith("ưu")) vow = "u0";
                if (PR.EndsWith("ai") || PR.EndsWith("ơi") || PR.EndsWith("oi") || PR.EndsWith("ôi") || PR.EndsWith("ui") || PR.EndsWith("ưi")) vow = "i0";
            }

            if (PR.EndsWith("uôN")) vow = "ng";
            else if (PR.EndsWith("uN") || PR.EndsWith("ôN") || PR.EndsWith("oN")) vow = "ng0";

            prevHasFinalC = PR.EndsWith("t") || PR.EndsWith("C") || PR.EndsWith("p") || PR.EndsWith("k") || PR.EndsWith("'");
            if (prevHasFinalC && startsWithC) {
                if (PR.EndsWith('t')) vow = "t";
                if (PR.EndsWith('C')) vow = "ch";
                if (PR.EndsWith('p')) vow = "p";
                if (PR.EndsWith('k')) vow = "k";
                if (PR.EndsWith('\'')) vow = "-";
            }

            string b1 = PR.Length > 0 ? PR.Substring(PR.Length - 1) : "";
            string b2 = currentLoi.Length > 0 ? currentLoi.Substring(0, 1) : "";
            bool mSame = (b1 == b2) && vow != "ng0";
            noVCP = (isH && prevHasFinalC) || mSame;

            return vow;
        }

        static readonly string[] VVC_LIST = ["iên", "iêN", "iêm", "iêt", "iêk", "iêp", "iêu", "yên", "yêN", "yêm", "yêt", "yêk", "yêp", "yêu", "uôn", "uôN", "uôm", "uôt", "uôk", "uôi", "ươn", "ươN", "ươm", "ươt", "ươk", "ươp", "ươi", "ươu"];
        static readonly string[] CCUOI_ENDS = ["k", "t", "C", "p", "."];
        static readonly string[] C_STARTS = ["b", "C", "d", "f", "g", "h", "k", "K", "l", "m", "n", "N", "J", "r", "s", "t", "T", "Z", "v", "w", "z", "p", "'", "."];
        // Trong code gốc có cái này nhưng lại không được dùng?
        // static readonly string[] VV_ENDS = ["ai", "ơi", "oi", "ôi", "ui", "ưi", "ao", "eo", "êu", "iu", "an", "ơn", "in", "en", "ên", "on", "ôn", "un", "ưn", "am", "ơm", "im", "em", "êm", "om", "ôm", "um", "ưm", "aN", "ơN", "iN", "eN", "êN", "ưN", "aJ", "iJ", "êJ", "at", "ơt", "it", "et", "êt", "ot", "ôt", "ut", "ưt", "aC", "iC", "êC", "ak", "ơk", "ik", "ek", "êk", "ok", "ôk", "uk", "ưk", "ap", "ơp", "ip", "ep", "êp", "op", "ôp", "up", "ưp", "ia", "ua", "ưa", "ay", "ây", "uy", "au", "âu", "oa", "oe", "uê"];
        static readonly string[] VITRINGAN_ENDS = ["ai", "ơi", "oi", "ôi", "ui", "ưi", "ao", "eo", "êu", "iu", "an", "ơn", "in", "en", "ên", "on", "ôn", "un", "ưn", "am", "ơm", "im", "em", "êm", "om", "ôm", "um", "ưm", "aN", "ơN", "iN", "eN", "êN", "ưN", "at", "ơt", "it", "et", "êt", "ot", "ôt", "ut", "ưt", "ak", "ơk", "ik", "ek", "êk", "ok", "ôk", "uk", "ưk", "ap", "ơp", "ip", "ep", "êp", "op", "ôp", "up", "ưp", "ia", "ua", "ưa", "uôN", "yt", "yn", "ym", "yC", "yp", "yk", "yN"];
        static readonly string[] VITRIDAI_ENDS = ["uy", "au", "âu", "oa", "oe", "uê"];
        static readonly string[] VITRITB_CONTAINS = ["ăt", "ât", "ăk", "âk", "ăp", "âp", "ăn", "ân", "ăN", "âN", "ăm", "âm", "aJ", "iJ", "êJ", "yJ", "ôN", "uN", "oN", "aC", "iC", "êC", "yC"];
        static readonly string[] VITRITB_ENDS = ["oay", "uây", "ay", "ây", "oay'", "uây'", "ay'", "ây'"];
        static readonly string[] _C_STARTS = ["f", "K", "l", "m", "n", "J", "N", "s", "v", "z"];
        static readonly string[] _CW_STARTS = ["Ku", "Koa", "Koe", "Koă", "su", "soa", "soe", "soă", "zu", "zoa", "zoe", "zoă", "Ky", "Ki"];
        static readonly string[] _CV_STARTS = ["g", "h", "'", "w", "y"];
        static readonly string[] WV_CONTAINS = ["oa", "oe", "uâ", "uê", "uy", "uơ", "oă", "wa"];
        static readonly string[] VV_UNDERSCORE_ENDS = ["ai", "eo", "ua", "ưa", "ơi", "oi", "ôi", "ui", "ưi", "ya", "êu", "ưu", "ao", "ia", "iu", "ai'", "eo'", "ua'", "ưa'", "ơi'", "oi'", "ôi'", "ui'", "ưi'", "ya'", "êu'", "ưu'", "ao'", "ia'", "iu'"];
        static readonly string[] WAN_STARTS = ["K", "z"];
        static readonly string[] H_STARTS = ["b", "d", "k", "l", "t", "T", "C", "m", "n", "J", "N", "h", "g", "."];
        static readonly string[] VCP70_STARTS = ["b", "d", "g", "k", "l", "m", "n", "nh", "ng", "t", "th", "v", "w", "y"];


        private USinger singer;

        public override void SetSinger(USinger singer) => this.singer = singer;
        // Legacy mapping. Might adjust later to new mapping style.
        public override bool LegacyMapping => true;
        // Timing Constants
        private const int DurationThreshold = 350;
        private const int VcpOffset = -90;
        private const int DefaultShortOffset = 170;
        private const int DefaultLongDuration = 90;
        private const int DefaultMediumDuration = 180;
        private const int DefaultEndOffset = 50;

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            var note = notes[0];
            if (!string.IsNullOrEmpty(note.phoneticHint)) {
                return MakeSimpleResult(note.phoneticHint);
            }
            int totalDuration = notes.Sum(n => n.duration);
            int Short = 0;
            int Long = 0;
            int Medium = 0;
            int VCP = VcpOffset;
            int End = 0;
            int ViTri = 0;

            if (totalDuration < DurationThreshold) {
                Short = totalDuration * 4 / 7;
                Long = totalDuration / 6;
                Medium = totalDuration / 3;
                End = totalDuration * 4 / 5;
            } else {
                Short = totalDuration - DefaultShortOffset;
                Long = DefaultLongDuration;
                Medium = DefaultMediumDuration;
                End = totalDuration - DefaultEndOffset;
            }
            ViTri = Short;
            var phonemes = new List<Phoneme>();
            bool a = false, BR = false;

            if (note.lyric.StartsWith("?")) {
                AddPhoneme(phonemes, note.lyric.Substring(1));
                // Map OTO và return ngay
                var attr0 = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
                if (singer.TryGetMappedOto($"{note.lyric.Substring(1)}{attr0.alternate?.ToString() ?? string.Empty}", note.tone + attr0.toneShift, attr0.voiceColor, out var oto0)) {
                    phonemes[0] = new Phoneme { phoneme = oto0.Alias };
                }
                return new Result { phonemes = phonemes.ToArray() };
            }

            bool NoNext = nextNeighbour == null && note.lyric != "R";
            bool fry = note.lyric.EndsWith("'");

            var rawLyric = note.lyric != "R" ? note.lyric.ToLower() : note.lyric;
            if (rawLyric == "quôc") {
                rawLyric = "quâc";
            }

            var lyricWithoutTones = RemoveTones(rawLyric);
            var loi = lyricWithoutTones;

            HashSet<string> specialGiEndings = new HashSet<string> { "gi", "gin", "gim", "ginh", "ging", "git", "gip", "gic", "gich" };
            if (!specialGiEndings.Contains(rawLyric)) {
                loi = NormalizePhonetics(loi);
            }

            bool tontaiVVC = VVC_LIST.Any(loi.EndsWith);
            bool tontaiCcuoi = CCUOI_ENDS.Any(loi.EndsWith);
            bool tontaiC = C_STARTS.Any(loi.StartsWith);

            bool _CV = _CV_STARTS.Any(loi.StartsWith);
            bool _C = _C_STARTS.Any(loi.StartsWith);
            bool _Cw = _CW_STARTS.Any(loi.StartsWith);

            bool wV = WV_CONTAINS.Any(loi.Contains) || loi.EndsWith("oa") || loi.EndsWith("oe") || loi.EndsWith("uê") || loi.EndsWith("uy") || loi.EndsWith("uơ");
            bool VV_ = VV_UNDERSCORE_ENDS.Any(loi.EndsWith);
            bool wAn = WAN_STARTS.Any(loi.StartsWith);
            bool H = H_STARTS.Any(loi.StartsWith);
            bool VCP70 = VCP70_STARTS.Any(loi.StartsWith);

            var (kocoC, koVVCchia) = (!tontaiC, !tontaiVVC);

            // Adjust timing positions based on lyric characteristics
            if (VITRINGAN_ENDS.Any(loi.EndsWith) || VITRITB_CONTAINS.Any(loi.Contains) || VITRITB_ENDS.Any(loi.EndsWith) || VCP70 || loi.EndsWith("uôN")) {
                ViTri = Short;
            }
            if (VITRIDAI_ENDS.Any(loi.EndsWith)) {
                ViTri = Long;
            }

            var phoneme = "";
            var dem = loi.Length;
            bool prevtontaiCcuoi = false;
            bool NoVCP = false;
            string vow = "";
            bool isFirstNote = prevNeighbour == null;
            if (!isFirstNote) {
                vow = TinhToanAmChuyenTiep(prevNeighbour.Value, loi, H, _C, out prevtontaiCcuoi, out NoVCP);
            }

            if (note.lyric.StartsWith("?")) {
                phoneme = note.lyric.Substring(1);
            } else if (loi == "R") {
                if (isFirstNote) {
                    string N = "R";
                    if (NoNext) {
                        AddPhoneme(phonemes, $"- {N}");
                        AddPhoneme(phonemes, $"{N} -", End);
                    } else {
                        AddPhoneme(phonemes, $"- {N}");
                    }
                } else {
                    AddPhoneme(phonemes, $"{vow} --");
                }
                // 1 âm
            } else if (dem == 1) {
                string N = loi;
                N = NormalizePhonetics(N);
                string N2 = N;
                if (!isFirstNote) {
                    bool A = vow == "o" || vow == "O" || vow == "u";
                    if (A && loi == "ng") N2 = "ng0";
                    if (!IsNasalLetter(loi)) {
                        vow += " ";
                    }
                    if (IsNasalLetter(loi) && prevtontaiCcuoi) { vow = "- "; } else if (prevtontaiCcuoi)
                        vow = ".";
                }
                string onsetPrefix = isFirstNote ? "- " : vow;

                if (NoNext) {
                    AddPhoneme(phonemes, $"{onsetPrefix}{N}");
                    AddPhoneme(phonemes, $"{N2} -", End);
                } else {
                    AddPhoneme(phonemes, $"{onsetPrefix}{N}");
                }
                // 2 âm CV, ví dụ: "ba"
            } else if ((dem == 2) && tontaiC) {
                string N = loi;
                string N1 = loi.Substring(0, 1);
                string N2 = loi.Substring(1, 1);
                N1 = NormalizePhonetics(N1);
                if (_Cw) {
                    if (N2 == "u") N1 = N1 + "w";
                    if ((N2 == "i") || (N2 == "y")) N1 = N1 + "y";
                }
                N = NormalizePhonetics(N);
                N2 = NormalizePhonetics(N2);

                if (_CV && (isFirstNote || prevtontaiCcuoi)) { N = "- " + N; }
                if (!isFirstNote) vow += " ";

                bool hasVCP = isFirstNote ? _C : !NoVCP;
                string prefixVCP = isFirstNote ? "- " : vow;

                if (NoNext) {
                    if (hasVCP) {
                        AddPhoneme(phonemes, $"{prefixVCP}{N1}", VCP);
                        AddPhoneme(phonemes, $"{N}");
                        AddPhoneme(phonemes, $"{N2} -", End);
                    } else {
                        AddPhoneme(phonemes, $"{N}");
                        AddPhoneme(phonemes, $"{N2} -", End);
                    }
                } else if (hasVCP) {
                    if (hasVCP) {
                        AddPhoneme(phonemes, $"{prefixVCP}{N1}", VCP);
                        AddPhoneme(phonemes, $"{N}");
                    } else {
                        AddPhoneme(phonemes, $"{N}");
                    }
                }
                // 3 âm CVV/CVC, ví dụ: "hoa" "hang" "hát"
            } else if (dem == 3 && tontaiC && !fry) {
                string C = loi.Substring(0, 1);
                string V1 = loi.Substring(1, 1);
                string V2 = loi.Substring(2);
                string V2_2 = V2;
                string Cw = C;
                string V1_1 = V1;
                if (loi.EndsWith("uy")) { V2 = "i"; V2_2 = V2; }
                bool kAn = loi.EndsWith("cân") || loi.EndsWith("kân");
                if (V1 == "â") V1 = "@";
                if (V1 == "ă") V1_1 = "ae";
                if (wV && _Cw) {
                    Cw = C + "w";
                    V1 = "w";
                } else if (wV) {
                    V1 = "w";
                } else if (_Cw) Cw = C + "w";
                if (V1 == "i" && _Cw) Cw = C + "y";
                Cw = NormalizePhonetics(Cw);
                C = NormalizePhonetics(C);
                V1 = NormalizePhonetics(V1);
                V2 = NormalizePhonetics(V2);
                V2_2 = NormalizePhonetics(V2_2);
                V1_1 = NormalizePhonetics(V1_1);
                a = IsToneShift(loi);
                if (a && note.lyric != "qua") {
                    V2 = "@";
                    V2_2 = "@";
                }
                string N = V2;
                if (IsNgCoda(V1 + V2)) {
                    N = "ng0";
                }
                if (V1 + V2 == "Ai") {
                    V2 = "y";
                    N = "i";
                }
                if (loi.EndsWith("ay")) {
                    V2 = "y";
                    N = "i";
                }
                if (_CV && (isFirstNote || prevtontaiCcuoi)) { C = "- " + C; }
                if (!isFirstNote) vow += " ";
                bool hasVCP = isFirstNote ? _C : !NoVCP;
                string prefixVCP = isFirstNote ? "- " : vow;

                if (tontaiCcuoi) { // có C cuối (at, ac,...)
                    if (hasVCP) {
                        AddPhoneme(phonemes, $"{prefixVCP}{Cw}", VCP);
                        AddPhoneme(phonemes, $"{C}{V1}");
                        AddPhoneme(phonemes, $"{V1_1}{V2_2}", ViTri);
                    } else {
                        AddPhoneme(phonemes, $"{C}{V1}");
                        AddPhoneme(phonemes, $"{V1_1}{V2_2}", ViTri);
                    }
                } else if (kAn) {
                    if (NoNext) {
                        if (hasVCP) {
                            AddPhoneme(phonemes, $"{prefixVCP}k", VCP);
                        }
                        AddPhoneme(phonemes, $"kAn");
                        AddPhoneme(phonemes, $"n -", End);
                    } else {
                        if (hasVCP) {
                            AddPhoneme(phonemes, $"{prefixVCP}k", VCP);
                        }
                        AddPhoneme(phonemes, $"kAn");
                    }
                } else if (NoNext) { // không có nốt kế tiếp
                    if (hasVCP) {
                        AddPhoneme(phonemes, $"{prefixVCP}{Cw}", VCP);
                    }
                    if (VV_) {
                        AddPhoneme(phonemes, $"{C}{V1}");
                        AddPhoneme(phonemes, $"{V1_1}{V2} -", End);
                    } else if (wV) {
                        AddPhoneme(phonemes, $"{C}{V1}{V2_2}");
                        AddPhoneme(phonemes, $"{V2} -", End);
                    } else { // bình thường
                        AddPhoneme(phonemes, $"{C}{V1}");
                        AddPhoneme(phonemes, $"{V1_1}{V2_2}", ViTri);
                        AddPhoneme(phonemes, $"{N} -", End);
                    }
                } else { // có nốt kế tiếp
                    if (hasVCP) {
                        AddPhoneme(phonemes, $"{prefixVCP}{Cw}", VCP);
                    }
                    if (VV_) {
                        AddPhoneme(phonemes, $"{C}{V1}");
                        AddPhoneme(phonemes, $"{V1_1}{V2_2}", ViTri);
                    } else if (wV) {
                        AddPhoneme(phonemes, $"{C}{V1}{V2_2}");
                    } else { // bths
                        AddPhoneme(phonemes, $"{C}{V1}");
                        AddPhoneme(phonemes, $"{V1_1}{V2_2}", ViTri);
                    }
                }
                // 3 âm VVV/VVC chia 2 nốt, ví dụ: "yên" "ướt"
            } else if (dem == 3 && !tontaiC && !fry) {
                // 3 âm VVV/VVC chia 2 nốt, ví dụ: "yên" "ướt"
                string V1 = loi.Substring(0, 1);
                string VVC = loi.Substring(0);
                string C = loi.Substring(2);
                V1 = NormalizePhonetics(V1);
                VVC = NormalizePhonetics(VVC);
                C = NormalizePhonetics(C);
                string prefix = isFirstNote ? "" : "- ";
                if (NoNext && tontaiCcuoi) {
                    AddPhoneme(phonemes, $"{prefix}{V1}");
                    AddPhoneme(phonemes, $"{VVC}", ViTri);
                } else if (NoNext) {
                    AddPhoneme(phonemes, $"{prefix}{V1}");
                    AddPhoneme(phonemes, $"{VVC}", ViTri);
                    AddPhoneme(phonemes, $"{C} -", End);
                } else {
                    AddPhoneme(phonemes, $"{prefix}{V1}");
                    AddPhoneme(phonemes, $"{VVC}", ViTri);
                }
            } else if (isFirstNote) {
                // 4 âm VVVC có VVC liền, chia 3 nốt, ví dụ "uyết" "uyên"
                if (!fry) {
                    string V1 = loi.Substring(0, 1);
                    string V2 = loi.Substring(1, 1);
                    string VVC = loi.Substring(1);
                    string C = loi.Substring(3);
                    if (V1 == "u") V1 = "w";
                    V1 = NormalizePhonetics(V1);
                    V2 = NormalizePhonetics(V2);
                    VVC = NormalizePhonetics(VVC);
                    C = NormalizePhonetics(C);

                    bool hasVCP = false;
                    bool hasPrefixVCP = false;
                    if (!isFirstNote) TinhToanAmChuyenTiep(note, loi, tontaiCcuoi, prevtontaiCcuoi, out hasVCP, out hasPrefixVCP);

                    string prefix = isFirstNote ? "- " : vow;
                    string prefixVCP = vow;
                    if (hasPrefixVCP) { prefixVCP = "- "; }
                    if (hasVCP) { prefix = vow; } // For subsequent notes with VCP, vow is usually "."

                    if (hasVCP) {
                        if (tontaiCcuoi) {
                            AddPhoneme(phonemes, $"{prefix}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                        } else if (NoNext) {
                            AddPhoneme(phonemes, $"{prefix}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                            AddPhoneme(phonemes, $"{C} -", End);
                        } else {
                            AddPhoneme(phonemes, $"{prefix}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                        }
                    } else {
                        if (NoNext && tontaiCcuoi) {
                            if (!isFirstNote) AddPhoneme(phonemes, prefixVCP, VCP);
                            AddPhoneme(phonemes, isFirstNote ? $"- {V1}{V2}" : $"{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                        } else if (NoNext) {
                            if (!isFirstNote) AddPhoneme(phonemes, prefixVCP, VCP);
                            AddPhoneme(phonemes, isFirstNote ? $"- {V1}{V2}" : $"{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                            AddPhoneme(phonemes, $"{C} -", End);
                        } else {
                            if (!isFirstNote) AddPhoneme(phonemes, prefixVCP, VCP);
                            AddPhoneme(phonemes, isFirstNote ? $"- {V1}{V2}" : $"{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                        }
                    }
                }
                // 4 âm CVVC/CVVV, chia 3 nốt, ví dụ "thoát" "toan" "toại"
                if (!tontaiVVC && !fry) {
                    string C = loi.Substring(0, 1);
                    string Cw = C;
                    string V1 = loi.Substring(1, 1);
                    string V2 = loi.Substring(2, 1);
                    string V2_2 = V2;
                    string VC = loi.Substring(2);
                    string N = loi.Substring(3);
                    string N_ = N;
                    a = IsToneShift(loi);
                    if (a && note.lyric != "qua") {
                        N = "@";
                        N_ = "@";
                    }
                    if (V1 == "u") V1 = "w";
                    if (wV && _Cw) {
                        Cw = C + "w";
                        V1 = "w";
                    } else if (wV)
                        V1 = "w";
                    if (V1 == "i")
                        Cw = C + "y";
                    if (V2 == "ă") V2_2 = "ae";
                    if (!isFirstNote && V2 == "â") V2 = "@"; // From else branch
                    C = NormalizePhonetics(C);
                    Cw = NormalizePhonetics(Cw);
                    V1 = NormalizePhonetics(V1);
                    V2_2 = NormalizePhonetics(V2_2); // Ensure V2_2 normalization exists (from else branch)
                    V2 = NormalizePhonetics(V2);
                    VC = NormalizePhonetics(VC);
                    N = NormalizePhonetics(N);
                    N_ = NormalizePhonetics(N_);

                    bool hasVCP = false;
                    bool hasPrefixVCP = false;
                    string prefixVCP = vow;
                    if (!isFirstNote) TinhToanAmChuyenTiep(note, loi, tontaiCcuoi, prevtontaiCcuoi, out hasVCP, out hasPrefixVCP);
                    if (hasPrefixVCP) prefixVCP = "- ";

                    if (_CV && isFirstNote) { C = "- " + C; } else if (_CV && !isFirstNote && prevtontaiCcuoi) { N = "- " + N; } // Specific to else branch for N prefixing

                    // For NoVCP logic in else branch
                    bool noVCP = !hasVCP && !isFirstNote;

                    if (!isFirstNote && noVCP) {
                        if (tontaiCcuoi) { // có C ngắt
                            AddPhoneme(phonemes, $"{C}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VC}", ViTri);
                        } else if (note.lyric.EndsWith("uân") || note.lyric.EndsWith("uâng")) {
                            if (wAn == false) {
                                if (NoNext) {
                                    if (loi.StartsWith(".")) {
                                        AddPhoneme(phonemes, $"{C}w@");
                                        AddPhoneme(phonemes, $"An", ViTri);
                                        AddPhoneme(phonemes, $"n -", End);
                                    } else {
                                        AddPhoneme(phonemes, $"{C}wAn");
                                        AddPhoneme(phonemes, $"n -", End);
                                    }
                                } else {
                                    if (loi.StartsWith(".")) {
                                        AddPhoneme(phonemes, $"{C}w@");
                                        AddPhoneme(phonemes, $"An", ViTri);
                                    } else {
                                        AddPhoneme(phonemes, $"{C}wAn");
                                    }
                                }
                            } else { // khuân luân
                                if (NoNext) {
                                    AddPhoneme(phonemes, $"{C}w");
                                    AddPhoneme(phonemes, $"w@", Long);
                                    AddPhoneme(phonemes, $"An", Medium);
                                    AddPhoneme(phonemes, $"n -", End);
                                } else {
                                    AddPhoneme(phonemes, $"{C}w");
                                    AddPhoneme(phonemes, $"w@", Long);
                                    AddPhoneme(phonemes, $"An", Medium);
                                }
                            }
                        } else if (NoNext) {
                            if (VV_) {
                                AddPhoneme(phonemes, $"{C}{V1}{V2}");
                                AddPhoneme(phonemes, $"{V2}{N} -", End);
                            } else { // ko có VV -
                                AddPhoneme(phonemes, $"{C}{V1}{V2}");
                                AddPhoneme(phonemes, $"{V2_2}{N}", ViTri);
                                AddPhoneme(phonemes, $"{N} -", End);
                            }
                        } else {
                            if (VV_) {
                                AddPhoneme(phonemes, $"{V2}{N}", ViTri);
                            } else { // ko có VV -
                                AddPhoneme(phonemes, $"{C}{V1}{V2}");
                                AddPhoneme(phonemes, $"{V2_2}{N}", ViTri);
                            }
                        }
                    } else { // isFirstNote OR (!isFirstNote && hasVCP)
                        if (tontaiCcuoi) { // có C ngắt
                            if (_C) {
                                AddPhoneme(phonemes, isFirstNote ? $"- {Cw}" : prefixVCP, VCP);
                                AddPhoneme(phonemes, $"{C}{V1}{V2}");
                                AddPhoneme(phonemes, $"{VC}", ViTri);
                            } else {
                                AddPhoneme(phonemes, $"{C}{V1}{V2}");
                                AddPhoneme(phonemes, $"{VC}", ViTri);
                            }
                        } else if (note.lyric.EndsWith("uân") || note.lyric.EndsWith("uâng")) {
                            if (wAn == false) {
                                if (NoNext) {
                                    if (loi.StartsWith(".")) {
                                        if (!isFirstNote && _C) AddPhoneme(phonemes, prefixVCP, VCP);
                                        AddPhoneme(phonemes, $"{C}w@");
                                        AddPhoneme(phonemes, isFirstNote ? $"A{N}" : $"A{N}", ViTri); // Kept same for now
                                        AddPhoneme(phonemes, $"{N_} -", End);
                                    } else if (_C) {
                                        AddPhoneme(phonemes, isFirstNote ? $"- {Cw}" : prefixVCP, VCP);
                                        AddPhoneme(phonemes, isFirstNote ? $"{C}wA{N}" : $"{C}wA{N}"); // Kept same for now
                                        AddPhoneme(phonemes, $"{N_} -", End);
                                    } else {
                                        AddPhoneme(phonemes, $"{C}wA{N}");
                                        AddPhoneme(phonemes, $"{N_} -", End);
                                    }
                                } else { //
                                    if (loi.StartsWith(".")) {
                                        if (!isFirstNote && _C) AddPhoneme(phonemes, prefixVCP, VCP);
                                        AddPhoneme(phonemes, $"{C}w@");
                                        AddPhoneme(phonemes, $"A{N}", ViTri);
                                    } else if (_C) {
                                        AddPhoneme(phonemes, isFirstNote ? $"- {Cw}" : prefixVCP, VCP);
                                        AddPhoneme(phonemes, $"{C}wA{N}");
                                    } else
                                        AddPhoneme(phonemes, $"{C}wA{N}");
                                }
                            } else { // khuân luân
                                if (NoNext) {
                                    if (_C) {
                                        AddPhoneme(phonemes, isFirstNote ? $"- {Cw}" : prefixVCP, VCP);
                                        AddPhoneme(phonemes, $"{C}w");
                                        AddPhoneme(phonemes, isFirstNote ? $"_w@" : $"_w@", Long);
                                        AddPhoneme(phonemes, $"A{N}", Medium);
                                        AddPhoneme(phonemes, $"{N_} -", End);
                                    } else {
                                        AddPhoneme(phonemes, $"{C}w");
                                        AddPhoneme(phonemes, $"_w@", Long);
                                        AddPhoneme(phonemes, $"A{N}", Medium);
                                        AddPhoneme(phonemes, $"{N_} -", End);
                                    }
                                } else { //
                                    if (_C) {
                                        AddPhoneme(phonemes, isFirstNote ? $"- {Cw}" : prefixVCP, VCP);
                                        AddPhoneme(phonemes, $"{C}w");
                                        AddPhoneme(phonemes, $"_w@", Long);
                                        AddPhoneme(phonemes, $"A{N}", Medium);
                                    } else {
                                        AddPhoneme(phonemes, $"{C}w");
                                        AddPhoneme(phonemes, $"_w@", Long);
                                        AddPhoneme(phonemes, $"A{N}", Medium);
                                    }
                                }
                            }
                        } else if (NoNext) {
                            if (VV_) {
                                if (_C) {
                                    AddPhoneme(phonemes, isFirstNote ? $"- {Cw}" : prefixVCP, VCP);
                                    AddPhoneme(phonemes, $"{C}{V1}{V2}");
                                    AddPhoneme(phonemes, isFirstNote ? $"{V2}{N_} -" : $"{V2}{N_} -", End);
                                } else {
                                    AddPhoneme(phonemes, $"{C}{V1}{V2}");
                                    AddPhoneme(phonemes, isFirstNote ? $"{V2}{N_} -" : $"{V2_2}{N}", End); // else-branch NoVCP logic check handled above
                                }
                            } else { // ko có VV -
                                if (_C) {
                                    AddPhoneme(phonemes, isFirstNote ? $"- {Cw}" : prefixVCP, VCP);
                                    AddPhoneme(phonemes, $"{C}{V1}{V2}");
                                    AddPhoneme(phonemes, $"{V2_2}{N}", ViTri);
                                    AddPhoneme(phonemes, $"{N_} -", End);
                                } else {
                                    AddPhoneme(phonemes, $"{C}{V1}{V2}");
                                    AddPhoneme(phonemes, $"{V2_2}{N}", ViTri);
                                    AddPhoneme(phonemes, $"{N_} -", End);
                                }
                            }
                        } else {
                            if (VV_) {
                                if (_C) {
                                    AddPhoneme(phonemes, isFirstNote ? $"- {Cw}" : prefixVCP, VCP);
                                    AddPhoneme(phonemes, $"{C}{V1}{V2}");
                                    AddPhoneme(phonemes, $"{V2}{N}", ViTri);
                                } else {
                                    AddPhoneme(phonemes, $"{C}{V1}{V2}");
                                    AddPhoneme(phonemes, $"{V2}{N}", ViTri);
                                }
                            } else { // ko có VV -
                                if (_C) {
                                    AddPhoneme(phonemes, isFirstNote ? $"- {Cw}" : prefixVCP, VCP);
                                    AddPhoneme(phonemes, $"{C}{V1}{V2}");
                                    AddPhoneme(phonemes, $"{V2_2}{N}", ViTri);
                                } else {
                                    AddPhoneme(phonemes, $"{C}{V1}{V2}");
                                    AddPhoneme(phonemes, $"{V2_2}{N}", ViTri);
                                }
                            }
                        }
                    }
                }
                // 4 âm CVVC/CVVV, (tiên, tiết)
                if (!fry && dem == 4 && tontaiVVC && tontaiC) {
                    string C = loi.Substring(0, 1);
                    string Cw = C;
                    string V1 = loi.Substring(1, 1);
                    string VVC = loi.Substring(1);
                    string N = loi.Substring(3);
                    if (V1 == "i" && _Cw) {
                        Cw = C + "y";
                    }
                    C = NormalizePhonetics(C);
                    Cw = NormalizePhonetics(Cw);
                    V1 = NormalizePhonetics(V1);
                    VVC = NormalizePhonetics(VVC);
                    N = NormalizePhonetics(N);

                    bool hasVCP = false;
                    bool hasPrefixVCP = false;
                    string prefixVCP = vow;
                    if (!isFirstNote) TinhToanAmChuyenTiep(note, loi, tontaiCcuoi, prevtontaiCcuoi, out hasVCP, out hasPrefixVCP);
                    if (hasPrefixVCP) prefixVCP = "- ";

                    if (_CV && isFirstNote) { C = "- " + C; } else if (_CV && !isFirstNote && prevtontaiCcuoi) { C = "- " + C; } // From else branch

                    bool noVCP = !hasVCP && !isFirstNote;

                    if (!isFirstNote && noVCP) {
                        if (tontaiCcuoi) { // có C ngắt
                            AddPhoneme(phonemes, $"{C}{V1}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                        } else if (NoNext) { // ko có note kế tiếp
                            AddPhoneme(phonemes, $"{C}{V1}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                            AddPhoneme(phonemes, $"{N} -", End);
                        } else { // có note kế tiếp
                            AddPhoneme(phonemes, $"{C}{V1}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                        }
                    } else {
                        if (tontaiCcuoi) { // có C ngắt
                            if (_C) {
                                AddPhoneme(phonemes, isFirstNote ? $"- {Cw}" : (vow + " " + Cw).Replace("  ", " "), VCP); // Added vow space logic from else
                                AddPhoneme(phonemes, $"{C}{V1}");
                                AddPhoneme(phonemes, $"{VVC}", ViTri);
                            } else {
                                AddPhoneme(phonemes, $"{C}{V1}");
                                AddPhoneme(phonemes, $"{VVC}", ViTri);
                            }
                        } else if (NoNext) { // ko có note kế tiếp
                            if (_C) {
                                AddPhoneme(phonemes, isFirstNote ? $"- {Cw}" : (vow + " " + Cw).Replace("  ", " "), VCP);
                                AddPhoneme(phonemes, $"{C}{V1}");
                                AddPhoneme(phonemes, $"{VVC}", ViTri);
                                AddPhoneme(phonemes, $"{N} -", End);
                            } else {
                                AddPhoneme(phonemes, $"{C}{V1}");
                                AddPhoneme(phonemes, $"{VVC}", ViTri);
                                AddPhoneme(phonemes, $"{N} -", End);
                            }
                        } else { // có note kế tiếp
                            if (_C) {
                                AddPhoneme(phonemes, isFirstNote ? $"- {Cw}" : (vow + " " + Cw).Replace("  ", " "), VCP);
                                AddPhoneme(phonemes, $"{C}{V1}");
                                AddPhoneme(phonemes, $"{VVC}", ViTri);
                            } else {
                                AddPhoneme(phonemes, $"{C}{V1}");
                                AddPhoneme(phonemes, $"{VVC}", ViTri);
                            }
                        }
                    }
                }
                // 5 âm CVVVC, có VVC liền, chia 3 nốt, ví dụ "thuyết"
                if (!fry) {
                    string C = loi.Substring(0, 1);
                    string Cw = C;
                    string V1 = loi.Substring(1, 1);
                    string V2 = loi.Substring(2, 1);
                    string VVC = loi.Substring(2);
                    string N = loi.Substring(4);
                    if (wV && _Cw) {
                        Cw = C + "w";
                        V1 = "w";
                    } else if (wV)
                        V1 = "w";
                    if (V1 == "i")
                        Cw = C + "y";
                    C = NormalizePhonetics(C);
                    Cw = NormalizePhonetics(Cw);
                    V1 = NormalizePhonetics(V1);
                    V2 = NormalizePhonetics(V2);
                    VVC = NormalizePhonetics(VVC);
                    N = NormalizePhonetics(N);
                    if (_CV) { C = "- " + C; }
                    if (tontaiCcuoi) { // có C ngắt
                        if (_C) {
                            AddPhoneme(phonemes, $"- {Cw}", VCP);
                            AddPhoneme(phonemes, $"{C}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                        } else {
                            AddPhoneme(phonemes, $"{C}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                        }
                    } else if (NoNext) { // không có nốt kế tiếp
                        if (_C) {
                            AddPhoneme(phonemes, $"- {Cw}", VCP);
                            AddPhoneme(phonemes, $"{C}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                            AddPhoneme(phonemes, $"{N} -", End);
                        } else {
                            AddPhoneme(phonemes, $"{C}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                            AddPhoneme(phonemes, $"{N} -", End);
                        }
                    } else { // có note kế tiếp
                        if (_C) {
                            AddPhoneme(phonemes, $"- {Cw}", VCP);
                            AddPhoneme(phonemes, $"{C}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                        } else {
                            AddPhoneme(phonemes, $"{C}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                        }
                    }
                }
                if (BR) {
                    string num = loi.Substring(5);
                    if (num == "") {
                        num = "1";
                    }
                    // isFirstNote: vow == "-", không cần thêm vow trước breath
                    AddPhoneme(phonemes, $"breath{num}");
                }
                // phụ âm y
                if (note.lyric.StartsWith("y") && koVVCchia) {
                    // Tính VCP prefix cho !isFirstNote
                    bool prevHasFinalC_y = false;
                    bool noVCP_y = false;
                    string vow_y = isFirstNote ? "-" : TinhToanAmChuyenTiep(prevNeighbour!.Value, loi, H, true, out prevHasFinalC_y, out noVCP_y);
                    bool hasPrefixVCP_y = prevHasFinalC_y; // C trước → dùng "- y" prefix
                    if (dem == 2) { // ya
                        string C = note.lyric.Substring(0, 1);
                        string V = note.lyric.Substring(1, 1);
                        V = NormalizePhonetics(V);
                        if (isFirstNote || hasPrefixVCP_y) {
                            // prefix là "- y" hoặc "- y" (khi có C trước)
                            if (NoNext) {
                                AddPhoneme(phonemes, $"- {C}{V}");
                                AddPhoneme(phonemes, $"{V} -", ViTri);
                            } else {
                                AddPhoneme(phonemes, $"- {C}{V}");
                            }
                        } else {
                            // !isFirstNote, không có VCP — dùng vow
                            string vowY = vow_y + " ";
                            if (NoNext) {
                                AddPhoneme(phonemes, $"{vowY}{C}", VCP);
                                AddPhoneme(phonemes, $"{C}{V}");
                                AddPhoneme(phonemes, $"{V} -", End);
                            } else {
                                AddPhoneme(phonemes, $"{vowY}{C}", VCP);
                                AddPhoneme(phonemes, $"{C}{V}");
                            }
                        }
                    } else if (dem == 3) {
                        string C = note.lyric.Substring(0, 1);
                        string V1 = note.lyric.Substring(1, 1);
                        string V2 = note.lyric.Substring(2, 1);
                        V1 = NormalizePhonetics(V1);
                        V2 = NormalizePhonetics(V2);
                        if (wV) { V1 = "w"; }
                        a = IsToneShift(loi);
                        if (a && note.lyric != "qua") { V2 = "@"; }
                        string N = V2;
                        if (IsNgCoda(V1 + V2)) { N = "ng0"; }
                        if (V1 + V2 == "Ai") { V2 = "y"; N = "i"; }
                        if (loi.EndsWith("ay")) { V2 = "y"; N = "i"; }
                        string vowY3 = isFirstNote || hasPrefixVCP_y ? string.Empty : (vow_y + " ");
                        if (tontaiCcuoi) {
                            if (!isFirstNote && !hasPrefixVCP_y) {
                                AddPhoneme(phonemes, $"{vowY3}{C}", VCP);
                                AddPhoneme(phonemes, $"{C}{V1}");
                            } else {
                                AddPhoneme(phonemes, $"- {C}{V1}");
                            }
                            AddPhoneme(phonemes, $"{V1}{V2}", ViTri);
                        } else if (NoNext) {
                            if (VV_) {
                                if (!isFirstNote && !hasPrefixVCP_y) {
                                    AddPhoneme(phonemes, $"{vowY3}{C}", VCP);
                                    AddPhoneme(phonemes, $"{C}{V1}");
                                } else {
                                    AddPhoneme(phonemes, $"- {C}{V1}");
                                }
                                AddPhoneme(phonemes, $"{V1}{V2} -", End);
                            } else if (wV) {
                                if (!isFirstNote && !hasPrefixVCP_y) {
                                    AddPhoneme(phonemes, $"{vowY3}{C}", VCP);
                                    AddPhoneme(phonemes, $"{C}{V1}{V2}");
                                } else {
                                    AddPhoneme(phonemes, $"- {C}{V1}{V2}");
                                }
                                AddPhoneme(phonemes, $"{V2} -", End);
                            } else {
                                if (!isFirstNote && !hasPrefixVCP_y) {
                                    AddPhoneme(phonemes, $"{vowY3}{C}", VCP);
                                    AddPhoneme(phonemes, $"{C}{V1}");
                                } else {
                                    AddPhoneme(phonemes, $"- {C}{V1}");
                                }
                                AddPhoneme(phonemes, $"{V1}{V2}", ViTri);
                                AddPhoneme(phonemes, $"{V2} -", End);
                            }
                        } else { // có note kế tiếp
                            if (wV) {
                                if (!isFirstNote && !hasPrefixVCP_y) {
                                    AddPhoneme(phonemes, $"{vowY3}{C}", VCP);
                                    AddPhoneme(phonemes, $"{C}{V1}{V2}");
                                } else {
                                    AddPhoneme(phonemes, $"- {C}{V1}{V2}");
                                }
                            } else {
                                if (!isFirstNote && !hasPrefixVCP_y) {
                                    AddPhoneme(phonemes, $"{vowY3}{C}", VCP);
                                    AddPhoneme(phonemes, $"{C}{V1}");
                                } else {
                                    AddPhoneme(phonemes, $"- {C}{V1}");
                                }
                                AddPhoneme(phonemes, $"{V1}{V2}", ViTri);
                            }
                        }
                    }
                } // phụ âm y
                else { // nếu ko phải phụ âm y
                       // 2 âm VV, ví dụ: "oa"
                    if (!fry && dem == 2 && kocoC) {
                        string V1 = loi.Substring(0, 1);
                        string V1_ = V1;
                        string V2 = loi.Substring(1, 1);
                        string N = V2;
                        if (loi.StartsWith("uy")) V2 = "i";
                        if (IsN0Coda(V1 + V2)) {
                            N = "ng0";
                        }
                        if (V2 == "y")
                            N = "i";
                        if (wV) {
                            V1 = "w";
                        }
                        if (V1 == "â") {
                            V1 = "@";
                        }
                        if (V1 + V2 == "ia" || V1 + V2 == "ua" || V1 + V2 == "ưa")
                            N = "@";
                        if (V1 == "ă") {
                            V1_ = "ae";
                        }
                        V1 = NormalizePhonetics(V1);
                        V1_ = NormalizePhonetics(V1_);
                        V2 = NormalizePhonetics(V2);
                        N = NormalizePhonetics(N);
                        a = IsToneShift(loi);
                        if (a) {
                            V2 = "@";
                        }
                        if (tontaiCcuoi) {
                            AddPhoneme(phonemes, $"- {V1}");
                            AddPhoneme(phonemes, $"{V1}{V2}", ViTri);
                        } else if (NoNext) { // không có nốt kế tiếp
                            if (wV) { // oa oe uê ,...
                                AddPhoneme(phonemes, $"- {V1}{V2}");
                                AddPhoneme(phonemes, $"{N} -", End);
                            } else if (VV_) { // ai eo êu ao,...
                                AddPhoneme(phonemes, $"- {V1}");
                                AddPhoneme(phonemes, $"{V1}{N} -", End);
                            } else { // an anh
                                AddPhoneme(phonemes, $"- {V1}");
                                AddPhoneme(phonemes, $"{V1_}{V2}", ViTri);
                                AddPhoneme(phonemes, $"{N} -", End);
                            }
                        } else {  // có nốt kế tiếp
                            if (wV) { // oa oe uê ,...
                                AddPhoneme(phonemes, $"- {V1}{V2}");
                            } else if (VV_) { // ai eo êu ao,...
                                AddPhoneme(phonemes, $"- {V1}");
                                AddPhoneme(phonemes, $"{V1}{N}", ViTri);
                            } else { // an anh
                                AddPhoneme(phonemes, $"- {V1}");
                                AddPhoneme(phonemes, $"{V1_}{V2}", ViTri);
                            }
                        }
                    }
                    // 3 âm VVC/VVV, ví dụ: "oát" "oan" "oai"
                    if (!fry && dem == 3 && koVVCchia && kocoC) {
                        string V1 = loi.Substring(0, 1);
                        string V2 = loi.Substring(1, 1);
                        string V2_2 = V2;
                        string V3 = loi.Substring(2, 1);
                        a = IsToneShift(loi);
                        if (a && note.lyric != "qua") {
                            V3 = "@";
                        }
                        if (wV) {
                            V1 = "w";
                        }
                        if (V2 == "ă") {
                            V2_2 = "ae";
                        }
                        if (V2 == "â") {
                            V2 = "@";
                        }
                        V1 = NormalizePhonetics(V1);
                        V2 = NormalizePhonetics(V2);
                        V2_2 = NormalizePhonetics(V2_2);
                        V3 = NormalizePhonetics(V3);
                        string N = V3;
                        if (IsNgCoda(V2 + V3)) {
                            N = "ng0";
                        }
                        if (V3 == "y") N = "i";
                        if (tontaiCcuoi && wV) {
                            AddPhoneme(phonemes, $"- {V1}{V2}");
                            AddPhoneme(phonemes, $"{V2}{V3}", ViTri);
                        } else if (NoNext) { // không có nốt kế tiếp
                            if (wV && VV_) {
                                AddPhoneme(phonemes, $"- {V1}{V2}");
                                AddPhoneme(phonemes, $"{V2_2}{N} -", End);
                            } else if (wV) {
                                AddPhoneme(phonemes, $"- {V1}{V2}");
                                AddPhoneme(phonemes, $"{V2_2}{V3}", ViTri);
                                AddPhoneme(phonemes, $"{N} -", End);
                            }
                        } else { // có nốt kế tiếp
                            if (wV) {
                                AddPhoneme(phonemes, $"- {V1}{V2}");
                                AddPhoneme(phonemes, $"{V2_2}{V3}", ViTri);
                            }
                        }
                    }
                }
            } else {
                // 4 âm VVVC có VVC liền, chia 3 nốt, ví dụ "uyết" "uyên"
                if (!fry) {
                    string V1 = loi.Substring(0, 1);
                    string V2 = loi.Substring(1, 1);
                    string VVC = loi.Substring(1);
                    string C = loi.Substring(3);
                    if (V1 == "u") V1 = "w";
                    V1 = NormalizePhonetics(V1);
                    V2 = NormalizePhonetics(V2);
                    VVC = NormalizePhonetics(VVC);
                    C = NormalizePhonetics(C);
                    if (prevtontaiCcuoi) vow = "."; else vow += " ";
                    if (prevtontaiCcuoi) {
                        if (tontaiCcuoi) {
                            AddPhoneme(phonemes, $"{vow}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                        } else if (NoNext) {
                            AddPhoneme(phonemes, $"{vow}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                            AddPhoneme(phonemes, $"{C} -", End);
                        } else {
                            AddPhoneme(phonemes, $"{vow}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                        }
                    } else if (NoNext && tontaiCcuoi) {
                        AddPhoneme(phonemes, $"{vow}{V1}", VCP);
                        AddPhoneme(phonemes, $"{V1}{V2}");
                        AddPhoneme(phonemes, $"{VVC}", ViTri);
                    } else if (NoNext) {
                        AddPhoneme(phonemes, $"{vow}{V1}", VCP);
                        AddPhoneme(phonemes, $"{V1}{V2}");
                        AddPhoneme(phonemes, $"{VVC}", ViTri);
                        AddPhoneme(phonemes, $"{C} -", End);
                    } else {
                        AddPhoneme(phonemes, $"{vow}{V1}", VCP);
                        AddPhoneme(phonemes, $"{V1}{V2}");
                        AddPhoneme(phonemes, $"{VVC}", ViTri);
                    }
                }
                // 4 âm CVVC/CVVV, chia 3 nốt, ví dụ "thoát" "toan" "toại"
                if (!tontaiVVC && !fry) {
                    string C = loi.Substring(0, 1);
                    string Cw = C;
                    string V1 = loi.Substring(1, 1);
                    string V2 = loi.Substring(2, 1);
                    string V2_2 = V2;
                    string VC = loi.Substring(2);
                    string N = loi.Substring(3);
                    string N_ = N;
                    a = IsToneShift(loi);
                    if (a && note.lyric != "qua") {
                        N = "@";
                        N_ = "@";
                    }
                    if (V1 == "u") V1 = "w";
                    if (wV && _Cw) {
                        Cw = C + "w";
                        V1 = "w";
                    } else if (wV)
                        V1 = "w";
                    if (V1 == "i")
                        Cw = C + "y";
                    if (V2 == "ă") V2_2 = "ae";
                    if (V2 == "â") V2 = "@";
                    C = NormalizePhonetics(C);
                    Cw = NormalizePhonetics(Cw);
                    V1 = NormalizePhonetics(V1);
                    V2_2 = NormalizePhonetics(V2_2);
                    V2 = NormalizePhonetics(V2);
                    VC = NormalizePhonetics(VC);
                    N = NormalizePhonetics(N);
                    N_ = NormalizePhonetics(N_);
                    if (_CV && prevtontaiCcuoi) { N = "- " + N; }
                    vow += " ";
                    if (NoVCP) {
                        if (tontaiCcuoi) { // có C ngắt
                            AddPhoneme(phonemes, $"{C}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VC}", ViTri);
                        } else if (note.lyric.EndsWith("uân") || note.lyric.EndsWith("uâng")) {
                            if (wAn == false) {
                                if (NoNext) {
                                    if (loi.StartsWith(".")) {
                                        AddPhoneme(phonemes, $"{C}w@");
                                        AddPhoneme(phonemes, $"An", ViTri);
                                        AddPhoneme(phonemes, $"n -", End);
                                    } else
                                        AddPhoneme(phonemes, $"{C}wAn");
                                    AddPhoneme(phonemes, $"n -", End);
                                } else { //
                                    if (loi.StartsWith(".")) {
                                        AddPhoneme(phonemes, $"{C}w@");
                                        AddPhoneme(phonemes, $"An", ViTri);
                                    } else {
                                        AddPhoneme(phonemes, $"{C}wAn");
                                    }
                                }
                            } else { // khuân luân
                                if (NoNext) {
                                    AddPhoneme(phonemes, $"{C}w");
                                    AddPhoneme(phonemes, $"w@", Long);
                                    AddPhoneme(phonemes, $"An", Medium);
                                    AddPhoneme(phonemes, $"n -", End);
                                } else { //
                                    AddPhoneme(phonemes, $"{C}w");
                                    AddPhoneme(phonemes, $"w@", Long);
                                    AddPhoneme(phonemes, $"An", Medium);
                                }
                            }
                        } else if (NoNext) {
                            if (VV_) {
                                AddPhoneme(phonemes, $"{C}{V1}{V2}");
                                AddPhoneme(phonemes, $"{V2}{N} -", End);
                            } else { // ko có VV -
                                AddPhoneme(phonemes, $"{C}{V1}{V2}");
                                AddPhoneme(phonemes, $"{V2_2}{N}", ViTri);
                                AddPhoneme(phonemes, $"{N} -", End);
                            }
                        } else {
                            if (VV_) {
                                AddPhoneme(phonemes, $"{C}{V1}{V2}");
                                AddPhoneme(phonemes, $"{V2}{N}", ViTri);
                            } else { // ko có VV -
                                AddPhoneme(phonemes, $"{C}{V1}{V2}");
                                AddPhoneme(phonemes, $"{V2_2}{N}", ViTri);
                            }
                        }
                    } else {
                        if (tontaiCcuoi) { // có C ngắt
                            AddPhoneme(phonemes, $"{vow}{Cw}", VCP);
                            AddPhoneme(phonemes, $"{C}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VC}", ViTri);
                        } else {
                            // Removed redundant !isFirstNote logic for uân/uâng and general VVC
                        }
                    }
                }

                // 4 âm CVVC/CVVV, (tiên, tiết)
                // 5 âm CVVVC, có VVC liền, chia 3 nốt, ví dụ "thuyết"
                if (!fry && dem == 5 && tontaiVVC && tontaiC) {
                    string C = loi.Substring(0, 1);
                    string Cw = C;
                    string V1 = loi.Substring(1, 1);
                    string V2 = loi.Substring(2, 1);
                    string VVC = loi.Substring(2);
                    string N = loi.Substring(4);
                    if (wV && _Cw) {
                        Cw = C + "w";
                        V1 = "w";
                    } else if (wV)
                        V1 = "w";
                    if (V1 == "i")
                        Cw = C + "y";
                    C = NormalizePhonetics(C);
                    Cw = NormalizePhonetics(Cw);
                    V1 = NormalizePhonetics(V1);
                    V2 = NormalizePhonetics(V2);
                    VVC = NormalizePhonetics(VVC);
                    N = NormalizePhonetics(N);

                    bool _hasVCP = false;
                    bool _hasPrefixVCP = false;
                    string prefixVCP = vow;
                    if (!isFirstNote) TinhToanAmChuyenTiep(note, loi, tontaiCcuoi, prevtontaiCcuoi, out _hasVCP, out _hasPrefixVCP);
                    if (_hasPrefixVCP) prefixVCP = "- ";

                    if (_CV && isFirstNote) { C = "- " + C; } else if (_CV && !isFirstNote && prevtontaiCcuoi) { N = "- " + N; } // Specific to !isFirstNote logic for CVVVC

                    bool noVCP = !_hasVCP && !isFirstNote;

                    if (!isFirstNote && noVCP) {
                        if (tontaiCcuoi) { // có C ngắt
                            AddPhoneme(phonemes, $"{C}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                        } else if (NoNext) { // ko có note kế tiếp
                            AddPhoneme(phonemes, $"{C}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                            AddPhoneme(phonemes, $"{N} -", End);
                        } else { // có note kế tiếp
                            AddPhoneme(phonemes, $"{C}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                        }
                    } else {
                        if (tontaiCcuoi) { // có C ngắt
                            AddPhoneme(phonemes, isFirstNote ? $"- {Cw}" : (vow + " " + Cw).Replace("  ", " "), VCP); // Added vow logic from else
                            AddPhoneme(phonemes, $"{C}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                        } else if (NoNext) { // ko có note kế tiếp
                            AddPhoneme(phonemes, isFirstNote ? $"- {Cw}" : (vow + " " + Cw).Replace("  ", " "), VCP);
                            AddPhoneme(phonemes, $"{C}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                            AddPhoneme(phonemes, $"{N} -", End);
                        } else { // có note kế tiếp
                            AddPhoneme(phonemes, isFirstNote ? $"- {Cw}" : (vow + " " + Cw).Replace("  ", " "), VCP);
                            AddPhoneme(phonemes, $"{C}{V1}{V2}");
                            AddPhoneme(phonemes, $"{VVC}", ViTri);
                        }
                    }
                } // end CVVC/CVVVC thuyết block
                  // Xử lý VV, VVC, VVVC (không đi vào phụ âm y)
                  // nếu ko phải phụ âm y (trong !isFirstNote)
                  // 2 âm VV, ví dụ: "oa"
                if (!fry && dem == 2 && kocoC) {
                    string V1 = loi.Substring(0, 1);
                    string V1_ = V1;
                    string V2 = loi.Substring(1, 1);
                    string N = V2;
                    if (loi.StartsWith("uy")) V2 = "i";
                    if (IsN0Coda(V1 + V2)) {
                        N = "ng0";
                    }
                    if (V2 == "y")
                        N = "i";
                    if (wV) {
                        V1 = "w";
                    }
                    if (V1 == "â") {
                        V1 = "@";
                    }
                    if (V1 + V2 == "ia" || V1 + V2 == "ua" || V1 + V2 == "ưa")
                        N = "@";
                    if (V1 == "ă") {
                        V1_ = "ae";
                    }
                    V1 = NormalizePhonetics(V1);
                    V1_ = NormalizePhonetics(V1_);
                    V2 = NormalizePhonetics(V2);
                    N = NormalizePhonetics(N);
                    a = IsToneShift(loi);
                    if (a) {
                        V2 = "@";
                    }
                    if (prevtontaiCcuoi) vow = "."; else vow += " ";
                    if (prevtontaiCcuoi) {
                        if (tontaiCcuoi) {
                            AddPhoneme(phonemes, $"{vow}{V1}");
                            AddPhoneme(phonemes, $"{V1}{V2}", ViTri);
                        } else if (NoNext) { // không có nốt kế tiếp
                            if (wV) { // oa oe uê ,...
                                AddPhoneme(phonemes, $"{vow}{V1}{V2}");
                                AddPhoneme(phonemes, $"{N} -", End);
                            } else if (VV_) { // ai eo êu ao,...
                                AddPhoneme(phonemes, $"{vow}{V1}");
                                AddPhoneme(phonemes, $"{V1}{N} -", End);
                            } else { // an anh
                                AddPhoneme(phonemes, $"{vow}{V1}");
                                AddPhoneme(phonemes, $"{V1_}{V2}", ViTri);
                                AddPhoneme(phonemes, $"{N} -", End);
                            }
                        } else {  // có nốt kế tiếp
                            if (wV) { // oa oe uê ,...
                                AddPhoneme(phonemes, $"{vow}{V1}{V2}");
                            } else if (VV_) { // ai eo êu ao,...
                                AddPhoneme(phonemes, $"{vow}{V1}");
                                AddPhoneme(phonemes, $"{V1}{N}", ViTri);
                            } else { // an anh
                                AddPhoneme(phonemes, $"{vow}{V1}");
                                AddPhoneme(phonemes, $"{V1_}{V2}", ViTri);
                            }
                        }
                    } else if (tontaiCcuoi) {
                        AddPhoneme(phonemes, $"{vow}{V1}");
                        AddPhoneme(phonemes, $"{V1}{V2}", ViTri);
                    } else if (NoNext) { // không có nốt kế tiếp
                        if (wV) { // oa oe uê ,...
                            AddPhoneme(phonemes, $"{vow}{V1}", VCP);
                            AddPhoneme(phonemes, $"{V1}{V2}");
                            AddPhoneme(phonemes, $"{N} -", End);
                        } else if (VV_) { // ai eo êu ao,...
                            AddPhoneme(phonemes, $"{vow}{V1}");
                            AddPhoneme(phonemes, $"{V1}{N} -", End);
                        } else { // an anh
                            AddPhoneme(phonemes, $"{vow}{V1}");
                            AddPhoneme(phonemes, $"{V1_}{V2}", ViTri);
                            AddPhoneme(phonemes, $"{N} -", End);
                        }
                    } else {  // có nốt kế tiếp
                        if (wV) { // oa oe uê ,...
                            AddPhoneme(phonemes, $"{vow}{V1}", VCP);
                            AddPhoneme(phonemes, $"{V1}{V2}");
                        } else if (VV_) { // ai eo êu ao,...
                            AddPhoneme(phonemes, $"{vow}{V1}");
                            AddPhoneme(phonemes, $"{V1}{N}", ViTri);
                        } else { // an anh
                            AddPhoneme(phonemes, $"{vow}{V1}");
                            AddPhoneme(phonemes, $"{V1_}{V2}", ViTri);
                        }
                    }
                }
                // 3 âm VVC/VVV, ví dụ: "oát" "oan" "oai" "uân"
                if (!fry && dem == 3 && koVVCchia && kocoC) {
                    string V1 = loi.Substring(0, 1);
                    string V2 = loi.Substring(1, 1);
                    string V2_2 = V2;
                    string V3 = loi.Substring(2, 1);
                    a = IsToneShift(loi);
                    if (a) {
                        V3 = "@";
                    }
                    if (wV) {
                        V1 = "w";
                    }
                    if (V2 == "ă") {
                        V2_2 = "ae";
                    }
                    if (V2 == "â") {
                        V2 = "@";
                    }
                    V1 = NormalizePhonetics(V1);
                    V2 = NormalizePhonetics(V2);
                    V2_2 = NormalizePhonetics(V2_2);

                    V3 = NormalizePhonetics(V3);
                    string N = V3;
                    if (V3 == "y") N = "i";
                    if (IsNgCoda(V2 + V3)) {
                        N = "ng0";
                    }
                    if (prevtontaiCcuoi) vow = "."; else vow += " ";
                    if (prevtontaiCcuoi) {
                        if (NoNext) { // không có nốt kế tiếp
                            if (VV_) {
                                AddPhoneme(phonemes, $"{vow}{V1}{V2}");
                                AddPhoneme(phonemes, $"{V2_2}{N} -", End);
                            } else {
                                AddPhoneme(phonemes, $"{vow}{V1}{V2}");
                                AddPhoneme(phonemes, $"{V2_2}{V3}", ViTri);
                                AddPhoneme(phonemes, $"{N} -", End);
                            }
                        } else { // có nốt kế tiếp
                            if (VV_) {
                                AddPhoneme(phonemes, $"{vow}{V1}{V2}");
                                AddPhoneme(phonemes, $"{V2_2}{V3}", ViTri);
                            } else {
                                AddPhoneme(phonemes, $"{vow}{V1}{V2}");
                                AddPhoneme(phonemes, $"{V2_2}{V3}", ViTri);
                            }
                        }
                    } else {
                        if (NoNext) { // không có nốt kế tiếp
                            if (wV && VV_) {
                                AddPhoneme(phonemes, $"{vow}{V1}", VCP);
                                AddPhoneme(phonemes, $"{V1}{V2}");
                                AddPhoneme(phonemes, $"{V2_2}{N} -", End);
                            } else if (wV) {
                                AddPhoneme(phonemes, $"{vow}{V1}", VCP);
                                AddPhoneme(phonemes, $"{V1}{V2}");
                                AddPhoneme(phonemes, $"{V2_2}{V3}", ViTri);
                                AddPhoneme(phonemes, $"{N} -", End);
                            }
                        } else { // có nốt kế tiếp
                            if (wV && VV_) {
                                AddPhoneme(phonemes, $"{vow}{V1}", VCP);
                                AddPhoneme(phonemes, $"{V1}{V2}");
                                AddPhoneme(phonemes, $"{V2_2}{V3}", ViTri);
                            } else if (wV) {
                                AddPhoneme(phonemes, $"{vow}{V1}", VCP);
                                AddPhoneme(phonemes, $"{V1}{V2}");
                                AddPhoneme(phonemes, $"{V2_2}{V3}", ViTri);
                            }
                        }
                    }
                }
                // 3 âm VVV/VVC chia 2 nốt, ví dụ: "yên" "ướt"
                if ((dem == 3) && tontaiVVC && kocoC) {
                    string V1 = loi.Substring(0, 1);
                    string VVC = loi.Substring(0);
                    string C = loi.Substring(2);
                    V1 = NormalizePhonetics(V1);
                    VVC = NormalizePhonetics(VVC);
                    C = NormalizePhonetics(C);
                    if (prevtontaiCcuoi) vow = "."; else vow += " ";
                    if (NoNext && tontaiCcuoi) {
                        AddPhoneme(phonemes, $"{vow}{V1}");
                        AddPhoneme(phonemes, $"{VVC}", ViTri);
                    } else if (NoNext) {
                        AddPhoneme(phonemes, $"{vow}{V1}");
                        AddPhoneme(phonemes, $"{VVC}", ViTri);
                        AddPhoneme(phonemes, $"{C} -", End);
                    } else {
                        AddPhoneme(phonemes, $"{vow}{V1}");
                        AddPhoneme(phonemes, $"{VVC}", ViTri);
                    }
                }
            }
            // BR
            if (BR) {
                string num = loi.Substring(5);
                if (num == "") {
                    num = "1";
                }
                if (vow == "-") {
                    AddPhoneme(phonemes, $"breath{num}");
                } else {
                    AddPhoneme(phonemes, $"{vow} -", -60);
                    AddPhoneme(phonemes, $"breath{num}");
                }
            }

            // OTO mapping và return
            int noteIndex = 0;
            for (int i = 0; i < phonemes.Count; i++) {
                var attr = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == i) ?? default;
                string alt = attr.alternate?.ToString() ?? string.Empty;
                string color = attr.voiceColor;
                int toneShift = attr.toneShift;
                var phoneme1 = phonemes[i];
                while (noteIndex < notes.Length - 1 && notes[noteIndex].position - note.position < phoneme1.position) {
                    noteIndex++;
                }
                int tone = (i == 0 && prevNeighbours != null && prevNeighbours.Length > 0)
                    ? prevNeighbours.Last().tone : notes[noteIndex].tone;
                if (singer.TryGetMappedOto($"{phoneme1.phoneme}{alt}", note.tone + toneShift, color, out var oto)) {
                    phoneme1.phoneme = oto.Alias;
                }
                phonemes[i] = phoneme1;
            }
            return new Result { phonemes = [.. phonemes] };
        }
    }
}
