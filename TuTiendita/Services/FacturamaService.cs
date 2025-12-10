using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TuTiendita.Services
{
    /// <summary>
    /// Servicio para integración con Facturama PAC
    /// Documentación: https://apisandbox.facturama.mx/docs
    /// </summary>
    public class FacturamaService : IDisposable
    {
        // URLs de la API REST
        private const string SANDBOX_URL = "https://apisandbox.facturama.mx";
        private const string PRODUCTION_URL = "https://api.facturama.mx";

        private readonly string _username;
        private readonly string _password;
        private readonly bool _isProduction;
        private readonly HttpClient _httpClient;
        private bool _disposed = false;

        public FacturamaService(string username, string password, bool isProduction = false)
        {
            _username = username ?? throw new ArgumentNullException(nameof(username));
            _password = password ?? throw new ArgumentNullException(nameof(password));
            _isProduction = isProduction;

            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
            _httpClient.BaseAddress = new Uri(BaseUrl);

            // Autenticación Basic
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_username}:{_password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        #region Propiedades

        public string BaseUrl => _isProduction ? PRODUCTION_URL : SANDBOX_URL;
        public bool IsProduction => _isProduction;

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
        }

        #endregion

        #region Timbrado

        /// <summary>
        /// Timbra un CFDI con Facturama usando la API Lite (multiemisor)
        /// </summary>
        /// <param name="cfdiRequest">Objeto con los datos del CFDI</param>
        /// <returns>Resultado del timbrado</returns>
        public async Task<TimbradoResult> TimbrarAsync(FacturamaCfdiRequest cfdiRequest)
        {
            var result = new TimbradoResult();

            try
            {
                var json = JsonSerializer.Serialize(cfdiRequest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api-lite/3/cfdis", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var cfdiResponse = JsonSerializer.Deserialize<FacturamaCfdiResponse>(responseBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (cfdiResponse != null)
                    {
                        result.Success = true;
                        result.UUID = cfdiResponse.Complement?.TaxStamp?.Uuid;
                        result.FechaTimbrado = cfdiResponse.Complement?.TaxStamp?.Date;
                        result.SelloSAT = cfdiResponse.Complement?.TaxStamp?.SatSign;
                        result.NoCertificadoSAT = cfdiResponse.Complement?.TaxStamp?.SatCertNumber;
                        result.SelloCFDI = cfdiResponse.Complement?.TaxStamp?.CfdiSign;
                        result.CadenaOriginalTFD = cfdiResponse.Complement?.TaxStamp?.OriginalString;
                        result.FacturamaId = cfdiResponse.Id;

                        // Obtener el XML timbrado
                        if (!string.IsNullOrEmpty(cfdiResponse.Id))
                        {
                            result.XMLTimbrado = await ObtenerXMLAsync(cfdiResponse.Id);
                        }
                    }
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<FacturamaErrorResponse>(responseBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    result.Success = false;
                    result.ErrorCode = response.StatusCode.ToString();
                    result.ErrorMessage = errorResponse?.Message ?? "Error desconocido";

                    if (errorResponse?.ModelState != null)
                    {
                        var errors = new List<string>();
                        foreach (var state in errorResponse.ModelState)
                        {
                            errors.AddRange(state.Value);
                        }
                        result.ErrorMessage += " - " + string.Join("; ", errors);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                result.Success = false;
                result.ErrorCode = "HTTP_ERROR";
                result.ErrorMessage = $"Error de conexión con Facturama: {ex.Message}";
            }
            catch (TaskCanceledException)
            {
                result.Success = false;
                result.ErrorCode = "TIMEOUT";
                result.ErrorMessage = "Tiempo de espera agotado al conectar con Facturama";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorCode = "UNKNOWN";
                result.ErrorMessage = $"Error inesperado: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Timbra un CFDI usando XML pre-generado
        /// </summary>
        public async Task<TimbradoResult> TimbrarXMLAsync(string xmlCFDI)
        {
            var result = new TimbradoResult();

            try
            {
                // Convertir XML a Base64
                string xmlBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(xmlCFDI));

                var request = new { xml = xmlBase64 };
                var json = JsonSerializer.Serialize(request);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api-lite/3/cfdis?xml=true", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var cfdiResponse = JsonSerializer.Deserialize<FacturamaCfdiResponse>(responseBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (cfdiResponse != null)
                    {
                        result.Success = true;
                        result.UUID = cfdiResponse.Complement?.TaxStamp?.Uuid;
                        result.FechaTimbrado = cfdiResponse.Complement?.TaxStamp?.Date;
                        result.SelloSAT = cfdiResponse.Complement?.TaxStamp?.SatSign;
                        result.NoCertificadoSAT = cfdiResponse.Complement?.TaxStamp?.SatCertNumber;
                        result.SelloCFDI = cfdiResponse.Complement?.TaxStamp?.CfdiSign;
                        result.CadenaOriginalTFD = cfdiResponse.Complement?.TaxStamp?.OriginalString;
                        result.FacturamaId = cfdiResponse.Id;

                        if (!string.IsNullOrEmpty(cfdiResponse.Id))
                        {
                            result.XMLTimbrado = await ObtenerXMLAsync(cfdiResponse.Id);
                        }
                    }
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<FacturamaErrorResponse>(responseBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    result.Success = false;
                    result.ErrorCode = response.StatusCode.ToString();
                    result.ErrorMessage = errorResponse?.Message ?? responseBody;

                    // Agregar detalles de ModelState si existen
                    if (errorResponse?.ModelState != null)
                    {
                        var errors = new List<string>();
                        foreach (var state in errorResponse.ModelState)
                        {
                            errors.AddRange(state.Value);
                        }
                        if (errors.Count > 0)
                        {
                            result.ErrorMessage += " - " + string.Join("; ", errors);
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                result.Success = false;
                result.ErrorCode = "HTTP_ERROR";
                result.ErrorMessage = $"Error de conexión con Facturama: {ex.Message}";
            }
            catch (TaskCanceledException)
            {
                result.Success = false;
                result.ErrorCode = "TIMEOUT";
                result.ErrorMessage = "Tiempo de espera agotado al conectar con Facturama";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorCode = "UNKNOWN";
                result.ErrorMessage = $"Error: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Obtiene el XML de un CFDI timbrado
        /// </summary>
        private async Task<string> ObtenerXMLAsync(string facturamaId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api-lite/cfdis/{facturamaId}?type=xml");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch
            {
                // Ignorar errores al obtener XML
            }

            return null;
        }

        #endregion

        #region Cancelación

        /// <summary>
        /// Cancela un CFDI timbrado
        /// </summary>
        public async Task<CancelacionResult> CancelarAsync(string facturamaId, string motivo, string folioSustitucion = "")
        {
            var result = new CancelacionResult();

            try
            {
                string url = $"/api-lite/cfdis/{facturamaId}?motive={motivo}";

                if (!string.IsNullOrEmpty(folioSustitucion))
                {
                    url += $"&uuidReplacement={folioSustitucion}";
                }

                var response = await _httpClient.DeleteAsync(url);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var cancelResponse = JsonSerializer.Deserialize<FacturamaCancelResponse>(responseBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    result.Success = true;
                    result.UUID = cancelResponse?.Uuid;
                    result.EstatusUUID = cancelResponse?.Status;
                    result.Acuse = cancelResponse?.AcuseXml;
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<FacturamaErrorResponse>(responseBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    result.Success = false;
                    result.ErrorCode = response.StatusCode.ToString();
                    result.ErrorMessage = errorResponse?.Message ?? "Error al cancelar";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorCode = "UNKNOWN";
                result.ErrorMessage = $"Error: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Cancela un CFDI por UUID (busca primero el ID de Facturama)
        /// </summary>
        public async Task<CancelacionResult> CancelarPorUUIDAsync(string rfcEmisor, string uuid, string motivo, string folioSustitucion = "")
        {
            var result = new CancelacionResult();

            try
            {
                // Buscar el CFDI por UUID
                var searchResponse = await _httpClient.GetAsync($"/api-lite/cfdis?keyword={uuid}");
                var searchBody = await searchResponse.Content.ReadAsStringAsync();

                if (searchResponse.IsSuccessStatusCode)
                {
                    var cfdis = JsonSerializer.Deserialize<List<FacturamaCfdiResponse>>(searchBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var cfdi = cfdis?.FirstOrDefault(c => c.Complement?.TaxStamp?.Uuid == uuid);

                    if (cfdi != null && !string.IsNullOrEmpty(cfdi.Id))
                    {
                        return await CancelarAsync(cfdi.Id, motivo, folioSustitucion);
                    }
                    else
                    {
                        result.Success = false;
                        result.ErrorCode = "NOT_FOUND";
                        result.ErrorMessage = "No se encontró el CFDI con el UUID especificado";
                    }
                }
                else
                {
                    result.Success = false;
                    result.ErrorCode = searchResponse.StatusCode.ToString();
                    result.ErrorMessage = "Error al buscar el CFDI";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorCode = "UNKNOWN";
                result.ErrorMessage = $"Error: {ex.Message}";
            }

            return result;
        }

        #endregion

        #region Consultas

        /// <summary>
        /// Verifica las credenciales y obtiene información de la cuenta
        /// </summary>
        public async Task<bool> VerificarCredencialesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Profile");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtiene los timbres disponibles en la cuenta (créditos)
        /// </summary>
        public async Task<int> ObtenerTimbresDisponiblesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Profile");
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var profile = JsonSerializer.Deserialize<FacturamaProfileResponse>(responseBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return profile?.FoliosRemaining ?? -1;
                }
            }
            catch
            {
                // Ignorar errores
            }

            return -1;
        }

        /// <summary>
        /// Consulta el estatus de un CFDI
        /// </summary>
        public async Task<ConsultaResult> ConsultarEstatusAsync(string facturamaId)
        {
            var result = new ConsultaResult();

            try
            {
                var response = await _httpClient.GetAsync($"/api-lite/cfdis/{facturamaId}");
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var cfdiResponse = JsonSerializer.Deserialize<FacturamaCfdiResponse>(responseBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    result.Success = true;
                    result.Estatus = cfdiResponse?.Status ?? "Desconocido";
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = "Error al consultar estatus";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        #endregion

        #region Utilidades para construir CFDI

        /// <summary>
        /// Crea un objeto de solicitud CFDI para Facturama desde los datos de la factura
        /// </summary>
        public static FacturamaCfdiRequest CrearSolicitudCFDI(
            // Datos del emisor
            string emisorRfc, string emisorNombre, string emisorRegimen,
            string certificadoBase64, string llaveBase64, string passwordLlave,
            // Datos del receptor
            string receptorRfc, string receptorNombre, string receptorRegimen,
            string receptorCodigoPostal, string usoCfdi,
            // Datos del comprobante
            string serie, int folio, string formaPago, string metodoPago,
            string lugarExpedicion,
            // Conceptos
            List<FacturamaConcepto> conceptos)
        {
            var request = new FacturamaCfdiRequest
            {
                Serie = serie,
                Folio = folio.ToString(),
                Currency = "MXN",
                ExpeditionPlace = lugarExpedicion,
                PaymentForm = formaPago,
                PaymentMethod = metodoPago,
                CfdiType = "I", // Ingreso
                Issuer = new FacturamaIssuer
                {
                    Rfc = emisorRfc,
                    Name = emisorNombre,
                    FiscalRegime = emisorRegimen
                },
                Receiver = new FacturamaReceiver
                {
                    Rfc = receptorRfc,
                    Name = receptorNombre,
                    CfdiUse = usoCfdi,
                    FiscalRegime = receptorRegimen,
                    TaxZipCode = receptorCodigoPostal
                },
                Items = conceptos?.Select(c => new FacturamaItem
                {
                    ProductCode = c.ClaveProdServ,
                    IdentificationNumber = c.NoIdentificacion,
                    Description = c.Descripcion,
                    Unit = c.Unidad,
                    UnitCode = c.ClaveUnidad,
                    UnitPrice = c.ValorUnitario,
                    Quantity = c.Cantidad,
                    Subtotal = c.Importe,
                    TaxObject = c.ObjetoImp,
                    Taxes = c.Impuestos?.Select(i => new FacturamaTax
                    {
                        Total = i.Importe,
                        Name = i.Impuesto == "002" ? "IVA" : "IEPS",
                        Base = i.Base,
                        Rate = i.TasaOCuota,
                        IsRetention = i.TipoFactor == "Retencion"
                    }).ToList() ?? new List<FacturamaTax>()
                }).ToList() ?? new List<FacturamaItem>()
            };

            return request;
        }

        #endregion
    }

    #region Modelos de Facturama

    public class FacturamaCfdiRequest
    {
        public string Serie { get; set; }
        public string Folio { get; set; }
        public string Currency { get; set; } = "MXN";
        public string ExpeditionPlace { get; set; }
        public string PaymentForm { get; set; }
        public string PaymentMethod { get; set; }
        public string CfdiType { get; set; } = "I";
        public FacturamaIssuer Issuer { get; set; }
        public FacturamaReceiver Receiver { get; set; }
        public List<FacturamaItem> Items { get; set; }
    }

    public class FacturamaIssuer
    {
        public string Rfc { get; set; }
        public string Name { get; set; }
        public string FiscalRegime { get; set; }
    }

    public class FacturamaReceiver
    {
        public string Rfc { get; set; }
        public string Name { get; set; }
        public string CfdiUse { get; set; }
        public string FiscalRegime { get; set; }
        public string TaxZipCode { get; set; }
    }

    public class FacturamaItem
    {
        public string ProductCode { get; set; }
        public string IdentificationNumber { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public string UnitCode { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal Subtotal { get; set; }
        public string TaxObject { get; set; }
        public List<FacturamaTax> Taxes { get; set; }
    }

    public class FacturamaTax
    {
        public decimal Total { get; set; }
        public string Name { get; set; }
        public decimal Base { get; set; }
        public decimal Rate { get; set; }
        public bool IsRetention { get; set; }
    }

    public class FacturamaCfdiResponse
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public string Serie { get; set; }
        public string Folio { get; set; }
        public FacturamaComplement Complement { get; set; }
    }

    public class FacturamaComplement
    {
        public FacturamaTaxStamp TaxStamp { get; set; }
    }

    public class FacturamaTaxStamp
    {
        public string Uuid { get; set; }
        public string Date { get; set; }
        public string SatSign { get; set; }
        public string SatCertNumber { get; set; }
        public string CfdiSign { get; set; }
        public string OriginalString { get; set; }
    }

    public class FacturamaCancelResponse
    {
        public string Uuid { get; set; }
        public string Status { get; set; }
        public string AcuseXml { get; set; }
    }

    public class FacturamaErrorResponse
    {
        public string Message { get; set; }
        public Dictionary<string, List<string>> ModelState { get; set; }
    }

    public class FacturamaProfileResponse
    {
        public string Rfc { get; set; }
        public string TaxName { get; set; }
        public int FoliosRemaining { get; set; }
        public string Plan { get; set; }
    }

    // Modelos auxiliares para construir CFDI
    public class FacturamaConcepto
    {
        public string ClaveProdServ { get; set; }
        public string NoIdentificacion { get; set; }
        public string Descripcion { get; set; }
        public string Unidad { get; set; }
        public string ClaveUnidad { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal Cantidad { get; set; }
        public decimal Importe { get; set; }
        public string ObjetoImp { get; set; }
        public List<FacturamaImpuesto> Impuestos { get; set; }
    }

    public class FacturamaImpuesto
    {
        public string Impuesto { get; set; } // 002 = IVA
        public string TipoFactor { get; set; } // Tasa, Cuota, Exento
        public decimal TasaOCuota { get; set; }
        public decimal Base { get; set; }
        public decimal Importe { get; set; }
    }

    #endregion
}
