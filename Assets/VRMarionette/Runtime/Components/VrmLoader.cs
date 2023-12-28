using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UniVRM10;

namespace VRMarionette
{
    public class VrmLoader : VrmMarionetteBuilder
    {
        public string vrmFileName;

        [Space]
        public UnityEvent<Vrm10Instance> onLoaded;

        [Space]
        public bool enableMixer;

        private void Start()
        {
            LoadVrmAsync();
        }

        private async void LoadVrmAsync()
        {
            var bytes = await ReadVrmBytesAsync(vrmFileName);
            var instance = await Vrm10.LoadBytesAsync(bytes);

            Build(instance);

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

            onLoaded.Invoke(instance);
        }

        // Assets/StreamingAssets に置かれたファイルを読み込む。
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