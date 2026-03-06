using System;
using OpenUtau.Api;
using OpenUtau.Plugin.Builtin;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Plugins {
    public class ViVinaTest : PhonemizerTestBase {
        public ViVinaTest(ITestOutputHelper output) : base(output) { }

        protected override Phonemizer CreatePhonemizer() {
            return new VietnameseVINAPhonemizer();
        }

        [Fact]
        public void GenerateBaselineTest() {
            string[] lyrics = new string[] {
                "a", "ba", "hoa", "hang", "hát", "quyết", "khuân", "luân", "tiên", "tiết",
                "nghiêng", "quốc", "giêng", "xuống", "chuyện", "khoảng", "rồi", "nghĩ", "bóng", "mất", "vào", "hoang", "vu", "chiều", "qua"
            };

            foreach (var lyric in lyrics) {
                try {
                    RunPhonemizeTest("ja_cvvc", new string[] { lyric }, new string[] { "" }, new string[] { "C4" }, new string[] { "" }, new string[] { "dump" });
                } catch (Exception e) {
                    System.IO.File.AppendAllText("baseline_output.txt", $"[RESULT] {lyric}: " + e.Message.Replace("\r", "").Replace("\n", " ") + "\n");
                }
            }
        }
    }
}
