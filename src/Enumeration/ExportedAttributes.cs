namespace Enumeration;

#pragma warning disable IDE0001, IDE0049, IDE0002
[global::System.AttributeUsage(global::System.AttributeTargets.Struct | global::System.AttributeTargets.Struct, AllowMultiple = false)]
internal class EnumerationAttribute : global::System.Attribute
{
}

[global::System.AttributeUsage(global::System.AttributeTargets.Struct | global::System.AttributeTargets.Struct, AllowMultiple = true)]
internal class ConstructorAttribute : global::System.Attribute
{
    public ConstructorAttribute(global::System.Type constructor, global::System.Type type)
    {
        this.Constructor = constructor;
        this.Type = type;
    }

    public global::System.Type Constructor { get; private set; }
    public global::System.Type Type { get; private set; }
}

[global::System.AttributeUsage(global::System.AttributeTargets.Struct | global::System.AttributeTargets.Struct, AllowMultiple = true)]
internal class CaseAttribute : global::System.Attribute
{
    public CaseAttribute(global::System.String identifier, params global::System.Type[] types)
    {
        this.Identifier = identifier;
        this.Types =  global::System.Collections.Immutable.ImmutableArray.CreateRange(types);
    }

    public global::System.String Identifier { get; }
    public global::System.Collections.Immutable.ImmutableArray<global::System.Type> Types { get; }
}
#pragma warning restore IDE0001, IDE0049, IDE0002