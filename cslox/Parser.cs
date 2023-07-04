namespace cslox
{
    internal class Parser
    {
        private readonly List<Token> tokens;
        private int current = 0;

        internal Parser(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        internal List<Stmt?> Parse()
        {
            var statements = new List<Stmt?>();
            while (!IsAtEnd())
            {
                statements.Add(Declaration());
            }
            return statements;
        }

        private Stmt? Declaration()
        {
            try
            {
                if (Match(TokenType.CLASS)) return ClassDeclaration();
                if (Match(TokenType.FUN)) return Function("function");
                if (Match(TokenType.VAR)) return VarDeclaration();
                return Statement();
            }
            catch (ParseException)
            {
                Synchronize();
                return null;
            }
        }

        private Stmt ClassDeclaration()
        {
            var name = Consume(TokenType.IDENTIFIER, "Expect class name.");
            Variable? superclass = null;
            if (Match(TokenType.LESS))
            {
                Consume(TokenType.IDENTIFIER, "Expect superclass name.");
                superclass = new Variable(Previous());
            }
            Consume(TokenType.LEFT_BRACE, "Expect '{' before class body.");
            var methods = new List<Function>();
            while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
            {
                methods.Add(Function("method"));
            }
            Consume(TokenType.RIGHT_BRACE, "Expect '}' after class body.");
            return new Class(name, superclass, methods);
        }

        private Function Function(string kind)
        {
            var name = Consume(TokenType.IDENTIFIER, "Expect " + kind + " name.");
            Consume(TokenType.LEFT_PAREN, "Expect '(' after " + kind + " name.");
            var parameters = new List<Token>();
            if (!Check(TokenType.RIGHT_PAREN))
            {
                do
                {
                    if (parameters.Count >= 255)
                    {
                        Error(Peek(), "Can't have more than 255 parameters.");
                    }
                    parameters.Add(Consume(TokenType.IDENTIFIER, "Expect parameter name."));
                } while (Match(TokenType.COMMA));
            }
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after parameters.");
            Consume(TokenType.LEFT_BRACE, "Expect '{' before " + kind + " body.");
            var body = BlockStatement();
            return new Function(name, parameters, body);
        }

        private Stmt VarDeclaration()
        {
            var name = Consume(TokenType.IDENTIFIER, "Expect variable name.");
            Expr? initializer = null;
            if (Match(TokenType.EQUAL))
            {
                initializer = Expression();
            }
            Consume(TokenType.SEMICOLON, "Expect ';' after variable declaration.");
            return new Var(name, initializer);
        }

        private Stmt Statement()
        {
            if (Match(TokenType.FOR)) return ForStatement();
            if (Match(TokenType.IF)) return IfStatement();
            if (Match(TokenType.PRINT)) return PrintStatement();
            if (Match(TokenType.RETURN)) return ReturnStatement();
            if (Match(TokenType.WHILE)) return WhileStatement();
            if (Match(TokenType.LEFT_BRACE)) return new Block(BlockStatement());
            return ExpressionStatement();
        }

        private Stmt ReturnStatement()
        {
            var keyword = Previous();
            Expr? value = null;
            if (!Check(TokenType.SEMICOLON))
            {
                value = Expression();
            }
            Consume(TokenType.SEMICOLON, "Expect ';' after return value.");
            return new Return(keyword, value);
        }

        private Stmt ForStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expect '(' after 'for'.");
            Stmt? initializer;
            if (Match(TokenType.SEMICOLON))
            {
                initializer = null;
            }
            else if (Match(TokenType.VAR))
            {
                initializer = VarDeclaration();
            }
            else
            {
                initializer = ExpressionStatement();
            }
            Expr? condition = null;
            if (!Check(TokenType.SEMICOLON))
            {
                condition = Expression();
            }
            Consume(TokenType.SEMICOLON, "Expect ';' after loop condition.");
            Expr? increment = null;
            if (!Check(TokenType.SEMICOLON))
            {
                increment = Expression();
            }
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after for clauses.");
            var body = Statement();
            if (increment != null)
            {
                body = new Block(new List<Stmt?>() { body, new Expression(increment) });
            }
            if (condition == null) condition = new Literal(true);
            body = new While(condition, body);
            if (initializer != null)
            {
                body = new Block(new List<Stmt?>() { initializer, body });
            }
            return body;
        }

        private Stmt WhileStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expect '(' after 'while'.");
            var condition = Expression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after if condition.");
            var body = Statement();
            return new While(condition, body);
        }

        private Stmt IfStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expect '(' after 'if'.");
            var condition = Expression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after if condition.");
            var thenBranch = Statement();
            Stmt? elseBranch = null;
            if (Match(TokenType.ELSE))
            {
                elseBranch = Statement();
            }
            return new If(condition, thenBranch, elseBranch);
        }

        private List<Stmt?> BlockStatement()
        {
            var statements = new List<Stmt?>();
            while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
            {
                statements.Add(Declaration());
            }
            Consume(TokenType.RIGHT_BRACE, "Expect '}' after block.");
            return statements;
        }

        private Stmt PrintStatement()
        {
            var value = Expression();
            Consume(TokenType.SEMICOLON, "Expect ';' after value.");
            return new Print(value);
        }

        private Stmt ExpressionStatement()
        {
            var expression = Expression();
            Consume(TokenType.SEMICOLON, "Expect ';' after expression.");
            return new Expression(expression);
        }

        private Expr Expression()
        {
            return Assignment();
        }

        private Expr Assignment()
        {
            var expr = Or();
            if (Match(TokenType.EQUAL))
            {
                var equals = Previous();
                var value = Assignment();
                if (expr is Variable)
                {
                    var name = ((Variable)expr).name;
                    return new Assign(name, value);
                }
                else if (expr is Get)
                {
                    var get = (Get)expr;
                    return new Set(get.obj, get.name, value);
                }
                Error(equals, "Invalid assignemt target.");
            }
            return expr;
        }

        private Expr Or()
        {
            var expr = And();
            while (Match(TokenType.OR))
            {
                var op = Previous();
                var right = And();
                expr = new Logical(expr, op, right);
            }
            return expr;
        }

        private Expr And()
        {
            var expr = Equality();
            while (Match(TokenType.AND))
            {
                var op = Previous();
                var right = Equality();
                expr = new Logical(expr, op, right);
            }
            return expr;
        }

        private Expr Equality()
        {
            var expr = Comparison();
            while (Match(TokenType.BANG_EQUAL, TokenType.EQUAL_EQUAL))
            {
                var op = Previous();
                var right = Comparison();
                expr = new Binary(expr, op, right);
            }
            return expr;
        }

        private Expr Comparison()
        {
            var expr = Term();
            while (Match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL))
            {
                var op = Previous();
                var right = Term();
                expr = new Binary(expr, op, right);
            }
            return expr;
        }

        private Expr Term()
        {
            var expr = Factor();
            while (Match(TokenType.MINUS, TokenType.PLUS))
            {
                var op = Previous();
                var right = Term();
                expr = new Binary(expr, op, right);
            }
            return expr;
        }

        private Expr Factor()
        {
            var expr = Unary();
            while (Match(TokenType.SLASH, TokenType.STAR))
            {
                var op = Previous();
                var right = Term();
                expr = new Binary(expr, op, right);
            }
            return expr;
        }

        private Expr Unary()
        {
            if (Match(TokenType.BANG, TokenType.MINUS))
            {
                var op = Previous();
                var right = Unary();
                return new Unary(op, right);
            }
            return Call();
        }

        private Expr Call()
        {
            var expr = Primary();
            while (true)
            {
                if (Match(TokenType.LEFT_PAREN))
                {
                    expr = FinishCall(expr);
                }
                else if (Match(TokenType.DOT))
                {
                    var name = Consume(TokenType.IDENTIFIER, "Expect property name after '.'.");
                    expr = new Get(expr, name);
                }
                else
                {
                    break;
                }
            }
            return expr;
        }

        private Expr FinishCall(Expr callee)
        {
            var arguments = new List<Expr>();
            if (!Check(TokenType.RIGHT_PAREN))
            {
                do
                {
                    if (arguments.Count >= 255)
                    {
                        Error(Peek(), "Can't have more than 255 arguments.");
                    }
                    arguments.Add(Expression());
                } while (Match(TokenType.COMMA));
            }
            var paren = Consume(TokenType.RIGHT_PAREN, "Expect ')' after arguments.");
            return new Call(callee, paren, arguments);
        }

        private Expr Primary()
        {
            if (Match(TokenType.FALSE)) return new Literal(false);
            if (Match(TokenType.TRUE)) return new Literal(true);
            if (Match(TokenType.NIL)) return new Literal(null);
            if (Match(TokenType.NUMBER, TokenType.STRING)) return new Literal(Previous().literal);
            if (Match(TokenType.SUPER))
            {
                var keyword = Previous();
                Consume(TokenType.DOT, "Expext '.' after 'super'.");
                var method = Consume(TokenType.IDENTIFIER, "Expect superclass method name.");
                return new Super(keyword, method);
            }
            if (Match(TokenType.THIS)) return new This(Previous());
            if (Match(TokenType.IDENTIFIER)) return new Variable(Previous());
            if (Match(TokenType.LEFT_PAREN))
            {
                var expr = Expression();
                Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
                return new Grouping(expr);
            }
            throw Error(Peek(), "Expect expression.");
        }

        private void Synchronize()
        {
            Advance();
            while (!IsAtEnd())
            {
                if (Previous().type == TokenType.SEMICOLON) return;
                switch (Peek().type)
                {
                    case TokenType.CLASS:
                    case TokenType.FUN:
                    case TokenType.VAR:
                    case TokenType.FOR:
                    case TokenType.IF:
                    case TokenType.WHILE:
                    case TokenType.PRINT:
                    case TokenType.RETURN:
                        return;
                }
                Advance();
            }
        }

        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            throw Error(Peek(), message);
        }

        private static ParseException Error(Token token, string message)
        {
            Lox.Error(token, message);
            return new ParseException();
        }

        private bool Match(params TokenType[] types)
        {
            foreach (var type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return Peek().type == type;
        }

        private Token Advance()
        {
            if (!IsAtEnd()) current++;
            return Previous();
        }

        private bool IsAtEnd()
        {
            return Peek().type == TokenType.EOF;
        }

        private Token Peek()
        {
            return tokens[current];
        }

        private Token Previous()
        {
            return tokens[current - 1];
        }
    }

    internal class ParseException : Exception { }
}
