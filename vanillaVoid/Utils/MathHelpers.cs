﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace vanillaVoid.Utils
{
    public static class MathHelpers
    {
        /// <summary>
        /// Converts a float number to a percentage string. Default is base 100, so 2 = 200%.
        /// </summary>
        /// <param name="number">The number you wish to convert to a percentage.</param>
        /// <param name="numberBase">The multiplier or base of the number.</param>
        /// <returns>A string representing the percentage value of the number converted using our number base.</returns>
        public static string FloatToPercentageString(float number, float numberBase = 100)
        {
            return (number * numberBase).ToString("##0") + "%";
        }

        /// <summary>
        /// Returns a Vector3 representing the closest point on a sphere to another point at a set radius.
        /// </summary>
        /// <param name="origin">The starting position.</param>
        /// <param name="radius">The radius of the sphere.</param>
        /// <param name="targetPosition">The target's position.</param>
        /// <returns>The point on the sphere closest to the target position.</returns>
        public static Vector3 ClosestPointOnSphereToPoint(Vector3 origin, float radius, Vector3 targetPosition)
        {
            Vector3 differenceVector = targetPosition - origin;
            differenceVector = Vector3.Normalize(differenceVector);
            differenceVector *= radius;

            return origin + differenceVector;
        }

        /// <summary>
        /// A method to create a list of points evenly spread around a sphere at a set radius.
        /// </summary>
        /// <param name="points">The number of points to spread around a sphere.</param>
        /// <param name="radius">The radius of our sphere.</param>
        /// <param name="origin">The point of reference for our sphere.</param>
        /// <returns>A list of points evenly distributed around the desired sphere.</returns>
        public static List<Vector3> DistributePointsEvenlyAroundSphere(int points, float radius, Vector3 origin)
        {
            List<Vector3> pointArray = new List<Vector3>();

            var phi = Math.PI * (3 - Math.Sqrt(5));

            for (int i = 0; i < points; i++)
            {
                var yCoord = 1 - (i / (points - 1)) * 2;
                var radiusCoord = Math.Sqrt(1 - yCoord * yCoord);
                var theta = phi * i;

                var xCoord = (float)(Math.Cos(theta) * radiusCoord);
                var zCoord = (float)(Math.Sin(theta) * radiusCoord);

                var calculatedPoint = origin + new Vector3(xCoord, yCoord, zCoord);

                pointArray.Add(calculatedPoint * radius);
            }

            return pointArray;

        }

        /// <summary>
        /// A method to create a list of points evenly spread around a circle at a set radius. Three points make a triangle, four make a square, and so on.
        /// </summary>
        /// <param name="points">The number of points to spread around a circle.</param>
        /// <param name="radius">The radius of our circle.</param>
        /// <param name="origin">The point of reference for our circle.</param>
        /// <param name="angleOffset">How far along the circle should we shift all our points?</param>
        /// <returns>A list of points evenly distributed around the desired circle.</returns>
        public static List<Vector3> DistributePointsEvenlyAroundCircle(int points, float radius, Vector3 origin, float angleOffset = 0)
        {
            List<Vector3> pointsList = new List<Vector3>();
            for (int i = 0; i < points; i++)
            {
                var theta = (Math.PI * 2) / points;
                var angle = theta * i + angleOffset;
                Vector3 positionChosen;

                positionChosen = new Vector3((float)(radius * Math.Cos(angle) + origin.x), origin.y, (float)(radius * Math.Sin(angle) + origin.z));

                pointsList.Add(positionChosen);
            }

            return pointsList;
        }

        public static Vector3 GetPointOnUnitSphereCap(Quaternion targetDirection, float angle)
        {
            var angleInRad = UnityEngine.Random.Range(0.0f, angle) * Mathf.Deg2Rad;
            var PointOnCircle = (UnityEngine.Random.insideUnitCircle.normalized) * Mathf.Sin(angleInRad);
            var V = new Vector3(PointOnCircle.x, PointOnCircle.y, Mathf.Cos(angleInRad));
            return targetDirection * V;
        }

        public static Vector3 GetPointOnUnitSphereCap(Vector3 targetDirection, float angle)
        {
            return GetPointOnUnitSphereCap(Quaternion.LookRotation(targetDirection), angle);
        }

        public static Vector3 RandomPointOnCircle(Vector3 origin, float radius, Xoroshiro128Plus random)
        {
            float angle = random.RangeFloat(0, 2f * Mathf.PI);
            return origin + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
        }

        /// <summary>
        /// Calculates inverse hyperbolic scaling (diminishing) for the parameters passed in, and returns the result.
        /// <para>Uses the formula: baseValue + (maxValue - baseValue) * (1 - 1 / (1 + additionalValue * (itemCount - 1)))</para>
        /// </summary>
        /// <param name="baseValue">The starting value of the function.</param>
        /// <param name="additionalValue">The value that is added per additional itemCount</param>
        /// <param name="maxValue">The maximum value that the function can possibly be.</param>
        /// <param name="itemCount">The amount of items/stacks that increments our function.</param>
        /// <returns>A float representing the inverse hyperbolic scaling of the parameters.</returns>
        public static float InverseHyperbolicScaling(float baseValue, float additionalValue, float maxValue, int itemCount)
        {
            return baseValue + (maxValue - baseValue) * (1 - 1 / (1 + additionalValue * (itemCount - 1)));
        }
    }
}
