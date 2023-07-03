namespace cslox
{
    internal class Resolver : IExprVisitor<object?>, IStmtVisitor<object?>
    {
        private readonly Interpreter interpreter;
        private readonly Stack<Dictionary<string, bool>> scopes = new();
        private FunctionType currentFunction = FunctionType.None;

        internal Resolver(Interpreter interpreter)
        {
            this.interpreter = interpreter;
        }

        private void BeginScope()
        {
            scopes.Push(new());
        }

        private void EndScope()
        {
            scopes.Pop();
        }

        internal void Resolve(List<Stmt?> statements)
        {
            foreach (var statement in statements)
            {
                Resolve(statement);
            }
        }

        private void Resolve(Stmt? stmt)
        {
            stmt?.Accept(this);
        }

        private void Resolve(Expr expr)
        {
            expr.Accept(this);
        }

        private void ResolveLocal(Expr expr, Token name)
        {
            for (int i = 0; i < scopes.Count; i++)
            {
                if (scopes.ElementAt(i).ContainsKey(name.lexeme))
                {
                    interpreter.Resolve(expr, i);
                    return;
                }
            }
        }

        private void ResolveFuntion(Function function, FunctionType type)
        {
            var enclosingFunction = currentFunction;
            currentFunction = type;
            BeginScope();
            foreach (var token in function.pars)
            {
                Declare(token);
                Define(token);
            }
            Resolve(function.body);
            EndScope();
            currentFunction = enclosingFunction;
        }

        private void Declare(Token name)
        {
            if (scopes.Count == 0) return;
            var scope = scopes.Peek();
            if (scope.ContainsKey(name.lexeme))
            {
                Lox.Error(name, "Already a variable with this name in this scope.");
            }
            if (scope.ContainsKey(name.lexeme))
            {
                scope[name.lexeme] = false;
            }
            else
            {
                scope.Add(name.lexeme, false);
            }
        }

        private void Define(Token name)
        {
            if (scopes.Count == 0) return;
            var scope = scopes.Peek();
            if (scope.ContainsKey(name.lexeme))
            {
                scope[name.lexeme] = true;
            }
            else
            {
                scope.Add(name.lexeme, true);
            }
        }

        public object? VisitAssignExpr(Assign expr)
        {
            Resolve(expr.value);
            ResolveLocal(expr, expr.name);
            return null;
        }

        public object? VisitBinaryExpr(Binary expr)
        {
            Resolve(expr.left);
            Resolve(expr.right);
            return null;
        }

        public object? VisitBlockStmt(Block stmt)
        {
            BeginScope();
            Resolve(stmt.statements);
            EndScope();
            return null;
        }

        public object? VisitCallExpr(Call expr)
        {
            Resolve(expr.callee);
            foreach (var argument in expr.arguments)
            {
                Resolve(argument);
            }
            return null;
        }

        public object? VisitExpressionStmt(Expression stmt)
        {
            Resolve(stmt.expression);
            return null;
        }

        public object? VisitFunctionStmt(Function stmt)
        {
            Declare(stmt.name);
            Define(stmt.name);
            ResolveFuntion(stmt, FunctionType.Function);
            return null;
        }

        public object? VisitGroupingExpr(Grouping expr)
        {
            Resolve(expr.expression);
            return null;
        }

        public object? VisitIfStmt(If stmt)
        {
            Resolve(stmt.condition);
            Resolve(stmt.thenBranch);
            if (stmt.Equals != null) Resolve(stmt.elseBranch);
            return null;
        }

        public object? VisitLiteralExpr(Literal expr)
        {
            return null;
        }

        public object? VisitLogicalExpr(Logical expr)
        {
            Resolve(expr.left);
            Resolve(expr.right);
            return null;
        }

        public object? VisitPrintStmt(Print stmt)
        {
            Resolve(stmt.expression);
            return null;
        }

        public object? VisitReturnStmt(Return stmt)
        {
            if (currentFunction == FunctionType.None)
            {
                Lox.Error(stmt.keyword, "Can't return from top-level code.");
            }
            if (stmt.value != null)
            {
                Resolve(stmt.value);
            }
            return null;
        }

        public object? VisitUnaryExpr(Unary expr)
        {
            Resolve(expr.right);
            return null;
        }

        public object? VisitVariableExpr(Variable expr)
        {
            if (scopes.Count != 0 && scopes.Peek().ContainsKey(expr.name.lexeme) && !scopes.Peek()[expr.name.lexeme])
            {
                Lox.Error(expr.name, "Can't read local variable in its own initializer.");
            }
            ResolveLocal(expr, expr.name);
            return null;
        }

        public object? VisitVarStmt(Var stmt)
        {
            Declare(stmt.name);
            if (stmt.initializer != null)
            {
                Resolve(stmt.initializer);
            }
            Define(stmt.name);
            return null;
        }

        public object? VisitWhileStmt(While stmt)
        {
            Resolve(stmt.condition);
            Resolve(stmt.body);
            return null;
        }
    }

    internal enum FunctionType
    {
        None,
        Function,
    }

}
