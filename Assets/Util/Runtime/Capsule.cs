using System;
using UnityEngine;

namespace VRMarionette.Util
{
    public class Capsule : MonoBehaviour
    {
        [SerializeField]
        private Vector3 center;

        [SerializeField]
        private float radius;

        [SerializeField]
        private float height;

        [SerializeField]
        private Direction direction;

        [SerializeField]
        private Color color;

        public float duration;

        private bool _initialized;
        private bool _useSharedMaterial;

        private Vector3 _scale;
        private Transform _container;
        private Transform _cylinder;
        private Transform _sphereTop;
        private Transform _sphereBottom;

        private Renderer _cylinderRenderer;
        private Renderer _sphereTopRenderer;
        private Renderer _sphereBottomRenderer;

        private Color _currentColor;
        private float _deltaAlpha;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly Quaternion RotationForXAxis = Quaternion.Euler(0, 0, 90);
        private static readonly Quaternion RotationForZAxis = Quaternion.Euler(90, 0, 0);

        private void Awake()
        {
            _container = transform.Find("Container");
            _cylinder = _container.Find("Cylinder");
            _sphereTop = _container.Find("SphereTop");
            _sphereBottom = _container.Find("SphereBottom");
            _cylinderRenderer = _cylinder.GetComponent<Renderer>();
            _sphereTopRenderer = _sphereTop.GetComponent<Renderer>();
            _sphereBottomRenderer = _sphereBottom.GetComponent<Renderer>();
            _initialized = true;
        }

        private void OnCenterChanged()
        {
            _container.localPosition = Vector3.Scale(center, _scale);
        }

        private void OnRadiusChanged()
        {
            var cylinderScale = _cylinder.localScale;
            var scale = radius * 2 * _scale;
            cylinderScale.x = scale.x;
            cylinderScale.z = scale.y;
            _cylinder.localScale = cylinderScale;
            _sphereTop.localScale = scale;
            _sphereBottom.localScale = scale;
        }

        private void OnHeightChanged()
        {
            var cylinderScaleY = height > radius * 2 ? (height - radius * 2) / 2 : 0;

            var cylinderScale = _cylinder.localScale;
            cylinderScale.y = cylinderScaleY * _scale.y;
            _cylinder.localScale = cylinderScale;

            var sphereTopPosition = _sphereTop.localPosition;
            sphereTopPosition.y = cylinderScaleY * _scale.y;
            _sphereTop.localPosition = sphereTopPosition;

            var sphereBottomPosition = _sphereBottom.localPosition;
            sphereBottomPosition.y = -cylinderScaleY * _scale.y;
            _sphereBottom.localPosition = sphereBottomPosition;
        }

        private void OnDirectionChanged()
        {
            _container.localRotation = direction switch
            {
                Direction.XAxis => RotationForXAxis,
                Direction.YAxis => Quaternion.identity,
                Direction.ZAxis => RotationForZAxis,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void OnCapsuleChanged()
        {
            OnCenterChanged();
            OnRadiusChanged();
            OnHeightChanged();
            OnDirectionChanged();
        }

        private void OnColorChanged()
        {
            UpdateMaterialColor();

            if (duration < Mathf.Epsilon) return;
            _deltaAlpha = _currentColor.a / duration;
        }

        private void UpdateMaterialColor()
        {
            if (_useSharedMaterial)
            {
                _cylinderRenderer.sharedMaterial.SetColor(BaseColor, _currentColor);
                _sphereTopRenderer.sharedMaterial.SetColor(BaseColor, _currentColor);
                _sphereBottomRenderer.sharedMaterial.SetColor(BaseColor, _currentColor);
            }
            else
            {
                _cylinderRenderer.material.SetColor(BaseColor, _currentColor);
                _sphereTopRenderer.material.SetColor(BaseColor, _currentColor);
                _sphereBottomRenderer.material.SetColor(BaseColor, _currentColor);
            }
        }

        private void Update()
        {
            if (_deltaAlpha < Mathf.Epsilon) return;
            var da = _deltaAlpha * Time.deltaTime;
            _currentColor.a = _currentColor.a > da ? _currentColor.a - da : 0f;
            if (_currentColor.a == 0f) _deltaAlpha = 0f;
            UpdateMaterialColor();
        }

        public void SetCapsule(CapsuleCollider capsuleCollider)
        {
            _scale = capsuleCollider.transform.lossyScale;
            _cylinder.localScale = _scale;
            _sphereTop.localScale = _scale;
            _sphereBottom.localScale = _scale;
            center = capsuleCollider.center;
            radius = capsuleCollider.radius;
            height = capsuleCollider.height;
            direction = (Direction)capsuleCollider.direction;
            OnCapsuleChanged();
        }

        public void Activate(bool activate)
        {
            _currentColor = activate ? color : Color.clear;
            OnColorChanged();
        }

        public void OnValidate()
        {
            // Edit Mode では SharedMaterial を使用する
            _useSharedMaterial = true;
            if (!_initialized) Awake();
            OnCapsuleChanged();
            OnColorChanged();
        }
    }
}