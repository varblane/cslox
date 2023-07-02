namespace cslox
{
    internal class Interpreter : IExprVisitor<object?>, IStmtVisitor<object?>
    {
        private Environment environment = new();

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

        public object? VisitVariableExpr(Variable expr)
        {
            return environment.Get(expr.name);
        }

        public object? VisitAssignExpr(Assign expr)
        {
            var value = Evaluate(expr.value);
            environment.Assign(expr.name, value);
            return value;
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

        private void ExecuteBlock(List<Stmt?> statements, Environment environment)
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
