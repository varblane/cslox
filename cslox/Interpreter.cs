namespace cslox
{
    internal class Interpreter : IExprVisitor<object?>, IStmtVisitor<object?>
    {
        internal readonly Environment globals;
        private Environment environment;
        private readonly Dictionary<Expr, int> locals;

        internal Interpreter()
        {
            globals = new();
            locals = new();
            environment = globals;
            globals.Define("clock", new Clock());
        }

        internal void Interpret(List<Stmt?> statements)
        {
            try
            {
                foreach (var statement in statements)
                {
                    if (statement != null)
                    {
                        Execute(statement);
                    }
                }
            }
            catch (RuntimeError e)
            {
                Lox.RuntimeError(e);
            }
        }

        private void Execute(Stmt stmt)
        {
            stmt.Accept(this);
        }

        internal void Resolve(Expr expr, int depth)
        {
            locals.Add(expr, depth);
        }

        private object? LookUpvariable(Token name, Expr expr)
        {
            if (locals.TryGetValue(expr, out var distance))
            {
                return environment.GetAt(distance, name.lexeme);
            }
            return globals.Get(name);
        }

        private static string Stringify(object? obj)
        {
            return obj?.ToString() ?? "nil";
        }

        public object? VisitBinaryExpr(Binary expr)
        {
            var left = Evaluate(expr.left);
            var right = Evaluate(expr.right);
            if (left == null || right == null) return null;
            switch (expr.op.type)
            {
                case TokenType.PLUS:
                    if (left is double && right is double)
                    {
                        return (double)left + (double)right;
                    }
                    if (left is string && right is string)
                    {
                        return (string)left + (string)right;
                    }
                    throw new RuntimeError(expr.op, "Operands must be two numbers or two strings.");
                case TokenType.MINUS:
                    CheckNumberOperands(expr.op, left, right);
                    return (double)left - (double)right;
                case TokenType.SLASH:
                    CheckNumberOperands(expr.op, left, right);
                    return (double)left / (double)right;
                case TokenType.STAR:
                    CheckNumberOperands(expr.op, left, right);
                    return (double)left * (double)right;
                case TokenType.GREATER:
                    CheckNumberOperands(expr.op, left, right);
                    return (double)left > (double)right;
                case TokenType.GREATER_EQUAL:
                    CheckNumberOperands(expr.op, left, right);
                    return (double)left >= (double)right;
                case TokenType.LESS:
                    CheckNumberOperands(expr.op, left, right);
                    return (double)left < (double)right;
                case TokenType.LESS_EQUAL:
                    CheckNumberOperands(expr.op, left, right);
                    return (double)left <= (double)right;
                case TokenType.EQUAL_EQUAL:
                    return IsEqual(left, right);
                case TokenType.BANG_EQUAL:
                    return !IsEqual(left, right);
            }
            return null;
        }

        public object? VisitLogicalExpr(Logical expr)
        {
            var left = Evaluate(expr.left);
            if (expr.op.type == TokenType.OR)
            {
                if (IsTruthy(left)) return left;
            }
            else
            {
                if (!IsTruthy(left)) return left;
            }
            return Evaluate(expr.right);
        }

        public object? VisitGroupingExpr(Grouping expr)
        {
            return Evaluate(expr.expression);
        }

        public object? VisitLiteralExpr(Literal expr)
        {
            return expr.value;
        }

        public object? VisitUnaryExpr(Unary expr)
        {
            var right = Evaluate(expr.right);
            if (right == null) return null;
            switch (expr.op.type)
            {
                case TokenType.BANG:
                    return !IsTruthy(right);
                case TokenType.MINUS:
                    CheckNumberOperand(expr.op, right);
                    return -(double)right;
            }
            return null;
        }

        public object? VisitCallExpr(Call expr)
        {
            var callee = Evaluate(expr.callee);
            var arguments = new List<object?>();
            foreach (var argument in expr.arguments)
            {
                arguments.Add(Evaluate(argument));
            }
            if (callee is not ICallable)
            {
                throw new RuntimeError(expr.paren, "Can only call functions and classes.");
            }
            var function = (ICallable)callee;
            if (arguments.Count != function.Arity())
            {
                throw new RuntimeError(expr.paren, "Expected " + function.Arity() + " arguments but got " + arguments.Count + ".");
            }
            return function.Call(this, arguments);
        }

        public object? VisitVariableExpr(Variable expr)
        {
            return LookUpvariable(expr.name, expr);
        }

        public object? VisitAssignExpr(Assign expr)
        {
            var value = Evaluate(expr.value);
            if (locals.TryGetValue(expr, out var distance))
            {
                environment.AssignAt(distance, expr.name, value);
            }
            else
            {
                globals.Assign(expr.name, value);
            }
            return value;
        }

        public object? VisitGetExpr(Get expr)
        {
            var obj = Evaluate(expr.obj);
            if (obj is LoxInstance)
            {
                return ((LoxInstance)obj).Get(expr.name);
            }
            throw new RuntimeError(expr.name, "Only instances have properties.");
        }

        public object? VisitSetExpr(Set expr)
        {
            var obj = Evaluate(expr.obj);
            if (obj is not LoxInstance)
            {
                throw new RuntimeError(expr.name, "Only instances have fields.");
            }
            var value = Evaluate(expr.value);
            ((LoxInstance)obj).Set(expr.name, value);
            return value;
        }

        public object? VisitThisExpr(This expr)
        {
            return LookUpvariable(expr.keyword, expr);
        }

        private static bool IsEqual(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null) return false;
            return a.Equals(b);
        }

        private static bool IsTruthy(object? obj)
        {
            if (obj == null) return false;
            if (obj is bool) return (bool)obj;
            return true;
        }

        private object? Evaluate(Expr expr)
        {
            return expr.Accept(this);
        }

        private static void CheckNumberOperand(Token op, object operand)
        {
            if (operand is double) return;
            throw new RuntimeError(op, "Operand must be a number.");
        }

        private static void CheckNumberOperands(Token op, object left, object right)
        {
            if (left is double && right is double) return;
            throw new RuntimeError(op, "Operands must be numbers.");
        }

        public object? VisitExpressionStmt(Expression stmt)
        {
            Evaluate(stmt.expression);
            return null;
        }

        public object? VisitPrintStmt(Print stmt)
        {
            var value = Evaluate(stmt.expression);
            Console.WriteLine(Stringify(value));
            return null;
        }

        public object? VisitVarStmt(Var stmt)
        {
            object? value = null;
            if (stmt.initializer != null)
            {
                value = Evaluate(stmt.initializer);
            }
            environment.Define(stmt.name.lexeme, value);
            return null;
        }

        public object? VisitBlockStmt(Block stmt)
        {
            ExecuteBlock(stmt.statements, new Environment(environment));
            return null;
        }

        public object? VisitIfStmt(If stmt)
        {
            if (IsTruthy(Evaluate(stmt.condition)))
            {
                Execute(stmt.thenBranch);
            }
            else if (stmt.elseBranch != null)
            {
                Execute(stmt.elseBranch);
            }
            return null;
        }

        public object? VisitWhileStmt(While stmt)
        {
            while (IsTruthy(Evaluate(stmt.condition)))
            {
                Execute(stmt.body);
            }
            return null;
        }

        public object? VisitFunctionStmt(Function stmt)
        {
            var function = new LoxFunction(stmt, environment, false);
            environment.Define(stmt.name.lexeme, function);
            return null;
        }

        public object? VisitReturnStmt(Return stmt)
        {
            object? value = null;
            if (stmt.value != null) value = Evaluate(stmt.value);
            throw new ReturnException(value);
        }

        public object? VisitClassStmt(Class stmt)
        {
            environment.Define(stmt.name.lexeme, null);
            var methods = new Dictionary<string, LoxFunction>();
            foreach (var method in stmt.methods)
            {
                var function = new LoxFunction(method, environment, method.name.lexeme.Equals("init"));
                if (methods.ContainsKey(method.name.lexeme))
                {
                    methods[method.name.lexeme] = function;
                }
                else
                {
                    methods.Add(method.name.lexeme, function);
                }
            }
            var klass = new LoxClass(stmt.name.lexeme, methods);
            environment.Assign(stmt.name, klass);
            return null;
        }

        internal void ExecuteBlock(List<Stmt?> statements, Environment environment)
        {
            var previous = this.environment;
            try
            {
                this.environment = environment;
                foreach (var statement in statements)
                {
                    if (statement != null)
                    {
                        Execute(statement);
                    }
                }
            }
            finally
            {
                this.environment = previous;
            }
        }
    }
}
