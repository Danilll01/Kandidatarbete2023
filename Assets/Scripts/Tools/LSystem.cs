using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LSystem
{
    /// <summary>
    /// Represents a movement
    /// </summary>
    public enum Movement
    {
        Forward,
        Stay,
        YawClockwise,
        YawCounterClockwise,
        PitchUp,
        PitchDown,
        StartBranch,
        EndBranch
    }

    /// <summary>
    /// Represents a sequence of movements
    /// </summary>
    [Serializable]
    public struct Sequence
    {
        public List<Movement> sequence;

        public override string ToString()
        {
            string res = string.Empty;
            for(int i = 0; i < sequence.Count - 1; i++)
            {
                res += sequence[i].ToString() + "->";
            }
            return res + sequence[^1].ToString();
        }
    }

    /// <summary>
    /// Represents a rule with a <see cref="Movement"/> to be replaced by a <see cref="Sequence"/>
    /// </summary>
    [Serializable]
    public struct Rule
    {
        public Movement identity;
        public Sequence replacement;

        public override string ToString()
        {
            return identity.ToString() + "=" + replacement.ToString();
        }
    }

    /// <summary>
    /// Settings for how the movement will be done, yawangles, stepsize etc.
    /// </summary>
    [Serializable]
    public struct MovementSettings
    {
        public float stepSize;
        public float yawAngle;
        public float pitchAngle;
    }

    /// <summary>
    /// Settings for the LSystem
    /// </summary>
    [Serializable]
    public struct Settings
    {
        public Sequence startSequence;
        public List<Rule> rules;
        public int iterations;
        public MovementSettings movementSettings;
    }

    private struct Position
    {
        public Vector3 position;
        public Vector3 direction;
        public Vector3 normal;

        public Position(Vector3 position, Vector3 direction, Vector3 normal)
        {
            this.position = position;
            this.direction = direction;
            this.normal = normal;
        }
    }

    private List<Rule> rules;
    private int iterations;
    private Sequence sequence;
    private MovementSettings movementSettings;

    private Position currentPos;
    private Stack<Position> branchPos;
    private int index;

    /// <summary>
    /// Initalizes and generates the L-System
    /// </summary>
    /// <param name="settings">Settings to be used for the LSystem</param>
    public LSystem(Settings settings)
    {
        sequence = settings.startSequence;
        rules = settings.rules;
        iterations = settings.iterations;

        Generate();

        movementSettings = settings.movementSettings;

        currentPos = new Position(Vector3.zero, Vector3.up, Vector3.forward);
        branchPos = new Stack<Position>();
        index = 0;
    }

    /// <summary>
    /// Steps through the generated L-System
    /// </summary>
    /// <param name="currentPos">Will assign current position to this variable</param>
    /// <param name="isFinished">Will be assigned true if the path is finished</param>
    public void Step(out Vector3 currentPos, out bool isFinished)
    {
        currentPos = this.currentPos.position;

        isFinished = index == sequence.sequence.Count;
        if (isFinished)
            return;

        Movement movement = sequence.sequence[index];

        Vector3 rotationAxis = Vector3.Cross(this.currentPos.direction, this.currentPos.normal);
        switch (movement)
        {
            case Movement.Forward:
                this.currentPos.position += this.currentPos.direction * movementSettings.stepSize;
                break;
            case Movement.Stay: break;
            case Movement.YawClockwise:
                this.currentPos.direction = Quaternion.AngleAxis(movementSettings.yawAngle, this.currentPos.normal) * this.currentPos.direction;
                break;
            case Movement.YawCounterClockwise:
                this.currentPos.direction = Quaternion.AngleAxis(-movementSettings.yawAngle, this.currentPos.normal) * this.currentPos.direction;
                break;
            case Movement.PitchUp:
                this.currentPos.direction = Quaternion.AngleAxis(movementSettings.pitchAngle, rotationAxis) * this.currentPos.direction;
                this.currentPos.normal = Quaternion.AngleAxis(movementSettings.pitchAngle, rotationAxis) * this.currentPos.normal;
                break;
            case Movement.PitchDown:
                this.currentPos.direction = Quaternion.AngleAxis(-movementSettings.pitchAngle, rotationAxis) * this.currentPos.direction;
                this.currentPos.normal = Quaternion.AngleAxis(-movementSettings.pitchAngle, rotationAxis) * this.currentPos.normal;
                break;
            case Movement.StartBranch:
                branchPos.Push(this.currentPos);
                break;
            case Movement.EndBranch:
                this.currentPos = branchPos.Pop();
                break;
        }

        index++;
    }

    private void Generate()
    {
        for(int i = 0; i < iterations; i++)
        {
            Sequence newSequence = new Sequence { sequence = new List<Movement>() };
            foreach (Movement movement in sequence.sequence)
            {
                newSequence.sequence.Add(movement);
                foreach (Rule rule in rules)
                {
                    if(rule.identity == movement)
                    {
                        newSequence.sequence.RemoveAt(newSequence.sequence.Count - 1);
                        newSequence.sequence.AddRange(rule.replacement.sequence);
                        break;
                    }
                }
            }
            sequence = newSequence;
        }
    }
}
