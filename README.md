# Animal-Flicker
ゲームパッドで文字入力するやつ(C#)

初代どうぶつの森とフリック入力から着想を得てゲームパッドから文字入力を可能にする.NETのアプリケーションです。<br>
とりあえずたたき台として機能を一通り作ってからpushしました。<br>
適当に落として遊んでみてください。<br>
(なんかこんなのいいんじゃない？って勝手に直したらブランチきってね)<br>

<h2>基本的な操作方法</h2>

※Nintendo Switchのプロコンの配置で説明します(動作確認もスイッチのプロコンでやりました)。

左スティック: 8方向で子音を決定(ア/カ/サ/タ/ナ/ハ/マ/ラ)<br>
右のボタンを押さずに左スティックを戻す: ア段を確定<br>
右のボタンを押さずに左スティックを一定時間倒す: 濁点のある子音なら濁点を、拗音のある子音なら拗音でア段を確定<br>
ZLボタン: ホールドで濁点<br>
Lボタン: ホールドで半濁点/拗音<br>
十字キー↑: 子音決定-ヤ行(即離して「ヤ」、押しっぱなしで「ャ」)<br>
十字キー↓: 子音決定-ワ行<br>
十字キー←: 記号モード<br>
十字キー→: 数字モード<br>
Aボタン: 母音決定-エ段(長押しで濁点/拗音)/子音決定していなければエンター/ZLホールドで→キー<br>
Bボタン: 母音決定-オ段(長押しで濁点/拗音)/子音決定していなければバックスペース/ZLホールドで↓キー<br>
Xボタン: 母音決定-ウ段(長押しで濁点/拗音)/ZLホールドで↑キー<br>
Yボタン: 母音決定-イ段(長押しで濁点/拗音)/ZLホールドで←キー<br>
Rボタン: 子音決定していなければスペース(変換)/ZLホールドで変換逆送り<br>

※あまり早く打つと反応しないことがあります。
