using Api.Services;
using Api.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Api.Tests
{
    public class ScriptExecutorTests
    {
        private static ScriptExecutor Exec() => new ScriptExecutor(new NullLogger<ScriptExecutor>());

        [Fact]
        public void Aggregator_SumsValues()
        {
            var exec = Exec();
            var script = new Script
            {
                Id = Guid.NewGuid(),
                Name = "agg",
                Code =
                    "function process(data){ const arr=Array.isArray(data)?data:[]; const corp=arr.filter(x=>x.produto==='Empresarial'); const map=new Map();" +
                    "for(const it of corp){ const k=`${it.trimestre}-${it.nomeBandeira}`; let acc=map.get(k);" +
                    "if(!acc){ acc={trimestre:it.trimestre,nomeBandeira:it.nomeBandeira,qtdCartoesEmitidos:0,qtdCartoesAtivos:0,qtdTransacoesNacionais:0,valorTransacoesNacionais:0}; map.set(k, acc);} " +
                    "acc.qtdCartoesEmitidos += Number(it.qtdCartoesEmitidos)||0; acc.qtdCartoesAtivos += Number(it.qtdCartoesAtivos)||0; acc.qtdTransacoesNacionais += Number(it.qtdTransacoesNacionais)||0; acc.valorTransacoesNacionais += Number(it.valorTransacoesNacionais)||0; }" +
                    "return Array.from(map.values()); }"
            };

            var payload = """
            [
              {"trimestre":"2023Q2","nomeBandeira":"VISA","produto":"Empresarial","qtdCartoesEmitidos":10,"qtdCartoesAtivos":7,"qtdTransacoesNacionais":100,"valorTransacoesNacionais":2000},
              {"trimestre":"2023Q2","nomeBandeira":"VISA","produto":"Empresarial","qtdCartoesEmitidos":5,"qtdCartoesAtivos":3,"qtdTransacoesNacionais":40,"valorTransacoesNacionais":800}
            ]
            """;

            var json = exec.Execute(script, payload);
            Assert.Contains("\"qtdCartoesEmitidos\":15", json);
            Assert.Contains("\"qtdCartoesAtivos\":10", json);
            Assert.Contains("\"qtdTransacoesNacionais\":140", json);
            Assert.Contains("\"valorTransacoesNacionais\":2800", json);
        }

        [Fact]
        public void InfiniteLoop_IsBlocked()
        {
            var exec = Exec();
            var bad = new Script { Id = Guid.NewGuid(), Name = "bad", Code = "function process(d){ while(true){} }" };
            var ex = Record.Exception(() => exec.Execute(bad, "[]"));
            Assert.NotNull(ex); // timeout/limite deve interromper
        }
    }
}
