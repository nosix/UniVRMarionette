using UnityEngine;
using UnityEngine.Events;

namespace VRMarionette
{
    /// <summary>
    /// 力の発生源。
    /// IFocusIndicator を実装した Component が存在する場合、
    /// 力を及ぼす対象の CapsuleCollider と接触した時に CapsuleCollider の形状と focusColor を IFocusIndicator に通知する。
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class VrmForceSource : MonoBehaviour
    {
        public bool hold;
        public bool onEnter;
        public Color focusColor = Color.yellow;

        [Space]
        public UnityEvent<FocusEvent> onFocus;

        private SphereCollider _collider;
        private IFocusIndicator _focusIndicator;
        private VrmForceGenerator _forceGenerator;
        private bool _isInitialized;

        // フォーカスの対象となる Capsule
        private CapsuleCollider _targetCapsule;

        // 摘まみ状態が終了した後に対象の Collider 内に留まっているときは true
        private bool _holdOff;

        // 摘まんでいる対象の Collider
        private Collider _holdCollider;

        // 摘まみ操作中の前フレームの位置と回転
        private Vector3 _prevPosition;
        private Quaternion _prevRotation;

        public void Initialize(VrmForceGenerator forceGenerator)
        {
            _collider = GetComponent<SphereCollider>();
            _focusIndicator = GetComponent<IFocusIndicator>();
            _forceGenerator = forceGenerator;
            _collider.isTrigger = true;
            _isInitialized = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isInitialized) return;

            if (other is CapsuleCollider capsule) Focus(capsule, true);

            if (!hold && !_holdOff && onEnter)
            {
                _forceGenerator.QueueForceGeneration(_collider, other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            _holdOff = false;

            if (other is CapsuleCollider capsule) Focus(capsule, false);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!_isInitialized) return;

            switch (hold)
            {
                case true when _holdCollider is null:
                    var t = transform;
                    _holdCollider = other;
                    _prevPosition = t.position;
                    _prevRotation = t.rotation;
                    return;
                case false when !_holdOff && !onEnter:
                    _forceGenerator.QueueForceGeneration(_collider, other);
                    break;
            }
        }

        private void Focus(CapsuleCollider capsule, bool on)
        {
            // 摘まみ状態ではフォーカスを変更しない
            if (_holdCollider is not null) return;

            // 身体部位に設定した CapsuleCollider 以外の場合は無視する
            if (!_forceGenerator.BoneProperties.TryGetValue(capsule.transform, out var nextBoneProperty)) return;

            var nextFocusColor = Color.clear;

            if (_targetCapsule is not null)
            {
                // フォーカスを外す
                // フォーカス ON の場合は targetCapsule と capsule が異なる場合に
                // フォーカス OFF の場合は targetCapsule と capsule が同じ場合に
                if (on == (_targetCapsule != capsule))
                {
                    var prevBoneProperty = _forceGenerator.BoneProperties.Get(_targetCapsule.transform);
                    _targetCapsule = null;
                    onFocus.Invoke(new FocusEvent
                    {
                        Bone = prevBoneProperty.Bone,
                        On = false
                    });
                }
            }

            // フォーカスが外れている場合はフォーカスを更新する
            if (on && _targetCapsule is null)
            {
                _targetCapsule = capsule;
                onFocus.Invoke(new FocusEvent
                {
                    Bone = nextBoneProperty.Bone,
                    On = true
                });
                nextFocusColor = focusColor;
            }

            if (_focusIndicator is null) return;

            _focusIndicator.SetCapsule(capsule);
            _focusIndicator.Color = nextFocusColor;
        }

        private void UpdateCapsule()
        {
            if (_focusIndicator is null || _targetCapsule is null) return;

            var dstTransform = _focusIndicator.Transform;
            var srcTransform = _targetCapsule.transform;
            dstTransform.position = srcTransform.position;
            dstTransform.rotation = srcTransform.rotation;
            dstTransform.localScale = srcTransform.localScale;
        }

        private void Update()
        {
            if (!_isInitialized) return;

            UpdateCapsule();

            if (!hold)
            {
                // 摘まみ状態を終了する
                if (_holdCollider is not null)
                {
                    _holdCollider = null;
                    _holdOff = true;
                }
            }

            if (_holdCollider is null) return;

            var t = transform;
            var currPosition = t.position;
            var currRotation = t.rotation;
            var force = currPosition - _prevPosition;
            var rotation = currRotation * Quaternion.Inverse(_prevRotation);
            _forceGenerator.QueueForceGeneration(_collider, _holdCollider, force, rotation);
            _prevPosition = currPosition;
            _prevRotation = currRotation;
        }

        public void SetHold(bool value)
        {
            hold = value;
        }
    }
}