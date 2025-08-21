using Esprima;
using Esprima.Ast;
using Esprima.Utils;

namespace Api.Services
{
    public class ScriptValidationService
    {
        public const int MaxScriptSizeBytes = 64_000;

        /// Validações simples de tamanho e tokens proibidos
        public string? ShallowGuards(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "Code vazio.";

            if (code.Length > MaxScriptSizeBytes) return $"Code excede {MaxScriptSizeBytes} bytes.";

            var banned = new[] { "eval", "Function(", "require(", "import ", "setTimeout(", "setInterval(" };
            if (banned.Any(b => code.Contains(b, StringComparison.OrdinalIgnoreCase)))
               return "Uso de função proibida (eval/Function/require/import/timeout).";

            return null;
        }

        /// Validação de AST para bloquear nós específicos
        public void ValidateAst(string code)
        {
            var parser = new JavaScriptParser(new ParserOptions { Tolerant = false });
            var program = parser.ParseScript(code);

            void Walk(Node n)
            {
                switch (n)
                {
                    case CallExpression ce when ce.Callee is Identifier id &&
                        (id.Name is "eval" or "require" or "setTimeout" or "setInterval"):
                        throw new InvalidOperationException($"Chamada proibida: {id.Name}()");
                    case NewExpression ne when ne.Callee is Identifier nid && nid.Name == "Function":
                        throw new InvalidOperationException("Uso proibido: new Function()");
                    case ImportDeclaration:
                        throw new InvalidOperationException("Import ESModule não permitido.");
                }
                foreach (var child in n.ChildNodes) Walk(child);
            }

            Walk(program);
        }
    }
}
