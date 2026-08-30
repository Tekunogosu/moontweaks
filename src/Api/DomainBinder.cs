using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

    /// <summary>
    /// Everything about one parameter that is the same on every call, worked out once
    /// when the function is bound rather than each time it is called. A script filling
    /// a shape crosses this boundary tens of thousands of times a second, and the name
    /// of an argument does not change between any two of them.
    /// </summary>
    /// <param name="Type">What the parameter holds.</param>
    /// <param name="IsOrigin">Whether it is the injected script line rather than an argument.</param>
    /// <param name="Path">How the parameter is named in a failure.</param>
    private sealed record Bound(Type Type, bool IsOrigin, string Path);

    private static FunctionBinding BindFunction(object domain, string modulePath, MethodInfo method)
    {
        var name = method.GetCustomAttribute<LuaFunctionAttribute>()!.Name;
        var call = $"{modulePath}.{name}";
        var count = ArgumentsOf(method).Count;
        var invoke = InvokerFor(method);

        var parameters = method.GetParameters()
            .Select(parameter => new Bound(
                parameter.ParameterType,
                parameter.ParameterType == typeof(ScriptOrigin),
                $"{call} argument '{parameter.Name}'"))
            .ToArray();

        return new FunctionBinding(name, (origin, values) =>
        {
            if (values.Count > count)
            {
                throw new ScriptError(origin, $"{call} takes {count} argument(s), got {values.Count}");
            }

            var supplied = new object?[parameters.Length];
            var index = 0;

            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].IsOrigin)
                {
                    supplied[i] = origin;
                    continue;
                }

                var value = index < values.Count ? values[index] : ScriptValue.Nil.Instance;
                supplied[i] = SpecBinder.Convert(parameters[i].Type, value, origin, parameters[i].Path);
                index++;
            }

            try
            {
                return Lift(invoke(domain, supplied));
            }
            catch (ScriptError)
            {
                // Already says where it happened and what the author got wrong.
                throw;
            }
            catch (Exception failure)
            {
                // Anything else is a failure this mod did not anticipate: a game call
                // refusing something no check covers, or a mistake of its own. Either
                // way it reaches a script author, who can act on the line it happened
                // on and can act on nothing at all without it. The type is named
                // because an unanticipated failure is worth reporting as a bug, and a
                // bare sentence is not enough to chase one with.
                throw new ScriptError(origin,
                    $"{call} failed unexpectedly ({failure.GetType().Name}): {failure.Message}");
            }
        });
    }

    /// <summary>
    /// Compiles a direct call to the bound method, so calling it costs what calling a
    /// delegate costs rather than what reflection costs.
    /// </summary>
    /// <remarks>
    /// This also puts whatever the method threw in the caller's hands unchanged, where
    /// reflection wrapped it in a <see cref="TargetInvocationException"/> that had to
    /// be unwrapped again. A <see cref="ScriptError"/> has to reach the run loop as
    /// itself: caught there it names the script, the line and the mistake, and uncaught
    /// it takes the mod down with a stack trace in place of that sentence. What the
    /// caller does with anything else it threw is decided in <see cref="BindFunction"/>,
    /// which is the last place the line it happened on is still known.
    /// </remarks>
    private static Func<object, object?[], object?> InvokerFor(MethodInfo method)
    {
        var target = Expression.Parameter(typeof(object), "target");
        var arguments = Expression.Parameter(typeof(object?[]), "arguments");

        var read = method.GetParameters()
            .Select((parameter, index) => (Expression)Expression.Convert(
                Expression.ArrayIndex(arguments, Expression.Constant(index)), parameter.ParameterType));

        var invocation = Expression.Call(
            Expression.Convert(target, method.DeclaringType!), method, read);

        // A method returning nothing still has to hand something back, since every
        // binding answers the same way and the lift above reads a null as nil.
        var body = method.ReturnType == typeof(void)
            ? Expression.Block(invocation, Expression.Constant(null, typeof(object)))
            : (Expression)Expression.Convert(invocation, typeof(object));

        return Expression.Lambda<Func<object, object?[], object?>>(body, target, arguments).Compile();
    }

    /// <summary>Lifts a return value back into the neutral model.</summary>
    private static ScriptValue Lift(object? result) => PayloadWriter.Value(result);
}
