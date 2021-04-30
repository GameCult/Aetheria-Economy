#if UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_EDITOR) || UNITY_WSA
public partial class AkBasePathGetter
{
	static string DefaultPlatformName = "Windows";
}
#endif