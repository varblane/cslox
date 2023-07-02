namespace cslox
{
    internal class LoxFunction : ICallable
    {
        private readonly Function declaration;
        private readonly Environment closure;

        internal LoxFunction(Function declaration, Environment closure)
        {
            this.declaration = declaration;
            this.closure = closure;
        }

        public int Arity()
        {
            return declaration.pars.Count;
        }

        public object? Call(Interpreter interpreter, List<object?> arguments)
        {
            var environment = new Environment(closure);
            for (int i = 0; i < declaration.pars.Count; i++)
            {
                environment.Define(declaration.pars[i].lexeme, arguments[i]);
            }
            try
            {
                interpreter.ExecuteBlock(declaration.body, environment);
            }
            catch (ReturnException returnValue)
            {
                return returnValue.value;
            }
            return null;
        }

        public override string ToString()
        {
            return "<fn " + declaration.name.lexeme + ">";
        }
    }
}
