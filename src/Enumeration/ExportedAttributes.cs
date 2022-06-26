namespace Enumeration;

#pragma warning disable IDE0001
internal class EnumerationAttribute : global::System.Attribute
{
    public EnumerationAttribute(params global::System.Type[] types)
    {
        this.Types = global::System.Collections.Immutable.ImmutableArray.CreateRange(types);
    }

    public global::System.Collections.Immutable.ImmutableArray<Type> Types { get; }
}

internal class ConstructorForAttribute : global::System.Attribute
{
    public ConstructorForAttribute(global::System.Type constructor, global::System.Type type)
    {
        this.Constructor = constructor;
        this.Type = type;
    }

    public global::System.Type Constructor { get; private set; }
    public global::System.Type Type { get; private set; }
}
#pragma warning restore IDE0001