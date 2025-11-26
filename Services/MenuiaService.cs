using System.Net.Http.Json;

namespace MarcaAi.Backend.Services
{
    public class MenuiaService
    {
        private readonly HttpClient _http;

        public MenuiaService(HttpClient http)
        {
            _http = http;
        }

        // ========== GERAR QR CODE ==========
        public async Task<GerarQrCodeResponse> AdicionarDispositivoQrCodeAsync(
            string adminAuthKey, string deviceName, string webhookUrl)
        {
            var payload = new
            {
                authkey = adminAuthKey,
                message = deviceName,
                conecteQR = "true",
                webhook = webhookUrl
            };

            var response = await _http.PostAsJsonAsync("https://chatbot.menuia.com/api/developer", payload);
            var json = await response.Content.ReadFromJsonAsync<GerarQrCodeResponse>();

            return json!;
        }

        // ========== VALIDAR DISPOSITIVO ==========
        public async Task<bool> ValidarDispositivoAsync(string adminAuthKey, string deviceId)
        {
            var payload = new
            {
                authkey = adminAuthKey,
                message = deviceId,
                checkDispositivo = "true"
            };

            var response = await _http.PostAsJsonAsync("https://chatbot.menuia.com/api/developer", payload);

            if (!response.IsSuccessStatusCode)
                return false;

            var result = await response.Content.ReadFromJsonAsync<MenuiaValidacaoResponse>();

            return result?.Status == 200;
        }
    }

    public class GerarQrCodeResponse
    {
        public int Status { get; set; }
        public MenuiaQrCodeMessage? Message { get; set; }
        public string? QrCodeBase64 => Message?.Qr;
        public string? DeviceId => Message?.DeviceId;
    }

    public class MenuiaQrCodeMessage
    {
        public string? Qr { get; set; }
        public string? DeviceId { get; set; }
    }

    public class MenuiaValidacaoResponse
    {
        public int Status { get; set; }
        public string? Message { get; set; }
    }
}
