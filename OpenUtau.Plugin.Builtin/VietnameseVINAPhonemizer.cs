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
        static readonly string[] vowels = new string[] {
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
        };

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
            bool isToneShift = loi.EndsWith("ia") || loi.EndsWith("ua") || loi.EndsWith("ưa") || loi.EndsWith("ya");
            if (isToneShift && !loi.Contains("qua")) {
                v2 = "@";
                v2_2 = "@";
                n = "@";
            }

            // Special Coda mappings
            if (loi.EndsWith("Ong") || loi.EndsWith("ung") || loi.EndsWith("ong")) {
                n = "ng0";
            }
            if (loi.EndsWith("Ai") || loi.EndsWith("ay")) {
                v2 = "y";
                n = "i";
            }
        }

        private string GetTransitionContext(Note prevNote, string currentLoi, bool isH, bool startsWithC, out bool prevHasFinalC, out bool noVCP) {
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
            PR = PR.Replace("ch", "C").Replace("d", "z").Replace("đ", "d").Replace("ph", "f")
                   .Replace("gi", "z").Replace("gh", "g").Replace("c", "k").Replace("kh", "K").Replace("ng", "N")
                   .Replace("ngh", "N").Replace("nh", "J").Replace("x", "s").Replace("tr", "Z").Replace("th", "T")
                   .Replace("qu", "w");

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
                if (PR.EndsWith("t")) vow = "t";
                if (PR.EndsWith("C")) vow = "ch";
                if (PR.EndsWith("p")) vow = "p";
                if (PR.EndsWith("k")) vow = "k";
                if (PR.EndsWith("'")) vow = "-";
            }

            string b1 = PR.Length > 0 ? PR.Substring(PR.Length - 1) : "";
            string b2 = currentLoi.Length > 0 ? currentLoi.Substring(0, 1) : "";
            bool mSame = (b1 == b2) && vow != "ng0";
            noVCP = (isH && prevHasFinalC) || mSame;

            return vow;
        }

        static readonly string[] VVC_LIST = { "iên", "iêN", "iêm", "iêt", "iêk", "iêp", "iêu", "yên", "yêN", "yêm", "yêt", "yêk", "yêp", "yêu", "uôn", "uôN", "uôm", "uôt", "uôk", "uôi", "ươn", "ươN", "ươm", "ươt", "ươk", "ươp", "ươi", "ươu" };
        static readonly string[] CCUOI_ENDS = { "k", "t", "C", "p", "." };
        static readonly string[] C_STARTS = { "b", "C", "d", "f", "g", "h", "k", "K", "l", "m", "n", "N", "J", "r", "s", "t", "T", "Z", "v", "w", "z", "p", "'", "." };
        static readonly string[] VV_ENDS = { "ai", "ơi", "oi", "ôi", "ui", "ưi", "ao", "eo", "êu", "iu", "an", "ơn", "in", "en", "ên", "on", "ôn", "un", "ưn", "am", "ơm", "im", "em", "êm", "om", "ôm", "um", "ưm", "aN", "ơN", "iN", "eN", "êN", "ưN", "aJ", "iJ", "êJ", "at", "ơt", "it", "et", "êt", "ot", "ôt", "ut", "ưt", "aC", "iC", "êC", "ak", "ơk", "ik", "ek", "êk", "ok", "ôk", "uk", "ưk", "ap", "ơp", "ip", "ep", "êp", "op", "ôp", "up", "ưp", "ia", "ua", "ưa", "ay", "ây", "uy", "au", "âu", "oa", "oe", "uê" };
        static readonly string[] VITRINGAN_ENDS = { "ai", "ơi", "oi", "ôi", "ui", "ưi", "ao", "eo", "êu", "iu", "an", "ơn", "in", "en", "ên", "on", "ôn", "un", "ưn", "am", "ơm", "im", "em", "êm", "om", "ôm", "um", "ưm", "aN", "ơN", "iN", "eN", "êN", "ưN", "at", "ơt", "it", "et", "êt", "ot", "ôt", "ut", "ưt", "ak", "ơk", "ik", "ek", "êk", "ok", "ôk", "uk", "ưk", "ap", "ơp", "ip", "ep", "êp", "op", "ôp", "up", "ưp", "ia", "ua", "ưa", "uôN", "yt", "yn", "ym", "yC", "yp", "yk", "yN" };
        static readonly string[] VITRIDAI_ENDS = { "uy", "au", "âu", "oa", "oe", "uê" };
        static readonly string[] VITRITB_CONTAINS = { "ăt", "ât", "ăk", "âk", "ăp", "âp", "ăn", "ân", "ăN", "âN", "ăm", "âm", "aJ", "iJ", "êJ", "yJ", "ôN", "uN", "oN", "aC", "iC", "êC", "yC" };
        static readonly string[] VITRITB_ENDS = { "oay", "uây", "ay", "ây", "oay'", "uây'", "ay'", "ây'" };
        static readonly string[] _C_STARTS = { "f", "K", "l", "m", "n", "J", "N", "s", "v", "z" };
        static readonly string[] _CW_STARTS = { "Ku", "Koa", "Koe", "Koă", "su", "soa", "soe", "soă", "zu", "zoa", "zoe", "zoă", "Ky", "Ki" };
        static readonly string[] _CV_STARTS = { "g", "h", "'", "w", "y" };
        static readonly string[] WV_CONTAINS = { "oa", "oe", "uâ", "uê", "uy", "uơ", "oă", "wa" };
        static readonly string[] VV_UNDERSCORE_ENDS = { "ai", "eo", "ua", "ưa", "ơi", "oi", "ôi", "ui", "ưi", "ya", "êu", "ưu", "ao", "ia", "iu", "ai'", "eo'", "ua'", "ưa'", "ơi'", "oi'", "ôi'", "ui'", "ưi'", "ya'", "êu'", "ưu'", "ao'", "ia'", "iu'" };
        static readonly string[] WAN_STARTS = { "K", "z" };
        static readonly string[] H_STARTS = { "b", "d", "k", "l", "t", "T", "C", "m", "n", "J", "N", "h", "g", "." };
        static readonly string[] VCP70_STARTS = { "b", "d", "g", "k", "l", "m", "n", "nh", "ng", "t", "th", "v", "w", "y" };


        private USinger singer;

        public override void SetSinger(USinger singer) => this.singer = singer;
        // Legacy mapping. Might adjust later to new mapping style.
        public override bool LegacyMapping => true;

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            var note = notes[0];
            if (!string.IsNullOrEmpty(note.phoneticHint)) {
                return MakeSimpleResult(note.phoneticHint);
            }
            int totalDuration = notes.Sum(n => n.duration);
            int Short = 0;
            int Long = 0;
            int Medium = 0;
            int VCP = 0;
            int End = 0;
            int ViTri = 0;
            if (totalDuration < 350) {
                Short = totalDuration * 4 / 7;
                Long = totalDuration / 6;
                Medium = totalDuration / 3;
                VCP = -90;
                End = totalDuration * 4 / 5;
                ViTri = Short;
            } else {
                Short = totalDuration - 170;
                Long = 90;
                Medium = 180;
                VCP = -90;
                End = totalDuration - 50;
                ViTri = Short;
            }
            var phonemes = new List<Phoneme>();
            bool a = false, BR = false;

            if (note.lyric.StartsWith("?")) {
                phonemes.Add(new Phoneme { phoneme = note.lyric.Substring(1) });
                goto MapOtoAndReturn;
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
                loi = loi.Replace("ch", "C").Replace("d", "z").Replace("đ", "d").Replace("ph", "f")
                         .Replace("gi", "z").Replace("gh", "g").Replace("c", "k").Replace("kh", "K").Replace("ng", "N")
                         .Replace("nh", "J").Replace("tr", "Z").Replace("th", "T").Replace("qu", "kw").Replace("q", "k");
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
            if (prevNeighbour == null) {
                {
                    // 1 âm
                    if (dem == 1) {
                        string N = loi.Substring(0, 1);
                        N = N.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                 .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                                 .Replace("Z", "tr").Replace("T", "th");
                        if (NoNext) {
                            phonemes.Add(
                            new Phoneme { phoneme = $"- {N}" }
                                );
                            phonemes.Add(
                            new Phoneme { phoneme = $"{N} -", position = End }
                            );
                        } else
                            phonemes.Add(
                            new Phoneme { phoneme = $"- {N}" }
                                );
                    }
                    // 2 âm CV, ví dụ: "ba"
                    if ((dem == 2) && tontaiC) {
                        string N = loi;
                        string N1 = loi.Substring(0, 1);
                        string N2 = loi.Substring(1, 1);
                        N1 = N1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                 .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                                 .Replace("Z", "tr").Replace("T", "th");
                        if (_Cw) {
                            if (N2 == "u")
                                N1 = N1 + "w";
                            if ((N2 == "i") || (N2 == "y"))
                                N1 = N1 + "y";
                        }
                        N = N.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                 .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                                 .Replace("Z", "tr").Replace("T", "th");
                        N2 = N2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                 .Replace("ư", "U");
                        if (_CV) { N = "- " + N; }
                        if (NoNext) { // ko co note ke tiep
                            if (_C) { // co - C
                                phonemes.Add(
                            new Phoneme { phoneme = $"- {N1}", position = VCP }
                                );
                                phonemes.Add(
                            new Phoneme { phoneme = $"{N}" }
                                );
                                phonemes.Add(
                            new Phoneme { phoneme = $"{N2} -", position = End }
                                );
                            } else // ko co gi
                                {
                                phonemes.Add(
                            new Phoneme { phoneme = $"{N}" }
                            );
                                phonemes.Add(
                                new Phoneme { phoneme = $"{N2} -", position = End }
                                );
                            }
                        } else // co note ke tiep
                            if (_C) { // co - C
                                phonemes.Add(
                                new Phoneme { phoneme = $"- {N1}", position = VCP }
                                );
                                phonemes.Add(
                                new Phoneme { phoneme = $"{N}" }
                                );
                            } else // ko co gi
                                phonemes.Add(
                                new Phoneme { phoneme = $"{N}" }
                                );
                    }
                }
                // 3 âm CVV/CVC, ví dụ: "hoa" "hang" "hát"
                if (fry) { } else
                    if (dem == 3 && tontaiC) {
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
                        } else
                            if (wV) {
                                V1 = "w";
                            } else if (_Cw) Cw = C + "w";
                        if (V1 == "i" && _Cw) Cw = C + "y";
                        Cw = Cw.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                        C = C.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                        V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                        V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U")
                                    .Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                        V2_2 = V2_2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U")
                                    .Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                        V1_1 = V1_1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U")
                                    .Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                        a = (loi.EndsWith("ia") || loi.EndsWith("ua") || loi.EndsWith("ưa") || loi.EndsWith("ya"));
                        if (a && note.lyric != "qua") {
                            V2 = "@";
                            V2_2 = "@";
                        }
                        string N = V2;
                        if (V1 + V2 == "Ong" || V1 + V2 == "ung" || V1 + V2 == "ong") {
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
                        if (_CV) { C = "- " + C; }
                        if (tontaiCcuoi) { // co C cuoi (at, ac,...)
                            if (_C) { // co - C
                                phonemes.Add(
                                new Phoneme { phoneme = $"- {Cw}", position = VCP }
                                );
                                phonemes.Add(
                                new Phoneme { phoneme = $"{C}{V1}" }
                                );
                                phonemes.Add(
                                new Phoneme { phoneme = $"{V1_1}{V2_2}", position = ViTri }
                                );
                            } else // bths
                                {
                                phonemes.Add(
                                new Phoneme { phoneme = $"{C}{V1}" }
                                );
                                phonemes.Add(
                                    new Phoneme { phoneme = $"{V1_1}{V2_2}", position = ViTri }
                                    );
                            }
                        } else
                            if (kAn) {
                                if (NoNext) {
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"kAn" }
                                    );
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"n -", position = End }
                                    );
                                } else
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"kAn" }
                                    );
                            } else
                                if (NoNext) { // ko co note ke tiep
                                    if (_C) { // co - C
                                        if (VV_) {
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"- {Cw}", position = VCP }
                                        );
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{C}{V1}" }
                                        );
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{V1_1}{V2} -", position = End }
                                        );
                                        } else if (wV) {
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"- {Cw}", position = VCP }
                                        );
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{C}{V1}{V2_2}" }
                                        );
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{V2} -", position = End }
                                        );
                                        } else { // bths
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"- {Cw}", position = VCP }
                                        );
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{C}{V1}" }
                                        );
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{V1_1}{V2_2}", position = ViTri }
                                        );
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{N} -", position = End }
                                        );
                                        }
                                    } else // ko - C, - CV
                                        if (VV_) {
                                            phonemes.Add(
                                            new Phoneme { phoneme = $"{C}{V1}" }
                                            );
                                            phonemes.Add(
                                            new Phoneme { phoneme = $"{V1_1}{V2} -", position = End }
                                            );
                                        } else if (wV) {
                                            phonemes.Add(
                                            new Phoneme { phoneme = $"{C}{V1}{V2_2}" }
                                            );
                                            phonemes.Add(
                                            new Phoneme { phoneme = $"{V2} -", position = End }
                                            );
                                        } else // bths
                                            {
                                            phonemes.Add(
                                            new Phoneme { phoneme = $"{C}{V1}" }
                                            );
                                            phonemes.Add(
                                                new Phoneme { phoneme = $"{V1_1}{V2_2}", position = ViTri }
                                                );
                                            phonemes.Add(
                                                new Phoneme { phoneme = $"{N} -", position = End }
                                                );
                                        }
                                } else { // co note ke tiep
                                    if (_C) { // co - C
                                        if (VV_) {
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"- {Cw}", position = VCP }
                                        );
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{C}{V1}" }
                                        );
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{V1_1}{V2_2}", position = ViTri }
                                        );
                                        } else if (wV) {
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"- {Cw}", position = VCP }
                                        );
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{C}{V1}{V2_2}" }
                                        );
                                        } else { // bths
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"- {Cw}", position = VCP }
                                        );
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{C}{V1}" }
                                        );
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{V1_1}{V2_2}", position = ViTri }
                                        );
                                        }
                                    } else { //bth ko - C, ko - CV
                                        if (VV_) {
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{C}{V1}" }
                                        );
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{V1_1}{V2_2}", position = ViTri }
                                        );
                                        } else if (wV) {
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{C}{V1}{V2_2}" }
                                        );
                                        } else // bths
                                            {
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{C}{V1}" }
                                        );
                                            phonemes.Add(
                                            new Phoneme { phoneme = $"{V1_1}{V2_2}", position = ViTri }
                                            );
                                        }
                                    }
                                }
                    }
                // 4 âm VVVC có VVC liền, chia 3 nốt, ví dụ "uyết" "uyên"
                if (fry) { } else
                    if (dem == 4 && kocoC && tontaiVVC) {
                        string V1 = loi.Substring(0, 1);
                        string V2 = loi.Substring(1, 1);
                        string VVC = loi.Substring(1);
                        string C = loi.Substring(3);
                        if (V1 == "u") V1 = "w";
                        V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                        V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                        VVC = VVC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                     .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                        C = C.Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                        if (NoNext && tontaiCcuoi) {
                            phonemes.Add(
                                new Phoneme { phoneme = $"- {V1}{V2}" }
                                );
                            phonemes.Add(
                                new Phoneme { phoneme = $"{VVC}", position = ViTri }
                                );
                        } else if (NoNext) {
                            phonemes.Add(
                                new Phoneme { phoneme = $"- {V1}{V2}" }
                                );
                            phonemes.Add(
                                new Phoneme { phoneme = $"{VVC}", position = ViTri }
                                );
                            phonemes.Add(
                                new Phoneme { phoneme = $"{C} -", position = End }
                                );
                        } else {
                            phonemes.Add(
                                new Phoneme { phoneme = $"- {V1}{V2}" }
                                );
                            phonemes.Add(
                                    new Phoneme { phoneme = $"{VVC}", position = ViTri }
                                    );
                        }
                    }
                // 4 âm CVVC/CVVV, chia 3 nốt, ví dụ "thoát" "toan" "toại"
                if (tontaiVVC) { } else
                    if (fry) { } else
                        if (dem == 4 && tontaiC) {
                            string C = loi.Substring(0, 1);
                            string Cw = C;
                            string V1 = loi.Substring(1, 1);
                            string V2 = loi.Substring(2, 1);
                            string V2_2 = V2;
                            string VC = loi.Substring(2);
                            string N = loi.Substring(3);
                            string N_ = N;
                            a = (loi.EndsWith("ia") || loi.EndsWith("ua") || loi.EndsWith("ưa") || loi.EndsWith("ya"));
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
                            C = C.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                            Cw = Cw.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                            V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                            V2 = V2.Replace("ă", "a").Replace("â", "@").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                            VC = VC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                         .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                            N = N.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("ê", "E").Replace("ô", "O")
                                         .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                            N_ = N_.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                        .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                            if (_CV) { C = "- " + C; }
                            if (tontaiCcuoi) { // có C ngắt
                                if (_C) {
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"- {Cw}", position = VCP }
                                    );
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}{V2}" }
                                    );
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{VC}", position = ViTri }
                                    );
                                } else {
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}{V2}" }
                                    );
                                    phonemes.Add(
                                        new Phoneme { phoneme = $"{VC}", position = ViTri }
                                        );
                                }
                            } else
                                if (note.lyric.EndsWith("uân") || note.lyric.EndsWith("uâng")) {
                                    if (wAn == false) {
                                        if (NoNext) {
                                            if (loi.StartsWith(".")) {
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"{C}w@" }
                                        );
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"A{N}", position = ViTri }
                                        );
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"{N_} -", position = End }
                                        );
                                            } else if (_C) {
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"- {Cw}", position = VCP }
                                        );
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"{C}wA{N}" }
                                        );
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"{N_} -", position = End }
                                        );
                                            } else {
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"{C}wA{N}" }
                                        );
                                                phonemes.Add(
                                            new Phoneme { phoneme = $"{N_} -", position = End }
                                            );
                                            }
                                        } else { //
                                            if (loi.StartsWith(".")) {
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"{C}w@" }
                                        );
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"A{N}", position = ViTri });
                                            } else if (_C) {
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"- {Cw}", position = VCP });
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"{C}wA{N}" });
                                            } else
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"{C}wA{N}" });
                                        }
                                    } else { // khuân luân
                                        if (NoNext) {
                                            if (_C) {
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"- {Cw}", position = VCP });
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"{C}w" });
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"_w@", position = Long });
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"A{N}", position = Medium });
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"{N_} -", position = End });
                                            } else {
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"{C}w" });
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"_w@", position = Long });
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"A{N}", position = Medium });
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"{N_} -", position = End });
                                            }
                                        } else { //
                                            if (_C) {
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"- {Cw}", position = VCP });
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"{C}w" });
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"_w@", position = Long });
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"A{N}", position = Medium });
                                            } else {
                                                phonemes.Add(
                                        new Phoneme { phoneme = $"{C}w" });
                                                phonemes.Add(
                                            new Phoneme { phoneme = $"_w@", position = Long });
                                                phonemes.Add(
                                            new Phoneme { phoneme = $"A{N}", position = Medium });
                                            }
                                        }
                                    }
                                } else
                                    if (NoNext) {
                                        if (VV_) {
                                            if (_C) {
                                                phonemes.Add(
                                            new Phoneme { phoneme = $"- {Cw}", position = VCP });
                                                phonemes.Add(
                                            new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                                phonemes.Add(
                                            new Phoneme { phoneme = $"{V2}{N_} -", position = End });
                                            } else {
                                                phonemes.Add(
                                            new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                                phonemes.Add(
                                                new Phoneme { phoneme = $"{V2}{N_} -", position = End });
                                            }
                                        } else { // ko có VV -
                                            if (_C) {
                                                phonemes.Add(
                                            new Phoneme { phoneme = $"- {Cw}", position = VCP });
                                                phonemes.Add(
                                            new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                                phonemes.Add(
                                            new Phoneme { phoneme = $"{V2_2}{N}", position = ViTri });
                                                phonemes.Add(
                                            new Phoneme { phoneme = $"{N_} -", position = End });
                                            } else {
                                                phonemes.Add(
                                            new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                                phonemes.Add(
                                                new Phoneme { phoneme = $"{V2_2}{N}", position = ViTri });
                                                phonemes.Add(
                                                new Phoneme { phoneme = $"{N_} -", position = End });
                                            }
                                        }
                                    } else {
                                        if (VV_) {
                                            if (_C) {
                                                phonemes.Add(
                                            new Phoneme { phoneme = $"- {Cw}", position = VCP });
                                                phonemes.Add(
                                            new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                                phonemes.Add(
                                            new Phoneme { phoneme = $"{V2}{N}", position = ViTri });
                                            } else {
                                                phonemes.Add(
                                            new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                                phonemes.Add(
                                                new Phoneme { phoneme = $"{V2}{N}", position = ViTri });
                                            }
                                        } else { // ko có VV -
                                            if (_C) {
                                                phonemes.Add(
                                            new Phoneme { phoneme = $"- {Cw}", position = VCP });
                                                phonemes.Add(
                                            new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                                phonemes.Add(
                                            new Phoneme { phoneme = $"{V2_2}{N}", position = ViTri });
                                            } else {
                                                phonemes.Add(
                                            new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                                phonemes.Add(
                                                new Phoneme { phoneme = $"{V2_2}{N}", position = ViTri });
                                            }
                                        }
                                    }
                        }
                // 4 âm CVVC/CVVV, (tiên, tiết)
                if (fry) { } else
                    if (dem == 4 && tontaiVVC && tontaiC) {
                        string C = loi.Substring(0, 1);
                        string Cw = C;
                        string V1 = loi.Substring(1, 1);
                        string VVC = loi.Substring(1);
                        string N = loi.Substring(3);
                        if (V1 == "i" && _Cw) {
                            Cw = C + "y";
                        }
                        C = C.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                        Cw = Cw.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                        V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                        VVC = VVC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                     .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                        N = N.Replace("N", "ng").Replace("J", "nh");
                        if (_CV) { C = "- " + C; }
                        if (tontaiCcuoi) { // có C ngắt
                            if (_C) {
                                phonemes.Add(
                                new Phoneme { phoneme = $"- {Cw}", position = VCP });
                                phonemes.Add(
                                new Phoneme { phoneme = $"{C}{V1}" });
                                phonemes.Add(
                                new Phoneme { phoneme = $"{VVC}", position = ViTri });
                            } else {
                                phonemes.Add(
                                new Phoneme { phoneme = $"{C}{V1}" });
                                phonemes.Add(
                                    new Phoneme { phoneme = $"{VVC}", position = ViTri });
                            }
                        } else
                            if (NoNext) { // ko có note kế tiếp
                                if (_C) {
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"- {Cw}", position = VCP });
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}" });
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{N} -", position = End });
                                } else {
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}" });
                                    phonemes.Add(
                                        new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                    phonemes.Add(
                                        new Phoneme { phoneme = $"{N} -", position = End });
                                }
                            } else { // có note kế tiếp
                                if (_C) {
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"- {Cw}", position = VCP });
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}" });
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                } else {
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}" });
                                    phonemes.Add(
                                        new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                }
                            }
                    }
                // 5 âm CVVVC, có VVC liền, chia 3 nốt, ví dụ "thuyết"
                if (fry) { } else
                    if (dem == 5 && tontaiVVC && tontaiC) {
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
                        C = C.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                        Cw = Cw.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                        V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                        V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                        VVC = VVC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                     .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                        N = N.Replace("N", "ng").Replace("J", "nh");
                        if (_CV) { C = "- " + C; }
                        if (tontaiCcuoi) { // có C ngắt
                            if (_C) {
                                phonemes.Add(
                                new Phoneme { phoneme = $"- {Cw}", position = VCP });
                                phonemes.Add(
                                new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                phonemes.Add(
                                new Phoneme { phoneme = $"{VVC}", position = ViTri });
                            } else {
                                phonemes.Add(
                                new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                phonemes.Add(
                                    new Phoneme { phoneme = $"{VVC}", position = ViTri });
                            }
                        } else
                            if (NoNext) { // ko có note kế tiếp
                                if (_C) {
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"- {Cw}", position = VCP });
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{N} -", position = End });
                                } else {
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                    phonemes.Add(
                                        new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                    phonemes.Add(
                                        new Phoneme { phoneme = $"{N} -", position = End });
                                }
                            } else { // có note kế tiếp
                                if (_C) {
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"- {Cw}", position = VCP });
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                } else {
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                    phonemes.Add(
                                        new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                }
                            }
                    }
                if (BR) {
                    string num = loi.Substring(5);
                    if (num == "") {
                        num = "1";
                    }
                    if (prevNeighbour == null) {
                        phonemes.Add(
                            new Phoneme { phoneme = $"breath{num}" });
                    }
                }
                if (note.lyric.StartsWith("y") && koVVCchia) {
                    if (dem == 2) { // ya
                        string C = note.lyric.Substring(0, 1);
                        string V = note.lyric.Substring(1, 1);
                        V = V.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                     .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                        if (NoNext) {
                            phonemes.Add(
                            new Phoneme { phoneme = $"- {C}{V}" });
                            phonemes.Add(
                            new Phoneme { phoneme = $"{V} -", position = ViTri });
                        } else phonemes.Add(
                            new Phoneme { phoneme = $"- {C}{V}" });
                    } else if (dem == 3) {
                        string C = note.lyric.Substring(0, 1);
                        string V1 = note.lyric.Substring(1, 1);
                        string V2 = note.lyric.Substring(2, 1);
                        V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
         .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                        V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
         .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                        if (wV) {
                            V1 = "w";
                        }
                        a = (loi.EndsWith("ia") || loi.EndsWith("ua") || loi.EndsWith("ưa") || loi.EndsWith("ya"));
                        if (a && note.lyric != "qua") {
                            V2 = "@";
                        }
                        string N = V2;
                        if (V1 + V2 == "Ong" || V1 + V2 == "ung" || V1 + V2 == "ong") {
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
                        if (tontaiCcuoi) {
                            phonemes.Add(
                            new Phoneme { phoneme = $"- {C}{V1}" });
                            phonemes.Add(
                            new Phoneme { phoneme = $"{V1}{V2}", position = ViTri });
                        } else
                            if (NoNext) {
                                if (VV_) {
                                    phonemes.Add(
                                new Phoneme { phoneme = $"- {C}{V1}" });
                                    phonemes.Add(
                                new Phoneme { phoneme = $"{V1}{V2} -", position = End });
                                } else if (wV) {
                                    phonemes.Add(
                                new Phoneme { phoneme = $"- {C}{V1}{V2}" });
                                    phonemes.Add(
                                new Phoneme { phoneme = $"{V2} -", position = End });
                                } else {
                                    phonemes.Add(
                                new Phoneme { phoneme = $"- {C}{V1}" });
                                    phonemes.Add(
                                new Phoneme { phoneme = $"{V1}{V2}", position = ViTri });
                                    phonemes.Add(
                                new Phoneme { phoneme = $"{V2} -", position = End });
                                }
                            } else
                                if (wV) {
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"- {C}{V1}{V2}" });
                                } else {
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"- {C}{V1}" });
                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{V1}{V2}", position = ViTri });
                                }
                    }
                } // phụ âm y
                else { // nếu ko phải phụ âm y
                       // 2 âm VV, ví dụ: "oa"
                    if (fry) { } else
                        if ((dem == 2) && kocoC) {
                            string V1 = loi.Substring(0, 1);
                            string V1_ = V1;
                            string V2 = loi.Substring(1, 1);
                            string N = V2;
                            if (loi.StartsWith("uy")) V2 = "i";
                            if (V1 + V2 == "ôN" || V1 + V2 == "uN" || V1 + V2 == "oN") {
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
                            V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                 .Replace("ư", "U");
                            V1_ = V1_.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                 .Replace("ư", "U");
                            V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U")
                                .Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                            N = N.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U")
                                .Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                            a = (loi.EndsWith("ia") || loi.EndsWith("ua") || loi.EndsWith("ưa"));
                            if (a) {
                                V2 = "@";
                            }
                            if (tontaiCcuoi) {
                                phonemes.Add(
                                new Phoneme { phoneme = $"- {V1}" });
                                phonemes.Add(
                                new Phoneme { phoneme = $"{V1}{V2}", position = ViTri });
                            } else
                                if (NoNext) { // ko co note ke tiep
                                    if (wV) { // oa oe uê ,...
                                        phonemes.Add(
                                    new Phoneme { phoneme = $"- {V1}{V2}" });
                                        phonemes.Add(
                                    new Phoneme { phoneme = $"{N} -", position = End });
                                    } else
                                        if (VV_) { // ai eo êu ao,...
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"- {V1}" });
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{V1}{N} -", position = End });
                                        } else { // an anh
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"- {V1}" });
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{V1_}{V2}", position = ViTri });
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{N} -", position = End });
                                        }
                                } else {  // co note ke tiep
                                    if (wV) { // oa oe uê ,...
                                        phonemes.Add(
                                    new Phoneme { phoneme = $"- {V1}{V2}" });
                                    } else
                                        if (VV_) { // ai eo êu ao,...
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"- {V1}" });
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{V1}{N}", position = ViTri });
                                        } else { // an anh
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"- {V1}" });
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{V1_}{V2}", position = ViTri });
                                        }
                                }
                        }
                    // 3 âm VVC/VVV, ví dụ: "oát" "oan" "oai"
                    if (fry) { } else
                        if ((dem == 3) && koVVCchia && kocoC) {
                            string V1 = loi.Substring(0, 1);
                            string V2 = loi.Substring(1, 1);
                            string V2_2 = V2;
                            string V3 = loi.Substring(2, 1);
                            a = (loi.EndsWith("ia") || loi.EndsWith("ua") || loi.EndsWith("ưa") || loi.EndsWith("ya"));
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
                            V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                   .Replace("ư", "U");
                            V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                   .Replace("ư", "U");
                            V2_2 = V2_2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                   .Replace("ư", "U");
                            V3 = V3.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("ê", "E").Replace("ô", "O")
                                   .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                            string N = V3;
                            if (V2 + V3 == "Ong" || V2 + V3 == "ung" || V2 + V3 == "ong") {
                                N = "ng0";
                            }
                            if (V3 == "y") N = "i";
                            if (tontaiCcuoi && wV) {
                                phonemes.Add(
                                new Phoneme { phoneme = $"- {V1}{V2}" });
                                phonemes.Add(
                                new Phoneme { phoneme = $"{V2}{V3}", position = ViTri });
                            } else
                                if (NoNext) { // ko co note ke tiep
                                    if (wV && VV_) {
                                        phonemes.Add(
                                    new Phoneme { phoneme = $"- {V1}{V2}" });
                                        phonemes.Add(
                                    new Phoneme { phoneme = $"{V2_2}{N} -", position = End });
                                    } else
                                        if (wV) {
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"- {V1}{V2}" });
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{V2_2}{V3}", position = ViTri });
                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{N} -", position = End });
                                        }
                                } else { // co note ke tiep
                                    if (wV) {
                                        phonemes.Add(
                                    new Phoneme { phoneme = $"- {V1}{V2}" });
                                        phonemes.Add(
                                    new Phoneme { phoneme = $"{V2_2}{V3}", position = ViTri });
                                    }
                                }
                        }
                    // 3 âm VVV/VVC chia 2 nốt, ví dụ: "yên" "ướt"
                    if ((dem == 3) && tontaiVVC && kocoC) {
                        string V1 = loi.Substring(0, 1);
                        string VVC = loi.Substring(0);
                        string C = loi.Substring(2);
                        V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                               .Replace("ư", "U");
                        VVC = VVC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                               .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                        C = C.Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                        if (NoNext && tontaiCcuoi) {
                            phonemes.Add(
                            new Phoneme { phoneme = $"- {V1}" });
                            phonemes.Add(
                            new Phoneme { phoneme = $"{VVC}", position = ViTri });
                        } else if (NoNext) {
                            phonemes.Add(
                            new Phoneme { phoneme = $"- {V1}" });
                            phonemes.Add(
                            new Phoneme { phoneme = $"{VVC}", position = ViTri });
                            phonemes.Add(
                            new Phoneme { phoneme = $"{C} -", position = End });
                        } else {
                            phonemes.Add(
                            new Phoneme { phoneme = $"- {V1}" });
                            phonemes.Add(
                            new Phoneme { phoneme = $"{VVC}", position = ViTri });
                        }
                    }
                }
            } else {
                string vow = GetTransitionContext(prevNeighbour.Value, loi, H, _C, out prevtontaiCcuoi, out NoVCP);
                if (note.lyric == "qua") {
                    // ... (Consolidated "qua" logic will go here in Group 2.2)
                }
                if (note.lyric.StartsWith("?")) {
                    phoneme = note.lyric.Substring(1);
                } else {
                    {
                        // 1 âm
                        if (dem == 1 && loi != "R") {
                            string N = loi;
                            N = N.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                     .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                                     .Replace("Z", "tr").Replace("T", "th");
                            string N2 = N;
                            bool A = (vow == "o" || vow == "O" || vow == "u");
                            if (A && loi == "ng") N2 = "ng0";
                            if (loi == "N" || loi == "n" || loi == "J" || loi == "m") { } else
                                vow = vow + " ";
                            if ((loi == "N" || loi == "n" || loi == "J" || loi == "m") && prevtontaiCcuoi) { vow = "- "; } else if (prevtontaiCcuoi)
                                vow = ".";
                            if (NoNext) {
                                phonemes.Add(
                        new Phoneme { phoneme = $"{vow}{N}" });
                                phonemes.Add(
                        new Phoneme { phoneme = $"{N2} -", position = End });
                            } else {
                                phonemes.Add(
                        new Phoneme { phoneme = $"{vow}{N}" });
                            }
                        }
                        if (note.lyric == "R") { // R
                            phonemes.Add(
                        new Phoneme { phoneme = $"{vow} --" });
                        }
                        // 2 âm CV, ví dụ: "ba"
                        if ((dem == 2) && tontaiC) {
                            string N = loi;
                            string N1 = loi.Substring(0, 1);
                            string N2 = loi.Substring(1, 1);
                            N1 = N1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                     .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                                     .Replace("Z", "tr").Replace("T", "th");
                            if (_Cw) {
                                if (N2 == "u")
                                    N1 = N1 + "w";
                                if ((N2 == "i") || (N2 == "y"))
                                    N1 = N1 + "y";
                            }
                            N = N.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                     .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                                     .Replace("Z", "tr").Replace("T", "th");
                            N2 = N2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                     .Replace("ư", "U");
                            if (_CV && prevtontaiCcuoi) { N = "- " + N; }
                            vow = vow + " ";
                            if (NoNext) { // ko co note ke tiep
                                if (NoVCP) {
                                    phonemes.Add(
                        new Phoneme { phoneme = $"{N}" });
                                    phonemes.Add(
                        new Phoneme { phoneme = $"{N2} -", position = End });
                                } else {
                                    phonemes.Add(
                        new Phoneme { phoneme = $"{vow}{N1}", position = VCP });
                                    phonemes.Add(
                            new Phoneme { phoneme = $"{N}" });
                                    phonemes.Add(
                            new Phoneme { phoneme = $"{N2} -", position = End });
                                }
                            } else // co note ke tiep
                                if (NoVCP) { // co - C
                                    phonemes.Add(
                            new Phoneme { phoneme = $"{N}" });
                                } else {// ko co gi
                                    phonemes.Add(
                            new Phoneme { phoneme = $"{vow}{N1}", position = VCP });
                                    phonemes.Add(
                                new Phoneme { phoneme = $"{N}" });
                                }
                        }
                        // 3 âm CVV/CVC, ví dụ: "hoa" "hang" "hát"
                        if (fry) { } else
                            if (dem == 3 && tontaiC) {
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
                                } else
                                    if (wV) {
                                        V1 = "w";
                                    } else if (_Cw) Cw = C + "w";
                                if (V1 == "i" && _Cw) Cw = C + "y";
                                Cw = Cw.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                                C = C.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                                V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                                V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U")
                                            .Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                                V2_2 = V2_2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U")
                                            .Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                                V1_1 = V1_1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U")
                                            .Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                                a = (loi.EndsWith("ia") || loi.EndsWith("ua") || loi.EndsWith("ưa") || loi.EndsWith("ya"));
                                if (a && note.lyric != "qua") {
                                    V2 = "@";
                                    V2_2 = "@";
                                }
                                string N = V2;
                                if (V1 + V2 == "Ong" || V1 + V2 == "ung" || V1 + V2 == "ong") {
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
                                if (_CV && prevtontaiCcuoi) { C = "- " + C; }
                                vow = vow + " ";
                                if (NoVCP) {
                                    if (tontaiCcuoi) { // co C cuoi (at, ac,...)
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{C}{V1}" });
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{V1_1}{V2_2}", position = ViTri });
                                    } else
                                        if (kAn) {
                                            if (NoNext) {
                                                phonemes.Add(
                                new Phoneme { phoneme = $"kAn" });
                                                phonemes.Add(
                                new Phoneme { phoneme = $"n -", position = End });
                                            } else {
                                                phonemes.Add(
                                new Phoneme { phoneme = $"kAn" });
                                            }
                                        } else
                                            if (NoNext) { // ko co note ke tiep
                                                if (VV_) {
                                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}" });
                                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{V1_1}{V2} -", position = End });
                                                } else if (wV) {
                                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}{V2_2}" });
                                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{V2} -", position = End });
                                                } else { // bths
                                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}" });
                                                    phonemes.Add(
                                        new Phoneme { phoneme = $"{V1_1}{V2_2}", position = ViTri });
                                                    phonemes.Add(
                                        new Phoneme { phoneme = $"{N} -", position = End });
                                                }
                                            } else { // co note ke tiep
                                                if (VV_) {
                                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}" });
                                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{V1_1}{V2_2}", position = ViTri });
                                                } else if (wV) {
                                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}{V2_2}" });
                                                } else {
                                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}" });
                                                    phonemes.Add(
                                        new Phoneme { phoneme = $"{V1_1}{V2_2}", position = ViTri });
                                                }
                                            }
                                } else
                                    if (tontaiCcuoi) { // co C cuoi (at, ac,...)
                                        if (_C) { // co - C
                                            phonemes.Add(
                                new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                            phonemes.Add(
                                new Phoneme { phoneme = $"{C}{V1}" });
                                            phonemes.Add(
                                new Phoneme { phoneme = $"{V1_1}{V2_2}", position = ViTri });
                                        } else { // bths
                                            phonemes.Add(
                                new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}" });
                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{V1_1}{V2_2}", position = ViTri });
                                        }
                                    } else
                                        if (kAn) {
                                            if (NoNext) {
                                                phonemes.Add(
                                    new Phoneme { phoneme = $"{vow}k", position = VCP });
                                                phonemes.Add(
                                    new Phoneme { phoneme = $"kAn" });
                                                phonemes.Add(
                                    new Phoneme { phoneme = $"n -", position = End });
                                            } else {
                                                phonemes.Add(
                                    new Phoneme { phoneme = $"{vow}k", position = VCP });
                                                phonemes.Add(
                                    new Phoneme { phoneme = $"kAn" });
                                            }
                                        } else
                                            if (NoNext) { // ko co note ke tiep
                                                if (_C) { // co - C
                                                    if (VV_) {
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{C}{V1}" });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{V1_1}{V2} -", position = End });
                                                    } else if (wV) {
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{C}{V1}{V2_2}" });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{V2} -", position = End });
                                                    } else { // bths
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{C}{V1}" });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{V1_1}{V2_2}", position = ViTri });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{N} -", position = End });
                                                    }
                                                } else // ko - C, - CV
                                                    if (VV_) {
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{C}{V1}" });
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{V1_1}{V2} -", position = End });
                                                    } else if (wV) {
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{C}{V1}{V2_2}" });
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{V2} -", position = End });
                                                    } else { // bths
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                        phonemes.Add(
                                                new Phoneme { phoneme = $"{C}{V1}" });
                                                        phonemes.Add(
                                                new Phoneme { phoneme = $"{V1_1}{V2_2}", position = ViTri });
                                                        phonemes.Add(
                                                new Phoneme { phoneme = $"{N} -", position = End });
                                                    }
                                            } else { // co note ke tiep
                                                if (_C) { // co - C
                                                    if (VV_) {
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{C}{V1}" });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{V1_1}{V2_2}", position = ViTri });
                                                    } else if (wV) {
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                        phonemes.Add(
                                          new Phoneme { phoneme = $"{C}{V1}{V2_2}" });
                                                    } else { // bths
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{C}{V1}" });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{V1_1}{V2_2}", position = ViTri });
                                                    }
                                                } else { //bth ko - C, ko - CV
                                                    if (VV_) {
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{C}{V1}" });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{V1_1}{V2_2}", position = ViTri });
                                                    } else if (wV) {
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{C}{V1}{V2_2}" });
                                                    } else { //
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{C}{V1}" });
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{V1_1}{V2_2}", position = ViTri });
                                                    }
                                                }
                                            }
                            }
                        // 4 âm VVVC có VVC liền, chia 3 nốt, ví dụ "uyết" "uyên"
                        if (fry) { } else
                            if (dem == 4 && kocoC && tontaiVVC) {
                                string V1 = loi.Substring(0, 1);
                                string V2 = loi.Substring(1, 1);
                                string VVC = loi.Substring(1);
                                string C = loi.Substring(3);
                                if (V1 == "u") V1 = "w";
                                V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                                V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                                VVC = VVC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                             .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                                C = C.Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                                if (prevtontaiCcuoi) vow = "."; else vow = vow + " ";
                                if (prevtontaiCcuoi) {
                                    if (tontaiCcuoi) {
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{vow}{V1}{V2}" });
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                    } else if (NoNext) {
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{vow}{V1}{V2}" });
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{C} -", position = End });
                                    } else {
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{vow}{V1}{V2}" });
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                    }
                                } else
                                    if (NoNext && tontaiCcuoi) {
                                        phonemes.Add(
                                new Phoneme { phoneme = $"{vow}{V1}", position = VCP });
                                        phonemes.Add(
                                new Phoneme { phoneme = $"{V1}{V2}" });
                                        phonemes.Add(
                                new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                    } else if (NoNext) {
                                        phonemes.Add(
                                new Phoneme { phoneme = $"{vow}{V1}", position = VCP });
                                        phonemes.Add(
                                new Phoneme { phoneme = $"{V1}{V2}" });
                                        phonemes.Add(
                                new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                        phonemes.Add(
                                new Phoneme { phoneme = $"{C} -", position = End });
                                    } else {
                                        phonemes.Add(
                                new Phoneme { phoneme = $"{vow}{V1}", position = VCP });
                                        phonemes.Add(
                                    new Phoneme { phoneme = $"{V1}{V2}" });
                                        phonemes.Add(
                                    new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                    }
                            }
                        // 4 âm CVVC/CVVV, chia 3 nốt, ví dụ "thoát" "toan" "toại"
                        if (tontaiVVC) { } else
                            if (fry) { } else
                                if (dem == 4 && tontaiC) {
                                    string C = loi.Substring(0, 1);
                                    string Cw = C;
                                    string V1 = loi.Substring(1, 1);
                                    string V2 = loi.Substring(2, 1);
                                    string V2_2 = V2;
                                    string VC = loi.Substring(2);
                                    string N = loi.Substring(3);
                                    string N_ = N;
                                    a = (loi.EndsWith("ia") || loi.EndsWith("ua") || loi.EndsWith("ưa") || loi.EndsWith("ya"));
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
                                    C = C.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                                    Cw = Cw.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                                    V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                                    V2_2 = V2_2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                                    V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                                    VC = VC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("ê", "E").Replace("ô", "O")
                                                 .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                                    N = N.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("ê", "E").Replace("ô", "O")
                                                 .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                                    N_ = N_.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                                .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                                    if (_CV && prevtontaiCcuoi) { N = "- " + N; }
                                    vow = vow + " ";
                                    if (NoVCP) {
                                        if (tontaiCcuoi) { // có C ngắt
                                            phonemes.Add(
                                new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                            phonemes.Add(
                                new Phoneme { phoneme = $"{VC}", position = ViTri });
                                        } else
                                            if (note.lyric.EndsWith("uân") || note.lyric.EndsWith("uâng")) {
                                                if (wAn == false) {
                                                    if (NoNext) {
                                                        if (loi.StartsWith(".")) {
                                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{C}w@" });
                                                            phonemes.Add(
                                    new Phoneme { phoneme = $"An", position = ViTri });
                                                            phonemes.Add(
                                    new Phoneme { phoneme = $"n -", position = End });
                                                        } else
                                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{C}wAn" });
                                                        phonemes.Add(
                                    new Phoneme { phoneme = $"n -", position = End });
                                                    } else { //
                                                        if (loi.StartsWith(".")) {
                                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{C}w@" });
                                                            phonemes.Add(
                                    new Phoneme { phoneme = $"An", position = ViTri });
                                                        } else {
                                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{C}wAn" });
                                                        }
                                                    }
                                                } else { // khuân luân
                                                    if (NoNext) {
                                                        phonemes.Add(
                                    new Phoneme { phoneme = $"{C}w" });
                                                        phonemes.Add(
                                    new Phoneme { phoneme = $"w@", position = Long });
                                                        phonemes.Add(
                                    new Phoneme { phoneme = $"An", position = Medium });
                                                        phonemes.Add(
                                    new Phoneme { phoneme = $"n -", position = End });
                                                    } else { //
                                                        phonemes.Add(
                                    new Phoneme { phoneme = $"{C}w" });
                                                        phonemes.Add(
                                    new Phoneme { phoneme = $"w@", position = Long });
                                                        phonemes.Add(
                                    new Phoneme { phoneme = $"An", position = Medium });
                                                    }
                                                }
                                            } else
                                                if (NoNext) {
                                                    if (VV_) {
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{V2}{N} -", position = End });
                                                    } else { // ko có VV -
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{V2_2}{N}", position = ViTri });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{N} -", position = End });
                                                    }
                                                } else {
                                                    if (VV_) {
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{V2}{N}", position = ViTri });
                                                    } else { // ko có VV -
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{V2_2}{N}", position = ViTri });
                                                    }
                                                }
                                    } else
                                        if (tontaiCcuoi) { // có C ngắt
                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{VC}", position = ViTri });
                                        } else
                                            if (note.lyric.EndsWith("uân") || note.lyric.EndsWith("uâng")) {
                                                if (wAn == false) {
                                                    if (NoNext) {
                                                        if (loi.StartsWith(".")) {
                                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{C}w@" });
                                                            phonemes.Add(
                                        new Phoneme { phoneme = $"A{N}", position = ViTri });
                                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{N_} -", position = End });
                                                        } else {
                                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                            phonemes.Add(
                                            new Phoneme { phoneme = $"{C}wA{N}" });
                                                            phonemes.Add(
                                            new Phoneme { phoneme = $"{N_} -", position = End });
                                                        }
                                                    } else { //
                                                        if (loi.StartsWith(".")) {
                                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{C}w@" });
                                                            phonemes.Add(
                                        new Phoneme { phoneme = $"A{N}", position = ViTri });
                                                        } else {
                                                            phonemes.Add(
                                        new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                            phonemes.Add(
                                            new Phoneme { phoneme = $"{C}wA{N}" });
                                                        }
                                                    }
                                                } else { // khuân luân
                                                    if (NoNext) {
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{C}w" });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"_w@", position = Long });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"A{N}", position = Medium });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{N_} -", position = End });
                                                    } else { //
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{C}w" });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"_w@", position = Long });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"A{N}", position = Medium });
                                                    }
                                                }
                                            } else
                                                if (NoNext) {
                                                    if (VV_) {
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{V2}{N_} -", position = End });
                                                    } else { // ko có VV -
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{V2_2}{N}", position = ViTri });
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{N_} -", position = End });
                                                    }
                                                } else {
                                                    if (VV_) {
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{V2}{N}", position = ViTri });
                                                    } else { // ko có VV -
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                                        phonemes.Add(
                                            new Phoneme { phoneme = $"{V2_2}{N}", position = ViTri });
                                                    }
                                                }
                                }
                        // 4 âm CVVC/CVVV, (tiên, tiết)
                        if (fry) { } else
                            if (dem == 4 && tontaiVVC && tontaiC) {
                                string C = loi.Substring(0, 1);
                                string Cw = C;
                                string V1 = loi.Substring(1, 1);
                                string VVC = loi.Substring(1);
                                string N = loi.Substring(3);
                                if (V1 == "i" && _Cw) {
                                    Cw = C + "y";
                                }
                                C = C.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                                Cw = Cw.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                                V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                                VVC = VVC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                             .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                                N = N.Replace("N", "ng").Replace("J", "nh");
                                if (_CV && prevtontaiCcuoi) { C = "- " + C; }
                                vow = vow + " ";
                                if (NoVCP) {
                                    if (tontaiCcuoi) { // có C ngắt
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{C}{V1}" });
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                    } else
                                        if (NoNext) { // ko có note kế tiếp
                                            phonemes.Add(
                                new Phoneme { phoneme = $"{C}{V1}" });
                                            phonemes.Add(
                                new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                            phonemes.Add(
                                new Phoneme { phoneme = $"{N} -", position = End });
                                        } else { // có note kế tiếp
                                            phonemes.Add(
                                new Phoneme { phoneme = $"{C}{V1}" });
                                            phonemes.Add(
                                new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                        }
                                } else
                                    if (tontaiCcuoi) { // có C ngắt
                                        phonemes.Add(
                                new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                        phonemes.Add(
                                new Phoneme { phoneme = $"{C}{V1}" });
                                        phonemes.Add(
                                new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                    } else
                                        if (NoNext) { // ko có note kế tiếp
                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}" });
                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{N} -", position = End });
                                        } else { // có note kế tiếp
                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}" });
                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                        }
                            }
                        // 5 âm CVVVC, có VVC liền, chia 3 nốt, ví dụ "thuyết"
                        if (fry) { } else
                            if (dem == 5 && tontaiVVC && tontaiC) {
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
                                C = C.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                                Cw = Cw.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                                V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                                V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                                VVC = VVC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                             .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                                N = N.Replace("N", "ng").Replace("J", "nh");
                                if (_CV && prevtontaiCcuoi) { N = "- " + N; }
                                vow = vow + " ";
                                if (NoVCP) {
                                    if (tontaiCcuoi) { // có C ngắt
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                    } else
                                        if (NoNext) { // ko có note kế tiếp
                                            phonemes.Add(
                                new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                            phonemes.Add(
                                new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                            phonemes.Add(
                                new Phoneme { phoneme = $"{N} -", position = End });
                                        } else { // có note kế tiếp
                                            phonemes.Add(
                                new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                            phonemes.Add(
                                new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                        }
                                } else
                                    if (tontaiCcuoi) { // có C ngắt
                                        phonemes.Add(
                                new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                        phonemes.Add(
                                new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                        phonemes.Add(
                                new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                    } else
                                        if (NoNext) { // ko có note kế tiếp
                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{N} -", position = End });
                                        } else { // có note kế tiếp
                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{vow}{Cw}", position = VCP });
                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                            phonemes.Add(
                                    new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                        }
                            }
                        // y
                        if (note.lyric.StartsWith("y") && koVVCchia) {
                            if (dem == 2) { // ya
                                string C = note.lyric.Substring(0, 1);
                                string V = note.lyric.Substring(1, 1);
                                V = V.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                             .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                                vow = vow + " ";
                                if (prevtontaiCcuoi) {
                                    if (NoNext) {
                                        phonemes.Add(
                        new Phoneme { phoneme = $"- {C}{V}" });
                                        phonemes.Add(
                        new Phoneme { phoneme = $"{V} -", position = End });
                                    } else phonemes.Add(
                        new Phoneme { phoneme = $"- {C}{V}" });
                                } else
                                    if (NoNext) {
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{vow}{C}", position = VCP });
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{C}{V}" });
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{V} -", position = End });
                                    } else {
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{vow}{C}", position = VCP });
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{C}{V}" });
                                    }
                            } else if (dem == 3) {
                                string C = note.lyric.Substring(0, 1);
                                string V1 = note.lyric.Substring(1, 1);
                                string V2 = note.lyric.Substring(2, 1);
                                V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                 .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                                V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                 .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                                if (wV) {
                                    V1 = "w";
                                }
                                a = (loi.EndsWith("ia") || loi.EndsWith("ua") || loi.EndsWith("ưa") || loi.EndsWith("ya"));
                                if (a && note.lyric != "qua") {
                                    V2 = "@";
                                }
                                string N = V2;
                                if (V1 + V2 == "Ong" || V1 + V2 == "ung" || V1 + V2 == "ong") {
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
                                vow = vow + " ";
                                if (prevtontaiCcuoi) {
                                    if (tontaiCcuoi) {
                                        phonemes.Add(
                        new Phoneme { phoneme = $"- {C}{V1}" });
                                        phonemes.Add(
                        new Phoneme { phoneme = $"{V1}{V2}", position = ViTri });
                                    } else
                                        if (NoNext) {
                                            if (VV_) {
                                                phonemes.Add(
                            new Phoneme { phoneme = $"- {C}{V1}" });
                                                phonemes.Add(
                            new Phoneme { phoneme = $"{V1}{V2} -", position = End });
                                            } else if (wV) {
                                                phonemes.Add(
                            new Phoneme { phoneme = $"- {C}{V1}{V2}" });
                                                phonemes.Add(
                            new Phoneme { phoneme = $"{V2} -", position = End });
                                            } else {
                                                phonemes.Add(
                            new Phoneme { phoneme = $"- {C}{V1}" });
                                                phonemes.Add(
                            new Phoneme { phoneme = $"{V1}{V2}", position = ViTri });
                                                phonemes.Add(
                            new Phoneme { phoneme = $"{V2} -", position = End });
                                            }
                                        } else
                                            if (wV) {
                                                phonemes.Add(
                                new Phoneme { phoneme = $"- {C}{V1}{V2}" });
                                            } else {
                                                phonemes.Add(
                                new Phoneme { phoneme = $"- {C}{V1}" });
                                                phonemes.Add(
                                new Phoneme { phoneme = $"{V1}{V2}", position = ViTri });
                                            }
                                } else
                                    if (tontaiCcuoi) {
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{vow}{C}", position = VCP });
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{C}{V1}" });
                                        phonemes.Add(
                            new Phoneme { phoneme = $"{V1}{V2}", position = ViTri });
                                    } else
                                        if (NoNext) {
                                            if (VV_) {
                                                phonemes.Add(
                                new Phoneme { phoneme = $"{vow}{C}", position = VCP });
                                                phonemes.Add(
                                new Phoneme { phoneme = $"{C}{V1}" });
                                                phonemes.Add(
                                new Phoneme { phoneme = $"{V1}{V2} -", position = End });
                                            } else if (wV) {
                                                phonemes.Add(
                                new Phoneme { phoneme = $"{vow}{C}", position = VCP });
                                                phonemes.Add(
                                new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                                phonemes.Add(
                                new Phoneme { phoneme = $"{V2} -", position = End });
                                            } else {
                                                phonemes.Add(
                                new Phoneme { phoneme = $"{vow}{C}", position = VCP });
                                                phonemes.Add(
                                new Phoneme { phoneme = $"{C}{V1}" });
                                                phonemes.Add(
                                new Phoneme { phoneme = $"{V1}{V2}", position = ViTri });
                                                phonemes.Add(
                                new Phoneme { phoneme = $"{V2} -", position = End });
                                            }
                                        } else
                                            if (wV) {
                                                phonemes.Add(
                                    new Phoneme { phoneme = $"{vow}{C}", position = VCP });
                                                phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}{V2}" });
                                            } else {
                                                phonemes.Add(
                                    new Phoneme { phoneme = $"{vow}{C}", position = VCP });
                                                phonemes.Add(
                                    new Phoneme { phoneme = $"{C}{V1}" });
                                                phonemes.Add(
                                    new Phoneme { phoneme = $"{V1}{V2}", position = ViTri });
                                            }
                            }
                        } else { // nếu ko phải phụ âm y
                                 // 2 âm VV, ví dụ: "oa"
                            if (fry) { } else
                                if ((dem == 2) && kocoC) {
                                    string V1 = loi.Substring(0, 1);
                                    string V1_ = V1;
                                    string V2 = loi.Substring(1, 1);
                                    string N = V2;
                                    if (loi.StartsWith("uy")) V2 = "i";
                                    if (V1 + V2 == "ôN" || V1 + V2 == "uN" || V1 + V2 == "oN") {
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
                                    V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                         .Replace("ư", "U");
                                    V1_ = V1_.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                         .Replace("ư", "U");
                                    V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U")
                                        .Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                                    N = N.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U")
                                        .Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                                    a = (loi.EndsWith("ia") || loi.EndsWith("ua") || loi.EndsWith("ưa"));
                                    if (a) {
                                        V2 = "@";
                                    }
                                    if (prevtontaiCcuoi) vow = "."; else vow = vow + " ";
                                    if (prevtontaiCcuoi) {
                                        if (tontaiCcuoi) {
                                            phonemes.Add(
                            new Phoneme { phoneme = $"{vow}{V1}" });
                                            phonemes.Add(
                            new Phoneme { phoneme = $"{V1}{V2}", position = ViTri });
                                        } else
                                            if (NoNext) { // ko co note ke tiep
                                                if (wV) { // oa oe uê ,...
                                                    phonemes.Add(
                                new Phoneme { phoneme = $"{vow}{V1}{V2}" });
                                                    phonemes.Add(
                                new Phoneme { phoneme = $"{N} -", position = End });
                                                } else
                                                    if (VV_) { // ai eo êu ao,...
                                                        phonemes.Add(
                                    new Phoneme { phoneme = $"{vow}{V1}" });
                                                        phonemes.Add(
                                    new Phoneme { phoneme = $"{V1}{N} -", position = End });
                                                    } else { // an anh
                                                        phonemes.Add(
                                    new Phoneme { phoneme = $"{vow}{V1}" });
                                                        phonemes.Add(
                                    new Phoneme { phoneme = $"{V1_}{V2}", position = ViTri });
                                                        phonemes.Add(
                                    new Phoneme { phoneme = $"{N} -", position = End });
                                                    }
                                            } else {  // co note ke tiep
                                                if (wV) { // oa oe uê ,...
                                                    phonemes.Add(
                                new Phoneme { phoneme = $"{vow}{V1}{V2}" });
                                                } else
                                                    if (VV_) { // ai eo êu ao,...
                                                        phonemes.Add(
                                    new Phoneme { phoneme = $"{vow}{V1}" });
                                                        phonemes.Add(
                                    new Phoneme { phoneme = $"{V1}{N}", position = ViTri });
                                                    } else { // an anh
                                                        phonemes.Add(
                                    new Phoneme { phoneme = $"{vow}{V1}" });
                                                        phonemes.Add(
                                    new Phoneme { phoneme = $"{V1_}{V2}", position = ViTri });
                                                    }
                                            }
                                    } else
                                        if (tontaiCcuoi) {
                                            phonemes.Add(
                                new Phoneme { phoneme = $"{vow}{V1}" });
                                            phonemes.Add(
                                new Phoneme { phoneme = $"{V1}{V2}", position = ViTri });
                                        } else
                                            if (NoNext) { // ko co note ke tiep
                                                if (wV) { // oa oe uê ,...
                                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{vow}{V1}", position = VCP });
                                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{V1}{V2}" });
                                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{N} -", position = End });
                                                } else
                                                    if (VV_) { // ai eo êu ao,...
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{vow}{V1}" });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{V1}{N} -", position = End });
                                                    } else { // an anh
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{vow}{V1}" });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{V1_}{V2}", position = ViTri });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{N} -", position = End });
                                                    }
                                            } else {  // co note ke tiep
                                                if (wV) { // oa oe uê ,...
                                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{vow}{V1}", position = VCP });
                                                    phonemes.Add(
                                    new Phoneme { phoneme = $"{V1}{V2}" });
                                                } else
                                                    if (VV_) { // ai eo êu ao,...
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{vow}{V1}" });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{V1}{N}", position = ViTri });
                                                    } else { // an anh
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{vow}{V1}" });
                                                        phonemes.Add(
                                        new Phoneme { phoneme = $"{V1_}{V2}", position = ViTri });
                                                    }
                                            }
                                }
                            // 3 âm VVC/VVV, ví dụ: "oát" "oan" "oai" "uân"
                            if (fry) { } else
                                if ((dem == 3) && koVVCchia && kocoC) {
                                    string V1 = loi.Substring(0, 1);
                                    string V2 = loi.Substring(1, 1);
                                    string V2_2 = V2;
                                    string V3 = loi.Substring(2, 1);
                                    a = (loi.EndsWith("ia") || loi.EndsWith("ua") || loi.EndsWith("ưa") || loi.EndsWith("ya"));
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
                                    V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                           .Replace("ư", "U");
                                    V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                           .Replace("ư", "U");
                                    V2_2 = V2_2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                           .Replace("ư", "U");

                                    V3 = V3.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("ê", "E").Replace("ô", "O")
                                           .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                                    string N = V3;
                                    if (V3 == "y") N = "i";
                                    if (V2 + V3 == "Ong" || V2 + V3 == "ung" || V2 + V3 == "ong") {
                                        N = "ng0";
                                    }
                                    if (prevtontaiCcuoi) vow = "."; else vow = vow + " ";
                                    if (prevtontaiCcuoi) {
                                        if (NoNext) { // ko co note ke tiep
                                            if (VV_) {
                                                phonemes.Add(
                            new Phoneme { phoneme = $"{vow}{V1}{V2}" });
                                                phonemes.Add(
                            new Phoneme { phoneme = $"{V2_2}{N} -", position = End });
                                            } else {
                                                phonemes.Add(
                            new Phoneme { phoneme = $"{vow}{V1}{V2}" });
                                                phonemes.Add(
                                new Phoneme { phoneme = $"{V2_2}{V3}", position = ViTri });
                                                phonemes.Add(
                                new Phoneme { phoneme = $"{N} -", position = End });
                                            }
                                        } else { // co note ke tiep
                                            if (VV_) {
                                                phonemes.Add(
                            new Phoneme { phoneme = $"{vow}{V1}{V2}" });
                                                phonemes.Add(
                            new Phoneme { phoneme = $"{V2_2}{V3}", position = ViTri });
                                            } else {
                                                phonemes.Add(
                            new Phoneme { phoneme = $"{vow}{V1}{V2}" });
                                                phonemes.Add(
                                new Phoneme { phoneme = $"{V2_2}{V3}", position = ViTri });
                                            }
                                        }
                                    } else {
                                        if (NoNext) { // ko co note ke tiep
                                            if (wV && VV_) {
                                                phonemes.Add(
                            new Phoneme { phoneme = $"{vow}{V1}", position = VCP });
                                                phonemes.Add(
                            new Phoneme { phoneme = $"{V1}{V2}" });
                                                phonemes.Add(
                            new Phoneme { phoneme = $"{V2_2}{N} -", position = End });
                                            } else
                                                if (wV) {
                                                    phonemes.Add(
                                new Phoneme { phoneme = $"{vow}{V1}", position = VCP });
                                                    phonemes.Add(
                                new Phoneme { phoneme = $"{V1}{V2}" });
                                                    phonemes.Add(
                                new Phoneme { phoneme = $"{V2_2}{V3}", position = ViTri });
                                                    phonemes.Add(
                                new Phoneme { phoneme = $"{N} -", position = End });
                                                }
                                        } else { // co note ke tiep
                                            if (wV && VV_) {
                                                phonemes.Add(
                            new Phoneme { phoneme = $"{vow}{V1}", position = VCP });
                                                phonemes.Add(
                            new Phoneme { phoneme = $"{V1}{V2}" });
                                                phonemes.Add(
                            new Phoneme { phoneme = $"{V2_2}{V3}", position = ViTri });
                                            } else
                                                if (wV) {
                                                    phonemes.Add(
                                new Phoneme { phoneme = $"{vow}{V1}", position = VCP });
                                                    phonemes.Add(
                                new Phoneme { phoneme = $"{V1}{V2}" });
                                                    phonemes.Add(
                                new Phoneme { phoneme = $"{V2_2}{V3}", position = ViTri });
                                                }
                                        }
                                    }
                                }
                            // 3 âm VVV/VVC chia 2 nốt, ví dụ: "yên" "ướt"
                            if ((dem == 3) && tontaiVVC && kocoC) {
                                string V1 = loi.Substring(0, 1);
                                string VVC = loi.Substring(0);
                                string C = loi.Substring(2);
                                V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                       .Replace("ư", "U");
                                VVC = VVC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                       .Replace("ư", "U").Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                                C = C.Replace("C", "ch").Replace("N", "ng").Replace("J", "nh");
                                if (prevtontaiCcuoi) vow = "."; else vow = vow + " ";
                                if (NoNext && tontaiCcuoi) {
                                    phonemes.Add(
                        new Phoneme { phoneme = $"{vow}{V1}" });
                                    phonemes.Add(
                        new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                } else if (NoNext) {
                                    phonemes.Add(
                        new Phoneme { phoneme = $"{vow}{V1}" });
                                    phonemes.Add(
                        new Phoneme { phoneme = $"{VVC}", position = ViTri });
                                    phonemes.Add(
                        new Phoneme { phoneme = $"{C} -", position = End });
                                } else {
                                    phonemes.Add(
                        new Phoneme { phoneme = $"{vow}{V1}" });
                                    phonemes.Add(
                            new Phoneme { phoneme = $"{VVC}", position = ViTri });
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
                                phonemes.Add(
                        new Phoneme { phoneme = $"breath{num}" });
                            } else {
                                phonemes.Add(
                        new Phoneme { phoneme = $"{vow} -", position = -60 });
                                phonemes.Add(
                        new Phoneme { phoneme = $"breath{num}" });
                            }
                        }
                    }
                }
            }

MapOtoAndReturn:
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
            return new Result { phonemes = phonemes.ToArray() };
        }
    }
}
