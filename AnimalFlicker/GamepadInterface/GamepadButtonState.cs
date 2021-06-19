using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnimalFlicker.GamepadInterface {
    // ボタンの状態を記述するクラス
    public class GamepadButtonState {
        // 長押し判定時間(単位:msec)
        private static int HOLD_TIME_MSEC { get; set; } = 250;
        // ボタンのID(ゲームパッド上でのボタン番号-1)
        public int id { get; set; }
        // 長押し判定を許可する
        private bool permitHold;
        // 長押し判定が発生したか
        private bool fireKeepEv;
        // 押しっぱなしにした時間を計測
        private Stopwatch holdTimer;
        // ボタン判定
        public ButtonStateEnum state { get; private set; }

        // コンストラクタ
        public GamepadButtonState(int buttonId, bool holdFlg) {
            id = buttonId;
            permitHold = holdFlg;
            holdTimer = new Stopwatch();
            fireKeepEv = false;
            state = ButtonStateEnum.NONE;
        }

        public void updButtonState(JoystickState st) {
            if (st.Buttons[id]) {
                // 押されている場合ホールド時間加算
                switch (state) {
                    case ButtonStateEnum.NONE:
                        // 直前まで押されていなければ押したことにする(タイマースタート)
                        state = ButtonStateEnum.PRESS;
                        holdTimer.Restart();
                        break;
                    case ButtonStateEnum.PRESS:
                    case ButtonStateEnum.KEEP_HOLD:
                        // 押した判定が発生してたらホールド状態にする(ホールド時間加算)
                        state = ButtonStateEnum.HOLD;
                        break;
                    case ButtonStateEnum.HOLD:
                        if (permitHold && !fireKeepEv && holdTimer.ElapsedMilliseconds > HOLD_TIME_MSEC) {
                            // 長押し判定発生前に長押し判定時間を超えたら長押しイベント発生
                            state = ButtonStateEnum.KEEP_HOLD;
                            fireKeepEv = true;
                        }

                        break;
                }
            } else {
                // ボタンが離された時
                switch (state) {
                    case ButtonStateEnum.PRESS:
                    case ButtonStateEnum.HOLD:
                    case ButtonStateEnum.KEEP_HOLD:
                        // 押してたときはリリース判定にする
                        // 長押し判定時間よりも先にボタンを離したら早く離した判定にする
                        state = permitHold && holdTimer.ElapsedMilliseconds <= HOLD_TIME_MSEC
                            ? ButtonStateEnum.FAST_RELEASE : ButtonStateEnum.RELEASE;
                        break;
                    default:
                        // 押してなかったら押してない状態に戻す
                        state = ButtonStateEnum.NONE;
                        break;
                }
                // タイマーストップ
                fireKeepEv = false;
                holdTimer.Stop();
            }
        }
    }
}
