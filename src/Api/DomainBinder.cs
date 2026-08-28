using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
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

            return Lift(Invoke(method, domain, supplied));
        });
    }

    /// <summary>
    /// Calls the bound method, unwrapping what reflection wraps around whatever it
    /// threw. A <see cref="ScriptError"/> has to reach the run loop as itself: caught
    /// there it names the script, the line and the mistake, and uncaught it takes the
    /// mod down with a stack trace in place of that sentence.
    /// </summary>
    private static object? Invoke(MethodInfo method, object domain, object?[] supplied)
    {
        try
        {
            return method.Invoke(domain, supplied);
        }
        catch (TargetInvocationException wrapped) when (wrapped.InnerException is { } cause)
        {
            ExceptionDispatchInfo.Capture(cause).Throw();
            throw;
        }
    }

    /// <summary>
    /// Converts one argument through the same conversion a table key uses, so a
    /// function may take anything a table may hold.
    /// </summary>
    private static object? Coerce(ParameterInfo parameter, ScriptValue value, ScriptOrigin origin, string path) =>
        SpecBinder.Convert(parameter.ParameterType, value, origin, path);

    /// <summary>Lifts a return value back into the neutral model.</summary>
    private static ScriptValue Lift(object? result) => PayloadWriter.Value(result);
}
