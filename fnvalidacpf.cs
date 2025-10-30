using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace httpValidaCpf
{
    public static class FnValidaCpf
    {
        [FunctionName("FnValidaCpf")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Iniciando a validação do CPF.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // obtem cpf do body ou da query string
            string cpf = data?.cpf;
            if (string.IsNullOrWhiteSpace(cpf))
            {
                cpf = req.Query["cpf"];
            }

            if (string.IsNullOrWhiteSpace(cpf))
            {
                return new BadRequestObjectResult("Por favor, informe o CPF (campo 'cpf' no body ou query string).");
            }

            bool valido = ValidaCPF(cpf);

            var responseMessage = new
            {
                cpf = cpf,
                valid = valido,
                message = valido ? "CPF válido." : "CPF inválido."
            };

            return new OkObjectResult(responseMessage);
        }

        public static bool ValidaCPF(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return false;

            // mantém apenas dígitos
            var numeros = new string(cpf.Where(char.IsDigit).ToArray());

            if (numeros.Length != 11)
                return false;

            // rejeita sequências como 00000000000, 11111111111, etc.
            if (numeros.Distinct().Count() == 1)
                return false;

            // cálculo do primeiro dígito verificador
            int soma = 0;
            for (int i = 0; i < 9; i++)
                soma += (numeros[i] - '0') * (10 - i);

            int resto = soma % 11;
            int dig1 = (resto < 2) ? 0 : 11 - resto;
            if (dig1 != (numeros[9] - '0'))
                return false;

            // cálculo do segundo dígito verificador
            soma = 0;
            for (int i = 0; i < 10; i++)
                soma += (numeros[i] - '0') * (11 - i);

            resto = soma % 11;
            int dig2 = (resto < 2) ? 0 : 11 - resto;
            if (dig2 != (numeros[10] - '0'))
                return false;

            return true;
        }
    }
}
