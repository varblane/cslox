namespace cslox
{
    internal class Lox
    {
        private static bool hadError = false;

        internal static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                Console.WriteLine("Usage: cslox [script]");
                Environment.Exit(64);
            }
            else if (args.Length == 1)
            {
                RunFile(args[0]);
                if (hadError) Environment.Exit(65);
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
            var expression = parser.Parse();

            if (hadError || expression == null) return;

            Console.WriteLine(new AstPrinter().Print(expression));
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

        private static void Report(int line, string where, string message)
        {
            Console.WriteLine($"[line {line}] Error {where}: {message}");
        }
    }
}