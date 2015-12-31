using LuaCP.IR.User;

namespace LuaCP.IR
{
    public enum ValueKind
    {
        Reference,
        Tuple,
        Value,
    }

    public interface IValue : IUsable<IValue>
    {
        ValueKind Kind { get; }
    }

    public static class ValueExtensions
    {
        public static bool IsNil(this IValue value)
        {
            Constant constant = value as Constant;
            return constant != null && constant.Literal.Kind == LiteralKind.Nil;
        }
    }
}
