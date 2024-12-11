using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace TLab.SFU
{
	public static class QuaternionExtension
	{
		public static Vector4 ToVec(this Quaternion quaternion) => new Vector4(quaternion.x, quaternion.y, quaternion.z, quaternion.w);

		public static Quaternion ToQuaternion(this Vector4 vector4) => new Quaternion(vector4.x, vector4.y, vector4.z, vector4.w);

		public static Quaternion LerpQuaternion(this Vector4 quaternion0, Vector4 quaternion1, float t) => Quaternion.Lerp(quaternion0.ToQuaternion(), quaternion1.ToQuaternion(), t);
	}

	public static class ComponentExtension
	{
		public static void Foreach<T>(UnityAction<T> action) where T : MonoBehaviour
		{
			var @objects = UnityEngine.Object.FindObjectsOfType<T>();
			foreach (var @object in @objects)
				action.Invoke(@object);
		}

		public static void Foreach<T>(this GameObject self, UnityAction<T> action) where T : MonoBehaviour
		{
			var @objects = self.GetComponentsInChildren<T>();
			foreach (var @object in @objects)
				action.Invoke(@object);
		}

		public static void Foreach<T>(this T[] @objects, UnityAction<T> action)
		{
			if (objects == null)
				return;

			foreach (var @object in @objects)
				action.Invoke(@object);
		}

		public static void Foreach<T>(this ICollection<T> @objects, UnityAction<T> action)
		{
			if (objects == null)
				return;

			foreach (var @object in @objects)
				action.Invoke(@object);
		}

		public static T[] GetComponentsInTargets<T>(GameObject[] targets) where T : Component
		{
			if (targets == null)
				return new T[0];

			var componentList = new List<T>();
			foreach (GameObject target in targets)
			{
				var component = target.GetComponent<T>();
				if (component != null)
					componentList.Add(component);
			}
			return componentList.ToArray();
		}

		public static T RequireComponent<T>(this GameObject self, UnityAction<T> callback = null) where T : Component
		{
			var result = self.GetComponent<T>();

			if (result == null)
				result = self.AddComponent<T>();

			callback?.Invoke(result);

			return result;
		}

		public static void RemoveComponent<T>(this GameObject self) where T : Component
		{
			var result = self.GetComponent<T>();

			if (result != null)
				UnityEngine.Object.Destroy(result);

			UnityEngine.Object.Destroy(result);
		}
	}

	/// <summary>
	/// By adding BeginStart and EndStart at the beginning and end of Start, MonoBehaviours with
	/// OnEnable and OnDisable logic can wrap their contents within a _started flag and effectively
	/// skip over logic in those methods until after Start has been invoked.
	///
	/// To not bypass the Unity Lifecycle, the enabled property is used to disable the most derived
	/// MonoBehaviour, invoke Start up the hierarchy chain, and finally re-enable the MonoBehaviour.
	/// </summary>
	public static class MonoBehaviourStartExtensions
	{
		public static void BeginStart(this MonoBehaviour monoBehaviour, ref bool started, Action baseStart = null)
		{
			if (!started)
			{
				monoBehaviour.enabled = false;
				started = true;
				baseStart?.Invoke();
				started = false;
			}
			else
			{
				baseStart?.Invoke();
			}
		}

		public static void EndStart(this MonoBehaviour monoBehaviour, ref bool started)
		{
			if (!started)
			{
				started = true;
				monoBehaviour.enabled = true;
			}
		}
	}

	public static class GizmosExtensions
	{
		// https://github.com/code-beans/GizmoExtensions/tree/master

		/// <summary>
		/// Draws a wire cube with a given rotation 
		/// </summary>
		/// <param name="center"></param>
		/// <param name="size"></param>
		/// <param name="rotation"></param>
		public static void DrawWireCube(Vector3 center, Vector3 size, Quaternion rotation = default(Quaternion))
		{
			if (rotation.Equals(default(Quaternion)))
				rotation = Quaternion.identity;

			var old = Gizmos.matrix;
			Gizmos.matrix = Matrix4x4.TRS(center, rotation, size);
			Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
			Gizmos.matrix = old;
		}

		public static void DrawArrow(Vector3 from, Vector3 to, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
		{
			Gizmos.DrawLine(from, to);
			var direction = to - from;
			var right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
			var left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
			Gizmos.DrawLine(to, to + right * arrowHeadLength);
			Gizmos.DrawLine(to, to + left * arrowHeadLength);
		}

		public static void DrawWireSphere(Vector3 center, float radius, Quaternion rotation = default(Quaternion))
		{
			if (rotation.Equals(default(Quaternion)))
				rotation = Quaternion.identity;

			var old = Gizmos.matrix;
			Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
			Gizmos.DrawWireSphere(Vector3.zero, radius);
			Gizmos.matrix = old;
		}


		/// <summary>
		/// Draws a flat wire circle (up)
		/// </summary>
		/// <param name="center"></param>
		/// <param name="radius"></param>
		/// <param name="segments"></param>
		/// <param name="rotation"></param>
		public static void DrawWireCircle(Vector3 center, float radius, int segments = 20, Quaternion rotation = default(Quaternion))
		{
			DrawWireArc(center, radius, 360, segments, rotation);
		}

		/// <summary>
		/// Draws an arc with a rotation around the center
		/// </summary>
		/// <param name="center">center point</param>
		/// <param name="radius">radiu</param>
		/// <param name="angle">angle in degrees</param>
		/// <param name="segments">number of segments</param>
		/// <param name="rotation">rotation around the center</param>
		public static void DrawWireArc(Vector3 center, float radius, float angle, int segments = 20,
			Quaternion rotation = default(Quaternion))
		{
			if (rotation.Equals(default(Quaternion)))
				rotation = Quaternion.identity;

			var old = Gizmos.matrix;

			Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
			var from = Vector3.forward * radius;
			var step = angle / segments;

			if (step <= float.Epsilon)
				return;

			for (float i = 0; i <= angle; i += step)
			{
				var to = new Vector3(radius * Mathf.Sin(i * Mathf.Deg2Rad), 0, radius * Mathf.Cos(i * Mathf.Deg2Rad));
				Gizmos.DrawLine(from, to);
				from = to;
			}

			{
				var to = new Vector3(radius * Mathf.Sin(angle * Mathf.Deg2Rad), 0, radius * Mathf.Cos(angle * Mathf.Deg2Rad));
				Gizmos.DrawLine(from, to);
			}

			Gizmos.matrix = old;
		}


		/// <summary>
		/// Draws an arc with a rotation around an arbitraty center of rotation
		/// </summary>
		/// <param name="center">the circle's center point</param>
		/// <param name="radius">radius</param>
		/// <param name="angle">angle in degrees</param>
		/// <param name="segments">number of segments</param>
		/// <param name="rotation">rotation around the centerOfRotation</param>
		/// <param name="centerOfRotation">center of rotation</param>
		public static void DrawWireArc(Vector3 center, float radius, float angle, int segments, Quaternion rotation, Vector3 centerOfRotation)
		{
			if (rotation.Equals(default(Quaternion)))
				rotation = Quaternion.identity;

			var old = Gizmos.matrix;

			Gizmos.matrix = Matrix4x4.TRS(centerOfRotation, rotation, Vector3.one);
			var deltaTranslation = centerOfRotation - center;
			var from = deltaTranslation + Vector3.forward * radius;
			var step = angle / segments;

			if (step <= float.Epsilon)
				return;

			for (float i = 0; i <= angle; i += step)
			{
				var to = new Vector3(radius * Mathf.Sin(i * Mathf.Deg2Rad), 0, radius * Mathf.Cos(i * Mathf.Deg2Rad)) + deltaTranslation;
				Gizmos.DrawLine(from, to);
				from = to;
			}

			{
				var to = new Vector3(radius * Mathf.Sin(angle * Mathf.Deg2Rad), 0, radius * Mathf.Cos(angle * Mathf.Deg2Rad));
				Gizmos.DrawLine(from, to);
			}

			Gizmos.matrix = old;
		}

		/// <summary>
		/// Draws an arc with a rotation around an arbitraty center of rotation
		/// </summary>
		/// <param name="matrix">Gizmo matrix applied before drawing</param>
		/// <param name="radius">radius</param>
		/// <param name="angle">angle in degrees</param>
		/// <param name="segments">number of segments</param>
		public static void DrawWireArc(Matrix4x4 matrix, float radius, float angle, int segments)
		{
			var old = Gizmos.matrix;

			Gizmos.matrix = matrix;
			var from = Vector3.forward * radius;
			var step = angle / segments;

			if (step <= float.Epsilon)
				return;

			for (float i = 0; i <= angle; i += step)
			{
				var to = new Vector3(radius * Mathf.Sin(i * Mathf.Deg2Rad), 0, radius * Mathf.Cos(i * Mathf.Deg2Rad));
				Gizmos.DrawLine(from, to);
				from = to;
			}

			{
				var to = new Vector3(radius * Mathf.Sin(angle * Mathf.Deg2Rad), 0, radius * Mathf.Cos(angle * Mathf.Deg2Rad));
				Gizmos.DrawLine(from, to);
			}

			Gizmos.matrix = old;
		}

		/// <summary>
		/// Draws a wire cylinder face up with a rotation around the center
		/// </summary>
		/// <param name="center"></param>
		/// <param name="radius"></param>
		/// <param name="height"></param>
		/// <param name="rotation"></param>
		public static void DrawWireCylinder(Vector3 center, float radius, float height, Quaternion rotation = default(Quaternion))
		{
			if (rotation.Equals(default(Quaternion)))
				rotation = Quaternion.identity;

			var old = Gizmos.matrix;

			Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
			var half = height / 2;

			//draw the 4 outer lines
			Gizmos.DrawLine(Vector3.right * radius - Vector3.up * half, Vector3.right * radius + Vector3.up * half);
			Gizmos.DrawLine(-Vector3.right * radius - Vector3.up * half, -Vector3.right * radius + Vector3.up * half);
			Gizmos.DrawLine(Vector3.forward * radius - Vector3.up * half, Vector3.forward * radius + Vector3.up * half);
			Gizmos.DrawLine(-Vector3.forward * radius - Vector3.up * half, -Vector3.forward * radius + Vector3.up * half);

			//draw the 2 cricles with the center of rotation being the center of the cylinder, not the center of the circle itself
			DrawWireArc(center + Vector3.up * half, radius, 360, 20, rotation, center);
			DrawWireArc(center + Vector3.down * half, radius, 360, 20, rotation, center);
			Gizmos.matrix = old;
		}

		/// <summary>
		/// Draws a wire capsule face up
		/// </summary>
		/// <param name="center"></param>
		/// <param name="radius"></param>
		/// <param name="height"></param>
		/// <param name="rotation"></param>
		public static void DrawWireCapsule(Vector3 center, float radius, float height, Quaternion rotation = default(Quaternion))
		{
			if (rotation.Equals(default(Quaternion)))
				rotation = Quaternion.identity;

			var old = Gizmos.matrix;
			Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
			var half = height / 2 - radius;

			//draw cylinder base
			DrawWireCylinder(center, radius, height - radius * 2, rotation);

			//draw upper cap
			//do some cool stuff with orthogonal matrices
			var mat = Matrix4x4.Translate(center + rotation * Vector3.up * half) * Matrix4x4.Rotate(rotation * Quaternion.AngleAxis(90, Vector3.forward));
			DrawWireArc(mat, radius, 180, 20);
			mat = Matrix4x4.Translate(center + rotation * Vector3.up * half) * Matrix4x4.Rotate(rotation * Quaternion.AngleAxis(90, Vector3.up) * Quaternion.AngleAxis(90, Vector3.forward));
			DrawWireArc(mat, radius, 180, 20);

			//draw lower cap
			mat = Matrix4x4.Translate(center + rotation * Vector3.down * half) * Matrix4x4.Rotate(rotation * Quaternion.AngleAxis(90, Vector3.up) * Quaternion.AngleAxis(-90, Vector3.forward));
			DrawWireArc(mat, radius, 180, 20);
			mat = Matrix4x4.Translate(center + rotation * Vector3.down * half) * Matrix4x4.Rotate(rotation * Quaternion.AngleAxis(-90, Vector3.forward));
			DrawWireArc(mat, radius, 180, 20);

			Gizmos.matrix = old;
		}

		/// <summary>
		/// Draws a visualization for Physics.BoxCast
		/// </summary>
		/// <param name="center"></param>
		/// <param name="halfExtents"></param>
		/// <param name="direction"></param>
		/// <param name="orientation"></param>
		/// <param name="maxDistance"></param>
		/// <param name="showCast"></param>
		public static void DrawBoxCast(
			Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, bool showCast = false)
		{
			DrawBoxCast(center, halfExtents, direction, orientation, maxDistance, Physics.DefaultRaycastLayers, showCast);
		}

		/// <summary>
		/// Draws a visualization for Physics.BoxCast
		/// </summary>
		/// <param name="center"></param>
		/// <param name="halfExtents"></param>
		/// <param name="direction"></param>
		/// <param name="orientation"></param>
		/// <param name="maxDistance"></param>
		/// <param name="layerMask"></param>
		/// <param name="showCast"></param>
		public static void DrawBoxCast(
			Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, int layerMask, bool showCast)
		{
			// calculate vars
			var gizmosColor = Gizmos.color;
			var end = center + direction * maxDistance;
			var halfExtentsZ = orientation * Vector3.forward * halfExtents.z;
			var halfExtentsY = orientation * Vector3.up * halfExtents.y;
			var halfExtentsX = orientation * Vector3.right * halfExtents.x;

			// change color and draw hitmarker if show BoxCast 
			if (showCast && Physics.BoxCast(center, halfExtents, direction, out RaycastHit hitInfo, orientation, maxDistance, layerMask))
			{
				Gizmos.color = Gizmos.color == Color.red ? Color.magenta : Color.red;
				Gizmos.DrawWireCube(hitInfo.point, 0.25f * Vector3.one);
			}

			// draw boxes
			var matrix = Gizmos.matrix;
			Gizmos.matrix = Matrix4x4.TRS(center, orientation, Vector3.one);
			Gizmos.DrawWireCube(Vector3.zero, 2 * halfExtents);
			Gizmos.matrix = Matrix4x4.TRS(end, orientation, Vector3.one);
			Gizmos.DrawWireCube(Vector3.zero, 2 * halfExtents);
			Gizmos.matrix = matrix;

			// draw connect lines 1
			Gizmos.DrawLine(center - halfExtentsX - halfExtentsY - halfExtentsZ, end - halfExtentsX - halfExtentsY - halfExtentsZ);
			Gizmos.DrawLine(center + halfExtentsX - halfExtentsY - halfExtentsZ, end + halfExtentsX - halfExtentsY - halfExtentsZ);
			Gizmos.DrawLine(center - halfExtentsX + halfExtentsY - halfExtentsZ, end - halfExtentsX + halfExtentsY - halfExtentsZ);
			Gizmos.DrawLine(center + halfExtentsX + halfExtentsY - halfExtentsZ, end + halfExtentsX + halfExtentsY - halfExtentsZ);

			// draw connect lines 2
			Gizmos.DrawLine(center - halfExtentsX - halfExtentsY + halfExtentsZ, end - halfExtentsX - halfExtentsY + halfExtentsZ);
			Gizmos.DrawLine(center + halfExtentsX - halfExtentsY + halfExtentsZ, end + halfExtentsX - halfExtentsY + halfExtentsZ);
			Gizmos.DrawLine(center - halfExtentsX + halfExtentsY + halfExtentsZ, end - halfExtentsX + halfExtentsY + halfExtentsZ);
			Gizmos.DrawLine(center + halfExtentsX + halfExtentsY + halfExtentsZ, end + halfExtentsX + halfExtentsY + halfExtentsZ);

			// reset color
			Gizmos.color = gizmosColor;
		}

	}
}
