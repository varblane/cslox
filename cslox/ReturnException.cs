namespace cslox
{
    internal class ReturnException : Exception
    {
        internal readonly object? value;

        internal ReturnException(object? value)
        {
            this.value = value;
        }
    }
}
