using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UniVRM10;

namespace VRMarionette
{
    public class VrmLoader : MonoBehaviour
    {
        public string vrmFileName;

        [Space]
        public VRM10SpringBoneColliderGroup[] springBoneColliderGroups;

        [Space]
        public VrmForceSource[] forceSources;

        [Space]
        public HumanLimitContainer humanLimits;

        public ForceFieldContainer forceFields;

        [Space]
        public bool enableMixer;

        public bool verbose;

        private void Start()
        {
            LoadVrmAsync();
        }

        private async void LoadVrmAsync()
        {
            var bytes = await ReadVrmBytesAsync(vrmFileName);
            var instance = await Vrm10.LoadBytesAsync(bytes);

            // モデルと相互作用する Collider を設定する
            foreach (var spring in instance.SpringBone.Springs)
            {
                spring.ColliderGroups.AddRange(springBoneColliderGroups);
            }

            instance.Runtime.ReconstructSpringBone();

            if (humanLimits)
            {
                // ControlRig を操作する機能を追加する
                instance.gameObject.AddComponent<VrmControlRigManipulator>().Initialize(instance, humanLimits);
            }

            if (forceFields)
            {
                // Collider の衝突により力を働かせる機能を追加する
                var forceGenerator = instance.gameObject.AddComponent<VrmForceGenerator>();
                forceGenerator.Initialize(instance, forceFields);
                forceGenerator.verbose = verbose;
                foreach (var forceTrigger in forceSources)
                {
                    forceTrigger.Initialize(forceGenerator);
                }
            }

            if (enableMixer)
            {
                instance.gameObject.AddComponent<VrmControlRigMixer>();
            }

            // 名前、位置、回転、スケールを引き継いで GameObject を置き換える
            // 諸々の構築後に Transform を設定しないと Transform の影響を受けた座標系になるので注意
            var o = instance.gameObject;
            o.name = name;
            var srcTransform = transform;
            o.transform.SetParent(srcTransform.parent);

            o.transform.localPosition = srcTransform.localPosition;
            o.transform.localRotation = srcTransform.localRotation;
            o.transform.localScale = srcTransform.localScale;

            Destroy(gameObject);
        }

        /**
         * Assets/StreamingAssets に置かれたファイルを読み込む。
         */
        private static async Task<byte[]> ReadVrmBytesAsync(string fileName)
        {
            var path = Path.Combine(Application.streamingAssetsPath, fileName);

            if (!path.Contains("://") && !path.Contains(":///")) return await File.ReadAllBytesAsync(path);

            // Android では、Application.streamingAssetsPath は通常のファイルパスではなく jar で始まる URL。
            // そのため、UnityWebRequest を使用してファイルの内容を読み込む。
            using var request = UnityEngine.Networking.UnityWebRequest.Get(path);
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Delay(100);
            }

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                return request.downloadHandler.data;

            Debug.LogError($"{request.error}: {path}");
            return null;
        }
    }
}