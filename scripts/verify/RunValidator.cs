using System;
using System.Linq;
using System.Reflection;

public static class RunValidator
{
    public static string Run()
    {
        Type type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("SengokuChronicle.Editor.FoundationSceneValidator", false))
            .FirstOrDefault(t => t != null);
        if (type == null)
            throw new InvalidOperationException("SengokuChronicle.Editor.FoundationSceneValidator not found in loaded assemblies");

        MethodInfo method = type.GetMethod("ValidateActiveScene",
            BindingFlags.Public | BindingFlags.Static);
        if (method == null)
            throw new InvalidOperationException("ValidateActiveScene(public static) not found");

        method.Invoke(null, null);
        return "validator invoked";
    }
}
