using Api.Models;
using Jint;
using System.Text.Json;
using STJ = System.Text.Json.JsonSerializer;
using Processing;
using Jint.Native.Json;

namespace Api.Services
{
    public class ScriptExecutor
    {
        private readonly ILogger<ScriptExecutor> _log;
        public ScriptExecutor(ILogger<ScriptExecutor> log) => _log = log;
        public string Execute(Script script, string jsonData)
        {
            var engine = CreateSandbox();

            engine.Execute(script.Code);

            if (engine.GetValue("process").IsUndefined())
                throw new InvalidOperationException("O script deve exportar function process(data).");

            //try { engine.Invoke("process", new object()); }
            //catch { throw new InvalidOperationException("O script deve exportar function process(data)."); }

            var jsData = new JsonParser(engine).Parse(jsonData);
            var resultJs = engine.Invoke("process", jsData);

            var resultNet = resultJs.ToObject();
            var resultJson = STJ.Serialize(resultNet);
          
            var shouldPostProcess = false;

            try
            {
                using var doc = JsonDocument.Parse(resultJson);
                var root = doc.RootElement;
                shouldPostProcess =
                    root.ValueKind == JsonValueKind.Array &&
                    root.GetArrayLength() > 0 &&
                    root[0].TryGetProperty("produto", out _);
            }
            catch (JsonException ex)
            {
                _log.LogDebug("Pós-processamento ignorado: JSON inválido/inesperado. {Msg}", ex.Message);
            }

            if (shouldPostProcess)
            {
                try { return PreProcessor.processJson(resultJson); }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Falha no F# PreProcessor; retornando resultado do JS como fallback.");
                }
            }

            return resultJson;
        }

        private static Engine CreateSandbox()
        {
            return new Engine(o => o
                .Strict()
                .LimitRecursion(64)
                .LimitMemory(20_000_000)                 
                .TimeoutInterval(TimeSpan.FromSeconds(5))
                .LocalTimeZone(TimeZoneInfo.Utc)
            );
        }

        public void Validate(string code)
        {
            var engine = CreateSandbox();
            engine.Execute(code);

            if(engine.GetValue("process").IsUndefined())
                throw new InvalidOperationException("O script deve exportar function process(data).");
               
        }
    }
}
