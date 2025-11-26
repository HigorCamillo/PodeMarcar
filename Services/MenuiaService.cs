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

        public MenuiaService(HttpClient httpClient, ILogger<MenuiaService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<MenuiaResponse> CriarAplicativoAsync(string masterAuthKey, string appName, string deviceIdentifier)
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
                var response = await _httpClient.PostAsync(BaseUrl, jsonContent);
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
                
                var response = await _httpClient.PostAsync(BaseUrl, jsonContent);
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
                
                var response = await _httpClient.PostAsync(BaseUrl, jsonContent);
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

                // Tenta extrair o status
                if (root.TryGetProperty("status", out var statusElement))
                {
                    response.Status = statusElement.GetInt32();
                    _logger.LogInformation($"Status encontrado: {response.Status}");
                }
                else
                {
                    response.Status = 200;
                }

                // Verifica se há erro
                if (root.TryGetProperty("error", out var errorElement))
                {
                    response.Status = 400;
                    response.Message = errorElement.ValueKind == JsonValueKind.String 
                        ? errorElement.GetString() ?? "Erro desconhecido"
                        : errorElement.GetRawText();
                    _logger.LogError($"Erro na resposta: {response.Message}");
                    return response;
                }

                // Tenta extrair o message (pode ser string ou objeto)
                string messageContent = null;
                if (root.TryGetProperty("message", out var messageElement))
                {
                    if (messageElement.ValueKind == JsonValueKind.String)
                    {
                        messageContent = messageElement.GetString() ?? string.Empty;
                        _logger.LogInformation($"Message (string) encontrado, comprimento: {messageContent.Length}");
                    }
                    else if (messageElement.ValueKind == JsonValueKind.Object)
                    {
                        messageContent = messageElement.GetRawText();
                        _logger.LogInformation($"Message (object) encontrado, comprimento: {messageContent.Length}");
                    }
                }

                // Tenta extrair qrCodeUrl (pode ser string ou objeto)
                string qrCodeUrlContent = null;
                if (root.TryGetProperty("qrCodeUrl", out var qrCodeUrlElement))
                {
                    if (qrCodeUrlElement.ValueKind == JsonValueKind.String)
                    {
                        qrCodeUrlContent = qrCodeUrlElement.GetString() ?? string.Empty;
                        _logger.LogInformation($"qrCodeUrl (string) encontrado, comprimento: {qrCodeUrlContent.Length}");
                    }
                    else if (qrCodeUrlElement.ValueKind == JsonValueKind.Object)
                    {
                        qrCodeUrlContent = qrCodeUrlElement.GetRawText();
                        _logger.LogInformation($"qrCodeUrl (object) encontrado, comprimento: {qrCodeUrlContent.Length}");
                    }
                }

                // Se não encontrou qrCodeUrl, tenta usar message (pode conter o JSON stringificado)
                var jsonStringContent = qrCodeUrlContent ?? messageContent;

                if (!string.IsNullOrEmpty(jsonStringContent))
                {
                    // Tenta fazer parsing do JSON stringificado
                    try
                    {
                        using var qrDocument = JsonDocument.Parse(jsonStringContent);
                        var qrRoot = qrDocument.RootElement;
                        
                        _logger.LogInformation($"JSON stringificado parseado com sucesso");
                        
                        // Tenta extrair o campo 'qr' (pode ser 'qr', 'qrcode', 'image', etc)
                        if (qrRoot.TryGetProperty("qr", out var qrElement) && qrElement.ValueKind == JsonValueKind.String)
                        {
                            response.QrCodeBase64 = qrElement.GetString();
                            _logger.LogInformation($"Campo 'qr' encontrado, comprimento: {response.QrCodeBase64?.Length ?? 0}");
                        }
                        else if (qrRoot.TryGetProperty("qrcode", out var qrcodeElement) && qrcodeElement.ValueKind == JsonValueKind.String)
                        {
                            response.QrCodeBase64 = qrcodeElement.GetString();
                            _logger.LogInformation($"Campo 'qrcode' encontrado, comprimento: {response.QrCodeBase64?.Length ?? 0}");
                        }
                        else if (qrRoot.TryGetProperty("image", out var imageElement) && imageElement.ValueKind == JsonValueKind.String)
                        {
                            response.QrCodeBase64 = imageElement.GetString();
                            _logger.LogInformation($"Campo 'image' encontrado, comprimento: {response.QrCodeBase64?.Length ?? 0}");
                        }
                        else
                        {
                            _logger.LogWarning("Nenhum campo de imagem encontrado no JSON stringificado");
                            // Log todas as propriedades disponíveis
                            foreach (var prop in qrRoot.EnumerateObject())
                            {
                                _logger.LogInformation($"Propriedade encontrada: {prop.Name} (tipo: {prop.Value.ValueKind})");
                            }
                        }
                        
                        // Tenta extrair o ID do dispositivo
                        if (qrRoot.TryGetProperty("id", out var idElement))
                        {
                            if (idElement.ValueKind == JsonValueKind.Number)
                            {
                                response.DeviceId = idElement.GetInt32();
                                _logger.LogInformation($"Device ID: {response.DeviceId}");
                            }
                        }
                        
                        response.Message = "QR Code gerado com sucesso";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Erro ao parsear JSON stringificado: {ex.Message}");
                        response.Message = jsonStringContent;
                    }
                }
                else
                {
                    _logger.LogWarning("Nenhum conteúdo de QR Code encontrado na resposta");
                    response.Message = messageContent ?? "Sem conteúdo";
                }

                // Tenta extrair appkey
                if (root.TryGetProperty("appkey", out var appkeyElement) && appkeyElement.ValueKind == JsonValueKind.String)
                {
                    response.AppKey = appkeyElement.GetString();
                    _logger.LogInformation($"AppKey encontrado");
                }

                // Tenta extrair authkey
                if (root.TryGetProperty("authkey", out var authkeyElement) && authkeyElement.ValueKind == JsonValueKind.String)
                {
                    response.AuthKey = authkeyElement.GetString();
                    _logger.LogInformation($"AuthKey encontrado");
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

                // Verifica se o dispositivo está conectado
                if (root.TryGetProperty("connected", out var connectedElement))
                {
                    response.IsConnected = connectedElement.GetBoolean();
                    _logger.LogInformation($"Dispositivo conectado: {response.IsConnected}");
                }

                // Extrai appkey se disponível
                if (root.TryGetProperty("appkey", out var appkeyElement) && appkeyElement.ValueKind == JsonValueKind.String)
                {
                    response.AppKey = appkeyElement.GetString();
                    _logger.LogInformation($"AppKey encontrado na verificação");
                }

                // Extrai authkey se disponível
                if (root.TryGetProperty("authkey", out var authkeyElement) && authkeyElement.ValueKind == JsonValueKind.String)
                {
                    response.AuthKey = authkeyElement.GetString();
                    _logger.LogInformation($"AuthKey encontrado na verificação");
                }

                // Extrai mensagem
                if (root.TryGetProperty("message", out var messageElement) && messageElement.ValueKind == JsonValueKind.String)
                {
                    response.Message = messageElement.GetString() ?? string.Empty;
                }

                response.Status = response.IsConnected ? 200 : 400;

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
