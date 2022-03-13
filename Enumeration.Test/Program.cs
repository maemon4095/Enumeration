using Enumeration;

var instance = C.M4(20, 2, "obj", "str", new byte[] { 1 });
var b = B<int, string>.M0();

switch (instance.Type)
{
    case C.Case.M0:
    {
        instance.M0();
    }
    break;
    case C.Case.M1:
    {
        instance.M1(out var num);
    }
    break;
    case C.Case.M2:
    {
        instance.M2(out var num, out var str);
        Console.WriteLine($"num: {num}, str: {str}");
    }
    break;
    case C.Case.M3:
    {
        instance.M3(out var obj, out var str, out var array);
    }
    break;
    case C.Case.M4:
    {
        instance.M4(out var num, out var a, out var obj, out var str, out var array);
        Console.WriteLine($"num: {num}, a:{a}, obj: {obj} str: {str}, array: {array}");
    }
    break;

    default: break;
}

Console.ReadLine();

[Enumeration]
partial struct C
{
    public static partial C M0();
    public static partial C M1(int num);
    public static partial C M2(ulong num, string str);
    public static partial C M3(object obj, string str, byte[] array);
    public static partial C M4(int num, int a, object obj, string str, byte[] array);
}

[Enumeration]
partial class B<T0, T1>
    where T1 : class
{
    public static partial B<T0, T1> M0();
    public static partial B<T0, T1> M1(int n0, int n1);
    public static partial B<T0, T1> M2(ulong num, string str);
    public static partial B<T0, T1> M3(object obj, T0 generic0, T1 generic1, byte b0);
}