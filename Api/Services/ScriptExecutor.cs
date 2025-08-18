using ApiPreProcessamento.Models;
using Jint;
using System.Text.Json;
using Processing;

namespace ApiPreProcessamento.Services
{
    public class ScriptExecutor
    {
        public string Execute(Script script, string jsonData)
        {
            var engine = new Engine();
            var data = JsonSerializer.Deserialize<object>(jsonData);

            engine.SetValue("data", data);
            engine.Execute(script.Code);
            var result = engine.Invoke("process", data).ToObject();

            var resultJson = JsonSerializer.Serialize(result);

            // Chama pré-processamento F#
            var processedJson = PreProcessor.processJson(resultJson);

            return processedJson;
        }
    }
}
