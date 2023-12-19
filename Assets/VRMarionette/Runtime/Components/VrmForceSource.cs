using UnityEngine;

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

        private SphereCollider _collider;
        private IFocusIndicator _focusIndicator;
        private VrmForceGenerator _forceGenerator;
        private bool _isInitialized;
        private CapsuleCollider _targetCapsule;
        private Collider _holdCollider;
        private Vector3 _prevPosition;
        private Quaternion _prevRotation;

        private void Start()
        {
            _collider = GetComponent<SphereCollider>();
            _focusIndicator = GetComponent<IFocusIndicator>();

            // Initialize を実行するまでは Trigger を無効にする
            _collider.isTrigger = false;
        }

        public void Initialize(VrmForceGenerator forceGenerator)
        {
            _forceGenerator = forceGenerator;
            _collider.isTrigger = true;
            _isInitialized = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isInitialized) return;

            if (other is CapsuleCollider capsule) Focus(capsule);

            if (!hold && onEnter)
            {
                _forceGenerator.QueueForceGeneration(_collider, other);
            }
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
                case false when !onEnter:
                    _forceGenerator.QueueForceGeneration(_collider, other);
                    break;
            }
        }

        private void Focus(CapsuleCollider capsule)
        {
            if (_focusIndicator is null) return;

            _targetCapsule = capsule;

            _focusIndicator.SetCapsule(capsule);
            _focusIndicator.Color = focusColor;
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
                _holdCollider = null;
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