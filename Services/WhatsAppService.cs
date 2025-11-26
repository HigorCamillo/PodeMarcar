using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MarcaAi.Backend.Services
{
    public class WhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WhatsAppService> _logger;

        public WhatsAppService(HttpClient httpClient, ILogger<WhatsAppService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        // Método auxiliar para normalizar número de celular
        private string NormalizarNumero(string numero)
        {
            var apenasDigitos = new string(numero.Where(char.IsDigit).ToArray());

            // Adiciona o DDI 55 se ainda não estiver presente e tiver 11 dígitos
            if (apenasDigitos.Length == 11 && !apenasDigitos.StartsWith("55"))
                apenasDigitos = "55" + apenasDigitos;

            return apenasDigitos;
        }

        public async Task<bool> SendMessage(
            string to,
            string message,
            string appKey,
            string authKey,
            bool sandbox = false)
        {
            to = NormalizarNumero(to);

            var url = "https://chatbot.menuia.com/api/create-message";

            var postData = new
            {
                appkey = appKey,
                authkey = authKey,
                sandbox = sandbox.ToString().ToLower(),
                to = to,
                message = message
            };

            _logger.LogInformation("Enviando dados para WhatsApp API: {PostData}", JsonSerializer.Serialize(postData));

            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(postData),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Resposta da WhatsApp API: {StatusCode} - {ResponseBody}", response.StatusCode, responseBody);

                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erro ao enviar mensagem para WhatsApp API: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<bool> SendButton(
            string to,
            string message,
            string appKey,
            string authKey,
            bool sandbox = false)
        {
            to = NormalizarNumero(to);

            var url = "https://chatbot.menuia.com/api/create-message";

            var postData = new
            {
                appkey = appKey,
                authkey = authKey,
                sandbox = sandbox.ToString().ToLower(),
                to = to,
                message = message
            };

            _logger.LogInformation("Enviando botões para WhatsApp API: {PostData}", JsonSerializer.Serialize(postData));

            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(postData),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Resposta da WhatsApp API: {StatusCode} - {ResponseBody}", response.StatusCode, responseBody);

                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erro ao enviar botões para WhatsApp API: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<bool> ScheduleReminder(
            string to,
            string message,
            string appKey,
            string authKey,
            DateTime scheduledTime,
            bool sandbox = false)
        {
            to = NormalizarNumero(to);

            var url = "https://chatbot.menuia.com/api/create-message";

            var agendamentoFormatado = scheduledTime.ToString("yyyy-MM-dd HH:mm:ss");

            var postData = new
            {
                appkey = appKey,
                authkey = authKey,
                to = to,
                agendamento = agendamentoFormatado,
                message = message
            };

            _logger.LogInformation("Agendando lembrete para {ScheduledTime}: {PostData}", scheduledTime, JsonSerializer.Serialize(postData));

            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(postData),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Resposta do agendamento WhatsApp: {StatusCode} - {ResponseBody}", response.StatusCode, responseBody);

                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erro ao agendar lembrete WhatsApp: {Message}", ex.Message);
                return false;
            }
        }
    }
}
