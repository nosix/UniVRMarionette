using System.Collections.Generic;
using UnityEngine;

namespace VRMarionette.MetaXR
{
    public class PositionInitializer
    {
        private readonly int _requireSampleNum;
        private readonly float _maxPositionDelta;
        private readonly List<Vector3> _samples = new();
        private int _skipSampleNum;
        private int _remainSampleNum;

        public PositionInitializer(int requireSampleNum, float maxPositionDelta)
        {
            _maxPositionDelta = maxPositionDelta;
            _requireSampleNum = requireSampleNum;

            // 前半のサンプルは無視する
            _skipSampleNum = requireSampleNum / 2;
            // 後半のサンプルを収集する
            _remainSampleNum = requireSampleNum / 2;
        }

        public void AddSample(Vector3 position)
        {
            if (_skipSampleNum-- > 0) return;
            if (_remainSampleNum-- > 0) _samples.Add(position);
        }

        public bool TryGet(out Vector3 position)
        {
            position = Vector3.zero;
            if (_remainSampleNum > 0) return false;

            // 25% のサンプルが連続して maxPositionDelta 以内の距離に集まっているならば、そのサンプルの位置を返す
            var minSampleNum = _samples.Count / 4;
            var sampleNum = 0;
            var i = _samples.Count - 1;

            position = _samples[i--];
            while (i >= 0)
            {
                var sample = _samples[i--];
                if (Vector3.Distance(sample, position) < _maxPositionDelta)
                {
                    sampleNum++;
                    if (sampleNum >= minSampleNum) return true;
                }
                else
                {
                    position = sample;
                    sampleNum = 0;
                }
            }

            _remainSampleNum = _requireSampleNum / 2;
            _skipSampleNum = _requireSampleNum / 2;
            _samples.Clear();

            return false;
        }
    }
}