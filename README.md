# UniVRMarionette

UniVRMarionette は UniVRM に対応したモデルに触れて操作する機能を提供する Unity 用ライブラリです。

Meta Quest 3 で動作確認していますが、他の環境でも使用できると思います。
(試した結果を Discussions で教えて頂けると助かります。)

## 機能

以下の機能を持ちます。
- 摘まんで動かす
- 押して動かす
- 両手で摘まんで回転
- 関節の角度に上限を設定
- 接触した部位をフォーカス

### デモンストレーション動画

[![VRMarionette Demo](https://img.youtube.com/vi/NItJ9BL7O6c/hqdefault.jpg)](https://www.youtube.com/watch?v=NItJ9BL7O6c)

### VRMarionette : `com.github.nosix.vrm.marionette`

VRMMarionette 本体のパッケージです。

Components

- VrmControlRigManipulator
   - モデルの各 Bone を回転させる Component
   - HumanLimitContainer により関節の角度に上限を設定する
- VrmControlRigMixer
   - Editor 上で VrmControlRigManipulator を介して各 Bone を操作できる様にする Component
   - デバッグ用
- VrmForceGenerator
   - Collider や Bone の状態に基づいて回転量を計算して各 Bone の状態を変化させる Component
   - Bone の回転には VrmControlRigManipulator を使用する
   - 各 Bone に CapsuleCollider を設定する
   - CapsuleCollider の位置や大きさは ForceFieldContainer で微調整可能
- VrmForceSource
   - 回転や移動のトリガーとなる Component
   - SphereCollider を設定する
   - SphereCollider が VrmForceGenerator の CapsuleCollider と接触することで各 Bone に変化が生じる
   - hold が true の場合は摘まみ操作、false の場合は押し操作になる
   - onEnter が true の場合は OnTriggerEnter が発生した場合のみトリガーし、
     false の場合は OnTriggerStay が発生した場合にトリガーする。
     (押し操作のみ)
   - GameObject に IFocusIndicator を実装した Component が設定されている場合、
     OnTriggerEnter が発生した時にその Component に focusColor を設定する 
- VrmLoader
   - モデルを [ランタイムロード](https://vrm-c.github.io/UniVRM/ja/api/0_44_runtime_import.html) する Component
   - ランタイムロードを使用しない場合は不要
   - Assets/StreamingAssets フォルダに置かれた VRM ファイルを読み込む
   - 初期化処理を行う (VrmLoader を使用しない場合は別途実装が必要)
      - 揺れ物と干渉する SpringBone の設定 (springBoneColliderGroups を使用)
      - VrmControlRigManipulator の初期化 (humanLimits を使用)
      - VrmForceGenerator の初期化 (forceFields を使用)
      - VrmForceSource の初期化 (forceSources を使用)
      - enableMixer が true の場合は VrmControlRigMixer の追加

Interface

- IFocusIndicator
   - VrmForceSource が設定された GameObject に IFocusIndicator を実装した Component を設定すると、
     Collider が接触した際に FocusIndicator に色が設定される

### MetaXR : `com.github.nosix.vrm.meta.xr`

Meta XR を使う場合のサポートパッケージです。
Meta XR All-in-One SDK に依存しており併せてダウンロードされます。

Prefab

- VrmMarionetteHand
   - 揺れ物と干渉する SpringBoneCollider や回転や移動のトリガーとなる VrmForceSource を持つ
   - OVRSkeleton の位置と回転を追跡して、中指の付け根(palm)、親指の先(thumb)、人差指の先(index)、小指の先(ring)に設定された SpringBoneCollider を動かす
   - 親指の先と人差指の先が grabThresholdDistance より近付いた場合に摘まみ状態にする
   - 状態が変わった時に onGrab を呼び出す (摘まみ状態ならば true を引数に渡す) 

URP (Shader and Material)

- Hand の表示を URP で行うための Shader と Material 集
- Meta XR SDK で提供されている Shader と Material を URP 用に書き換えた物

### Util : `com.github.nosix.vrm.util`

接触した部位をフォーカス表示するための Capsule を提供するパッケージです。
使用は必須ではありません。

Prefab

- Capsule
  - CapsuleCollider の形を可視化するための GameObject
  - 標準の Capsule とは異なり CapsuleCollider と同様に Center, Radius, Height, Direction の設定を持つ
  - 色を設定すると一定時間でアニメーションしながら透明になる
     - 時間は Duration で秒数を指定する
     - Duration が 0 の場合は透明にしない

## 導入方法

### Meta Quest 3 で使う

完成した Scene の例は Assets/MetaXR_Samples/MetaXRDemoApp を参照してください。

#### 手順

1. Packages/manifest.json に以下の依存を追加する (バージョンは書き換える)
    ```
    "com.github.nosix.vrm.marionette": "https://github.com/nosix/UniVRMarionette.git?path=/Assets/VRMarionette#[VRMarionetteバージョン]",
    "com.github.nosix.vrm.meta.xr": "https://github.com/nosix/UniVRMarionette.git?path=/Assets/MetaXR#[VRMarionetteバージョン]",
    "com.vrmc.gltf": "https://github.com/vrm-c/UniVRM.git?path=/Assets/UniGLTF#[UniVRMバージョン]",
    "com.vrmc.vrm": "https://github.com/vrm-c/UniVRM.git?path=/Assets/VRM10#[UniVRMバージョン]",
    "com.vrmc.vrmshaders": "https://github.com/vrm-c/UniVRM.git?path=/Assets/VRMShaders#[UniVRMバージョン]",
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
   4. Game Object を作成する
      - 以降 VRM Model と呼ぶ
   5. VRM Model に VrmLoader Script を追加し、以下を設定する
      - Vrm File Name
      - Spring Bone Collider Groups
         - VrmMarionetteHand
      - Force Sources
         - Palm (VrmMarionetteHand の子)
      - Human Limits
         - Packages/VRMarionette/Runtime/ScriptableObjects/HumanLimitContainer
      - Force Fields
         - Packages/VRMarionette/Runtime/ScriptableObjects/ForceFieldContainer
   6. VRM Model の位置と回転を調整する
      - 例えば、Position z=1, Rotation y=180
5. Build Settings を開く
   - Android に Switch Platform
   - Build And Run

### 接触した部位をフォーカスする場合

以下を追加で行ってください。

1. Packages/manifest.json に以下の依存を追加する (バージョンは書き換える)
    ```
    "com.github.nosix.vrm.util": "https://github.com/nosix/UniVRMarionette.git?path=/Assets/Util#[VRMarionetteバージョン]",
    ```
2. Scene を編集する
    1. Packages/VRMarionette Util/Runtime/Capsule prefab を 2 つ追加する
        - それぞれ Left, Right 用
        - Color の alpha を 0 にして非表示にする
          (Capsule の Color はデフォルト色であり、実行中に設定される色は VrmMarionetteHand で指定する)
        - Duration を 1 にして表示時間を 1 秒にする
        - 以降 FocusIndicator と呼ぶ
    2. Assembly Definition を作成し、以下を設定する
        - Assembly Definition References
            - VRMarionette
            - VRMarionette.Util
    3. FocusIndicator Script を作成する
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
    4. VrmMarionetteHand の Palm に FocusIndicator Script を追加する
        - Capsule に FocusIndicator を設定する

## バージョン

| VRMarionette | UniVRM   |
|--------------|----------|
| v0.1.4       | v0.115.0 |

## お知らせ

- Meta Quest 3 以外の Platform やランタイムロードを使用しない場合についても使えると思いますが検証はしていません。検証したい思いはありますが手が回らないので、検証して頂けた場合は Discussions で教えて頂けると助かります。
- Issues や Discussions は日本語で構いません。季節により仕事が忙しく反応が遅い場合はありますが気長にお待ちください。
- 英語版の文書はそのうち作成するかもしれませんが翻訳は歓迎します。An English version of the document may be created in the future, but translations are welcome.
