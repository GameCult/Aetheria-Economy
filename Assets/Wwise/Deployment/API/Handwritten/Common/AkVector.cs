[System.Obsolete(AkSoundEngine.Deprecation_2019_2_0)]
public class AkVector
{
	private UnityEngine.Vector3 Vector = UnityEngine.Vector3.zero;

	public void Zero() { Vector.Set(0, 0, 0); }

	public float X { set { Vector.x = value; } get { return Vector.x; } }

	public float Y { set { Vector.y = value; } get { return Vector.y; } }

	public float Z { set { Vector.z = value; } get { return Vector.z; } }

	public static implicit operator UnityEngine.Vector3(AkVector vector) { return vector.Vector; }
}
