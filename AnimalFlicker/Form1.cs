//using Vortice.XInput;
using AnimalFlicker.GamepadInterface;
using AnimalFlicker.InputMethodInterface;
using SharpDX;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace AnimalFlicker {
    public partial class Form1 : Form {
        // 入力を受け付けるタイマー
        private Timer inputTimer;
        // 定期的に初期化処理を走らせるタイマー
        private Timer initTimer;
        // ゲームパッドインスタンス
        private GamepadController gpc;
        // InputMethodインスタンス
        private InputMethodConfig imc;

        public Form1() {
            InitializeComponent();
            // ゲームパッド初期化
            gpc = new GamepadController();
            gpc.setLabelConnectionState(connectionLabel);

            // 入力設定を初期化
            imc = new InputMethodConfig();

            // 入力タイマー設定(60FPSで稼働)
            inputTimer = new Timer() {
                Interval = GamepadController.FRAME_SEC,
                Enabled = true,
            };
            // 初期化タイマー設定(2秒毎に再検出を走らせる)
            initTimer = new Timer() {
                Interval = 2000,
                Enabled = true,
            };

            this.DoubleBuffered = true;

            // 入力タイマーイベント
            inputTimer.Tick += (sender, e) => {
                // ゲームパッドの入力記述
                gpc.connection = gpc.getGamepadResponse((btn, ls, rs, pov) => {
                    // ゲームパッドの入力から入力キーを取得
                    string input = imc.getInputStr(btn, ls, pov);
                    if (input != null) {
                        inputLabel.Text = "input: " + input;
                        SendKeys.Send(input);
                    }
                });
                // 再描画
                this.Invalidate();
            };
            // 初期化タイマーイベント
            initTimer.Tick += (sender, e) => {
                // 接続が切れてたら初期化する
                if (!gpc.connection) gpc.initialize();
                gpc.setLabelConnectionState(connectionLabel);
            };
        }

        // アナログスティックの状態を画面に描画
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            gpc.drawAnalogState(e);
        }
    }
}