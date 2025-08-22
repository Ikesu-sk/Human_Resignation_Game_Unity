# Human_Resignation_Game_Unityfeature/2025-08-23
2025年8月23日時点でのunityをベースとしたヒト辞任ゲームの進捗

## このデータの進捗状況
- チュートリアル部分の完成

L 会話内容を最後まで作った。

Ｌ 会話の途中でyolo認識を開始させる仕組みを作った。人を認識したらその認識した瞬間のカメラ画面で停止するようになっている。

- Enterで全文表示機能の改善

L 前回の仕組みだとEnterを２回押しても反応しなかったので、8frameごとに１文字表示されるようにし、そのframeが増え続けているタイミングでEnterキーが押されたら、そのEnterキーを認識されるように改善。前回のだと0.1秒ごとに1回しかEnterを認識しない仕組みだった（のかもしれない）

## インストール方法
```bash
git clone https://github.com/Ikesu-sk/Human_Resignation_Game_Unity.git
```
### 使い方（How to Use）
インストールしたディレクトリ`Human_Resignation_Game_Unity`をunity hubで開く。
その後、Assets > Scenes > Main を開けば参考動画と同じような画面が出てくる。
