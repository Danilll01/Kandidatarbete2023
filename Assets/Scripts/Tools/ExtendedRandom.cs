using UnityEngine;

namespace ExtendedRandom
{
    /// <summary>
    /// Class for random with various random functions
    /// </summary>
    public class RandomX
    {
        readonly private System.Random random;

        public RandomX()
        {
            random = new System.Random();
        }

        public RandomX(int seed)
        {
            random = new System.Random(seed);
        }

        /// <summary>
        /// Returns a random point inside or on a circle with radius 1.0
        /// </summary>
        public Vector2 InsideUnitCircle()
        {
            float angle = Value(0, 2) * Mathf.PI; // Generate angle between [0, 2*Pi]
            float radius = Value(); // Generate radius between [0, 1]

            // Convert to from polar coordinates to regular coordinates
            return new Vector2(
                x: radius * Mathf.Cos(angle),
                y: radius * Mathf.Sin(angle));
        }

        /// <summary>
        /// Returns a random point inside or on a sphere with radius 1.0
        /// </summary>
        public Vector3 InsideUnitSphere()
        {
            float phi = Value(0, 2) * Mathf.PI; // Generates angle between [0, 2*Pi]
            float theta = Value() * Mathf.PI; // Generates angle between [0, Pi]
            float radius = Value(); // Generates radius between [0, 1]

            // Convert from spherical coordinates to regular coordinates
            return new Vector3(
                x: radius * Mathf.Sin(theta) * Mathf.Cos(phi),
                y: radius * Mathf.Sin(theta) * Mathf.Sin(phi),
                z: radius * Mathf.Cos(theta));
        }

        /// <summary>
        /// Returns a random point on the surface of a sphere with radius 1.0
        /// </summary>
        public Vector3 OnUnitSphere()
        {
            float phi = Value(0, 2) * Mathf.PI; // Generates angle between [0, 2*Pi]
            float theta = Value() * Mathf.PI; // Generates angle between [0, Pi]

            // Convert from spherical coordinates to regular coordinates
            return new Vector3(
                x: Mathf.Sin(theta) * Mathf.Cos(phi),
                y: Mathf.Sin(theta) * Mathf.Sin(phi),
                z: Mathf.Cos(theta));
        }

        /// <summary>
        /// Returns a random rotation 
        /// </summary>
        public Quaternion Rotation()
        {
            float x = Value();
            float y = Value();
            float z = Value();
            float w = Value();

            return new Quaternion(x, y, z, w).normalized;
        }

        /// <summary>
        /// Returns a random float within [0, 1]
        /// </summary>
        public float Value()
        {
            return (float)random.NextDouble();
        }

        /// <summary>
        /// Returns a random float within [a, b]
        /// </summary>
        public float Value(float a, float b)
        {
            if (a < b)
                return Value() * (b - a) + a;
            else
                return Value() * (a - b) + b;
        }

        /// <summary>
        /// Returns random integer value
        /// </summary>
        public int Next()
        {
            return random.Next();
        }

        /// <summary>
        /// Returns random integer value in range [0, maxValue]
        /// </summary>
        public int Next(int maxValue)
        {
            return random.Next(maxValue);
        }

        /// <summary>
        /// Returns random integer value in range [minValue, maxValue]
        /// </summary>
        public int Next(int minValue, int maxValue)
        {
            return random.Next(minValue, maxValue);
        }
    }
}
