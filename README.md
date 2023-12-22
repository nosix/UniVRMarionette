# UniVRMarionette

## 導入方法

### Meta Quest 3 で使う

#### 注意

接触した部位をフォーカス表示する機能が不要な場合は以下のステップは無視してください。

- `com.github.nosix.vrm.util` への依存追加
- (Option1) と書かれたステップ

完成した Scene の例は Assets/MetaXR_Samples/MetaXRDemoApp を参照してください。

#### 手順

1. Packages/manifest.json に以下の依存を追加する
    ```
    "com.github.nosix.vrm.marionette": "https://github.com/nosix/UniVRMarionette.git?path=/Assets/VRMarionette#v0.1.3",
    "com.github.nosix.vrm.meta.xr": "https://github.com/nosix/UniVRMarionette.git?path=/Assets/MetaXR#v0.1.3",
    "com.github.nosix.vrm.util": "https://github.com/nosix/UniVRMarionette.git?path=/Assets/Util#v0.1.3",
    "com.vrmc.gltf": "https://github.com/vrm-c/UniVRM.git?path=/Assets/UniGLTF#v0.115.0",
    "com.vrmc.vrm": "https://github.com/vrm-c/UniVRM.git?path=/Assets/VRM10#v0.115.0",
    "com.vrmc.vrmshaders": "https://github.com/vrm-c/UniVRM.git?path=/Assets/VRMShaders#v0.115.0",
    ```
   `com.github.nosix.vrm.meta.xr` は `com.meta.xr.sdk.all` に依存するので、
   Meta XR All-in-One SDK もダウンロードされます。 
2. メニューから Oculus > Tools > Project Setup Tool と選択し、以下を行う
   - 全て Fix All
   - 全て Apply All
3. Project Settings > XR Plug-in Management > Oculus > Android を選択し、以下を行う
   - Target Devices を設定する
4. Scene を作成する
   1. メニューから Oculus > Tools > Building Blocks と選択し、以下を追加する
      - Camera Rig
      - Hand Tracking
   2. Packages/VRMarionette MetaXR/Runtime/VrmMarionetteHand prefab を 2 つ追加する
      - それぞれ Left, Right 用
      - 以降 VrmMarionetteHand と呼ぶ
   3. VrmMarionetteHand の Skeleton に以下を設定する
      - Hand Tracking left
      - Hand Tracking right
   4. (Option1) Packages/VRMarionette Util/Runtime/Capsule prefab を 2 つ追加する
      - それぞれ Left, Right 用
      - Color の alpha を 0 にして非表示にする
         (Capsule の Color はデフォルト色であり、実行中に設定される色は VrmMarionetteHand で指定する)
      - Duration を 1 にして表示時間を 1 秒にする
      - 以降 FocusIndicator と呼ぶ
   5. (Option1) Assembly Definition を作成し、以下を設定する
      - Assembly Definition References
         - VRMarionette
         - VRMarionette.Util
   6. (Option1) FocusIndicator Script を作成する
      ```
      using UnityEngine;
      using VRMarionette;
      using VRMarionette.Util;
      
      public class FocusIndicator : MonoBehaviour, IFocusIndicator
      {
          public Capsule capsule;
      
          public Transform Transform => capsule.transform;
      
          public Color Color
          {
              set => capsule.Color = value;
          }
      
          public void SetCapsule(CapsuleCollider capsuleCollider)
          {
              capsule.SetCapsule(capsuleCollider);
          }
      }
      ```
   7. (Option1) VrmMarionetteHand の Palm に FocusIndicator Script を追加する
      - Capsule に FocusIndicator を設定する
   8. Game Object を作成する
      - 以降 VRM Model と呼ぶ
   9. VRM Model に VrmLoader Script を追加し、以下を設定する
      - Vrm File Name
      - Spring Bone Collider Groups
         - VrmMarionetteHand
      - Force Sources
         - Palm (VrmMarionetteHand の子)
      - Human Limits
         - Packages/VRMarionette/Runtime/ScriptableObjects/HumanLimitContainer
      - Force Fields
         - Packages/VRMarionette/Runtime/ScriptableObjects/ForceFieldContainer
   10. VRM Model の位置と回転を調整する
       - 例えば、Position z=1, Rotation y=180
5. Build Settings を開く
   - Android に Switch Platform
   - Build And Run
