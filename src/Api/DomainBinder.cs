using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MoonTweaks.Scripting;

namespace MoonTweaks.Api;

/// <summary>
/// Turns a domain object annotated with <see cref="LuaModuleAttribute"/> into the
/// neutral bindings a script host exposes. Every callable function must take a
/// <see cref="ScriptOrigin"/> first so failures can name the line that caused them.
/// </summary>
public static class DomainBinder
{
    /// <summary>Builds the module binding for one annotated domain object.</summary>
    public static ModuleBinding Bind(object domain)
    {
        var type = domain.GetType();
        var module = type.GetCustomAttribute<LuaModuleAttribute>()
            ?? throw new InvalidOperationException($"{type.Name} is not annotated with [LuaModule]");

        var functions = FunctionsOf(type)
            .Select(method => BindFunction(domain, module.Path, method))
            .ToList();

        return new ModuleBinding(module.Path, functions);
    }

    /// <summary>Every function a module exposes, in declaration order.</summary>
    public static IEnumerable<MethodInfo> FunctionsOf(Type moduleType) =>
        moduleType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.GetCustomAttribute<LuaFunctionAttribute>() is not null);

    /// <summary>Parameters a script actually supplies, excluding the injected origin.</summary>
    public static IReadOnlyList<ParameterInfo> ArgumentsOf(MethodInfo method) =>
        method.GetParameters().Where(parameter => parameter.ParameterType != typeof(ScriptOrigin)).ToList();

    private static FunctionBinding BindFunction(object domain, string modulePath, MethodInfo method)
    {
        var name = method.GetCustomAttribute<LuaFunctionAttribute>()!.Name;
        var parameters = method.GetParameters();
        var arguments = ArgumentsOf(method);
        var call = $"{modulePath}.{name}";

        return new FunctionBinding(name, (origin, values) =>
        {
            if (values.Count > arguments.Count)
            {
                throw new ScriptError(origin,
                    $"{call} takes {arguments.Count} argument(s), got {values.Count}");
            }

            var supplied = new object?[parameters.Length];
            var index = 0;

            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType == typeof(ScriptOrigin))
                {
                    supplied[i] = origin;
                    continue;
                }

                var value = index < values.Count ? values[index] : ScriptValue.Nil.Instance;
                supplied[i] = Coerce(parameters[i], value, origin, $"{call} argument '{parameters[i].Name}'");
                index++;
            }

            return Lift(method.Invoke(domain, supplied));
        });
    }

    /// <summary>Converts one argument, sending table shapes through the spec binder.</summary>
    private static object? Coerce(ParameterInfo parameter, ScriptValue value, ScriptOrigin origin, string path)
    {
        if (parameter.ParameterType == typeof(string))
        {
            return value is ScriptValue.Str s
                ? s.Value
                : throw new ScriptError(origin, $"{path} expects a string, got {value.TypeName}");
        }

        return SpecBinder.Bind(parameter.ParameterType, value, origin, path);
    }

    /// <summary>Lifts a return value back into the neutral model.</summary>
    private static ScriptValue Lift(object? result) => result switch
    {
        null => ScriptValue.Nil.Instance,
        string s => new ScriptValue.Str(s),
        int i => new ScriptValue.Num(i),
        bool b => new ScriptValue.Bool(b),
        _ => ScriptValue.Nil.Instance,
    };
}
