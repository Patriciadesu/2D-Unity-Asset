// ClassChecker.cs
using System;
using System.Linq;
using System.Reflection;

public static class ClassChecker
{
    /// <summary>
    /// Checks if a class with the given name exists in the project (loaded assemblies).
    /// </summary>
    /// <param name="className">Name of the class to check (case sensitive).</param>
    /// <returns>True if the class exists, false otherwise.</returns>
    public static bool Exists(string className)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(asm => asm.GetTypes())
            .Any(type => type.Name == className || type.FullName == className);
    }

    /// <summary>
    /// Tries to get the Type of a class if it exists.
    /// </summary>
    /// <param name="className">Class name (with or without namespace).</param>
    /// <param name="type">Out Type if found.</param>
    /// <returns>True if found, false otherwise.</returns>
    public static bool TryGetType(string className, out Type type)
    {
        type = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(asm => asm.GetTypes())
            .FirstOrDefault(t => t.Name == className || t.FullName == className);

        return type != null;
    }
}
