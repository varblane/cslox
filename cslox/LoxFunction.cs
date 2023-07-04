namespace cslox
{
    internal class LoxFunction : ICallable
    {
        private readonly Function declaration;
        private readonly Environment closure;
        private readonly bool isInitializer;

        internal LoxFunction(Function declaration, Environment closure, bool isInitializer)
        {
            this.declaration = declaration;
            this.closure = closure;
            this.isInitializer = isInitializer;
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
                if (isInitializer) return closure.GetAt(0, "this");
                return returnValue.value;
            }
            if (isInitializer) return closure.GetAt(0, "this");
            return null;
        }

        internal LoxFunction Bind(LoxInstance instance)
        {
            var environment = new Environment(closure);
            environment.Define("this", instance);
            return new LoxFunction(declaration, environment, isInitializer);
        }

        public override string ToString()
        {
            return "<fn " + declaration.name.lexeme + ">";
        }
    }
}
