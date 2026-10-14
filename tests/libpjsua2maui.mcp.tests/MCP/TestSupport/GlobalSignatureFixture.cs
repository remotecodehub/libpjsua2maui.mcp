public sealed class GlobalSignatureFixture
{
    public event EventHandler? Changed;

    public void Raise()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
