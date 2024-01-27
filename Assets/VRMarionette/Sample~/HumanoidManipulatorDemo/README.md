# HumanoidManipulatorDemo

## 使い方

1. Assets/StreamingAssets フォルダに VRM ファイルを置く
2. VRM Model GameObject の設定を行う
   - VrmLoader Component
      - Vrm File Name に VRM ファイルのファイル名を指定する
      - (Option) Human Limits を編集して関節の角度の制約を変更する
3. BoneRotationTest GameObject の設定を行う
   - (Option) Run Test を ON にする
4. Play
5. VRM Model GameObject を操作する
   - HumanoidMixer で各関節の角度を変更する
      - 関節の角度は Human Limits で制約される