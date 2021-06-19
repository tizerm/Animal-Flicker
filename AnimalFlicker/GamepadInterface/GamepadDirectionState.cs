using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AnimalFlicker.GamepadInterface {
    // スティックと十字キーの状態を記述するクラス
    public class GamepadDirectionState {
        // スティックをデッドゾーン(単位:%)
        private static int DEAD_ZONE { get; set; } = 55;
        // 長押し判定時間(単位:msec)
        private static int HOLD_TIME_MSEC { get; set; } = 250;
        // インプットデバイスの種類
        private DirectionInputEnum inputDevice;

        // X座標とY座標
        private int x;
        private int y;

        // 角度
        private double deg;
        // スティックの中心からの距離
        private double rds;
        // 倒しっぱなしにした時間を計測
        private Stopwatch holdTimer;
        // 長押し判定が発生したか
        private bool fireKeepEv;
        // 方向判定
        public DirectionEnum direction { get; private set; }
        // ボタン判定
        public ButtonStateEnum state { get; private set;  }

        // コンストラクタ
        public GamepadDirectionState(DirectionInputEnum di) {
            x = 0;
            y = 0;
            deg = 0;
            rds = 0;
            inputDevice = di;
            holdTimer = new Stopwatch();
            fireKeepEv = false;
            direction = DirectionEnum.RELEASE;
            state = ButtonStateEnum.NONE;
        }

        // スティック/十字キーの状態をアップデートする
        public void updDirectionState(JoystickState st) {
            switch (inputDevice) {
                case DirectionInputEnum.LEFT_STICK:
                    // 左スティック
                    updAnalogDirection(st.X, st.Y);
                    break;
                case DirectionInputEnum.RIGHT_STICK:
                    // 右スティック
                    updAnalogDirection(st.RotationX, st.RotationY);
                    break;
                case DirectionInputEnum.POV:
                    // 十字キー
                    updPOVDirection(st.PointOfViewControllers[0]);
                    break;
            }
        }

        // アナログスティックの方向情報を更新
        private void updAnalogDirection(int sX, int sY) {
            // 正規化
            x = sX - 32768;
            y = 32768 - sY;

            // 角度と距離を計算
            deg = 180 * Math.Atan2(x, y) / Math.PI;
            rds = Math.Sqrt(x * x + y * y) / 327.68;

            // 角度と距離から方向Enumを決定する
            if (rds < DEAD_ZONE) {
                // デッドゾーンより内側は指を離した判定
                // 直前までホールドしてた場合はリリース判定を出す
                if (state == ButtonStateEnum.HOLD)
                    state = holdTimer.ElapsedMilliseconds <= HOLD_TIME_MSEC
                        ? ButtonStateEnum.FAST_RELEASE : ButtonStateEnum.RELEASE;
                else {
                    // リリース判定が出ていた場合は方向と入力をフリーに
                    state = ButtonStateEnum.NONE;
                    direction = DirectionEnum.RELEASE;
                }
                // タイマーリセット
                holdTimer.Stop();
                fireKeepEv = false;
            } else {
                // 方向入力とホールド判定を付与(直前まで倒してなかったらタイマー開始)
                if (state != ButtonStateEnum.HOLD && state != ButtonStateEnum.KEEP_HOLD) holdTimer.Restart();
                if (!fireKeepEv && holdTimer.ElapsedMilliseconds > HOLD_TIME_MSEC) {
                    // 長押し時間を過ぎたら一回だけイベント発生
                    fireKeepEv = true;
                    state =  ButtonStateEnum.KEEP_HOLD;
                } else state = ButtonStateEnum.HOLD;

                if      (-22.5 < deg && deg <= 22.5)    direction = DirectionEnum.UP;
                else if (22.5 < deg && deg <= 67.5)     direction = DirectionEnum.UP_RIGHT;
                else if (67.5 < deg && deg <= 112.5)    direction = DirectionEnum.RIGHT;
                else if (112.5 < deg && deg <= 157.5)   direction = DirectionEnum.DW_RIGHT;
                else if (157.5 < deg || deg <= -157.5)  direction = DirectionEnum.DOWN;
                else if (-157.5 < deg && deg <= -112.5) direction = DirectionEnum.DW_LEFT;
                else if (-112.5 < deg && deg <= -67.5)  direction = DirectionEnum.LEFT;
                else if (-67.5 < deg && deg <= -22.5)   direction = DirectionEnum.UP_LEFT;
            }
        }

        // POV入力を方向Enumに変換する
        private void updPOVDirection(int pov) {
            // ナナメ入力は上下に吸収する
            if (pov == 0)           direction = DirectionEnum.UP;
            else if (pov == 4500)   direction = DirectionEnum.UP;
            else if (pov == 9000)   direction = DirectionEnum.RIGHT;
            else if (pov == 13500)  direction = DirectionEnum.DOWN;
            else if (pov == 18000)  direction = DirectionEnum.DOWN;
            else if (pov == 22500)  direction = DirectionEnum.DOWN;
            else if (pov == 27000)  direction = DirectionEnum.LEFT;
            else if (pov == 31500)  direction = DirectionEnum.UP;
            else {
                // 直前までホールドしてた場合はリリース判定を出す
                if (state == ButtonStateEnum.HOLD)
                    state = holdTimer.ElapsedMilliseconds <= HOLD_TIME_MSEC
                        ? ButtonStateEnum.FAST_RELEASE : ButtonStateEnum.RELEASE;
                else {
                    // リリース判定が出ていた場合は方向と入力をフリーに
                    state = ButtonStateEnum.NONE;
                    direction = DirectionEnum.RELEASE;
                }
                // タイマーリセット
                holdTimer.Stop();
                fireKeepEv = false;
                return;
            }
            // 直前まで倒してなかったらタイマー開始
            if (state != ButtonStateEnum.HOLD && state != ButtonStateEnum.KEEP_HOLD) holdTimer.Restart();
            if (!fireKeepEv && holdTimer.ElapsedMilliseconds > HOLD_TIME_MSEC) {
                // 長押し時間を過ぎたら一回だけイベント発生
                fireKeepEv = true;
                state = ButtonStateEnum.KEEP_HOLD;
            } else state = ButtonStateEnum.HOLD;
        }

        // 状態をラベルに貼り付ける
        public void setLabelDirectionState(Label label) {
            label.Text = toStringDir() + ", " + "deg: " + deg + ", rds: " + rds;
        }

        public int getCompressX(int rate) {
            return rate * x / 32768;
        }
        public int getCompressY(int rate) {
            return rate * -y / 32768;
        }

        // 方向を文字にする
        public string toStringDir() {
            switch (direction) {
                case DirectionEnum.UP:
                    return "↑";
                case DirectionEnum.UP_RIGHT:
                    return "↗";
                case DirectionEnum.RIGHT:
                    return "→";
                case DirectionEnum.DW_RIGHT:
                    return "↘";
                case DirectionEnum.DOWN:
                    return "↓";
                case DirectionEnum.DW_LEFT:
                    return "↙";
                case DirectionEnum.LEFT:
                    return "←";
                case DirectionEnum.UP_LEFT:
                    return "↖";
                default:
                    return "○";
            }
        } 
    }
}
