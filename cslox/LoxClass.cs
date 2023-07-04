namespace cslox
{
    internal class LoxClass : ICallable
    {
        internal readonly string name;
        private readonly Dictionary<string, LoxFunction> methods;

        internal LoxClass(string name, Dictionary<string, LoxFunction> methods)
        {
            this.name = name;
            this.methods = methods;
        }

        public int Arity()
        {
            var initializer = FindMethod("init");
            if (initializer == null) return 0;
            return initializer.Arity();
        }

        public object? Call(Interpreter interpreter, List<object?> arguments)
        {
            var instance = new LoxInstance(this);
            var initializer = FindMethod("init");
            if (initializer != null)
            {
                initializer.Bind(instance).Call(interpreter, arguments);
            }
            return instance;
        }

        public override string ToString()
        {
            return name;
        }

        internal LoxFunction? FindMethod(string lexeme)
        {
            if (methods.ContainsKey(lexeme))
            {
                return methods[lexeme];
            }
            return null;
        }
    }
}
