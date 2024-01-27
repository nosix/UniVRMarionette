# GravityApplierDemo

このデモは実験中です。

## 使い方

1. Assets/StreamingAssets フォルダに VRM ファイルを置く
2. VRM Model GameObject の設定を行う
   - VrmLoader Component
      - Vrm File Name に VRM ファイルのファイル名を指定する
      - (Option) Body Weights を編集して身体部位に割り当てられた重さを変更する
3. Play
4. XxxRotationSamples GameObject の Rotation Samples Component を操作する
   1. Slider で Element のインデックスを指定する
   2. Apply ボタンを押下する
      - 下半身の部位の関節が曲がり、姿勢によってはバランスを崩して倒れる
   3. Reset ボタンを押下する
      - 元の姿勢と位置に戻る 
5. VRM Model GameObject の HumanoidMixer Component を操作する
   - 姿勢を変更して影響を確認する
