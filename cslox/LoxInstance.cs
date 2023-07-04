namespace cslox
{
    internal class LoxInstance
    {
        private LoxClass klass;
        private readonly Dictionary<string, object?> fields = new();

        internal LoxInstance(LoxClass klass)
        {
            this.klass = klass;
        }

        internal void Set(Token name, object? value)
        {
            if (!fields.ContainsKey(name.lexeme))
            {
                fields.Add(name.lexeme, value);
            }
            else
            {
                fields[name.lexeme] = value;
            }
        }

        internal object? Get(Token name)
        {
            if (fields.ContainsKey(name.lexeme))
            {
                return fields[name.lexeme];
            }

            var method = klass.FindMethod(name.lexeme);
            if (method != null) return method.Bind(this);

            throw new RuntimeError(name, "Undefined property '" + name.lexeme + "'.");
        }

        public override string ToString()
        {
            return klass.name + " instance";
        }
    }
}
