using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MarcaAi.Backend.Services
{
    public class MenuiaService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MenuiaService> _logger;
        private const string BaseUrl = "https://chatbot.menuia.com/api/developer";

        public MenuiaService(HttpClient httpClient, ILogger<MenuiaService> logger )
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<MenuiaResponse> CriarAplicativoAsync(string masterAuthKey, string appName, string deviceIdentifier )
        {
            var requestBody = new
            {
                authkey = masterAuthKey,
                message = appName,
                criarApp = deviceIdentifier
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                var response = await _httpClient.PostAsync(BaseUrl, jsonContent );
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Resposta do Menuia (CriarAplicativo): {responseBody}");
                return ParseMenuiaResponse(responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao criar aplicativo: {ex.Message}");
                return new MenuiaResponse { Status = 500, Message = $"Erro ao criar aplicativo: {ex.Message}" };
            }
        }

        public async Task<MenuiaResponse> ObterChavesAppAsync(string masterAuthKey, string appName, string deviceIdentifier)
        {
            var requestBody = new
            {
                authkey = masterAuthKey,
                message = appName,
                criarApp = deviceIdentifier
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                _logger.LogInformation($"Enviando requisição para Menuia (ObterChavesApp): {JsonSerializer.Serialize(requestBody)}");
                
                var response = await _httpClient.PostAsync(BaseUrl, jsonContent );
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Resposta do Menuia (ObterChavesApp): {responseBody}");
                
                // O ParseMenuiaResponse já extrai appkey e authkey se presentes
                return ParseMenuiaResponse(responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao obter chaves do aplicativo: {ex.Message}");
                return new MenuiaResponse { Status = 500, Message = $"Erro ao obter chaves do aplicativo: {ex.Message}" };
            }
        }

        public async Task<MenuiaResponse> VerificarDispositivoAsync(string authKey, string deviceIdentifier)
        {
            var requestBody = new
            {
                authkey = authKey,
                message = deviceIdentifier,
                checkDispositivo = "true"
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                _logger.LogInformation($"Verificando dispositivo: {JsonSerializer.Serialize(requestBody)}");
                
                var response = await _httpClient.PostAsync(BaseUrl, jsonContent );
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Resposta da verificação: {responseBody}");
                
                return ParseVerificacaoResponse(responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao verificar dispositivo: {ex.Message}");
                return new MenuiaResponse { Status = 500, Message = $"Erro ao verificar dispositivo: {ex.Message}" };
            }
        }

        public async Task<MenuiaResponse> AdicionarDispositivoQrCodeAsync(string masterAuthKey, string deviceName, string webhookUrl)
        {
            var requestBody = new
            {
                authkey = masterAuthKey,
                message = deviceName,
                conecteQR = true,
                webhook = webhookUrl
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                _logger.LogInformation($"Enviando requisição para Menuia: {JsonSerializer.Serialize(requestBody)}");
                
                var response = await _httpClient.PostAsync(BaseUrl, jsonContent );
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Resposta bruta do Menuia: {responseBody}");
                
                var parsedResponse = ParseMenuiaResponse(responseBody);
                _logger.LogInformation($"Resposta parseada - Status: {parsedResponse.Status}, QrCodeBase64: {(string.IsNullOrEmpty(parsedResponse.QrCodeBase64) ? "VAZIO" : "PREENCHIDO")}");
                
                return parsedResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao adicionar dispositivo: {ex.Message}");
                return new MenuiaResponse { Status = 500, Message = $"Erro ao adicionar dispositivo: {ex.Message}" };
            }
        }

        /// <summary>
        /// Parseia a resposta do Menuia com tratamento robusto de tipos.
        /// A resposta pode ter diferentes estruturas:
        /// 1. {"message": "Sucesso", "qrCodeUrl": "{\"qr\": \"data:image/png;base64,...\", \"id\": 8773}"}
        /// 2. {"message": "{\"qr\": \"data:image/png;base64,...\", \"id\": 8773}"}
        /// 3. {"error": "Mensagem de erro"}
        /// </summary>
        private MenuiaResponse ParseMenuiaResponse(string jsonResponse)
{
    try
    {
        using var document = JsonDocument.Parse(jsonResponse);
        var root = document.RootElement;

        var response = new MenuiaResponse();

        // Status
        if (root.TryGetProperty("status", out var statusElement))
            response.Status = statusElement.GetInt32();
        else
            response.Status = 200;

        // Mensagem e possível objeto dentro de message
        JsonElement? messageObj = null;
        string messageStr = null;

        if (root.TryGetProperty("message", out var messageElement))
        {
            if (messageElement.ValueKind == JsonValueKind.String)
            {
                messageStr = messageElement.GetString() ?? string.Empty;
            }
            else if (messageElement.ValueKind == JsonValueKind.Object)
            {
                messageObj = messageElement;
            }
        }

        // qrCodeUrl pode estar separado ou dentro de message
        string qrContent = null;

        if (root.TryGetProperty("qrCodeUrl", out var qrCodeElement))
        {
            qrContent = qrCodeElement.ValueKind == JsonValueKind.String
                ? qrCodeElement.GetString()
                : qrCodeElement.GetRawText();
        }
        else if (messageObj.HasValue && messageObj.Value.TryGetProperty("qr", out var qrElement) && qrElement.ValueKind == JsonValueKind.String)
        {
            qrContent = qrElement.GetString();
        }

        response.QrCodeBase64 = qrContent;

        // Extrair appkey e authkey do nível raiz
        if (root.TryGetProperty("appkey", out var appkeyElement) && appkeyElement.ValueKind == JsonValueKind.String)
            response.AppKey = appkeyElement.GetString();

        if (root.TryGetProperty("authkey", out var authkeyElement) && authkeyElement.ValueKind == JsonValueKind.String)
            response.AuthKey = authkeyElement.GetString();

        // Se estiver dentro de message (objeto)
        if (messageObj.HasValue)
        {
            var msg = messageObj.Value;

            if (string.IsNullOrEmpty(response.AppKey) && msg.TryGetProperty("appkey", out var appkeyInMsg) && appkeyInMsg.ValueKind == JsonValueKind.String)
                response.AppKey = appkeyInMsg.GetString();

            if (string.IsNullOrEmpty(response.AuthKey) && msg.TryGetProperty("authkey", out var authkeyInMsg) && authkeyInMsg.ValueKind == JsonValueKind.String)
                response.AuthKey = authkeyInMsg.GetString();

            // DeviceId
            if (msg.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.Number)
                response.DeviceId = idElement.GetInt32();

            response.Message = "QR Code gerado com sucesso";
        }
        else
        {
            response.Message = messageStr ?? "Sem conteúdo";
        }

        // Verifica se há erro
        if (root.TryGetProperty("error", out var errorElement))
        {
            response.Status = 400;
            response.Message = errorElement.ValueKind == JsonValueKind.String
                ? errorElement.GetString() ?? "Erro desconhecido"
                : errorElement.GetRawText();
        }

        return response;
    }
    catch (Exception ex)
    {
        _logger.LogError($"Erro ao parsear resposta do Menuia: {ex.Message}");
        return new MenuiaResponse { Status = 500, Message = $"Erro ao parsear resposta do Menuia: {ex.Message}" };
    }
}


        private MenuiaResponse ParseVerificacaoResponse(string jsonResponse)
        {
            try
            {
                using var document = JsonDocument.Parse(jsonResponse);
                var root = document.RootElement;

                var response = new MenuiaResponse();

                // Extrai status
                if (root.TryGetProperty("status", out var statusElement))
                {
                    response.Status = statusElement.GetInt32();
                    _logger.LogInformation($"Status: {response.Status}");
                }
                else
                {
                    response.Status = 200;
                }

                // Extrai mensagem
                if (root.TryGetProperty("message", out var messageElement) && messageElement.ValueKind == JsonValueKind.String)
                {
                    response.Message = messageElement.GetString() ?? string.Empty;
                    _logger.LogInformation($"Message: {response.Message}");
                }

                // Verifica se o dispositivo está conectado baseado na mensagem
                // A API retorna "Dispositivo Conectado" quando está conectado
                response.IsConnected = response.Status == 200 && 
                                      !string.IsNullOrEmpty(response.Message) && 
                                      response.Message.Contains("Conectado", StringComparison.OrdinalIgnoreCase);

                _logger.LogInformation($"Dispositivo conectado: {response.IsConnected}");

                // Se está conectado, tenta extrair dados do dispositivo
                if (response.IsConnected && root.TryGetProperty("dados", out var dadosElement))
                {
                    _logger.LogInformation("Extraindo dados do dispositivo...");
                    
                    // Extrai ID do dispositivo
                    if (dadosElement.TryGetProperty("id", out var idElement))
                    {
                        response.DeviceId = idElement.GetInt32();
                        _logger.LogInformation($"Device ID: {response.DeviceId}");
                    }

                    // Nota: As chaves (appkey e authkey) são enviadas via webhook
                    // quando o dispositivo conecta, não são retornadas nesta verificação
                }

                // Tenta extrair appkey se disponível (pode não estar presente)
                if (root.TryGetProperty("appkey", out var appkeyElement) && appkeyElement.ValueKind == JsonValueKind.String)
                {
                    response.AppKey = appkeyElement.GetString();
                    _logger.LogInformation($"AppKey encontrado na verificação");
                }

                // Tenta extrair authkey se disponível (pode não estar presente)
                if (root.TryGetProperty("authkey", out var authkeyElement) && authkeyElement.ValueKind == JsonValueKind.String)
                {
                    response.AuthKey = authkeyElement.GetString();
                    _logger.LogInformation($"AuthKey encontrado na verificação");
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao parsear resposta de verificação: {ex.Message}");
                return new MenuiaResponse { Status = 500, Message = $"Erro ao parsear resposta: {ex.Message}" };
            }
        }
    }

    public class MenuiaResponse
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        
        [JsonPropertyName("appkey")]
        public string? AppKey { get; set; }
        
        [JsonPropertyName("authkey")]
        public string? AuthKey { get; set; }

        [JsonPropertyName("qrCodeBase64")]
        public string? QrCodeBase64 { get; set; }

        [JsonPropertyName("deviceId")]
        public int? DeviceId { get; set; }

        [JsonPropertyName("isConnected")]
        public bool IsConnected { get; set; }
    }
}
