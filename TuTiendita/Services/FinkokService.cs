using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace TuTiendita.Services
{
    /// <summary>
    /// Servicio para integración con Finkok PAC
    /// Documentación: https://wiki.finkok.com/
    /// </summary>
    public class FinkokService
    {
        // URLs de los servicios SOAP
        private const string SANDBOX_STAMP_URL = "https://demo-facturacion.finkok.com/servicios/soap/stamp.wsdl";
        private const string PRODUCTION_STAMP_URL = "https://facturacion.finkok.com/servicios/soap/stamp.wsdl";
        private const string SANDBOX_CANCEL_URL = "https://demo-facturacion.finkok.com/servicios/soap/cancel.wsdl";
        private const string PRODUCTION_CANCEL_URL = "https://facturacion.finkok.com/servicios/soap/cancel.wsdl";

        private readonly string _username;
        private readonly string _password;
        private readonly bool _isProduction;
        private readonly HttpClient _httpClient;

        public FinkokService(string username, string password, bool isProduction = false)
        {
            _username = username ?? throw new ArgumentNullException(nameof(username));
            _password = password ?? throw new ArgumentNullException(nameof(password));
            _isProduction = isProduction;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        #region Propiedades

        public string StampUrl => _isProduction ? PRODUCTION_STAMP_URL : SANDBOX_STAMP_URL;
        public string CancelUrl => _isProduction ? PRODUCTION_CANCEL_URL : SANDBOX_CANCEL_URL;
        public bool IsProduction => _isProduction;

        #endregion

        #region Timbrado

        /// <summary>
        /// Timbra un CFDI con Finkok
        /// </summary>
        /// <param name="xmlCFDI">XML del CFDI sin timbrar (ya firmado)</param>
        /// <returns>Resultado del timbrado</returns>
        public async Task<TimbradoResult> TimbrarAsync(string xmlCFDI)
        {
            var result = new TimbradoResult();

            try
            {
                // Convertir XML a Base64
                string xmlBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(xmlCFDI));

                // Construir envelope SOAP para stamp
                string soapEnvelope = BuildStampEnvelope(xmlBase64);

                // Enviar petición
                var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                content.Headers.Add("SOAPAction", "stamp");

                var response = await _httpClient.PostAsync(StampUrl.Replace(".wsdl", ""), content);
                string responseXml = await response.Content.ReadAsStringAsync();

                // Parsear respuesta
                result = ParseStampResponse(responseXml);
            }
            catch (HttpRequestException ex)
            {
                result.Success = false;
                result.ErrorCode = "HTTP_ERROR";
                result.ErrorMessage = $"Error de conexión con Finkok: {ex.Message}";
            }
            catch (TaskCanceledException)
            {
                result.Success = false;
                result.ErrorCode = "TIMEOUT";
                result.ErrorMessage = "Tiempo de espera agotado al conectar con Finkok";
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
        /// Timbra un CFDI usando el método quick_stamp (más rápido, requiere XML ya sellado)
        /// </summary>
        public async Task<TimbradoResult> QuickStampAsync(string xmlCFDI)
        {
            var result = new TimbradoResult();

            try
            {
                string xmlBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(xmlCFDI));
                string soapEnvelope = BuildQuickStampEnvelope(xmlBase64);

                var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                content.Headers.Add("SOAPAction", "quick_stamp");

                var response = await _httpClient.PostAsync(StampUrl.Replace(".wsdl", ""), content);
                string responseXml = await response.Content.ReadAsStringAsync();

                result = ParseStampResponse(responseXml);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorCode = "UNKNOWN";
                result.ErrorMessage = $"Error: {ex.Message}";
            }

            return result;
        }

        private string BuildStampEnvelope(string xmlBase64)
        {
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:stamp=""http://facturacion.finkok.com/stamp"">
   <soapenv:Header/>
   <soapenv:Body>
      <stamp:stamp>
         <stamp:xml>{xmlBase64}</stamp:xml>
         <stamp:username>{_username}</stamp:username>
         <stamp:password>{_password}</stamp:password>
      </stamp:stamp>
   </soapenv:Body>
</soapenv:Envelope>";
        }

        private string BuildQuickStampEnvelope(string xmlBase64)
        {
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:stamp=""http://facturacion.finkok.com/stamp"">
   <soapenv:Header/>
   <soapenv:Body>
      <stamp:quick_stamp>
         <stamp:xml>{xmlBase64}</stamp:xml>
         <stamp:username>{_username}</stamp:username>
         <stamp:password>{_password}</stamp:password>
      </stamp:quick_stamp>
   </soapenv:Body>
</soapenv:Envelope>";
        }

        private TimbradoResult ParseStampResponse(string responseXml)
        {
            var result = new TimbradoResult();

            try
            {
                var doc = XDocument.Parse(responseXml);
                XNamespace ns = "http://facturacion.finkok.com/stamp";

                // Buscar el resultado del timbrado
                var stampResult = doc.Descendants(ns + "stampResult").FirstOrDefault()
                    ?? doc.Descendants("stampResult").FirstOrDefault();

                if (stampResult == null)
                {
                    // Intentar buscar quick_stampResult
                    stampResult = doc.Descendants(ns + "quick_stampResult").FirstOrDefault()
                        ?? doc.Descendants("quick_stampResult").FirstOrDefault();
                }

                if (stampResult != null)
                {
                    // Obtener XML timbrado
                    var xmlElement = stampResult.Descendants("xml").FirstOrDefault();
                    if (xmlElement != null && !string.IsNullOrEmpty(xmlElement.Value))
                    {
                        result.XMLTimbrado = xmlElement.Value;
                        result.Success = true;

                        // Extraer datos del timbre del XML timbrado
                        ExtractTimbreData(result);
                    }

                    // Obtener UUID
                    var uuidElement = stampResult.Descendants("UUID").FirstOrDefault();
                    if (uuidElement != null)
                    {
                        result.UUID = uuidElement.Value;
                    }

                    // Obtener código de estatus
                    var codEstatusElement = stampResult.Descendants("CodEstatus").FirstOrDefault();
                    if (codEstatusElement != null)
                    {
                        result.StatusCode = codEstatusElement.Value;
                    }

                    // Verificar errores
                    var incidencias = stampResult.Descendants("Incidencia");
                    foreach (var incidencia in incidencias)
                    {
                        var codigoError = incidencia.Descendants("CodigoError").FirstOrDefault()?.Value;
                        var mensajeError = incidencia.Descendants("MensajeIncidencia").FirstOrDefault()?.Value;

                        if (!string.IsNullOrEmpty(codigoError))
                        {
                            result.Success = false;
                            result.ErrorCode = codigoError;
                            result.ErrorMessage = mensajeError ?? "Error desconocido";
                            break;
                        }
                    }
                }
                else
                {
                    // Buscar errores en el SOAP fault
                    var faultString = doc.Descendants("faultstring").FirstOrDefault();
                    if (faultString != null)
                    {
                        result.Success = false;
                        result.ErrorCode = "SOAP_FAULT";
                        result.ErrorMessage = faultString.Value;
                    }
                    else
                    {
                        result.Success = false;
                        result.ErrorCode = "PARSE_ERROR";
                        result.ErrorMessage = "No se pudo parsear la respuesta de Finkok";
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorCode = "PARSE_ERROR";
                result.ErrorMessage = $"Error al parsear respuesta: {ex.Message}";
            }

            return result;
        }

        private void ExtractTimbreData(TimbradoResult result)
        {
            if (string.IsNullOrEmpty(result.XMLTimbrado)) return;

            try
            {
                var doc = XDocument.Parse(result.XMLTimbrado);
                XNamespace tfd = "http://www.sat.gob.mx/TimbreFiscalDigital";
                XNamespace cfdi = "http://www.sat.gob.mx/cfd/4";

                var timbre = doc.Descendants(tfd + "TimbreFiscalDigital").FirstOrDefault();
                if (timbre != null)
                {
                    result.UUID = timbre.Attribute("UUID")?.Value;
                    result.FechaTimbrado = timbre.Attribute("FechaTimbrado")?.Value;
                    result.SelloSAT = timbre.Attribute("SelloSAT")?.Value;
                    result.NoCertificadoSAT = timbre.Attribute("NoCertificadoSAT")?.Value;
                    result.CadenaOriginalTFD = BuildCadenaOriginalTFD(timbre);
                }

                // Obtener sello del CFDI
                var comprobante = doc.Descendants(cfdi + "Comprobante").FirstOrDefault();
                if (comprobante != null)
                {
                    result.SelloCFDI = comprobante.Attribute("Sello")?.Value;
                    result.NoCertificadoCFDI = comprobante.Attribute("NoCertificado")?.Value;
                }
            }
            catch
            {
                // Si falla la extracción, al menos tenemos el XML
            }
        }

        private string BuildCadenaOriginalTFD(XElement timbre)
        {
            // Cadena original del TFD según especificación del SAT
            var version = timbre.Attribute("Version")?.Value ?? "1.1";
            var uuid = timbre.Attribute("UUID")?.Value ?? "";
            var fechaTimbrado = timbre.Attribute("FechaTimbrado")?.Value ?? "";
            var rfcProvCertif = timbre.Attribute("RfcProvCertif")?.Value ?? "";
            var leyenda = timbre.Attribute("Leyenda")?.Value ?? "";
            var selloCFD = timbre.Attribute("SelloCFD")?.Value ?? "";
            var noCertificadoSAT = timbre.Attribute("NoCertificadoSAT")?.Value ?? "";

            return $"||{version}|{uuid}|{fechaTimbrado}|{rfcProvCertif}|{leyenda}|{selloCFD}|{noCertificadoSAT}||";
        }

        #endregion

        #region Cancelación

        /// <summary>
        /// Cancela un CFDI timbrado
        /// </summary>
        public async Task<CancelacionResult> CancelarAsync(string rfcEmisor, string uuid,
            string certificadoBase64, string llaveBase64, string passwordLlave, string motivo = "02", string folioSustitucion = "")
        {
            var result = new CancelacionResult();

            try
            {
                string soapEnvelope = BuildCancelEnvelope(rfcEmisor, uuid, certificadoBase64,
                    llaveBase64, passwordLlave, motivo, folioSustitucion);

                var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                content.Headers.Add("SOAPAction", "cancel");

                var response = await _httpClient.PostAsync(CancelUrl.Replace(".wsdl", ""), content);
                string responseXml = await response.Content.ReadAsStringAsync();

                result = ParseCancelResponse(responseXml);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorCode = "UNKNOWN";
                result.ErrorMessage = $"Error: {ex.Message}";
            }

            return result;
        }

        private string BuildCancelEnvelope(string rfcEmisor, string uuid, string certificadoBase64,
            string llaveBase64, string passwordLlave, string motivo, string folioSustitucion)
        {
            string uuidsXml = $@"<cancel:UUID>{uuid}</cancel:UUID>";

            if (!string.IsNullOrEmpty(folioSustitucion))
            {
                uuidsXml = $@"<cancel:UUIDS>
                    <cancel:uuids>
                        <cancel:uuid>{uuid}</cancel:uuid>
                        <cancel:motivo>{motivo}</cancel:motivo>
                        <cancel:foliosustitucion>{folioSustitucion}</cancel:foliosustitucion>
                    </cancel:uuids>
                </cancel:UUIDS>";
            }

            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:cancel=""http://facturacion.finkok.com/cancel"">
   <soapenv:Header/>
   <soapenv:Body>
      <cancel:cancel>
         <cancel:UUIDS>
            <cancel:uuids>
               <cancel:uuid>{uuid}</cancel:uuid>
               <cancel:motivo>{motivo}</cancel:motivo>
               <cancel:foliosustitucion>{folioSustitucion}</cancel:foliosustitucion>
            </cancel:uuids>
         </cancel:UUIDS>
         <cancel:username>{_username}</cancel:username>
         <cancel:password>{_password}</cancel:password>
         <cancel:taxpayer_id>{rfcEmisor}</cancel:taxpayer_id>
         <cancel:cer>{certificadoBase64}</cancel:cer>
         <cancel:key>{llaveBase64}</cancel:key>
         <cancel:passphrase>{passwordLlave}</cancel:passphrase>
      </cancel:cancel>
   </soapenv:Body>
</soapenv:Envelope>";
        }

        private CancelacionResult ParseCancelResponse(string responseXml)
        {
            var result = new CancelacionResult();

            try
            {
                var doc = XDocument.Parse(responseXml);
                XNamespace ns = "http://facturacion.finkok.com/cancel";

                var cancelResult = doc.Descendants(ns + "cancelResult").FirstOrDefault()
                    ?? doc.Descendants("cancelResult").FirstOrDefault();

                if (cancelResult != null)
                {
                    var folios = cancelResult.Descendants("Folio");
                    foreach (var folio in folios)
                    {
                        var uuid = folio.Descendants("UUID").FirstOrDefault()?.Value;
                        var estatusUUID = folio.Descendants("EstatusUUID").FirstOrDefault()?.Value;

                        if (!string.IsNullOrEmpty(uuid))
                        {
                            result.UUID = uuid;
                            result.EstatusUUID = estatusUUID;

                            // Códigos de éxito: 201 = Cancelado, 202 = Cancelación en proceso
                            result.Success = estatusUUID == "201" || estatusUUID == "202";
                        }
                    }

                    var codEstatus = cancelResult.Descendants("CodEstatus").FirstOrDefault()?.Value;
                    result.StatusCode = codEstatus;

                    // Verificar si hay acuse
                    var acuse = cancelResult.Descendants("Acuse").FirstOrDefault()?.Value;
                    if (!string.IsNullOrEmpty(acuse))
                    {
                        result.Acuse = acuse;
                    }
                }
                else
                {
                    var faultString = doc.Descendants("faultstring").FirstOrDefault();
                    result.Success = false;
                    result.ErrorCode = "SOAP_FAULT";
                    result.ErrorMessage = faultString?.Value ?? "Error desconocido";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorCode = "PARSE_ERROR";
                result.ErrorMessage = $"Error al parsear respuesta: {ex.Message}";
            }

            return result;
        }

        #endregion

        #region Consultas

        /// <summary>
        /// Consulta el estatus de un CFDI en el SAT
        /// </summary>
        public async Task<ConsultaResult> ConsultarEstatusAsync(string rfcEmisor, string rfcReceptor,
            decimal total, string uuid)
        {
            var result = new ConsultaResult();

            try
            {
                string soapEnvelope = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:stamp=""http://facturacion.finkok.com/stamp"">
   <soapenv:Header/>
   <soapenv:Body>
      <stamp:query_pending>
         <stamp:username>{_username}</stamp:username>
         <stamp:password>{_password}</stamp:password>
         <stamp:uuid>{uuid}</stamp:uuid>
      </stamp:query_pending>
   </soapenv:Body>
</soapenv:Envelope>";

                var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                content.Headers.Add("SOAPAction", "query_pending");

                var response = await _httpClient.PostAsync(StampUrl.Replace(".wsdl", ""), content);
                string responseXml = await response.Content.ReadAsStringAsync();

                // Parsear respuesta
                var doc = XDocument.Parse(responseXml);
                var status = doc.Descendants("status").FirstOrDefault()?.Value;

                result.Success = true;
                result.Estatus = status ?? "Desconocido";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Obtiene los timbres disponibles en la cuenta
        /// </summary>
        public async Task<int> ObtenerTimbresDisponiblesAsync()
        {
            try
            {
                string soapEnvelope = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:reg=""http://facturacion.finkok.com/registration"">
   <soapenv:Header/>
   <soapenv:Body>
      <reg:get>
         <reg:reseller_username>{_username}</reg:reseller_username>
         <reg:reseller_password>{_password}</reg:reseller_password>
         <reg:taxpayer_id>{_username}</reg:taxpayer_id>
      </reg:get>
   </soapenv:Body>
</soapenv:Envelope>";

                var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                var registrationUrl = _isProduction
                    ? "https://facturacion.finkok.com/servicios/soap/registration"
                    : "https://demo-facturacion.finkok.com/servicios/soap/registration";

                var response = await _httpClient.PostAsync(registrationUrl, content);
                string responseXml = await response.Content.ReadAsStringAsync();

                var doc = XDocument.Parse(responseXml);
                var credits = doc.Descendants("credit").FirstOrDefault()?.Value;

                if (int.TryParse(credits, out int timbres))
                {
                    return timbres;
                }
            }
            catch
            {
                // Ignorar errores, retornar -1
            }

            return -1;
        }

        #endregion

        #region Utilidades para Firma Digital

        /// <summary>
        /// Firma un XML CFDI con el certificado CSD
        /// </summary>
        public static string FirmarXML(string xmlCFDI, string certificadoPath, string llavePath, string password)
        {
            try
            {
                // Cargar certificado
                var certBytes = File.ReadAllBytes(certificadoPath);
                var certBase64 = Convert.ToBase64String(certBytes);

                // Cargar llave privada
                var keyBytes = File.ReadAllBytes(llavePath);

                // Extraer número de certificado
                var cert = new X509Certificate2(certBytes);
                string noCertificado = ExtractNoCertificado(cert);

                // Generar cadena original
                string cadenaOriginal = GenerarCadenaOriginal(xmlCFDI);

                // Firmar cadena original
                string sello = FirmarCadenaOriginal(cadenaOriginal, keyBytes, password);

                // Insertar datos en el XML
                var doc = XDocument.Parse(xmlCFDI);
                XNamespace cfdi = "http://www.sat.gob.mx/cfd/4";

                var comprobante = doc.Descendants(cfdi + "Comprobante").FirstOrDefault();
                if (comprobante != null)
                {
                    comprobante.SetAttributeValue("Certificado", certBase64);
                    comprobante.SetAttributeValue("NoCertificado", noCertificado);
                    comprobante.SetAttributeValue("Sello", sello);
                }

                return doc.ToString(SaveOptions.DisableFormatting);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al firmar XML: {ex.Message}", ex);
            }
        }

        private static string ExtractNoCertificado(X509Certificate2 cert)
        {
            // El número de certificado está en el serial number en formato hexadecimal
            string serial = cert.SerialNumber;

            // Convertir de hex a string (cada par de hex es un caracter ASCII)
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < serial.Length; i += 2)
            {
                string hex = serial.Substring(i, 2);
                int value = Convert.ToInt32(hex, 16);
                if (value >= 48 && value <= 57) // Solo dígitos
                {
                    sb.Append((char)value);
                }
            }

            return sb.ToString().PadLeft(20, '0');
        }

        private static string GenerarCadenaOriginal(string xmlCFDI)
        {
            // En producción, esto debería usar el XSLT oficial del SAT
            // Por simplicidad, generamos una cadena básica
            var doc = XDocument.Parse(xmlCFDI);
            XNamespace cfdi = "http://www.sat.gob.mx/cfd/4";

            var comp = doc.Descendants(cfdi + "Comprobante").FirstOrDefault();
            if (comp == null) return "";

            var sb = new StringBuilder("||");

            // Atributos del comprobante
            sb.Append(comp.Attribute("Version")?.Value + "|");
            sb.Append(comp.Attribute("Serie")?.Value + "|");
            sb.Append(comp.Attribute("Folio")?.Value + "|");
            sb.Append(comp.Attribute("Fecha")?.Value + "|");
            sb.Append(comp.Attribute("FormaPago")?.Value + "|");
            sb.Append(comp.Attribute("NoCertificado")?.Value + "|");
            sb.Append(comp.Attribute("SubTotal")?.Value + "|");
            sb.Append(comp.Attribute("Descuento")?.Value + "|");
            sb.Append(comp.Attribute("Moneda")?.Value + "|");
            sb.Append(comp.Attribute("Total")?.Value + "|");
            sb.Append(comp.Attribute("TipoDeComprobante")?.Value + "|");
            sb.Append(comp.Attribute("Exportacion")?.Value + "|");
            sb.Append(comp.Attribute("MetodoPago")?.Value + "|");
            sb.Append(comp.Attribute("LugarExpedicion")?.Value + "|");

            // Emisor
            var emisor = comp.Descendants(cfdi + "Emisor").FirstOrDefault();
            if (emisor != null)
            {
                sb.Append(emisor.Attribute("Rfc")?.Value + "|");
                sb.Append(emisor.Attribute("Nombre")?.Value + "|");
                sb.Append(emisor.Attribute("RegimenFiscal")?.Value + "|");
            }

            // Receptor
            var receptor = comp.Descendants(cfdi + "Receptor").FirstOrDefault();
            if (receptor != null)
            {
                sb.Append(receptor.Attribute("Rfc")?.Value + "|");
                sb.Append(receptor.Attribute("Nombre")?.Value + "|");
                sb.Append(receptor.Attribute("DomicilioFiscalReceptor")?.Value + "|");
                sb.Append(receptor.Attribute("RegimenFiscalReceptor")?.Value + "|");
                sb.Append(receptor.Attribute("UsoCFDI")?.Value + "|");
            }

            // Conceptos
            var conceptos = comp.Descendants(cfdi + "Concepto");
            foreach (var concepto in conceptos)
            {
                sb.Append(concepto.Attribute("ClaveProdServ")?.Value + "|");
                sb.Append(concepto.Attribute("Cantidad")?.Value + "|");
                sb.Append(concepto.Attribute("ClaveUnidad")?.Value + "|");
                sb.Append(concepto.Attribute("Descripcion")?.Value + "|");
                sb.Append(concepto.Attribute("ValorUnitario")?.Value + "|");
                sb.Append(concepto.Attribute("Importe")?.Value + "|");
                sb.Append(concepto.Attribute("ObjetoImp")?.Value + "|");
            }

            sb.Append("|");
            return sb.ToString();
        }

        private static string FirmarCadenaOriginal(string cadenaOriginal, byte[] keyBytes, string password)
        {
            try
            {
                // Decodificar la llave privada .key del SAT
                using var rsa = DecodePrivateKey(keyBytes, password);

                byte[] cadenaBytes = Encoding.UTF8.GetBytes(cadenaOriginal);
                byte[] firma = rsa.SignData(cadenaBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                return Convert.ToBase64String(firma);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al firmar: {ex.Message}", ex);
            }
        }

        private static RSA DecodePrivateKey(byte[] keyBytes, string password)
        {
            // Las llaves .key del SAT están en formato PKCS#8 encriptado
            try
            {
                var rsa = RSA.Create();
                rsa.ImportEncryptedPkcs8PrivateKey(
                    Encoding.UTF8.GetBytes(password),
                    keyBytes,
                    out _);
                return rsa;
            }
            catch
            {
                // Intentar como PEM si falla
                var rsa = RSA.Create();
                rsa.ImportPkcs8PrivateKey(keyBytes, out _);
                return rsa;
            }
        }

        #endregion
    }

    #region Modelos de Resultado

    public class TimbradoResult
    {
        public bool Success { get; set; }
        public string UUID { get; set; }
        public string FechaTimbrado { get; set; }
        public string XMLTimbrado { get; set; }
        public string SelloCFDI { get; set; }
        public string SelloSAT { get; set; }
        public string NoCertificadoCFDI { get; set; }
        public string NoCertificadoSAT { get; set; }
        public string CadenaOriginalTFD { get; set; }
        public string StatusCode { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class CancelacionResult
    {
        public bool Success { get; set; }
        public string UUID { get; set; }
        public string EstatusUUID { get; set; }
        public string StatusCode { get; set; }
        public string Acuse { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ConsultaResult
    {
        public bool Success { get; set; }
        public string Estatus { get; set; }
        public string ErrorMessage { get; set; }
    }

    #endregion
}
