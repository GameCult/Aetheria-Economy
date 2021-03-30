using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public static class CultCacheExtensions
{
    private static Dictionary<Type,Type[]> InterfaceClasses = new Dictionary<Type, Type[]>();
    public static Type[] GetAllInterfaceClasses(this Type type)
    {
        if (InterfaceClasses.ContainsKey(type))
            return InterfaceClasses[type];
        return InterfaceClasses[type] = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(ass => ass.GetTypes()).Where(t => t.IsClass && t.GetInterfaces().Contains(type)).ToArray();
    }
    
    private static Dictionary<Type,Type[]> ParentTypes = new Dictionary<Type, Type[]>();

    public static Type[] GetParentTypes(this Type type)
    {
        if (ParentTypes.ContainsKey(type))
            return ParentTypes[type];
        return ParentTypes[type] = type.GetParents().ToArray();
    }
    
    private static IEnumerable<Type> GetParents(this Type type)
    {
        // is there any base type?
        if (type == null)
        {
            yield break;
        }

        yield return type;

        // return all implemented or inherited interfaces
        foreach (var i in type.GetInterfaces())
        {
            yield return i;
        }

        // return all inherited types
        var currentBaseType = type.BaseType;
        while (currentBaseType != null)
        {
            yield return currentBaseType;
            currentBaseType= currentBaseType.BaseType;
        }
    }
	
    private static Dictionary<Type,Type[]> ChildClasses = new Dictionary<Type, Type[]>();
    public static Type[] GetAllChildClasses(this Type type)
    {
        if (ChildClasses.ContainsKey(type))
            return ChildClasses[type];
        return ChildClasses[type] = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(ass => ass.GetTypes()).Where(type.IsAssignableFrom).ToArray();
    }
    
    public static async void WrapAwait(this Task task)
    {
        await task;
    }
}