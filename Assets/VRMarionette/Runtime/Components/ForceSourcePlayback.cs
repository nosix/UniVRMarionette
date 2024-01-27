using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VRMarionette
{
    /// <summary>
    /// ForceSource の trace ログから動きを再現する。
    /// </summary>
    [RequireComponent(typeof(ForceSource))]
    public class ForceSourcePlayback : MonoBehaviour
    {
        public string logFilePath;
        public string targetObjectName;

        [Space]
        public bool pause;

        private ForceSource _controlTarget;
        private readonly Queue<Command> _commands = new();

        private static readonly Regex TraceLog = new(@"ForceSource (\w+) \(([^)]+)\) \(([^)]+)\) (\w+)");

        public void OnModelReady()
        {
            _controlTarget = GetComponent<ForceSource>();
        }

        private void Start()
        {
            var lines = File.ReadAllLines(logFilePath);
            foreach (var line in lines)
            {
                var match = TraceLog.Match(line);
                if (!match.Success) continue;

                var objectName = match.Groups[1].Value;
                if (objectName != targetObjectName) continue;

                var position = match.Groups[2].Value;
                var rotation = match.Groups[3].Value;
                var hold = match.Groups[4].Value;

                _commands.Enqueue(new Command
                {
                    Position = ToVector3(position),
                    Rotation = ToVector3(rotation),
                    Hold = hold == "True"
                });
            }
        }

        private void FixedUpdate()
        {
            if (_controlTarget is null || _commands.Count == 0 || pause) return;
            Debug.Log($"Playback {_commands.Count}");
            var command = _commands.Dequeue();
            var t = _controlTarget.transform;
            t.position = command.Position;
            t.eulerAngles = command.Rotation;
            _controlTarget.hold = command.Hold;
        }

        private static Vector3 ToVector3(string value)
        {
            var values = value.Split(",");
            return new Vector3(
                float.Parse(values[0]),
                float.Parse(values[1]),
                float.Parse(values[2])
            );
        }

        private struct Command
        {
            public Vector3 Position;
            public Vector3 Rotation;
            public bool Hold;
        }
    }
}