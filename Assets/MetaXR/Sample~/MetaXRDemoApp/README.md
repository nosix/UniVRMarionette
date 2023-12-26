# MetaXRDemoApp

## 使い方

1. Build Settings
   - Scenes in Build : MetaXRDemoApp
   - Platform : Android
2. Build And Run

## 参考 - アプリ構築手順

1. [Meta XR All-in-One SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-all-in-one-sdk-269657)
  をマイアセットに追加する
2. Package Manager で `Packages: My Assets` を選択する
   1. Meta XR All-in-One SDK をインストールする
3. Oculus > Tools > Project Setup Tool
   1. 全てのタブで Fix All と Apply All を行う
4. Project Settings > XR Plug-in Management > Oculus
   - Target Devices を選択する
5. Scene を作成する
   1. Main Camera を削除する
   2. Oculus > Tools > Building Blocks
      - Camera Rig
      - Hand Tracking
      - Controller Tracking
      - (Option) Passthrough
   3. Camera Rig を編集する
      - OVR Manager
         - Target Devices を選択する
   4. MetaXRSupport パッケージの VrmMarionetteHand prefab を追加する
      - 左手(LeftVrmMarionetteHand)
      - 右手(RightVrmMarionetteHand)
   5. Capsule パッケージの Capsule prefab を追加する
      - 左手(LeftFocusIndicator)
      - 右手(RightFocusIndicator)
   6. Game Object を作成し VRM Model と命名する
      1. VrmLoader Script を追加する
      2. Vrm File Name を設定する
         - Assets/StreamingAssets に置いたファイルの名前を指定する
      3. Human Limits を設定する
         - VRMarionette/Runtime/ScriptableObjects/HumanLimitContainer
      4. Force Fields を設定する
         - VRMarionette/Runtime/ScriptableObjects/ForceFieldContainer
      5. Spring Bone Collider Groups を設定する
         - LeftVrmMarionetteHand
         - RightVrmMarionetteHand
      6. Force Sources を設定する
         - LeftVrmMarionetteHand の Palm
         - RightVrmMarionetteHand の Palm
   7. VrmMarionetteHand Component を編集する
      - Hand に [Building Block] Hand Tracking を指定する 
      - Controller に [Building Block] Controller Tracking を指定する
   8. VrmMarionetteHand の Palm に FocusIndicator を追加する
      - Capsule に FocusIndicator の Capsule を指定する
   9. Game Object を作成し GrabSupport と命名する
      - GrabSupport Script を追加する
         - Left Hand と Right Hand を設定する