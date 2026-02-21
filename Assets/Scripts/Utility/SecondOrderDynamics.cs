using UnityEngine;

/// <summary>
/// Frame-rate-independent second-order dynamics (spring-damper) for smooth value tracking.
/// Replaces exponential lerp with physically-motivated motion that supports underdamping
/// for organic feel and handles discontinuities (teleportation) gracefully.
///
/// The system models: x'' + 2*zeta*omega*x' + omega^2*x = omega^2*target
/// where zeta is the damping ratio and omega is the natural frequency.
///
/// Usage:
///   var spring = new SecondOrderDynamics(responseTime: 0.3f, dampingRatio: 0.9f, initialValue: 0f);
///   // Each frame:
///   float smoothed = spring.Update(dt, targetValue);
///   // On teleport:
///   spring.SnapTo(newValue);
/// </summary>
[System.Serializable]
public struct SecondOrderDynamics
{
    private float position;
    private float velocity;
    private float omega;  // Natural frequency: 2*PI / responseTime
    private float zeta;   // Damping ratio (1.0 = critical, <1 = underdamped, >1 = overdamped)

    /// <summary>
    /// Creates a new second-order dynamics system.
    /// </summary>
    /// <param name="responseTime">Time in seconds to reach ~95% of target (lower = faster)</param>
    /// <param name="dampingRatio">1.0 = critically damped (no overshoot), 0.9 = slight organic overshoot</param>
    /// <param name="initialValue">Starting position value</param>
    public SecondOrderDynamics(float responseTime, float dampingRatio, float initialValue)
    {
        omega = 2f * Mathf.PI / Mathf.Max(0.001f, responseTime);
        zeta = Mathf.Clamp(dampingRatio, 0.1f, 2f);
        position = initialValue;
        velocity = 0f;
    }

    /// <summary>
    /// Advances the system by dt seconds, tracking toward the given target.
    /// Uses the analytical solution for critically damped case, semi-implicit Euler otherwise.
    /// </summary>
    public float Update(float dt, float target)
    {
        if (dt <= 0f) return position;

        // Clamp dt to prevent instability from huge time steps (e.g., after unpause)
        dt = Mathf.Min(dt, 0.1f);

        float error = position - target;

        if (Mathf.Abs(zeta - 1f) < 0.01f)
        {
            // Critically damped (zeta ~= 1): closed-form analytical solution
            // x(t) = target + e^(-w*t) * (error + (velocity + w*error)*t)
            float expTerm = Mathf.Exp(-omega * dt);
            float c2 = velocity + omega * error;
            position = target + expTerm * (error + c2 * dt);
            velocity = expTerm * (c2 - omega * (error + c2 * dt));
        }
        else if (zeta < 1f)
        {
            // Underdamped: analytical solution with damped oscillation
            float omegaD = omega * Mathf.Sqrt(1f - zeta * zeta);
            float expTerm = Mathf.Exp(-zeta * omega * dt);
            float cosTerm = Mathf.Cos(omegaD * dt);
            float sinTerm = Mathf.Sin(omegaD * dt);

            // Coefficients from initial conditions: x(0) = error, x'(0) = velocity
            float c1 = error;
            float c2 = (velocity + zeta * omega * error) / omegaD;

            position = target + expTerm * (c1 * cosTerm + c2 * sinTerm);
            velocity = expTerm * (
                (c2 * omegaD - c1 * zeta * omega) * cosTerm
                - (c1 * omegaD + c2 * zeta * omega) * sinTerm
            );
        }
        else
        {
            // Overdamped (zeta > 1): semi-implicit Euler (safe fallback, rarely used)
            float accel = omega * omega * (target - position) - 2f * zeta * omega * velocity;
            velocity += accel * dt;
            position += velocity * dt;
        }

        return position;
    }

    /// <summary>
    /// Immediately moves to the given value with zero velocity.
    /// Use for teleportation or mode transitions.
    /// </summary>
    public void SnapTo(float value)
    {
        position = value;
        velocity = 0f;
    }

    /// <summary>
    /// Returns the current position without advancing time.
    /// </summary>
    public float Current => position;

    /// <summary>
    /// Returns the current velocity.
    /// </summary>
    public float CurrentVelocity => velocity;

    /// <summary>
    /// Reconfigures the response time without resetting state.
    /// </summary>
    public void SetResponseTime(float responseTime)
    {
        omega = 2f * Mathf.PI / Mathf.Max(0.001f, responseTime);
    }

    /// <summary>
    /// Reconfigures the damping ratio without resetting state.
    /// </summary>
    public void SetDampingRatio(float dampingRatio)
    {
        zeta = Mathf.Clamp(dampingRatio, 0.1f, 2f);
    }
}
