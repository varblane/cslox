namespace cslox
{
    internal class Clock : ICallable
    {
        public int Arity()
        {
            return 0;
        }

        public object Call(Interpreter interpreter, List<object?> arguments)
        {
            return DateTime.Now.Ticks / 1000.0;
        }

        public override string ToString()
        {
            return "<native fn>";
        }
    }
}
