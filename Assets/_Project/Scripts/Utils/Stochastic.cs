using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Handles random number generation for sensor noise simulation.
    /// </summary>
    public static class Stochastic
    {
        private static readonly System.Random rng = new System.Random();

        /// <summary>
        /// Generates a standard normal distribution number (Mean = 0, StdDev = 1)
        /// using the Box-Muller transform.
        /// </summary>
        public static float GenerateGaussian()
        {
            float u1 = 1.0f - (float)rng.NextDouble();
            float u2 = 1.0f - (float)rng.NextDouble();
            return Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
        }

        /// <summary>
        /// Generates a Vector3 where each component is independent Gaussian noise.
        /// </summary>
        public static Vector3 GenerateWhiteNoiseVector(float stdDev)
        {
            return new Vector3(
                GenerateGaussian() * stdDev,
                GenerateGaussian() * stdDev,
                GenerateGaussian() * stdDev
            );
        }

        /// <summary>
        /// Generates a random value between 0 and 1.
        /// </summary>
        public static float GenerateUniform()
        {
            return (float)rng.NextDouble();
        }
    }

    /// <summary>
    /// Models a First-Order Gauss-Markov process (Bounded Random Walk).
    /// Used for simulating sensor bias instability that naturally reverts toward zero.
    /// 
    /// Mathematical Model: x(k+1) = A * x(k) + B * w(k)
    /// Where:
    ///   A = exp(-dt / correlationTime)  [Decay toward zero]
    ///   B = sigma * sqrt(1 - A²)        [Driving noise scale]
    ///   w(k) = Gaussian white noise
    /// </summary>
    public class GaussMarkovVector
    {
        private float _coeffA; // Decay coefficient
        private float _coeffB; // Driving noise scale
        
        /// <summary>
        /// Current bias value (call Step() each FixedUpdate to evolve)
        /// </summary>
        public Vector3 CurrentBias { get; private set; }

        /// <summary>
        /// Creates a new Gauss-Markov process.
        /// </summary>
        /// <param name="correlationTime">Time constant (seconds) - larger = slower drift</param>
        /// <param name="biasSigma">Steady-state standard deviation of bias</param>
        /// <param name="dt">Fixed timestep (Time.fixedDeltaTime)</param>
        public GaussMarkovVector(float correlationTime, float biasSigma, float dt)
        {
            RecalculateConstants(correlationTime, biasSigma, dt);
            CurrentBias = Vector3.zero;
        }

        /// <summary>
        /// Recalculates internal coefficients. Call if parameters change at runtime.
        /// </summary>
        public void RecalculateConstants(float correlationTime, float biasSigma, float dt)
        {
            float beta = 1f / Mathf.Max(correlationTime, 0.001f); // Avoid divide by zero
            
            // Exact discrete-time solution coefficients
            _coeffA = Mathf.Exp(-beta * dt);
            _coeffB = biasSigma * Mathf.Sqrt(1f - Mathf.Exp(-2f * beta * dt));
        }

        /// <summary>
        /// Advances the bias by one time step. Call in FixedUpdate.
        /// </summary>
        /// <returns>The updated bias vector</returns>
        public Vector3 Step()
        {
            // x(k+1) = A*x(k) + B*noise
            Vector3 noise = Stochastic.GenerateWhiteNoiseVector(1.0f);
            
            CurrentBias = new Vector3(
                (CurrentBias.x * _coeffA) + (noise.x * _coeffB),
                (CurrentBias.y * _coeffA) + (noise.y * _coeffB),
                (CurrentBias.z * _coeffA) + (noise.z * _coeffB)
            );

            return CurrentBias;
        }

        /// <summary>
        /// Resets the bias to zero.
        /// </summary>
        public void Reset()
        {
            CurrentBias = Vector3.zero;
        }
    }

    /// <summary>
    /// Single-axis Gauss-Markov process for scalar biases.
    /// </summary>
    public class GaussMarkovScalar
    {
        private float _coeffA;
        private float _coeffB;
        
        public float CurrentBias { get; private set; }

        public GaussMarkovScalar(float correlationTime, float biasSigma, float dt)
        {
            RecalculateConstants(correlationTime, biasSigma, dt);
            CurrentBias = 0f;
        }

        public void RecalculateConstants(float correlationTime, float biasSigma, float dt)
        {
            float beta = 1f / Mathf.Max(correlationTime, 0.001f);
            _coeffA = Mathf.Exp(-beta * dt);
            _coeffB = biasSigma * Mathf.Sqrt(1f - Mathf.Exp(-2f * beta * dt));
        }

        public float Step()
        {
            CurrentBias = (CurrentBias * _coeffA) + (Stochastic.GenerateGaussian() * _coeffB);
            return CurrentBias;
        }

        public void Reset()
        {
            CurrentBias = 0f;
        }
    }
}
