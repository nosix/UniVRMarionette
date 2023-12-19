using System;
using UnityEngine;

namespace Capsule
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

        public Color Color
        {
            set
            {
                color = value;
                OnColorChanged();
            }
        }

        private bool _initialized;
        private bool _useSharedMaterial;

        private Transform _container;
        private Transform _cylinder;
        private Transform _sphereTop;
        private Transform _sphereBottom;

        private Renderer _cylinderRenderer;
        private Renderer _sphereTopRenderer;
        private Renderer _sphereBottomRenderer;

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
            _container.localPosition = center;
        }

        private void OnRadiusChanged()
        {
            var cylinderScale = _cylinder.localScale;
            var scale = radius * 2;
            cylinderScale.x = scale;
            cylinderScale.z = scale;
            _cylinder.localScale = cylinderScale;
            _sphereTop.localScale = new Vector3(scale, scale, scale);
            _sphereBottom.localScale = new Vector3(scale, scale, scale);
        }

        private void OnHeightChanged()
        {
            var cylinderScaleY = height > radius * 2 ? (height - radius * 2) / 2 : 0;

            var cylinderScale = _cylinder.localScale;
            cylinderScale.y = cylinderScaleY;
            _cylinder.localScale = cylinderScale;

            var sphereTopPosition = _sphereTop.localPosition;
            sphereTopPosition.y = cylinderScaleY;
            _sphereTop.localPosition = sphereTopPosition;

            var sphereBottomPosition = _sphereBottom.localPosition;
            sphereBottomPosition.y = -cylinderScaleY;
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
            _deltaAlpha = color.a / duration;
        }

        private void UpdateMaterialColor()
        {
            if (_useSharedMaterial)
            {
                _cylinderRenderer.sharedMaterial.SetColor(BaseColor, color);
                _sphereTopRenderer.sharedMaterial.SetColor(BaseColor, color);
                _sphereBottomRenderer.sharedMaterial.SetColor(BaseColor, color);
            }
            else
            {
                _cylinderRenderer.material.SetColor(BaseColor, color);
                _sphereTopRenderer.material.SetColor(BaseColor, color);
                _sphereBottomRenderer.material.SetColor(BaseColor, color);
            }
        }

        private void Update()
        {
            if (_deltaAlpha < Mathf.Epsilon) return;
            var da = _deltaAlpha * Time.deltaTime;
            color.a = color.a > da ? color.a - da : 0f;
            if (color.a == 0f) _deltaAlpha = 0f;
            UpdateMaterialColor();
        }

        public void SetCapsule(CapsuleCollider capsuleCollider)
        {
            center = capsuleCollider.center;
            radius = capsuleCollider.radius;
            height = capsuleCollider.height;
            direction = (Direction)capsuleCollider.direction;
            OnCapsuleChanged();
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