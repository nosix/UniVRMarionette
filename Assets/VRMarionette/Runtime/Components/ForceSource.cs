using System;
using UnityEngine;
using UnityEngine.Events;

namespace VRMarionette
{
    /// <summary>
    /// 力の発生源。
    /// IFocusIndicator を実装した Component が存在する場合、
    /// 力を及ぼす対象の CapsuleCollider と接触した時に CapsuleCollider の形状を IFocusIndicator に通知する。
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class ForceSource : MonoBehaviour
    {
        public bool hold;

        [Tooltip("The force not used for joint rotation is applied to movement.")]
        public bool useRemainingForceForMovement;

        public bool trace;

        [Space]
        public UnityEvent<FocusEvent> onFocus;

        public ForceEvent? ForceEvent { private set; get; }

        private SphereCollider _collider;
        private IFocusIndicator _focusIndicator;
        private ForceResponder _forceResponder;
        private bool _isInitialized;

        // フォーカスの対象となる Capsule
        private CapsuleCollider _targetCapsule;

        // 摘まんでいる対象の Collider
        private Collider _holdCollider;

        // 押している対象の Collider
        private Collider _pushCollider;

        // 前フレームの位置と回転
        private Vector3 _prevPosition;
        private Quaternion _prevRotation;

        public void Initialize(ForceResponder forceResponder)
        {
            if (!forceResponder)
            {
                throw new InvalidOperationException(
                    "The ForceSource component requires the ForceResponder object.");
            }

            _collider = GetComponent<SphereCollider>();
            _focusIndicator = GetComponent<IFocusIndicator>();
            _forceResponder = forceResponder;
            _collider.isTrigger = true;
            _isInitialized = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isInitialized || !ShouldUpdateCollider(_pushCollider, other)) return;

            _pushCollider = other;

            // 押しの場合は表面の位置と姿勢のみ記録する
            if (!hold) RecordTransform();

            if (other is CapsuleCollider capsule) Focus(capsule, true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_isInitialized) return;

            // other が Enter で無視した Collider の場合には Exit も無視する
            if (_pushCollider != other) return;

            if (other is CapsuleCollider capsule) Focus(capsule, false);

            _pushCollider = null;
        }

        private void OnTriggerStay(Collider other)
        {
            if (!_isInitialized || !hold || !ShouldUpdateCollider(_holdCollider, other)) return;

            _holdCollider = other;

            // 摘まむときは移動中の位置と姿勢を記録し続ける
            RecordTransform();
        }

        private bool ShouldUpdateCollider(Collider current, Collider other)
        {
            // current が未設定の場合は更新する
            if (current is null) return true;
            // other が current の親の場合は更新する
            return other.transform == current.transform.parent;
        }

        private void RecordTransform()
        {
            var t = transform;
            _prevPosition = t.position;
            _prevRotation = t.rotation;
        }

        private void Focus(CapsuleCollider capsule, bool on)
        {
            // 摘まみ状態ではフォーカスを変更しない
            if (_holdCollider is not null) return;

            // 身体部位に設定した CapsuleCollider 以外の場合は無視する
            if (!_forceResponder.BoneProperties.TryGetValue(capsule.transform, out var nextBoneProperty)) return;

            var activate = false;

            if (_targetCapsule is not null)
            {
                // フォーカスを外す
                // フォーカス ON の場合は targetCapsule と capsule が異なる場合に
                // フォーカス OFF の場合は targetCapsule と capsule が同じ場合に
                if (on == (_targetCapsule != capsule))
                {
                    var prevBoneProperty = _forceResponder.BoneProperties.Get(_targetCapsule.transform);
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
                activate = true;
            }

            if (_focusIndicator is null) return;

            _focusIndicator.SetCapsule(capsule);
            _focusIndicator.Activate(activate);
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

            ForceEvent = null;

            UpdateCapsule();

            if (!hold)
            {
                // 摘まみ状態を終了する
                if (_holdCollider is not null)
                {
                    _holdCollider = null;
                }
            }

            if (_holdCollider is not null)
            {
                QueueHoldForce();
            }
            else if (_pushCollider is not null)
            {
                QueuePushForce();
            }
        }

        private void QueueHoldForce()
        {
            var t = transform;
            var currPosition = t.position;
            var currRotation = t.rotation;
            var force = currPosition - _prevPosition;
            var rotation = currRotation * Quaternion.Inverse(_prevRotation);
            ForceEvent = _forceResponder.QueueForce(
                _holdCollider.transform,
                _collider.transform.position,
                force,
                rotation,
                allowMultiSource: true,
                useRemainingForceForMovement
            );
            _prevPosition = currPosition;
            _prevRotation = currRotation;
        }

        private void QueuePushForce()
        {
            var t = transform;
            var currPosition = t.position;
            var currRotation = t.rotation;
            var force = currPosition - _prevPosition;

            // 前方向のみ力を働かせる
            var forward = _collider.transform.rotation * Vector3.forward;
            force = force.ProjectOnto(forward);
            if (Vector3.Dot(force, forward) < 0) force = Vector3.zero;

            ForceEvent = _forceResponder.QueueForce(
                _pushCollider.transform,
                _collider.transform.position,
                force,
                useRemainingForceForMovement
            );
            _prevPosition = currPosition;
            _prevRotation = currRotation;
        }

        public void SetHold(bool value)
        {
            hold = value;
        }

        /// <summary>
        /// trace が有効な場合にオブジェクトの position, rotation, hold をログに出力する。
        /// </summary>
        private void FixedUpdate()
        {
            if (!trace || !_isInitialized) return;

            var t = transform;
            Debug.Log($"ForceSource {gameObject.name} {t.position} {t.eulerAngles} {hold}");
        }
    }
}