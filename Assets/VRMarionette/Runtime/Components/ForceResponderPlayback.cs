using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VRMarionette
{
    /// <summary>
    /// ForceResponder の trace ログから動きを再現する。
    /// </summary>
    public class ForceResponderPlayback : MonoBehaviour
    {
        public string logFilePath;
        public ForceResponder forceResponder;

        private readonly Queue<Command> _commands = new();
        private Animator _animator;
        private Command? _pendingCommand;
        private int _frameCount;

        // private static readonly Regex TraceLog = new(@"QueueForce(?:\\s+(\\S+)){7}");
        private static readonly Regex TraceLog = new(@"QueueForce (.+)");

        private void OnEnable()
        {
            _animator = forceResponder.GetComponent<Animator>();

            var lines = File.ReadAllLines(logFilePath);
            foreach (var line in lines)
            {
                Debug.Log(line);
                var match = TraceLog.Match(line);
                if (!match.Success) continue;

                var values = match.Groups[1].Value.Split(" ");
                var frameCount = values[0];
                var bone = values[1];
                var forcePoint = values[2];
                var force = values[3];
                var rotation = values[4];
                var isPushing = values[5];
                var allowBodyMovement = values[6];

                _commands.Enqueue(new Command
                {
                    FrameCount = int.Parse(frameCount),
                    Bone = Enum.Parse<HumanBodyBones>(bone),
                    ForcePoint = Utils.ParseVector(forcePoint) / 100,
                    Force = Utils.ParseVector(force) / 100,
                    Rotation = rotation != "null" ? Quaternion.Euler(Utils.ParseVector(rotation)) : null,
                    IsPushing = bool.Parse(isPushing),
                    AllowBodyMovement = bool.Parse(allowBodyMovement)
                });
            }

            Debug.Log($"Queue {_commands.Count} commands");

            var command = _commands.Dequeue();
            _pendingCommand = command;
            _frameCount = command.FrameCount;
        }

        private void Update()
        {
            if (_pendingCommand.HasValue)
            {
                var command = _pendingCommand.Value;
                if (command.FrameCount == _frameCount)
                {
                    QueueForce(command);
                    _pendingCommand = null;
                }
            }
            else
            {
                if (_commands.Count == 0) return;
            }

            while (_commands.Count > 0)
            {
                var command = _commands.Dequeue();
                if (command.FrameCount == _frameCount)
                {
                    QueueForce(command);
                }
                else
                {
                    _pendingCommand = command;
                    break;
                }
            }

            Debug.Log($"Playback {_frameCount++}");

            if (_commands.Count == 0) Debug.Log("Playback finished");
        }

        private void QueueForce(Command command)
        {
            if (command.Rotation.HasValue)
            {
                forceResponder.QueueForce(
                    _animator.GetBoneTransform(command.Bone),
                    command.ForcePoint,
                    command.Force,
                    command.Rotation.Value,
                    command.IsPushing,
                    command.AllowBodyMovement
                );
            }
            else
            {
                forceResponder.QueueForce(
                    _animator.GetBoneTransform(command.Bone),
                    command.ForcePoint,
                    command.Force,
                    command.AllowBodyMovement
                );
            }
        }

        private struct Command
        {
            public int FrameCount;
            public HumanBodyBones Bone;
            public Vector3 ForcePoint;
            public Vector3 Force;
            public Quaternion? Rotation;
            public bool IsPushing;
            public bool AllowBodyMovement;
        }
    }
}