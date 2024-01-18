using UniVRM10;

namespace VRMarionette
{
    public interface IPostureControl
    {
        public void Initialize(Vrm10Instance instance);

        /// <summary>
        /// 姿勢制御の有効/無効を切り替える
        /// </summary>
        /// <param name="isEnabled">有効にするならば true</param>
        /// <returns>有効/無効を受け付けた場合は true</returns>
        public bool SetPostureControlState(bool isEnabled);
    }
}