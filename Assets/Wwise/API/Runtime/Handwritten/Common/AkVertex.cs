[System.Obsolete(AkSoundEngine.Deprecation_2019_2_0)]
public class AkVertex
{
	private UnityEngine.Vector3 Vector = UnityEngine.Vector3.zero;

	public void Zero() { Vector.Set(0, 0, 0); }

	public float X { set { Vector.x = value; } get { return Vector.x; } }

	public float Y { set { Vector.y = value; } get { return Vector.y; } }

	public float Z { set { Vector.z = value; } get { return Vector.z; } }

	public static implicit operator UnityEngine.Vector3(AkVertex vector) { return vector.Vector; }

	public AkVertex() { }

	public AkVertex(float x, float y, float z) { Vector.Set(x, y, z); }

	public void Clear() { Vector.Set(0, 0, 0); }

	public static int GetSizeOf() { return sizeof(float) * 3; }

	public void Clone(AkVertex other) { Vector = other.Vector; }
}

[System.Obsolete(AkSoundEngine.Deprecation_2019_2_0)]
/// AkVertexArray is now deprecated. Users should instead use UnityEngine.Vector3[].
public class AkVertexArray : AkBaseArray<AkVertex>
{
	public AkVertexArray(int count) : base(count) {}

	protected override int StructureSize { get { return AkVertex.GetSizeOf(); } }

	protected override AkVertex CreateNewReferenceFromIntPtr(System.IntPtr address) { return null; }

	protected override void CloneIntoReferenceFromIntPtr(System.IntPtr address, AkVertex other) {}
}