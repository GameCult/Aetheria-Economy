using System.Collections;
using System.Collections.Generic;
using MessagePack;
using MessagePack.Resolvers;

public static class RegisterResolver
{
    private static bool registered = false;
    public static void Register()
    {
        if (registered) return;
        
        // Set extensions to default resolver.
        var resolver = CompositeResolver.Create(
            MathResolver.Instance,
            NativeGuidResolver.Instance,
            StandardResolver.Instance
        );
        var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
        MessagePackSerializer.DefaultOptions = options;

        registered = true;
    }
}
