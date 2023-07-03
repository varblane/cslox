namespace cslox
{
    internal class Environment
    {
        internal readonly Environment? enclosing;
        private readonly Dictionary<string, object?> values = new();

        internal Environment()
        {
            enclosing = null;
        }

        internal Environment(Environment enclosing)
        {
            this.enclosing = enclosing;
        }

        internal void Define(string name, object? value)
        {
            if (values.ContainsKey(name))
            {
                values[name] = value;
            }
            else
            {
                values.Add(name, value);
            }
        }

        internal void Assign(Token name, object? value)
        {
            if (values.ContainsKey(name.lexeme))
            {
                values[name.lexeme] = value;
                return;
            }
            if (enclosing != null)
            {
                enclosing.Assign(name, value);
                return;
            }
            throw new RuntimeError(name, "Undefined variable '" + name.lexeme + "'.");
        }

        internal object? Get(Token name)
        {
            if (values.ContainsKey(name.lexeme))
            {
                return values[name.lexeme];
            }
            if (enclosing != null) return enclosing.Get(name);
            throw new RuntimeError(name, "Undefined variable '" + name.lexeme + "'.");
        }

        internal object? GetAt(int distance, string name)
        {
            var ancestor = Ancestor(distance);
            if (ancestor != null && ancestor.values.ContainsKey(name))
            {
                return ancestor.values[name];
            }
            return null;
        }

        internal void AssignAt(int distance, Token name, object? value)
        {
            var ancestor = Ancestor(distance);
            if (ancestor != null) ancestor.values[name.lexeme] = value;
        }

        private Environment? Ancestor(int distance)
        {
            var environment = this;
            for (int i = 0; i < distance; i++)
            {
                environment = environment?.enclosing;
            }
            return environment;
        }
    }
}
