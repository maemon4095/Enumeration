using Enumeration;

var instance = C.M4(20, 2, "obj", "str", new byte[] { 1 });
var b = B<int, string>.M0();
Console.WriteLine(b);
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
partial struct B<T0, T1>
    where T1 : class
{
    public static partial B<T0, T1> M0();
    public static partial B<T0, T1> M1(int n0, int n1);
    public static partial B<T0, T1> M2(ulong num, string str);
    public static partial B<T0, T1> M3(object obj, T0 generic0, T1 generic1, byte b0);
}