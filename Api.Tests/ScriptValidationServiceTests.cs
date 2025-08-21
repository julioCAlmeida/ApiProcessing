using Api.Services;
using Xunit;

namespace Api.Tests
{
    public class ScriptValidationServiceTests
    {
        [Fact]
        public void ShallowGuards_BlocksEval()
        {
            var v = new ScriptValidationService();
            var msg = v.ShallowGuards("function process(d){ eval('2+2'); return d; }");
            Assert.NotNull(msg);
        }

        [Fact]
        public void Ast_Validator_BlocksNewFunction()
        {
            var v = new ScriptValidationService();
           Assert.Throws<InvalidOperationException>(() =>
                v.ValidateAst("function process(d){ const F = new Function('return 1'); return d; }"));
        }
    }
}
