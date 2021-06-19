namespace AnimalFlicker.GamepadInterface {
    // ボタンの押している状態を表すEnum
    public enum ButtonStateEnum {
        NONE,           // 何も押していない状態
        PRESS,          // 押した瞬間
        HOLD,           // 押しっぱなし
        FAST_RELEASE,   // 長押し判定前にボタンを離す
        KEEP_HOLD,      // 長押し判定が発生した瞬間
        RELEASE         // 離した瞬間
    }

    // 方向を示すEnum
    public enum DirectionEnum {
        RELEASE     = 0b_1111, // スティック、十字キーから指を離した状態
        UP          = 0b_0000,
        UP_RIGHT    = 0b_0001,
        RIGHT       = 0b_0010,
        DW_RIGHT    = 0b_0011,
        DOWN        = 0b_0100,
        DW_LEFT     = 0b_0101,
        LEFT        = 0b_0110,
        UP_LEFT     = 0b_0111
    }

    // 方向入力デバイスの種類
    public enum DirectionInputEnum {
        LEFT_STICK   = 0b_0000,     // 左スティック
        RIGHT_STICK  = 0b_0010,    // 右スティック
        POV          = 0b_0001    // 十字キー
    }
}