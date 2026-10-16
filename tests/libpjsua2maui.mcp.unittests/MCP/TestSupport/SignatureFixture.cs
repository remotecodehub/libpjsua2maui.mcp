namespace libpjsua2maui.mcp.tests.MCP.TestSupport;

public interface ISignatureFixture
{
    string Name { get; set; }
}

public enum SignatureFixtureEnum
{
    None,
    Value
}

public readonly struct SignatureFixtureStruct
{
    public int Value { get; init; }
}

public delegate void SignatureFixtureDelegate(string value);

public class SignatureFixture<T> : ISignatureFixture where T : class
{
    public sealed class Nested
    {
    }

    public const int Constant = 1;
    public event EventHandler? Changed;
    public string Name { get; set; } = string.Empty;
    public T? Item { get; set; }

    public void Primitive(
        bool boolean,
        char character,
        sbyte signedByte,
        byte unsignedByte,
        short shortValue,
        ushort unsignedShort,
        int integer,
        uint unsignedInteger,
        long longValue,
        ulong unsignedLong,
        float single,
        double doubleValue,
        string text,
        object value)
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public string[] Array(string[] values) => values;

    public int[,] Matrix(int[,] values) => values;

    public int[,,] Cube(int[,,] values) => values;

    public List<T> Generic(List<T> values) => values;

    public T Echo<TValue>(TValue value) => Item!;

    public void ByReference(ref int value, out string text)
    {
        text = value.ToString();
    }

    public IntPtr Native(IntPtr value, UIntPtr unsignedValue) => value;

    public SignatureFixtureEnum EnumValue(SignatureFixtureEnum value) => value;
}
