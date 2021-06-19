using AnimalFlicker.GamepadInterface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnimalFlicker.InputMethodInterface {
    // 文字入力機構の設定と実際の出力を担うクラス
    public class InputMethodConfig {
        // 前回の文字入力からこの時間以内にスティックを離した場合ア行が入力されない(単位:msec)
        public static int INVOKE_TIME_MSEC { get; set; } = 300;
        // 直前の文字を長押し入力した場合こちらの時間が適用(単位:msec)
        public static int INVOKE_TIME_HOLD_MSEC { get; set; } = 500;
        // 最後に文字を打った時間から時間を計測
        private Stopwatch inputTimer;
        // 直前が長押しだったかフラグ
        private bool preHoldFlg;
        // かな入力フラグ
        private bool kanaFlg;
        // かな文字のキーマップ
        public Dictionary<uint, string> kanaAssignMap { get; private set; }
        // 英字のキーマップ
        public Dictionary<uint, string> alphAssignMap { get; private set; }

        // コンストラクタ
        public InputMethodConfig() {
            initKanaAssignMap();
            initAlphAssignMap();
            preHoldFlg = false;
            kanaFlg = true;
            inputTimer = new Stopwatch();
            inputTimer.Start();
        }

        public string getInputStr(Dictionary<string, GamepadButtonState> btn,
            GamepadDirectionState lAnalog, GamepadDirectionState pov) {
            uint bin = getGpInputBinary(btn, lAnalog, pov);
            // 入力モードによってキーマップ切り替え
            Dictionary<uint, string> keyMap = kanaFlg ? kanaAssignMap : alphAssignMap;
            if (keyMap.TryGetValue(bin, out string str)) {
                // 送る文字が取得できた場合はタイマー再起動
                inputTimer.Restart();
                return str;
            }
            return null;
        }

        // ゲームパッドの入力を対応する入力バイナリ値に変換する
        public uint getGpInputBinary(Dictionary<string, GamepadButtonState> btn,
            GamepadDirectionState lAnalog, GamepadDirectionState pov) {
            // 右スティック押し込みが発生したら英字カナ切り替えをして即終了
            if (btn.TryGetValue("RS", out GamepadButtonState rss)) {
                if (rss.state == ButtonStateEnum.PRESS) {
                    kanaFlg = !kanaFlg;
                    return 0;
                }
            }
            // ※ビット列定義
            // XX-XXX-XXX-X-X: 左から、Lステ十字判定、方向、ボタンの識別子、濁点、拗音フラグ(10bit)
            uint state = 0;
            GamepadDirectionState input = null;

            if (lAnalog.direction != DirectionEnum.RELEASE) {
                // Lスティックが入力されている
                state |= 0b_10 << 8;
                input = lAnalog;
            } else if (pov.direction != DirectionEnum.RELEASE) {
                // 十字キーが入力されている
                state |= 0b_11 << 8;
                input = pov;
            }

            // どちらかの方向が入力されていれば方向をビット列に入れる
            if (input != null) state |= ((uint)input.direction) << 5;

            // 各ボタンの入力判定
            uint btnSt = 0;
            // スティックの長押しイベントは他のボタンが押されたら中断
            if (!isHolded4Buttons(btn)) btnSt = input != null
                    ? getDirectionRelease(state, input, 0b_0001) : getButtonCode(state, btn, "R", 0b_0001);
            if (btnSt == 0) btnSt = getButtonCode(state, btn, "Y", 0b_0010);
            if (btnSt == 0) btnSt = getButtonCode(state, btn, "X", 0b_0011);
            if (btnSt == 0) btnSt = getButtonCode(state, btn, "A", 0b_0100);
            if (btnSt == 0) btnSt = getButtonCode(state, btn, "B", 0b_0101);
            state |= btnSt;

            // 濁点拗音フラグが立っていなかったらシフトのフラグ処理
            if ((state & 0b_11) == 0) {
                state |= getButtonFlg(btn, "L",  0b_01); // 拗音
                state |= getButtonFlg(btn, "ZL", 0b_10); // 濁点
            }

            return state;
        }
        
        // ボタンの状態からコードを取得
        private uint getButtonCode(uint state, Dictionary<string, GamepadButtonState> btn, string key, uint bin) {
            if (btn.TryGetValue(key, out GamepadButtonState st)) {
                if (st.state == ButtonStateEnum.FAST_RELEASE) {
                    // ボタンを(長押しせずに)離した場合
                    preHoldFlg = false;
                    return bin << 2;
                } else if (st.state == ButtonStateEnum.KEEP_HOLD) {
                    // ボタンを長押ししていた場合濁点フラグを付与
                    preHoldFlg = true;
                    return (bin << 2) | getShiftFlg(state, bin);
                }
            }
            return 0;
        }

        // 方向入力がリリースされた瞬間を取得
        private uint getDirectionRelease(uint state, GamepadDirectionState input, uint bin) {
            if (inputTimer.ElapsedMilliseconds > (preHoldFlg ? INVOKE_TIME_HOLD_MSEC : INVOKE_TIME_MSEC)) {
                // 前回の入力直後でなければイベントに対応したキーコード度を出力
                if (input.state == ButtonStateEnum.FAST_RELEASE) {
                    // スティックをすぐ離した場合
                    preHoldFlg = false;
                    return bin << 2;
                } else if (input.state == ButtonStateEnum.KEEP_HOLD) {
                    // スティックを倒し続けた場合濁点フラグを付与
                    preHoldFlg = true;
                    return (bin << 2) | getShiftFlg(state, bin);
                }
            }
            return 0;
        }
        
        // ホールドボタンの状態からフラグを立てる
        private uint getButtonFlg(Dictionary<string, GamepadButtonState> btn, string key, uint bin) {
            if (btn.TryGetValue(key, out GamepadButtonState st)) {
                // ボタンを押しっぱなしにしている場合
                if (st.state == ButtonStateEnum.PRESS || st.state == ButtonStateEnum.HOLD) return bin;
            }
            return 0;
        }

        // 長押しシフトで立てるフラグを決定
        private uint getShiftFlg(uint state, uint bin) {
            // シフトを拗音に設定
            if ((state ^ 0b_10_000_000_00) == 0 // ア行
                || (state ^ 0b_11_000_000_00) == 0 // ヤ行
                || ((state ^ 0b_10_011_000_00) == 0 && bin == 0b_0011) // ツ
                ) return 0b_01;
            // それ以外はシフトを濁点に設定
            else return 0b_10;
        }

        // ABXYボタンが押されている場合はtrueを返す
        private bool isHolded4Buttons(Dictionary<string, GamepadButtonState> btn) {
            if (btn.TryGetValue("A", out GamepadButtonState st) && st.state != ButtonStateEnum.NONE) return true;
            if (btn.TryGetValue("B", out st) && st.state != ButtonStateEnum.NONE) return true;
            if (btn.TryGetValue("X", out st) && st.state != ButtonStateEnum.NONE) return true;
            if (btn.TryGetValue("Y", out st) && st.state != ButtonStateEnum.NONE) return true;
            return false;
        }

        // ひらがなのキーマップを初期化する
        public void initKanaAssignMap() {
            // ※ビット列定義(大事なのことなので2回言いました)
            // XX-XXX-XXX-X-X: 左から、Lステ十字判定、方向、ボタンの識別子、濁点、拗音フラグ(10bit)
            kanaAssignMap = new Dictionary<uint, string>() {
                {0b_10_000_001_00, "あ"},
                {0b_10_000_010_00, "い"},
                {0b_10_000_011_00, "う"},
                {0b_10_000_100_00, "え"},
                {0b_10_000_101_00, "お"},
                {0b_10_000_001_01, "ぁ"},
                {0b_10_000_010_01, "ぃ"},
                {0b_10_000_011_01, "ぅ"},
                {0b_10_000_100_01, "ぇ"},
                {0b_10_000_101_01, "ぉ"},
                {0b_10_001_001_00, "か"},
                {0b_10_001_010_00, "き"},
                {0b_10_001_011_00, "く"},
                {0b_10_001_100_00, "け"},
                {0b_10_001_101_00, "こ"},
                {0b_10_001_001_10, "が"},
                {0b_10_001_010_10, "ぎ"},
                {0b_10_001_011_10, "ぐ"},
                {0b_10_001_100_10, "げ"},
                {0b_10_001_101_10, "ご"},
                {0b_10_010_001_00, "さ"},
                {0b_10_010_010_00, "し"},
                {0b_10_010_011_00, "す"},
                {0b_10_010_100_00, "せ"},
                {0b_10_010_101_00, "そ"},
                {0b_10_010_001_10, "ざ"},
                {0b_10_010_010_10, "じ"},
                {0b_10_010_011_10, "ず"},
                {0b_10_010_100_10, "ぜ"},
                {0b_10_010_101_10, "ぞ"},
                {0b_10_011_001_00, "た"},
                {0b_10_011_010_00, "ち"},
                {0b_10_011_011_00, "つ"},
                {0b_10_011_100_00, "て"},
                {0b_10_011_101_00, "と"},
                {0b_10_011_001_10, "だ"},
                {0b_10_011_010_10, "ぢ"},
                {0b_10_011_011_10, "づ"},
                {0b_10_011_100_10, "で"},
                {0b_10_011_101_10, "ど"},
                {0b_10_011_011_01, "っ"},
                {0b_10_100_001_00, "な"},
                {0b_10_100_010_00, "に"},
                {0b_10_100_011_00, "ぬ"},
                {0b_10_100_100_00, "ね"},
                {0b_10_100_101_00, "の"},
                {0b_10_101_001_00, "は"},
                {0b_10_101_010_00, "ひ"},
                {0b_10_101_011_00, "ふ"},
                {0b_10_101_100_00, "へ"},
                {0b_10_101_101_00, "ほ"},
                {0b_10_101_001_10, "ば"},
                {0b_10_101_010_10, "び"},
                {0b_10_101_011_10, "ぶ"},
                {0b_10_101_100_10, "べ"},
                {0b_10_101_101_10, "ぼ"},
                {0b_10_101_001_01, "ぱ"},
                {0b_10_101_010_01, "ぴ"},
                {0b_10_101_011_01, "ぷ"},
                {0b_10_101_100_01, "ぺ"},
                {0b_10_101_101_01, "ぽ"},
                {0b_10_110_001_00, "ま"},
                {0b_10_110_010_00, "み"},
                {0b_10_110_011_00, "む"},
                {0b_10_110_100_00, "め"},
                {0b_10_110_101_00, "も"},
                {0b_11_000_001_00, "や"},
                {0b_11_000_010_00, "（"},
                {0b_11_000_011_00, "ゆ"},
                {0b_11_000_100_00, "）"},
                {0b_11_000_101_00, "よ"},
                {0b_11_000_001_01, "ゃ"},
                {0b_11_000_011_01, "ゅ"},
                {0b_11_000_101_01, "ょ"},
                {0b_10_111_001_00, "ら"},
                {0b_10_111_010_00, "り"},
                {0b_10_111_011_00, "る"},
                {0b_10_111_100_00, "れ"},
                {0b_10_111_101_00, "ろ"},
                {0b_11_100_001_00, "わ"},
                {0b_11_100_010_00, "を"},
                {0b_11_100_011_00, "ん"},
                {0b_11_100_100_00, "ー"},
                {0b_11_100_101_00, "～"},
                // ここから記号/数字
                {0b_11_110_001_00, "、"},
                {0b_11_110_010_00, "。"},
                {0b_11_110_011_00, "？"},
                {0b_11_110_100_00, "！"},
                {0b_11_110_101_00, "…"},
                {0b_11_110_001_10, "・"},
                {0b_11_110_010_10, "「"},
                {0b_11_110_011_10, "，"},
                {0b_11_110_100_10, "」"},
                {0b_11_110_101_10, "．"},
                {0b_11_010_001_00, "0"},
                {0b_11_010_010_00, "1"},
                {0b_11_010_011_00, "2"},
                {0b_11_010_100_00, "3"},
                {0b_11_010_101_00, "4"},
                {0b_11_010_001_10, "5"},
                {0b_11_010_010_10, "6"},
                {0b_11_010_011_10, "7"},
                {0b_11_010_100_10, "8"},
                {0b_11_010_101_10, "9"},
                // ここからは制御系キー
                {0b_00_000_001_00, " "},        // R: スペース/変換
                {0b_00_000_100_00, "{ENTER}"},  // A: エンター
                {0b_00_000_101_00, "{BS}"},     // B: BACKSPACE
                {0b_00_000_001_10, "+ "},       // ZL+R: 変換逆送り
                {0b_00_000_100_10, "{RIGHT}"},  // ZL+A: →
                {0b_00_000_101_10, "{DOWN}"},   // ZL+B: ↓
                {0b_00_000_011_10, "{UP}"},     // ZL+X: ↑
                {0b_00_000_010_10, "{LEFT}"},   // ZL+Y: ←
            };
        }

        // 英字のキーマップを初期化する
        public void initAlphAssignMap() {
            alphAssignMap = new Dictionary<uint, string>() {
                {0b_10_000_001_00, "a"},
                {0b_10_000_010_00, "i"},
                {0b_10_000_011_00, "u"},
                {0b_10_000_100_00, "e"},
                {0b_10_000_101_00, "o"},
                {0b_10_000_001_01, "A"},
                {0b_10_000_010_01, "I"},
                {0b_10_000_011_01, "U"},
                {0b_10_000_100_01, "E"},
                {0b_10_000_101_01, "O"},
                {0b_10_001_001_00, "k"},
                {0b_10_001_011_00, "q"},
                {0b_10_001_101_00, "g"},
                {0b_10_001_001_10, "K"},
                {0b_10_001_011_10, "Q"},
                {0b_10_001_101_10, "G"},
                {0b_10_010_001_00, "s"},
                {0b_10_010_011_00, "z"},
                {0b_10_010_101_00, "j"},
                {0b_10_010_001_10, "S"},
                {0b_10_010_011_10, "Z"},
                {0b_10_010_101_10, "J"},
                {0b_10_011_001_00, "t"},
                {0b_10_011_011_00, "c"},
                {0b_10_011_101_00, "d"},
                {0b_10_011_001_10, "T"},
                {0b_10_011_011_10, "C"},
                {0b_10_011_101_10, "D"},
                {0b_10_011_011_01, "C"},
                {0b_10_100_001_00, "n"},
                {0b_10_100_001_10, "N"},
                {0b_10_101_001_00, "h"},
                {0b_10_101_010_00, "f"},
                {0b_10_101_011_00, "b"},
                {0b_10_101_100_00, "p"},
                {0b_10_101_101_00, "v"},
                {0b_10_101_001_10, "H"},
                {0b_10_101_010_10, "F"},
                {0b_10_101_011_10, "B"},
                {0b_10_101_100_10, "P"},
                {0b_10_101_101_10, "V"},
                {0b_10_110_001_00, "m"},
                {0b_10_110_001_10, "M"},
                {0b_11_000_001_00, "y"},
                {0b_11_000_010_00, "{(}"},
                {0b_11_000_100_00, "{)}"},
                {0b_11_000_001_01, "Y"},
                {0b_10_111_001_00, "r"},
                {0b_10_111_101_00, "l"},
                {0b_10_111_001_10, "R"},
                {0b_10_111_101_10, "L"},
                {0b_11_100_001_00, "w"},
                {0b_11_100_011_00, "x"},
                {0b_11_100_100_00, "-"},
                {0b_11_100_001_10, "W"},
                {0b_11_100_011_10, "X"},
                // ここから記号/数字
                {0b_11_110_001_00, ","},
                {0b_11_110_010_00, "."},
                {0b_11_110_011_00, "?"},
                {0b_11_110_100_00, "!"},
                {0b_11_110_101_00, "@"},
                {0b_11_110_001_10, "#"},
                {0b_11_110_010_10, "$"},
                {0b_11_110_011_10, "%"},
                {0b_11_110_100_10, "*"},
                {0b_11_110_101_10, "&"},
                {0b_11_010_001_00, "0"},
                {0b_11_010_010_00, "1"},
                {0b_11_010_011_00, "2"},
                {0b_11_010_100_00, "3"},
                {0b_11_010_101_00, "4"},
                {0b_11_010_001_10, "5"},
                {0b_11_010_010_10, "6"},
                {0b_11_010_011_10, "7"},
                {0b_11_010_100_10, "8"},
                {0b_11_010_101_10, "9"},
                // ここからは制御系キー
                {0b_00_000_001_00, " "},        // R: スペース/変換
                {0b_00_000_100_00, "{ENTER}"},  // A: エンター
                {0b_00_000_101_00, "{BS}"},     // B: BACKSPACE
                {0b_00_000_001_10, "+ "},       // ZL+R: 変換逆送り
                {0b_00_000_100_10, "{RIGHT}"},  // ZL+A: →
                {0b_00_000_101_10, "{DOWN}"},   // ZL+B: ↓
                {0b_00_000_011_10, "{UP}"},     // ZL+X: ↑
                {0b_00_000_010_10, "{LEFT}"},   // ZL+Y: ←
            };
        }
    }
}
