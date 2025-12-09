using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TuTiendita.Helpers
{
    /// <summary>
    /// Validador completo para CFDI 4.0 conforme a las reglas del SAT
    /// IMPORTANTE: Errores en facturación pueden tener consecuencias legales graves
    /// </summary>
    public static class ValidadorCFDI
    {
        #region Resultado de Validación

        public class ResultadoValidacion
        {
            public bool EsValido => Errores.Count == 0;
            public List<ErrorValidacion> Errores { get; } = new List<ErrorValidacion>();
            public List<string> Advertencias { get; } = new List<string>();

            public void AgregarError(string codigo, string mensaje, string campo = null)
            {
                Errores.Add(new ErrorValidacion { Codigo = codigo, Mensaje = mensaje, Campo = campo });
            }

            public void AgregarAdvertencia(string mensaje)
            {
                Advertencias.Add(mensaje);
            }

            public string ObtenerResumen()
            {
                if (EsValido && Advertencias.Count == 0)
                    return "Validacion exitosa. El CFDI cumple con los requisitos del SAT.";

                var resumen = new List<string>();

                if (Errores.Count > 0)
                {
                    resumen.Add($"ERRORES ({Errores.Count}):");
                    foreach (var error in Errores)
                    {
                        resumen.Add($"  - [{error.Codigo}] {error.Mensaje}");
                    }
                }

                if (Advertencias.Count > 0)
                {
                    resumen.Add($"\nADVERTENCIAS ({Advertencias.Count}):");
                    foreach (var adv in Advertencias)
                    {
                        resumen.Add($"  - {adv}");
                    }
                }

                return string.Join("\n", resumen);
            }
        }

        public class ErrorValidacion
        {
            public string Codigo { get; set; }
            public string Mensaje { get; set; }
            public string Campo { get; set; }
        }

        #endregion

        #region Validación de RFC

        /// <summary>
        /// Valida un RFC de forma exhaustiva según las reglas del SAT
        /// </summary>
        public static ResultadoValidacion ValidarRFC(string rfc, bool esReceptor = false)
        {
            var resultado = new ResultadoValidacion();

            if (string.IsNullOrWhiteSpace(rfc))
            {
                resultado.AgregarError("RFC001", "El RFC es obligatorio", "RFC");
                return resultado;
            }

            rfc = rfc.Trim().ToUpper();

            // RFCs genéricos permitidos
            if (rfc == "XAXX010101000") // Público en general
            {
                if (!esReceptor)
                    resultado.AgregarError("RFC002", "El RFC generico XAXX010101000 solo puede usarse como receptor", "RFC");
                return resultado;
            }

            if (rfc == "XEXX010101000") // Extranjero
            {
                if (!esReceptor)
                    resultado.AgregarError("RFC003", "El RFC de extranjero XEXX010101000 solo puede usarse como receptor", "RFC");
                return resultado;
            }

            // Validar longitud
            if (rfc.Length != 12 && rfc.Length != 13)
            {
                resultado.AgregarError("RFC004",
                    $"El RFC debe tener 12 caracteres (persona moral) o 13 caracteres (persona fisica). Longitud actual: {rfc.Length}",
                    "RFC");
                return resultado;
            }

            // Patrones según tipo de persona
            bool esPersonaFisica = rfc.Length == 13;
            string patron = esPersonaFisica
                ? @"^[A-ZÑ&]{4}\d{6}[A-Z0-9]{3}$"
                : @"^[A-ZÑ&]{3}\d{6}[A-Z0-9]{3}$";

            if (!Regex.IsMatch(rfc, patron))
            {
                resultado.AgregarError("RFC005",
                    esPersonaFisica
                        ? "Formato invalido para persona fisica. Debe ser: 4 letras + 6 digitos (fecha) + 3 caracteres (homoclave)"
                        : "Formato invalido para persona moral. Debe ser: 3 letras + 6 digitos (fecha) + 3 caracteres (homoclave)",
                    "RFC");
                return resultado;
            }

            // Validar fecha de nacimiento/constitución
            string fechaStr = esPersonaFisica ? rfc.Substring(4, 6) : rfc.Substring(3, 6);
            if (!ValidarFechaRFC(fechaStr))
            {
                resultado.AgregarError("RFC006",
                    "La fecha en el RFC no es valida (formato AAMMDD)",
                    "RFC");
            }

            // Validar caracteres no permitidos
            if (rfc.Contains("Ñ") && rfc.IndexOf("Ñ") >= (esPersonaFisica ? 4 : 3))
            {
                resultado.AgregarError("RFC007",
                    "El caracter Ñ solo esta permitido en la parte inicial del RFC (nombre)",
                    "RFC");
            }

            return resultado;
        }

        private static bool ValidarFechaRFC(string fechaStr)
        {
            if (fechaStr.Length != 6) return false;

            if (!int.TryParse(fechaStr.Substring(0, 2), out int año) ||
                !int.TryParse(fechaStr.Substring(2, 2), out int mes) ||
                !int.TryParse(fechaStr.Substring(4, 2), out int dia))
            {
                return false;
            }

            // Año puede ser 00-99 (1900s o 2000s)
            if (mes < 1 || mes > 12) return false;
            if (dia < 1 || dia > 31) return false;

            // Validación básica de días por mes
            int[] diasPorMes = { 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            if (dia > diasPorMes[mes - 1]) return false;

            return true;
        }

        /// <summary>
        /// Determina si el RFC corresponde a persona física
        /// </summary>
        public static bool EsPersonaFisica(string rfc)
        {
            if (string.IsNullOrWhiteSpace(rfc)) return false;
            return rfc.Trim().Length == 13;
        }

        #endregion

        #region Validación de Código Postal

        /// <summary>
        /// Valida un código postal mexicano
        /// </summary>
        public static ResultadoValidacion ValidarCodigoPostal(string cp)
        {
            var resultado = new ResultadoValidacion();

            if (string.IsNullOrWhiteSpace(cp))
            {
                resultado.AgregarError("CP001", "El codigo postal es obligatorio", "CodigoPostal");
                return resultado;
            }

            cp = cp.Trim();

            if (cp.Length != 5)
            {
                resultado.AgregarError("CP002",
                    $"El codigo postal debe tener exactamente 5 digitos. Longitud actual: {cp.Length}",
                    "CodigoPostal");
                return resultado;
            }

            if (!Regex.IsMatch(cp, @"^\d{5}$"))
            {
                resultado.AgregarError("CP003",
                    "El codigo postal solo debe contener digitos",
                    "CodigoPostal");
                return resultado;
            }

            // Validar que el primer dígito sea válido (01-33 para estados mexicanos)
            int primerosDos = int.Parse(cp.Substring(0, 2));
            if (primerosDos < 1 || primerosDos > 33)
            {
                resultado.AgregarAdvertencia(
                    $"El codigo postal {cp} podria no ser valido. Los primeros dos digitos ({primerosDos}) estan fuera del rango comun (01-33).");
            }

            return resultado;
        }

        #endregion

        #region Validación de Montos

        /// <summary>
        /// Valida los montos del CFDI
        /// </summary>
        public static ResultadoValidacion ValidarMontos(decimal subtotal, decimal iva, decimal total, decimal descuento = 0)
        {
            var resultado = new ResultadoValidacion();

            // Validar que no sean negativos
            if (subtotal < 0)
                resultado.AgregarError("MONTO001", "El subtotal no puede ser negativo", "Subtotal");

            if (iva < 0)
                resultado.AgregarError("MONTO002", "El IVA no puede ser negativo", "IVA");

            if (total < 0)
                resultado.AgregarError("MONTO003", "El total no puede ser negativo", "Total");

            if (descuento < 0)
                resultado.AgregarError("MONTO004", "El descuento no puede ser negativo", "Descuento");

            // Validar que el descuento no sea mayor al subtotal
            if (descuento > subtotal)
                resultado.AgregarError("MONTO005", "El descuento no puede ser mayor al subtotal", "Descuento");

            // Validar cálculo del total
            decimal totalCalculado = subtotal - descuento + iva;
            if (Math.Abs(total - totalCalculado) > 0.01m)
            {
                resultado.AgregarError("MONTO006",
                    $"El total ({total:F2}) no coincide con el calculo esperado ({totalCalculado:F2}). " +
                    $"Formula: Subtotal ({subtotal:F2}) - Descuento ({descuento:F2}) + IVA ({iva:F2})",
                    "Total");
            }

            // Validar decimales (máximo 2 para totales según SAT)
            if (decimal.Round(subtotal, 2) != subtotal)
                resultado.AgregarAdvertencia("El subtotal tiene mas de 2 decimales. Se recomienda redondear.");

            if (decimal.Round(total, 2) != total)
                resultado.AgregarAdvertencia("El total tiene mas de 2 decimales. Se recomienda redondear.");

            // Advertencia para montos muy pequeños
            if (total > 0 && total < 1)
                resultado.AgregarAdvertencia("El total es menor a $1.00 MXN. Verifique que sea correcto.");

            // Advertencia para factura a público en general con monto alto
            if (total > 2000)
                resultado.AgregarAdvertencia(
                    "IMPORTANTE: Para ventas mayores a $2,000 MXN a publico en general, " +
                    "se recomienda solicitar datos fiscales del cliente para su deducibilidad.");

            return resultado;
        }

        /// <summary>
        /// Valida el cálculo del IVA
        /// </summary>
        public static ResultadoValidacion ValidarCalculoIVA(decimal baseCalculo, decimal ivaCalculado, decimal tasaIVA = 0.16m)
        {
            var resultado = new ResultadoValidacion();

            decimal ivaEsperado = Math.Round(baseCalculo * tasaIVA, 2);
            decimal diferencia = Math.Abs(ivaCalculado - ivaEsperado);

            // Permitir diferencia de hasta 1 centavo por redondeo
            if (diferencia > 0.01m)
            {
                resultado.AgregarError("IVA001",
                    $"El IVA calculado ({ivaCalculado:F2}) no coincide con el esperado ({ivaEsperado:F2}) " +
                    $"para la base {baseCalculo:F2} con tasa {tasaIVA:P0}",
                    "IVA");
            }

            return resultado;
        }

        #endregion

        #region Validación de Régimen Fiscal

        /// <summary>
        /// Valida que el régimen fiscal sea compatible con el tipo de persona
        /// </summary>
        public static ResultadoValidacion ValidarRegimenFiscal(string claveRegimen, string rfc)
        {
            var resultado = new ResultadoValidacion();

            if (string.IsNullOrWhiteSpace(claveRegimen))
            {
                resultado.AgregarError("REG001", "El regimen fiscal es obligatorio", "RegimenFiscal");
                return resultado;
            }

            var regimen = CatalogosSAT.GetRegimenFiscalPorClave(claveRegimen);
            if (regimen == null)
            {
                resultado.AgregarError("REG002",
                    $"La clave de regimen fiscal '{claveRegimen}' no existe en el catalogo del SAT",
                    "RegimenFiscal");
                return resultado;
            }

            bool esPersonaFisica = EsPersonaFisica(rfc);

            if (esPersonaFisica && !regimen.PersonaFisica)
            {
                resultado.AgregarError("REG003",
                    $"El regimen '{regimen.Descripcion}' no aplica para personas fisicas",
                    "RegimenFiscal");
            }

            if (!esPersonaFisica && !regimen.PersonaMoral)
            {
                resultado.AgregarError("REG004",
                    $"El regimen '{regimen.Descripcion}' no aplica para personas morales",
                    "RegimenFiscal");
            }

            return resultado;
        }

        #endregion

        #region Validación de Uso CFDI

        /// <summary>
        /// Valida que el uso del CFDI sea compatible con el tipo de persona
        /// </summary>
        public static ResultadoValidacion ValidarUsoCFDI(string claveUso, string rfcReceptor)
        {
            var resultado = new ResultadoValidacion();

            if (string.IsNullOrWhiteSpace(claveUso))
            {
                resultado.AgregarError("USO001", "El uso del CFDI es obligatorio", "UsoCFDI");
                return resultado;
            }

            var uso = CatalogosSAT.GetUsoCFDIPorClave(claveUso);
            if (uso == null)
            {
                resultado.AgregarError("USO002",
                    $"La clave de uso CFDI '{claveUso}' no existe en el catalogo del SAT",
                    "UsoCFDI");
                return resultado;
            }

            // RFC público en general solo puede usar S01
            if (rfcReceptor == "XAXX010101000" && claveUso != "S01")
            {
                resultado.AgregarError("USO003",
                    "Para receptor con RFC generico (publico en general), el uso CFDI debe ser S01 (Sin efectos fiscales)",
                    "UsoCFDI");
            }

            bool esPersonaFisica = EsPersonaFisica(rfcReceptor);

            if (esPersonaFisica && !uso.PersonaFisica)
            {
                resultado.AgregarError("USO004",
                    $"El uso '{uso.Descripcion}' no aplica para personas fisicas",
                    "UsoCFDI");
            }

            if (!esPersonaFisica && !uso.PersonaMoral)
            {
                resultado.AgregarError("USO005",
                    $"El uso '{uso.Descripcion}' no aplica para personas morales",
                    "UsoCFDI");
            }

            return resultado;
        }

        #endregion

        #region Validación de Forma y Método de Pago

        /// <summary>
        /// Valida la combinación de forma y método de pago
        /// </summary>
        public static ResultadoValidacion ValidarFormaPagoMetodoPago(string claveFormaPago, string claveMetodoPago)
        {
            var resultado = new ResultadoValidacion();

            if (string.IsNullOrWhiteSpace(claveFormaPago))
            {
                resultado.AgregarError("PAGO001", "La forma de pago es obligatoria", "FormaPago");
            }

            if (string.IsNullOrWhiteSpace(claveMetodoPago))
            {
                resultado.AgregarError("PAGO002", "El metodo de pago es obligatorio", "MetodoPago");
            }

            // Validar que existan en el catálogo
            var formaPago = CatalogosSAT.GetFormaPagoPorClave(claveFormaPago);
            if (formaPago == null && !string.IsNullOrWhiteSpace(claveFormaPago))
            {
                resultado.AgregarError("PAGO003",
                    $"La clave de forma de pago '{claveFormaPago}' no existe en el catalogo del SAT",
                    "FormaPago");
            }

            var metodoPago = CatalogosSAT.GetMetodoPagoPorClave(claveMetodoPago);
            if (metodoPago == null && !string.IsNullOrWhiteSpace(claveMetodoPago))
            {
                resultado.AgregarError("PAGO004",
                    $"La clave de metodo de pago '{claveMetodoPago}' no existe en el catalogo del SAT",
                    "MetodoPago");
            }

            // Reglas de combinación
            if (claveMetodoPago == "PUE" && claveFormaPago == "99")
            {
                resultado.AgregarError("PAGO005",
                    "No se puede usar forma de pago '99 - Por definir' con metodo de pago 'PUE - Pago en una sola exhibicion'",
                    "FormaPago");
            }

            if (claveMetodoPago == "PPD" && claveFormaPago != "99")
            {
                resultado.AgregarAdvertencia(
                    "Para metodo de pago 'PPD - Pago en parcialidades o diferido', " +
                    "generalmente se usa forma de pago '99 - Por definir'. " +
                    "La forma de pago real se indica en el complemento de pago.");
            }

            return resultado;
        }

        #endregion

        #region Validación Completa de CFDI

        /// <summary>
        /// Realiza una validación completa del CFDI antes de timbrar
        /// </summary>
        public static ResultadoValidacion ValidarCFDICompleto(CFDI cfdi)
        {
            var resultado = new ResultadoValidacion();

            if (cfdi == null)
            {
                resultado.AgregarError("CFDI000", "El objeto CFDI es nulo", null);
                return resultado;
            }

            // Validar Serie y Folio
            if (!string.IsNullOrEmpty(cfdi.Serie) && !Regex.IsMatch(cfdi.Serie, @"^[A-Za-z0-9]{1,25}$"))
            {
                resultado.AgregarError("CFDI001",
                    "La serie solo puede contener letras y numeros (maximo 25 caracteres)",
                    "Serie");
            }

            if (string.IsNullOrWhiteSpace(cfdi.Folio))
            {
                resultado.AgregarError("CFDI002", "El folio es obligatorio", "Folio");
            }
            else if (!Regex.IsMatch(cfdi.Folio, @"^[A-Za-z0-9]{1,40}$"))
            {
                resultado.AgregarError("CFDI003",
                    "El folio solo puede contener letras y numeros (maximo 40 caracteres)",
                    "Folio");
            }

            // Validar fecha
            if (string.IsNullOrWhiteSpace(cfdi.Fecha))
            {
                resultado.AgregarError("CFDI004", "La fecha es obligatoria", "Fecha");
            }
            else
            {
                if (!DateTime.TryParse(cfdi.Fecha, out DateTime fechaCfdi))
                {
                    resultado.AgregarError("CFDI005", "El formato de fecha no es valido", "Fecha");
                }
                else
                {
                    // La fecha no puede ser futura
                    if (fechaCfdi > DateTime.Now.AddMinutes(5))
                    {
                        resultado.AgregarError("CFDI006", "La fecha del CFDI no puede ser futura", "Fecha");
                    }

                    // La fecha no puede ser muy antigua (más de 72 horas)
                    if (fechaCfdi < DateTime.Now.AddHours(-72))
                    {
                        resultado.AgregarError("CFDI007",
                            "La fecha del CFDI no puede ser anterior a 72 horas",
                            "Fecha");
                    }
                }
            }

            // Validar Emisor
            var validacionRFCEmisor = ValidarRFC(cfdi.EmisorRFC, esReceptor: false);
            foreach (var error in validacionRFCEmisor.Errores)
            {
                resultado.AgregarError(error.Codigo, $"Emisor: {error.Mensaje}", "EmisorRFC");
            }

            if (string.IsNullOrWhiteSpace(cfdi.EmisorNombre))
            {
                resultado.AgregarError("CFDI010", "El nombre del emisor es obligatorio", "EmisorNombre");
            }
            else if (cfdi.EmisorNombre.Length > 300)
            {
                resultado.AgregarError("CFDI011",
                    "El nombre del emisor no puede exceder 300 caracteres",
                    "EmisorNombre");
            }

            var validacionRegimenEmisor = ValidarRegimenFiscal(cfdi.EmisorRegimenFiscal, cfdi.EmisorRFC);
            foreach (var error in validacionRegimenEmisor.Errores)
            {
                resultado.AgregarError(error.Codigo, $"Emisor: {error.Mensaje}", "EmisorRegimenFiscal");
            }

            // Validar Receptor
            var validacionRFCReceptor = ValidarRFC(cfdi.ReceptorRFC, esReceptor: true);
            foreach (var error in validacionRFCReceptor.Errores)
            {
                resultado.AgregarError(error.Codigo, $"Receptor: {error.Mensaje}", "ReceptorRFC");
            }

            if (string.IsNullOrWhiteSpace(cfdi.ReceptorNombre))
            {
                resultado.AgregarError("CFDI020", "El nombre del receptor es obligatorio", "ReceptorNombre");
            }
            else if (cfdi.ReceptorNombre.Length > 300)
            {
                resultado.AgregarError("CFDI021",
                    "El nombre del receptor no puede exceder 300 caracteres",
                    "ReceptorNombre");
            }

            // En CFDI 4.0, el régimen fiscal del receptor es obligatorio (excepto para algunos casos)
            if (cfdi.ReceptorRFC != "XAXX010101000" && cfdi.ReceptorRFC != "XEXX010101000")
            {
                if (string.IsNullOrWhiteSpace(cfdi.ReceptorRegimenFiscal))
                {
                    resultado.AgregarError("CFDI022",
                        "El regimen fiscal del receptor es obligatorio en CFDI 4.0",
                        "ReceptorRegimenFiscal");
                }
                else
                {
                    var validacionRegimenReceptor = ValidarRegimenFiscal(cfdi.ReceptorRegimenFiscal, cfdi.ReceptorRFC);
                    foreach (var error in validacionRegimenReceptor.Errores)
                    {
                        resultado.AgregarError(error.Codigo, $"Receptor: {error.Mensaje}", "ReceptorRegimenFiscal");
                    }
                }
            }

            var validacionCPReceptor = ValidarCodigoPostal(cfdi.ReceptorDomicilioFiscalCP);
            foreach (var error in validacionCPReceptor.Errores)
            {
                resultado.AgregarError(error.Codigo, $"Receptor: {error.Mensaje}", "ReceptorDomicilioFiscalCP");
            }

            var validacionUsoCFDI = ValidarUsoCFDI(cfdi.ReceptorUsoCFDI, cfdi.ReceptorRFC);
            foreach (var error in validacionUsoCFDI.Errores)
            {
                resultado.AgregarError(error.Codigo, error.Mensaje, "ReceptorUsoCFDI");
            }

            // Validar Lugar de Expedición
            var validacionLugarExp = ValidarCodigoPostal(cfdi.LugarExpedicion);
            foreach (var error in validacionLugarExp.Errores)
            {
                resultado.AgregarError(error.Codigo, $"Lugar Expedicion: {error.Mensaje}", "LugarExpedicion");
            }

            // Validar Forma y Método de Pago
            var validacionPagos = ValidarFormaPagoMetodoPago(cfdi.FormaPagoClave, cfdi.MetodoPagoClave);
            foreach (var error in validacionPagos.Errores)
            {
                resultado.AgregarError(error.Codigo, error.Mensaje, error.Campo);
            }
            foreach (var adv in validacionPagos.Advertencias)
            {
                resultado.AgregarAdvertencia(adv);
            }

            // Validar Montos
            var validacionMontos = ValidarMontos(cfdi.Subtotal, cfdi.IVATrasladado, cfdi.Total, cfdi.Descuento);
            foreach (var error in validacionMontos.Errores)
            {
                resultado.AgregarError(error.Codigo, error.Mensaje, error.Campo);
            }
            foreach (var adv in validacionMontos.Advertencias)
            {
                resultado.AgregarAdvertencia(adv);
            }

            // Validar Conceptos
            if (cfdi.Conceptos == null || cfdi.Conceptos.Count == 0)
            {
                resultado.AgregarError("CFDI030", "El CFDI debe tener al menos un concepto", "Conceptos");
            }
            else
            {
                decimal sumaImportes = 0;
                decimal sumaImpuestos = 0;
                int numConcepto = 1;

                foreach (var concepto in cfdi.Conceptos)
                {
                    // Validar ClaveProdServ
                    if (string.IsNullOrWhiteSpace(concepto.ClaveProdServ))
                    {
                        resultado.AgregarError("CFDI031",
                            $"Concepto {numConcepto}: La clave de producto/servicio SAT es obligatoria",
                            "ClaveProdServ");
                    }
                    else if (!Regex.IsMatch(concepto.ClaveProdServ, @"^\d{8}$"))
                    {
                        resultado.AgregarError("CFDI032",
                            $"Concepto {numConcepto}: La clave de producto/servicio debe tener 8 digitos",
                            "ClaveProdServ");
                    }

                    // Validar ClaveUnidad
                    if (string.IsNullOrWhiteSpace(concepto.ClaveUnidad))
                    {
                        resultado.AgregarError("CFDI033",
                            $"Concepto {numConcepto}: La clave de unidad SAT es obligatoria",
                            "ClaveUnidad");
                    }

                    // Validar Descripción
                    if (string.IsNullOrWhiteSpace(concepto.Descripcion))
                    {
                        resultado.AgregarError("CFDI034",
                            $"Concepto {numConcepto}: La descripcion es obligatoria",
                            "Descripcion");
                    }
                    else if (concepto.Descripcion.Length > 1000)
                    {
                        resultado.AgregarError("CFDI035",
                            $"Concepto {numConcepto}: La descripcion no puede exceder 1000 caracteres",
                            "Descripcion");
                    }

                    // Validar Cantidad
                    if (concepto.Cantidad <= 0)
                    {
                        resultado.AgregarError("CFDI036",
                            $"Concepto {numConcepto}: La cantidad debe ser mayor a cero",
                            "Cantidad");
                    }

                    // Validar Valor Unitario
                    if (concepto.ValorUnitario < 0)
                    {
                        resultado.AgregarError("CFDI037",
                            $"Concepto {numConcepto}: El valor unitario no puede ser negativo",
                            "ValorUnitario");
                    }

                    // Validar Importe
                    decimal importeCalculado = concepto.Cantidad * concepto.ValorUnitario;
                    if (Math.Abs(concepto.Importe - importeCalculado) > 0.01m)
                    {
                        resultado.AgregarError("CFDI038",
                            $"Concepto {numConcepto}: El importe ({concepto.Importe:F2}) no coincide con Cantidad x ValorUnitario ({importeCalculado:F2})",
                            "Importe");
                    }

                    sumaImportes += concepto.Importe;
                    sumaImpuestos += concepto.ImporteImpuesto;
                    numConcepto++;
                }

                // Verificar que la suma de importes coincida con el subtotal
                if (Math.Abs(sumaImportes - cfdi.Subtotal) > 0.01m)
                {
                    resultado.AgregarError("CFDI039",
                        $"La suma de importes de conceptos ({sumaImportes:F2}) no coincide con el subtotal ({cfdi.Subtotal:F2})",
                        "Subtotal");
                }

                // Verificar que la suma de impuestos coincida con el IVA total
                if (Math.Abs(sumaImpuestos - cfdi.IVATrasladado) > 0.01m)
                {
                    resultado.AgregarError("CFDI040",
                        $"La suma de impuestos de conceptos ({sumaImpuestos:F2}) no coincide con el IVA total ({cfdi.IVATrasladado:F2})",
                        "IVATrasladado");
                }
            }

            // Agregar advertencias de catálogos
            foreach (var adv in validacionRFCEmisor.Advertencias) resultado.AgregarAdvertencia($"Emisor RFC: {adv}");
            foreach (var adv in validacionRFCReceptor.Advertencias) resultado.AgregarAdvertencia($"Receptor RFC: {adv}");
            foreach (var adv in validacionCPReceptor.Advertencias) resultado.AgregarAdvertencia($"Receptor CP: {adv}");
            foreach (var adv in validacionLugarExp.Advertencias) resultado.AgregarAdvertencia($"Lugar Expedicion: {adv}");

            return resultado;
        }

        #endregion

        #region Validación de Configuración Fiscal

        /// <summary>
        /// Valida que la configuración fiscal esté completa para emitir facturas
        /// </summary>
        public static ResultadoValidacion ValidarConfiguracionFiscal(ConfiguracionFiscal config)
        {
            var resultado = new ResultadoValidacion();

            if (config == null)
            {
                resultado.AgregarError("CONFIG001", "No hay configuracion fiscal", null);
                return resultado;
            }

            // Validar RFC
            var validacionRFC = ValidarRFC(config.RFC, esReceptor: false);
            foreach (var error in validacionRFC.Errores)
            {
                resultado.AgregarError(error.Codigo, error.Mensaje, "RFC");
            }

            // Validar Razón Social
            if (string.IsNullOrWhiteSpace(config.RazonSocial))
            {
                resultado.AgregarError("CONFIG002", "La razon social es obligatoria", "RazonSocial");
            }

            // Validar Régimen Fiscal
            if (string.IsNullOrWhiteSpace(config.RegimenFiscalClave))
            {
                resultado.AgregarError("CONFIG003", "El regimen fiscal es obligatorio", "RegimenFiscal");
            }
            else
            {
                var validacionRegimen = ValidarRegimenFiscal(config.RegimenFiscalClave, config.RFC);
                foreach (var error in validacionRegimen.Errores)
                {
                    resultado.AgregarError(error.Codigo, error.Mensaje, "RegimenFiscal");
                }
            }

            // Validar Código Postal
            var validacionCP = ValidarCodigoPostal(config.CodigoPostal);
            foreach (var error in validacionCP.Errores)
            {
                resultado.AgregarError(error.Codigo, error.Mensaje, "CodigoPostal");
            }

            // Advertencias sobre configuración PAC
            if (string.IsNullOrWhiteSpace(config.PAC) || config.PAC == "(Sin configurar)")
            {
                resultado.AgregarAdvertencia("No hay PAC configurado. No podra timbrar facturas automaticamente.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(config.PACUsuario))
                    resultado.AgregarAdvertencia("No hay usuario de PAC configurado.");

                if (string.IsNullOrWhiteSpace(config.PACContrasena))
                    resultado.AgregarAdvertencia("No hay contrasena de PAC configurada.");
            }

            // Advertencias sobre certificados
            if (string.IsNullOrWhiteSpace(config.CertificadoCSD))
            {
                resultado.AgregarAdvertencia("No hay certificado CSD configurado. Algunas operaciones pueden requerirlo.");
            }
            else if (!System.IO.File.Exists(config.CertificadoCSD))
            {
                resultado.AgregarError("CONFIG010",
                    $"El archivo de certificado no existe: {config.CertificadoCSD}",
                    "CertificadoCSD");
            }

            if (string.IsNullOrWhiteSpace(config.LlaveCSD))
            {
                resultado.AgregarAdvertencia("No hay llave privada CSD configurada. Algunas operaciones pueden requerirla.");
            }
            else if (!System.IO.File.Exists(config.LlaveCSD))
            {
                resultado.AgregarError("CONFIG011",
                    $"El archivo de llave privada no existe: {config.LlaveCSD}",
                    "LlaveCSD");
            }

            return resultado;
        }

        #endregion
    }
}
