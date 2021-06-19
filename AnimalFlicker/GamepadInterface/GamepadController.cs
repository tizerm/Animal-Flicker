using SharpDX;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AnimalFlicker.GamepadInterface {
    // ゲームパッド統合クラス
    public class GamepadController {
        public static int FRAME_SEC { get; } = 16;

        // アナログスティック描画時の中心と半径
        private static int DRAW_ANALOG_X = 80;
        private static int DRAW_ANALOG_Y = 100;
        private static int DRAW_ANALOG_R = 30;
        private static int DRAW_POV_X = 150;
        private static int DRAW_POV_Y = 180;
        private static int DRAW_BUTTON_X = 260;
        private static int DRAW_BUTTON_Y = 120;
        private static int DRAW_BUTTON_R = 35;

        private Joystick js;
        private Guid instanceId;
        // ゲームパッドが有効かどうかの判定
        public bool connection { get; set; }
        // 各ボタンの状態保存(連想配列)
        public Dictionary<string, GamepadButtonState> btn { get; }
        // 左スティックの状態
        private GamepadDirectionState lAnalog;
        // 右スティックの状態
        private GamepadDirectionState rAnalog;
        // 十字キーの状態
        private GamepadDirectionState pov;

        // コンストラクタ
        public GamepadController() {
            // コントローラの配置初期化
            btn = new Dictionary<string, GamepadButtonState>() {
                {"A", new GamepadButtonState(1, true)},
                {"B", new GamepadButtonState(0, true)},
                {"X", new GamepadButtonState(3, true)},
                {"Y", new GamepadButtonState(2, true)},
                {"L", new GamepadButtonState(4, false)},
                {"R", new GamepadButtonState(5, true)},
                {"ZL", new GamepadButtonState(6, false)},
                {"ZR", new GamepadButtonState(7, false)},
                {"LS", new GamepadButtonState(10, true)},
                {"RS", new GamepadButtonState(11, true)},
                {"STA", new GamepadButtonState(9, true)},
                {"SEL", new GamepadButtonState(8, true)}
            };
            lAnalog = new GamepadDirectionState(DirectionInputEnum.LEFT_STICK);
            rAnalog = new GamepadDirectionState(DirectionInputEnum.RIGHT_STICK);
            pov = new GamepadDirectionState(DirectionInputEnum.POV);
            initialize();
        }

        //　ゲームパッドの初期化/再検出
        public void initialize() {
            // インスタンスIDを初期化
            DirectInput dinput = new DirectInput();
            instanceId = Guid.Empty;
            connection = false;
            // ゲームパッドから検出
            if (instanceId == Guid.Empty) {
                foreach (DeviceInstance device in dinput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices)) {
                    instanceId = device.InstanceGuid;
                    break;
                }
            }
            // ジョイスティックから検出
            if (instanceId == Guid.Empty) {
                foreach (DeviceInstance device in dinput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices)) {
                    instanceId = device.InstanceGuid;
                    break;
                }
            }
            // 検出できたらインスタンスIDが振られるのでジョイスティックを初期化
            if (instanceId != Guid.Empty) {
                // ジョイスティック初期化
                js = new Joystick(dinput, instanceId);
                if (js != null) {
                    // バッファサイズ設定
                    js.Properties.BufferSize = 128;
                    // 接続を確認
                    connection = true;
                }
            }
        }

        // 入力受付処理(ラムダで処理を記述)
        // @return 正常に完了した場合true, なんらか問題がある場合はfalse
        public bool getGamepadResponse(Action<Dictionary<string, GamepadButtonState>,
            GamepadDirectionState, GamepadDirectionState, GamepadDirectionState> action) {
            // ゲームパッドが初期化されてない場合はエラー
            if (js == null) return false;

            // パッドの接続が切れた時用にtry-catchで囲む
            try {
                // レスポンスを取得するデバイスの取得
                js.Acquire();
                js.Poll();

                // パッドのステートを取得
                JoystickState state = js.GetCurrentState();
                // 取得できない場合はエラー
                if (state == null) return false;

                // ボタンとスティックの状態を取得してからラムダの処理を実行
                updGamepadState(state);
                action(btn, lAnalog, rAnalog, pov);
            } catch (SharpDXException e) {
                // パッドの接続が途中で切れるとここに飛ぶ
                // パッドのインスタンスを無効化して終了
                js = null;
                return false;
            }

            return true;
        }

        // ボタンとスティックの現時点のフレームの状態を取得
        private void updGamepadState(JoystickState st) {
            // ボタンの状態を更新
            foreach (KeyValuePair<string, GamepadButtonState> e in btn) e.Value.updButtonState(st);
            lAnalog.updDirectionState(st);
            rAnalog.updDirectionState(st);
            pov.updDirectionState(st);
        }

        // スティックの状態を描画する
        public void drawAnalogState(PaintEventArgs e) {
            Pen p1 = new Pen(Color.Black, 1);
            SolidBrush p2 = new SolidBrush(Color.Red);
            SolidBrush p3 = new SolidBrush(Color.Gray);

            // 左スティックの状態を表す円を描写
            e.Graphics.DrawEllipse(p1,
                DRAW_ANALOG_X - DRAW_ANALOG_R, DRAW_ANALOG_Y - DRAW_ANALOG_R,
                DRAW_ANALOG_R * 2, DRAW_ANALOG_R * 2);
            e.Graphics.FillEllipse(p2,
                DRAW_ANALOG_X - 3 + lAnalog.getCompressX(DRAW_ANALOG_R),
                DRAW_ANALOG_Y - 3 + lAnalog.getCompressY(DRAW_ANALOG_R), 6, 6);

            // 十字キー
            e.Graphics.FillRectangle(p3, DRAW_POV_X - 8, DRAW_POV_Y - DRAW_ANALOG_R, 16, DRAW_ANALOG_R * 2);
            e.Graphics.FillRectangle(p3, DRAW_POV_X - DRAW_ANALOG_R, DRAW_POV_Y - 8, DRAW_ANALOG_R * 2, 16);

            // ボタン
            e.Graphics.FillEllipse(p3, DRAW_BUTTON_X - DRAW_BUTTON_R, DRAW_BUTTON_Y, 28, 28);
            e.Graphics.FillEllipse(p3, DRAW_BUTTON_X + DRAW_BUTTON_R, DRAW_BUTTON_Y, 28, 28);
            e.Graphics.FillEllipse(p3, DRAW_BUTTON_X, DRAW_BUTTON_Y - DRAW_BUTTON_R, 28, 28);
            e.Graphics.FillEllipse(p3, DRAW_BUTTON_X, DRAW_BUTTON_Y + DRAW_BUTTON_R, 28, 28);

        }

        // 接続状態をラベルに貼り付ける
        public void setLabelConnectionState(Label label) {
            label.Text = connection ? "接続中." : "接続が切れています！";
        }
    }
}
