using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Itqueuein_Check.Network
{
    public class RestlinkClient
    {
        private static readonly HttpClient _client = new HttpClient();

        // Configuración
        private readonly string _loginUrl = "http://10.168.111.3:81/RestLinkCore/RestLinkCore.svc/CheckIn?AppUser=admin&Password=HNZacamil";
        private readonly string _acceptMessageUrl = "http://10.168.111.3:81/RestLinkCore/RestLinkCore.svc/acceptMessage";

        // Gestión de Token en memoria
        private string _currentToken = null;
        private DateTime _tokenExpiration = DateTime.MinValue;

        public RestlinkClient()
        {
            if (_client.Timeout == System.Threading.Timeout.InfiniteTimeSpan)
            {
                _client.Timeout = TimeSpan.FromSeconds(15);
            }
        }

        // --- 1. MÉTODO PRINCIPAL DE ENVÍO ---
        public async Task<(bool Exito, string MensajeRespuesta)> ReenviarOrdenAsync(string mensajeHl7)
        {
            try
            {
                // 1. Garantizar que tenemos un token válido antes de disparar
                bool tokenValido = await AsegurarTokenActivoAsync();
                if (!tokenValido)
                {
                    return (false, "Error Interno: No se pudo obtener el Token de seguridad de Restlink.");
                }

                // 2. Preparar el Payload
                var requestBody = new
                {
                    Token = _currentToken,
                    Mensaje = mensajeHl7,
                    Checksum = CalcularMD5(mensajeHl7)
                };

                string jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 3. Enviar el POST
                HttpResponseMessage response = await _client.PostAsync(_acceptMessageUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "200 OK");
                }
                else
                {
                    string errorDetail = await response.Content.ReadAsStringAsync();
                    return (false, $"HTTP {(int)response.StatusCode}: {errorDetail}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Excepción de red: {ex.Message}");
            }
        }

        // --- 2. GESTOR INTELIGENTE DE TOKEN ---
        private async Task<bool> AsegurarTokenActivoAsync()
        {
            // Si el token aún es válido (damos 1 minuto de margen de seguridad), no hacemos login de nuevo
            if (!string.IsNullOrEmpty(_currentToken) && DateTime.Now < _tokenExpiration.AddMinutes(-1))
            {
                return true;
            }

            var loginBody = new
            {
                AppUser = "admin",
                Password = "admin"
            };

            var content = new StringContent(JsonSerializer.Serialize(loginBody), Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await _client.PostAsync(_loginUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var loginResult = JsonSerializer.Deserialize<LoginResponse>(jsonResponse);

                    if (loginResult != null && loginResult.estado && !string.IsNullOrEmpty(loginResult.Token))
                    {
                        _currentToken = loginResult.Token;
                        // El JWT dice 10 mins, pero lo fijamos a 9 minutos para renovar antes de que muera
                        _tokenExpiration = DateTime.Now.AddMinutes(9);
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        // --- 3. UTILIDAD: CÁLCULO DE CHECKSUM ---
        private string CalcularMD5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convertir a string hexadecimal (minúsculas, como lo pide Restlink)
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        // --- 4. CLASES DTO INTERNAS ---
        private class LoginResponse
        {
            public string Token { get; set; }
            public bool estado { get; set; }
            public string mensaje { get; set; }
        }
    }
}