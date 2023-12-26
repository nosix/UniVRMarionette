# VrmForceGeneratorDemo

## 使い方

1. Assets/StreamingAssets フォルダに VRM ファイルを置く
2. VRM Model GameObject の設定を行う
   - VrmLoader Component
      - Vrm File Name に VRM ファイルのファイル名を指定する
      - (Option) Force Fields を編集して身体部位に割り当てられた CapsuleCollider の位置や大きさを変更する
3. Play
4. TestXxxCollision GameObject の Transform Samples Component を操作する
   1. Slider で Element のインデックスを指定する
   2. Apply ボタンを押下する
      - Sphere が衝突して関節が曲がる (関節が曲がらない場合は以下を試す)
   3. Reset ボタンを押下する
      - Sphere の位置が原点に戻る 
   4. VRM Model GameObject の VrmControlRigMixer Component の Reset を押下する
      - モデルのポーズが戻る
   5. 再び Apply ボタンを押下する
5. Sphere GameObject の VrmForceSource Component を操作する
   1. Hold を ON にする
   2. Sphere の Position を変更して身体に接触させる
      - Sphere の移動に合わせて身体部位が動く
   3. Sphere の Rotation を変更する
      - Hips, Hand, Foot では回転に合わせて身体部位が回転する
      - 他の身体部位では変化がない
   4. Hold を OFF にする
      - Sphere の移動に追従しなくなる
