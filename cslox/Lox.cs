namespace cslox
{
    internal class Lox
    {
        private static readonly Interpreter interpreter = new();
        private static bool hadError = false;
        private static bool hadRuntimeError = false;

        internal static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                Console.WriteLine("Usage: cslox [script]");
                System.Environment.Exit(64);
            }
            else if (args.Length == 1)
            {
                RunFile(args[0]);
                if (hadError) System.Environment.Exit(65);
                if (hadRuntimeError) System.Environment.Exit(70);
            }
            else
            {
                RunPrompt();
            }
        }

        private static void RunFile(string path)
        {
            var content = File.ReadAllText(path);
            Run(content);
        }

        private static void RunPrompt()
        {
            while (true)
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                if (line != null) Run(line);
                hadError = false;
            }
        }

        private static void Run(string source)
        {
            var scanner = new Scanner(source);
            var tokens = scanner.ScanTokens();
            var parser = new Parser(tokens);
            var statements = parser.Parse();

            if (hadError || statements == null) return;

            interpreter.Interpret(statements);
        }

        internal static void Error(int line, string message)
        {
            Report(line, "", message);
        }

        internal static void Error(Token token, string message)
        {
            if (token.type == TokenType.EOF)
            {
                Report(token.line, " at end", message);
            }
            else
            {
                Report(token.line, " at '" + token.lexeme + "'", message);
            }
        }

        internal static void RuntimeError(RuntimeError e)
        {
            Console.WriteLine(e.Message + "\n[line " + e.token.line.ToString() + "]");
            hadRuntimeError = true;

        }

        private static void Report(int line, string where, string message)
        {
            Console.WriteLine($"[line {line}] Error{where}: {message}");
            hadError = true;
        }
    }
}