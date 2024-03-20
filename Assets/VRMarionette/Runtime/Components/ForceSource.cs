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
        public bool HasCollision => _holdCollider != null || _pushCollider != null;
        public CapsuleCollider Collision => _holdCollider != null ? _holdCollider : _pushCollider;

        private SphereCollider _collider;
        private IFocusIndicator _focusIndicator;
        private ForceResponder _forceResponder;
        private bool _isInitialized;

        // フォーカスの対象となる Capsule
        private CapsuleCollider _targetCapsule;

        // 摘まんでいる対象の Collider
        private CapsuleCollider _holdCollider;

        // 押している対象の Collider
        private CapsuleCollider _pushCollider;

        // 前フレームの位置
        private Vector3 _prevPosition;

        // 摘まみ始め時点での摘まみ対象を基準としたローカル回転
        private Quaternion _holdRotation;

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
            if (!_isInitialized || _pushCollider == other) return;
            if (other is not CapsuleCollider capsule) return;
            if (!_forceResponder.BoneProperties.TryGetValue(capsule.transform, out _)) return;

            _pushCollider = capsule;

            // 押しの場合は表面の位置を記録する
            if (!hold) _prevPosition = transform.position;

            Focus(capsule, true);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!_isInitialized || !ShouldUpdateHoldCollider(_holdCollider, other)) return;
            if (other is not CapsuleCollider capsule) return;
            if (!_forceResponder.BoneProperties.TryGetValue(capsule.transform, out _)) return;

            _holdCollider = capsule;

            var t = transform;
            _prevPosition = t.position;
            _holdRotation = Quaternion.Inverse(t.rotation) * other.transform.rotation;
        }

        private bool ShouldUpdateHoldCollider(Collider current, Collider other)
        {
            // 摘まみ状態でなければ更新しない
            if (!hold) return false;
            // current が未設定の場合は更新する
            if (current is null) return true;
            // other が current の親の場合は更新する
            return other.transform == current.transform.parent;
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

            // 相手方が無効化された場合や接触が無くなった場合は押しを止める
            // OnTriggerExit は呼ばれない場合があるので使わない
            // holdCollider は接触が無くなっていても摘まんでいる間は保持し続ける
            if (_pushCollider is not null)
            {
                if (!_pushCollider.gameObject.activeInHierarchy ||
                    !_pushCollider.HasCollision(_collider))
                {
                    Focus(_pushCollider, false);
                    _pushCollider = null;
                }
            }

            ForceEvent = null;

            UpdateCapsule();

            // 摘まみ状態を終了する
            if (!hold && _holdCollider is not null)
            {
                _holdCollider = null;
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
            var force = currPosition - _prevPosition;
            _prevPosition = currPosition;

            var expectedRotation = t.rotation * _holdRotation;
            var rotation = expectedRotation * Quaternion.Inverse(_holdCollider.transform.rotation);

            ForceEvent = _forceResponder.QueueForce(
                _holdCollider.transform,
                _collider.transform.position,
                force,
                rotation,
                isPushing: false,
                useRemainingForceForMovement
            );
        }

        private void QueuePushForce()
        {
            var t = transform;
            var currPosition = t.position;
            var force = currPosition - _prevPosition;
            _prevPosition = currPosition;

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