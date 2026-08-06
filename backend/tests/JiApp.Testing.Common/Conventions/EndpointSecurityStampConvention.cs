using System.Reflection;
using System.Reflection.Emit;
using JiApp.Common.Middleware;

namespace JiApp.Testing.Common.Conventions;

/// <summary>
/// Shared convention: every listed endpoint mapping class must wire the
/// <see cref="SecurityStampRecheckFilter"/>. Runtime endpoint metadata does not expose
/// the filter factories, so detection reflects over each endpoint's Map* method IL and
/// asserts that its call chain includes <c>AddEndpointFilter&lt;SecurityStampRecheckFilter&gt;</c>.
/// </summary>
public static class EndpointSecurityStampConvention
{
    /// <summary>
    /// Collect the listed endpoint mapping classes whose Map* method does not wire the
    /// security-stamp filter. A required type that no longer exists is reported too, so
    /// a renamed or removed endpoint fails the convention instead of passing vacuously.
    /// </summary>
    /// <param name="assembly">The service assembly that owns the endpoint classes.</param>
    /// <param name="endpointTypeNames">
    /// Full names of endpoint mapping classes that must reference the filter.
    /// </param>
    /// <returns>A <see cref="ConventionResult"/> with violations and the count of required types discovered.</returns>
    public static ConventionResult CollectEndpointsMissingSecurityStampFilter(
        Assembly assembly, IReadOnlyCollection<string> endpointTypeNames)
    {
        var violations = new List<string>();
        var scanned = 0;

        foreach (var typeName in endpointTypeNames)
        {
            var type = assembly.GetType(typeName);
            if (type is null)
            {
                violations.Add($"  {typeName} — type not found in {assembly.GetName().Name}");
                continue;
            }

            scanned++;

            var mapMethod = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name.StartsWith("Map", StringComparison.Ordinal));
            if (mapMethod is null)
            {
                violations.Add($"  {typeName} — no Map* method found");
                continue;
            }

            if (!MethodReferencesSecurityStampFilter(mapMethod))
                violations.Add($"  {typeName} — {mapMethod.Name} does not call AddEndpointFilter<SecurityStampRecheckFilter>");
        }

        return new ConventionResult(violations, scanned);
    }

    private static bool MethodReferencesSecurityStampFilter(MethodInfo method)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray();
        if (il is null)
            return false;

        var module = method.Module;
        var offset = 0;
        while (offset < il.Length)
        {
            var opCode = ReadOpCode(il, ref offset);
            if (opCode.OperandType == OperandType.InlineMethod)
            {
                var token = ReadInt32(il, ref offset);
                MethodInfo? called;
                try
                {
                    called = module.ResolveMethod(token) as MethodInfo;
                }
                catch
                {
                    continue;
                }

                if (called is { IsGenericMethod: true }
                    && called.Name == "AddEndpointFilter"
                    && called.DeclaringType?.Name == "EndpointFilterExtensions"
                    && called.GetGenericArguments().Contains(typeof(SecurityStampRecheckFilter)))
                {
                    return true;
                }
            }
            else
            {
                SkipOperand(opCode, il, ref offset);
            }
        }

        return false;
    }

    private static void SkipOperand(OpCode opCode, byte[] il, ref int offset)
    {
        switch (opCode.OperandType)
        {
            case OperandType.InlineField:
            case OperandType.InlineType:
            case OperandType.InlineTok:
            case OperandType.InlineString:
            case OperandType.InlineSig:
            case OperandType.InlineBrTarget:
            case OperandType.InlineI:
            case OperandType.ShortInlineR:
                offset += 4;
                break;
            case OperandType.ShortInlineBrTarget:
            case OperandType.ShortInlineI:
            case OperandType.ShortInlineVar:
                offset += 1;
                break;
            case OperandType.InlineSwitch:
                var count = ReadInt32(il, ref offset);
                offset += count * 4;
                break;
            case OperandType.InlineI8:
            case OperandType.InlineR:
                offset += 8;
                break;
            case OperandType.InlineVar:
                offset += 2;
                break;
            case OperandType.InlineNone:
#pragma warning disable CS0618 // deprecated, but must be handled so the IL walker never desyncs
            case OperandType.InlinePhi:
#pragma warning restore CS0618
                break;
        }
    }

    private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(f => (OpCode)f.GetValue(null)!)
        .ToDictionary(o => o.Value, o => o);

    private static OpCode ReadOpCode(byte[] il, ref int offset)
    {
        var first = il[offset++];
        short value = first;
        if (first == 0xFE)
        {
            var second = il[offset++];
            value = (short)((first << 8) | second);
        }
        return OpCodesByValue[value];
    }

    private static int ReadInt32(byte[] il, ref int offset)
    {
        var value = BitConverter.ToInt32(il, offset);
        offset += 4;
        return value;
    }
}
