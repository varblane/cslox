using System.Text;

namespace cslox
{
    internal class AstPrinter : IExprVisitor<string>
    {
        internal string Print(Expr expr)
        {
            return expr.Accept(this);
        }

        public string VisitBinaryExpr(Binary expr)
        {
            return Parenthesize(expr.op.lexeme, expr.left, expr.right);
        }

        public string VisitLogicalExpr(Logical expr)
        {
            return Parenthesize(expr.op.lexeme, expr.left, expr.right);
        }

        public string VisitGroupingExpr(Grouping expr)
        {
            return Parenthesize("group", expr.expression);
        }

        public string VisitLiteralExpr(Literal expr)
        {
            return expr.value?.ToString() ?? "nil";
        }

        public string VisitUnaryExpr(Unary expr)
        {
            return Parenthesize(expr.op.lexeme, expr.right);
        }

        public string VisitVariableExpr(Variable expr)
        {
            return Parenthesize(expr.name.lexeme);
        }

        public string VisitAssignExpr(Assign expr)
        {
            return Parenthesize(expr.name.lexeme);
        }

        public string VisitCallExpr(Call expr)
        {
            return Parenthesize("call");
        }

        private string Parenthesize(string name, params Expr[] exprs)
        {
            var builder = new StringBuilder();
            builder.Append('(').Append(name);
            foreach (var expr in exprs)
            {
                builder.Append(' ');
                builder.Append(expr.Accept(this));
            }
            builder.Append(')');
            return builder.ToString();
        }
    }
}
