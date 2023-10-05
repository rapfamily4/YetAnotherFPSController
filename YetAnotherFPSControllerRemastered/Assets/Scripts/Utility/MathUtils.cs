using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathUtils {
    public static void SpringDamper(ref float position, ref float velocity, float elasticity, float damp) {
        SpringDamper(ref position, ref velocity, elasticity, damp, Time.deltaTime);
    }

    public static void SpringDamper(ref float position, ref float velocity, float elasticity, float damp, float deltaTime) {
        // Compute acceleration through spring-damping force (mass = 1)
        float hooke = -elasticity * position;
        float stoke = -damp * velocity;
        float acceleration = hooke + stoke;

        // Update velocity and position
        velocity += acceleration * deltaTime;
        position += velocity * deltaTime + acceleration * deltaTime * deltaTime * 0.5f;
    }

    public static void SpringDamper(ref Vector3 position, ref Vector3 velocity, float elasticity, float damp) {
        SpringDamper(ref position, ref velocity, elasticity, damp, Time.deltaTime);
    }

    public static void SpringDamper(ref Vector3 position, ref Vector3 velocity, float elasticity, float damp, float deltaTime) {
        // Compute acceleration through spring-damping force (mass = 1)
        Vector3 hooke = -elasticity * position;
        Vector3 stoke = -damp * velocity;
        Vector3 acceleration = hooke + stoke;

        // Update velocity and position
        velocity += acceleration * deltaTime;
        position += velocity * deltaTime + acceleration * deltaTime * deltaTime * 0.5f;
    }

    public static void SpringDamperVariableDeltaTime(ref float position, ref float velocity, float elasticity, float damp) {
        // Handle a deltaTime greater than the fixedDeltaTime
        float delta = Time.deltaTime;
        while (delta > Time.fixedDeltaTime) {
            SpringDamper(ref position, ref velocity, elasticity, damp, Time.fixedDeltaTime);
            delta -= Time.fixedDeltaTime;
        }

        // By this point, delta is less or equal than fixedDeltaTime
        SpringDamper(ref position, ref velocity, elasticity, damp, delta);
    }

    public static void SpringDamperVariableDeltaTime(ref Vector3 position, ref Vector3 velocity, float elasticity, float damp) {
        // Handle a deltaTime greater than the fixedDeltaTime
        float delta = Time.deltaTime;
        while (delta > Time.fixedDeltaTime) {
            SpringDamper(ref position, ref velocity, elasticity, damp, Time.fixedDeltaTime);
            delta -= Time.fixedDeltaTime;
        }

        // By this point, delta is less or equal than fixedDeltaTime
        SpringDamper(ref position, ref velocity, elasticity, damp, delta);
    }

    public static float ConvertToNewRange (float value, float oldMin, float oldMax, float newMin, float newMax) {
        // Source: https://stackoverflow.com/questions/929103/convert-a-number-range-to-another-range-maintaining-ratio
        return ((value - oldMin) * (newMax - newMin) / (oldMax - oldMin)) + newMin;
    }

    public static Vector3 ConvertToNewRange (Vector3 value, float oldMin, float oldMax, float newMin, float newMax) {
        return new Vector3(
            ConvertToNewRange(value.x, oldMin, oldMax, newMin, newMax),
            ConvertToNewRange(value.y, oldMin, oldMax, newMin, newMax),
            ConvertToNewRange(value.z, oldMin, oldMax, newMin, newMax)
        );
    }
}
