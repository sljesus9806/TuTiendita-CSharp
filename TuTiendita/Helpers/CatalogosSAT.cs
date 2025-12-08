using System;
using System.Collections.Generic;
using System.Linq;

namespace TuTiendita.Helpers
{
    /// <summary>
    /// Catálogos oficiales del SAT para facturación electrónica CFDI 4.0
    /// </summary>
    public static class CatalogosSAT
    {
        #region Régimen Fiscal (c_RegimenFiscal)

        public class RegimenFiscal
        {
            public string Clave { get; set; }
            public string Descripcion { get; set; }
            public bool PersonaFisica { get; set; }
            public bool PersonaMoral { get; set; }

            public override string ToString() => $"{Clave} - {Descripcion}";
        }

        public static List<RegimenFiscal> RegimenesFiscales { get; } = new List<RegimenFiscal>
        {
            // Personas Morales
            new RegimenFiscal { Clave = "601", Descripcion = "General de Ley Personas Morales", PersonaFisica = false, PersonaMoral = true },
            new RegimenFiscal { Clave = "603", Descripcion = "Personas Morales con Fines no Lucrativos", PersonaFisica = false, PersonaMoral = true },

            // Personas Físicas
            new RegimenFiscal { Clave = "605", Descripcion = "Sueldos y Salarios e Ingresos Asimilados a Salarios", PersonaFisica = true, PersonaMoral = false },
            new RegimenFiscal { Clave = "606", Descripcion = "Arrendamiento", PersonaFisica = true, PersonaMoral = false },
            new RegimenFiscal { Clave = "607", Descripcion = "Régimen de Enajenación o Adquisición de Bienes", PersonaFisica = true, PersonaMoral = false },
            new RegimenFiscal { Clave = "608", Descripcion = "Demás ingresos", PersonaFisica = true, PersonaMoral = false },
            new RegimenFiscal { Clave = "610", Descripcion = "Residentes en el Extranjero sin Establecimiento Permanente en México", PersonaFisica = true, PersonaMoral = true },
            new RegimenFiscal { Clave = "611", Descripcion = "Ingresos por Dividendos (socios y accionistas)", PersonaFisica = true, PersonaMoral = false },
            new RegimenFiscal { Clave = "612", Descripcion = "Personas Físicas con Actividades Empresariales y Profesionales", PersonaFisica = true, PersonaMoral = false },
            new RegimenFiscal { Clave = "614", Descripcion = "Ingresos por intereses", PersonaFisica = true, PersonaMoral = false },
            new RegimenFiscal { Clave = "615", Descripcion = "Régimen de los ingresos por obtención de premios", PersonaFisica = true, PersonaMoral = false },
            new RegimenFiscal { Clave = "616", Descripcion = "Sin obligaciones fiscales", PersonaFisica = true, PersonaMoral = false },

            // Ambos
            new RegimenFiscal { Clave = "620", Descripcion = "Sociedades Cooperativas de Producción que optan por diferir sus ingresos", PersonaFisica = false, PersonaMoral = true },
            new RegimenFiscal { Clave = "621", Descripcion = "Incorporación Fiscal", PersonaFisica = true, PersonaMoral = false },
            new RegimenFiscal { Clave = "622", Descripcion = "Actividades Agrícolas, Ganaderas, Silvícolas y Pesqueras", PersonaFisica = false, PersonaMoral = true },
            new RegimenFiscal { Clave = "623", Descripcion = "Opcional para Grupos de Sociedades", PersonaFisica = false, PersonaMoral = true },
            new RegimenFiscal { Clave = "624", Descripcion = "Coordinados", PersonaFisica = false, PersonaMoral = true },
            new RegimenFiscal { Clave = "625", Descripcion = "Régimen de las Actividades Empresariales con ingresos a través de Plataformas Tecnológicas", PersonaFisica = true, PersonaMoral = false },
            new RegimenFiscal { Clave = "626", Descripcion = "Régimen Simplificado de Confianza", PersonaFisica = true, PersonaMoral = true },
        };

        public static List<RegimenFiscal> GetRegimenesPersonaFisica() =>
            RegimenesFiscales.Where(r => r.PersonaFisica).ToList();

        public static List<RegimenFiscal> GetRegimenesPersonaMoral() =>
            RegimenesFiscales.Where(r => r.PersonaMoral).ToList();

        #endregion

        #region Uso CFDI (c_UsoCFDI)

        public class UsoCFDI
        {
            public string Clave { get; set; }
            public string Descripcion { get; set; }
            public bool PersonaFisica { get; set; }
            public bool PersonaMoral { get; set; }
            public string RegimenesAplicables { get; set; } // Regímenes donde aplica

            public override string ToString() => $"{Clave} - {Descripcion}";
        }

        public static List<UsoCFDI> UsosCFDI { get; } = new List<UsoCFDI>
        {
            // Adquisición de mercancías
            new UsoCFDI { Clave = "G01", Descripcion = "Adquisición de mercancías", PersonaFisica = true, PersonaMoral = true },
            new UsoCFDI { Clave = "G02", Descripcion = "Devoluciones, descuentos o bonificaciones", PersonaFisica = true, PersonaMoral = true },
            new UsoCFDI { Clave = "G03", Descripcion = "Gastos en general", PersonaFisica = true, PersonaMoral = true },

            // Construcciones e inversiones
            new UsoCFDI { Clave = "I01", Descripcion = "Construcciones", PersonaFisica = true, PersonaMoral = true },
            new UsoCFDI { Clave = "I02", Descripcion = "Mobiliario y equipo de oficina por inversiones", PersonaFisica = true, PersonaMoral = true },
            new UsoCFDI { Clave = "I03", Descripcion = "Equipo de transporte", PersonaFisica = true, PersonaMoral = true },
            new UsoCFDI { Clave = "I04", Descripcion = "Equipo de cómputo y accesorios", PersonaFisica = true, PersonaMoral = true },
            new UsoCFDI { Clave = "I05", Descripcion = "Dados, troqueles, moldes, matrices y herramental", PersonaFisica = true, PersonaMoral = true },
            new UsoCFDI { Clave = "I06", Descripcion = "Comunicaciones telefónicas", PersonaFisica = true, PersonaMoral = true },
            new UsoCFDI { Clave = "I07", Descripcion = "Comunicaciones satelitales", PersonaFisica = true, PersonaMoral = true },
            new UsoCFDI { Clave = "I08", Descripcion = "Otra maquinaria y equipo", PersonaFisica = true, PersonaMoral = true },

            // Deducciones personales (solo persona física)
            new UsoCFDI { Clave = "D01", Descripcion = "Honorarios médicos, dentales y gastos hospitalarios", PersonaFisica = true, PersonaMoral = false },
            new UsoCFDI { Clave = "D02", Descripcion = "Gastos médicos por incapacidad o discapacidad", PersonaFisica = true, PersonaMoral = false },
            new UsoCFDI { Clave = "D03", Descripcion = "Gastos funerales", PersonaFisica = true, PersonaMoral = false },
            new UsoCFDI { Clave = "D04", Descripcion = "Donativos", PersonaFisica = true, PersonaMoral = false },
            new UsoCFDI { Clave = "D05", Descripcion = "Intereses reales efectivamente pagados por créditos hipotecarios (casa habitación)", PersonaFisica = true, PersonaMoral = false },
            new UsoCFDI { Clave = "D06", Descripcion = "Aportaciones voluntarias al SAR", PersonaFisica = true, PersonaMoral = false },
            new UsoCFDI { Clave = "D07", Descripcion = "Primas por seguros de gastos médicos", PersonaFisica = true, PersonaMoral = false },
            new UsoCFDI { Clave = "D08", Descripcion = "Gastos de transportación escolar obligatoria", PersonaFisica = true, PersonaMoral = false },
            new UsoCFDI { Clave = "D09", Descripcion = "Depósitos en cuentas para el ahorro, primas que tengan como base planes de pensiones", PersonaFisica = true, PersonaMoral = false },
            new UsoCFDI { Clave = "D10", Descripcion = "Pagos por servicios educativos (colegiaturas)", PersonaFisica = true, PersonaMoral = false },

            // Sin efectos fiscales
            new UsoCFDI { Clave = "S01", Descripcion = "Sin efectos fiscales", PersonaFisica = true, PersonaMoral = true },

            // Pagos (complemento de recepción de pagos)
            new UsoCFDI { Clave = "CP01", Descripcion = "Pagos", PersonaFisica = true, PersonaMoral = true },

            // Nómina
            new UsoCFDI { Clave = "CN01", Descripcion = "Nómina", PersonaFisica = true, PersonaMoral = false },
        };

        public static List<UsoCFDI> GetUsosPersonaFisica() =>
            UsosCFDI.Where(u => u.PersonaFisica).ToList();

        public static List<UsoCFDI> GetUsosPersonaMoral() =>
            UsosCFDI.Where(u => u.PersonaMoral).ToList();

        #endregion

        #region Forma de Pago (c_FormaPago)

        public class FormaPago
        {
            public string Clave { get; set; }
            public string Descripcion { get; set; }
            public string Bancarizado { get; set; } // Sí, No, Opcional

            public override string ToString() => $"{Clave} - {Descripcion}";
        }

        public static List<FormaPago> FormasPago { get; } = new List<FormaPago>
        {
            new FormaPago { Clave = "01", Descripcion = "Efectivo", Bancarizado = "No" },
            new FormaPago { Clave = "02", Descripcion = "Cheque nominativo", Bancarizado = "Sí" },
            new FormaPago { Clave = "03", Descripcion = "Transferencia electrónica de fondos", Bancarizado = "Sí" },
            new FormaPago { Clave = "04", Descripcion = "Tarjeta de crédito", Bancarizado = "Sí" },
            new FormaPago { Clave = "05", Descripcion = "Monedero electrónico", Bancarizado = "Sí" },
            new FormaPago { Clave = "06", Descripcion = "Dinero electrónico", Bancarizado = "Sí" },
            new FormaPago { Clave = "08", Descripcion = "Vales de despensa", Bancarizado = "No" },
            new FormaPago { Clave = "12", Descripcion = "Dación en pago", Bancarizado = "No" },
            new FormaPago { Clave = "13", Descripcion = "Pago por subrogación", Bancarizado = "No" },
            new FormaPago { Clave = "14", Descripcion = "Pago por consignación", Bancarizado = "No" },
            new FormaPago { Clave = "15", Descripcion = "Condonación", Bancarizado = "No" },
            new FormaPago { Clave = "17", Descripcion = "Compensación", Bancarizado = "No" },
            new FormaPago { Clave = "23", Descripcion = "Novación", Bancarizado = "No" },
            new FormaPago { Clave = "24", Descripcion = "Confusión", Bancarizado = "No" },
            new FormaPago { Clave = "25", Descripcion = "Remisión de deuda", Bancarizado = "No" },
            new FormaPago { Clave = "26", Descripcion = "Prescripción o caducidad", Bancarizado = "No" },
            new FormaPago { Clave = "27", Descripcion = "A satisfacción del acreedor", Bancarizado = "No" },
            new FormaPago { Clave = "28", Descripcion = "Tarjeta de débito", Bancarizado = "Sí" },
            new FormaPago { Clave = "29", Descripcion = "Tarjeta de servicios", Bancarizado = "Sí" },
            new FormaPago { Clave = "30", Descripcion = "Aplicación de anticipos", Bancarizado = "No" },
            new FormaPago { Clave = "31", Descripcion = "Intermediario pagos", Bancarizado = "Sí" },
            new FormaPago { Clave = "99", Descripcion = "Por definir", Bancarizado = "No" }, // Solo para PPD
        };

        // Formas de pago más comunes para comercio
        public static List<FormaPago> GetFormasPagoComunes() =>
            FormasPago.Where(f => new[] { "01", "02", "03", "04", "28", "99" }.Contains(f.Clave)).ToList();

        #endregion

        #region Método de Pago (c_MetodoPago)

        public class MetodoPago
        {
            public string Clave { get; set; }
            public string Descripcion { get; set; }

            public override string ToString() => $"{Clave} - {Descripcion}";
        }

        public static List<MetodoPago> MetodosPago { get; } = new List<MetodoPago>
        {
            new MetodoPago { Clave = "PUE", Descripcion = "Pago en una sola exhibición" },
            new MetodoPago { Clave = "PPD", Descripcion = "Pago en parcialidades o diferido" },
        };

        #endregion

        #region Tipo de Comprobante (c_TipoDeComprobante)

        public class TipoComprobante
        {
            public string Clave { get; set; }
            public string Descripcion { get; set; }

            public override string ToString() => $"{Clave} - {Descripcion}";
        }

        public static List<TipoComprobante> TiposComprobante { get; } = new List<TipoComprobante>
        {
            new TipoComprobante { Clave = "I", Descripcion = "Ingreso" },
            new TipoComprobante { Clave = "E", Descripcion = "Egreso" },
            new TipoComprobante { Clave = "T", Descripcion = "Traslado" },
            new TipoComprobante { Clave = "N", Descripcion = "Nómina" },
            new TipoComprobante { Clave = "P", Descripcion = "Pago" },
        };

        #endregion

        #region Objeto del Impuesto (c_ObjetoImp) - Nuevo en CFDI 4.0

        public class ObjetoImpuesto
        {
            public string Clave { get; set; }
            public string Descripcion { get; set; }

            public override string ToString() => $"{Clave} - {Descripcion}";
        }

        public static List<ObjetoImpuesto> ObjetosImpuesto { get; } = new List<ObjetoImpuesto>
        {
            new ObjetoImpuesto { Clave = "01", Descripcion = "No objeto de impuesto" },
            new ObjetoImpuesto { Clave = "02", Descripcion = "Sí objeto de impuesto" },
            new ObjetoImpuesto { Clave = "03", Descripcion = "Sí objeto del impuesto y no obligado al desglose" },
            new ObjetoImpuesto { Clave = "04", Descripcion = "Sí objeto del impuesto y no causa impuesto" },
        };

        #endregion

        #region Impuestos (c_Impuesto)

        public class Impuesto
        {
            public string Clave { get; set; }
            public string Descripcion { get; set; }
            public bool Retencion { get; set; }
            public bool Traslado { get; set; }

            public override string ToString() => $"{Clave} - {Descripcion}";
        }

        public static List<Impuesto> Impuestos { get; } = new List<Impuesto>
        {
            new Impuesto { Clave = "001", Descripcion = "ISR", Retencion = true, Traslado = false },
            new Impuesto { Clave = "002", Descripcion = "IVA", Retencion = true, Traslado = true },
            new Impuesto { Clave = "003", Descripcion = "IEPS", Retencion = false, Traslado = true },
        };

        #endregion

        #region Tasas de IVA

        public class TasaIVA
        {
            public decimal Tasa { get; set; }
            public string Descripcion { get; set; }
            public string TipoFactor { get; set; } // Tasa, Cuota, Exento

            public override string ToString() => Descripcion;
        }

        public static List<TasaIVA> TasasIVA { get; } = new List<TasaIVA>
        {
            new TasaIVA { Tasa = 0.16m, Descripcion = "IVA 16%", TipoFactor = "Tasa" },
            new TasaIVA { Tasa = 0.08m, Descripcion = "IVA 8% (Frontera)", TipoFactor = "Tasa" },
            new TasaIVA { Tasa = 0.00m, Descripcion = "IVA 0%", TipoFactor = "Tasa" },
            new TasaIVA { Tasa = 0.00m, Descripcion = "Exento", TipoFactor = "Exento" },
        };

        #endregion

        #region Exportación (c_Exportacion) - Nuevo en CFDI 4.0

        public class Exportacion
        {
            public string Clave { get; set; }
            public string Descripcion { get; set; }

            public override string ToString() => $"{Clave} - {Descripcion}";
        }

        public static List<Exportacion> Exportaciones { get; } = new List<Exportacion>
        {
            new Exportacion { Clave = "01", Descripcion = "No aplica" },
            new Exportacion { Clave = "02", Descripcion = "Definitiva" },
            new Exportacion { Clave = "03", Descripcion = "Temporal" },
            new Exportacion { Clave = "04", Descripcion = "Definitiva con clave distinta a A1 o con complemento CCE" },
        };

        #endregion

        #region Moneda (c_Moneda) - Más comunes

        public class Moneda
        {
            public string Clave { get; set; }
            public string Descripcion { get; set; }
            public int Decimales { get; set; }

            public override string ToString() => $"{Clave} - {Descripcion}";
        }

        public static List<Moneda> Monedas { get; } = new List<Moneda>
        {
            new Moneda { Clave = "MXN", Descripcion = "Peso Mexicano", Decimales = 2 },
            new Moneda { Clave = "USD", Descripcion = "Dolar americano", Decimales = 2 },
            new Moneda { Clave = "EUR", Descripcion = "Euro", Decimales = 2 },
            new Moneda { Clave = "XXX", Descripcion = "Los códigos asignados para transacciones en que intervenga ninguna moneda", Decimales = 0 },
        };

        #endregion

        #region Motivo de Cancelación (c_MotivoCancelacion)

        public class MotivoCancelacion
        {
            public string Clave { get; set; }
            public string Descripcion { get; set; }
            public bool RequiereFolioSustituto { get; set; }

            public override string ToString() => $"{Clave} - {Descripcion}";
        }

        public static List<MotivoCancelacion> MotivosCancelacion { get; } = new List<MotivoCancelacion>
        {
            new MotivoCancelacion { Clave = "01", Descripcion = "Comprobante emitido con errores con relación", RequiereFolioSustituto = true },
            new MotivoCancelacion { Clave = "02", Descripcion = "Comprobante emitido con errores sin relación", RequiereFolioSustituto = false },
            new MotivoCancelacion { Clave = "03", Descripcion = "No se llevó a cabo la operación", RequiereFolioSustituto = false },
            new MotivoCancelacion { Clave = "04", Descripcion = "Operación nominativa relacionada en una factura global", RequiereFolioSustituto = false },
        };

        #endregion

        #region Claves de Productos y Servicios más comunes para tiendas

        public class ClaveProductoServicio
        {
            public string Clave { get; set; }
            public string Descripcion { get; set; }
            public string Categoria { get; set; }

            public override string ToString() => $"{Clave} - {Descripcion}";
        }

        public static List<ClaveProductoServicio> ClavesProductosComunes { get; } = new List<ClaveProductoServicio>
        {
            // Alimentos y bebidas
            new ClaveProductoServicio { Clave = "50000000", Descripcion = "Alimentos, bebidas y tabaco", Categoria = "General" },
            new ClaveProductoServicio { Clave = "50101500", Descripcion = "Frutas frescas", Categoria = "Alimentos" },
            new ClaveProductoServicio { Clave = "50101600", Descripcion = "Vegetales frescos", Categoria = "Alimentos" },
            new ClaveProductoServicio { Clave = "50111500", Descripcion = "Carne de res", Categoria = "Alimentos" },
            new ClaveProductoServicio { Clave = "50112000", Descripcion = "Carne de cerdo", Categoria = "Alimentos" },
            new ClaveProductoServicio { Clave = "50131700", Descripcion = "Leche y productos lácteos", Categoria = "Lácteos" },
            new ClaveProductoServicio { Clave = "50151500", Descripcion = "Chocolates y dulces", Categoria = "Dulces" },
            new ClaveProductoServicio { Clave = "50161800", Descripcion = "Pan y productos de panadería", Categoria = "Panadería" },
            new ClaveProductoServicio { Clave = "50181700", Descripcion = "Sopas y caldos enlatados", Categoria = "Abarrotes" },
            new ClaveProductoServicio { Clave = "50192100", Descripcion = "Bebidas no alcohólicas", Categoria = "Bebidas" },
            new ClaveProductoServicio { Clave = "50201700", Descripcion = "Cerveza", Categoria = "Bebidas" },
            new ClaveProductoServicio { Clave = "50202300", Descripcion = "Vino", Categoria = "Bebidas" },

            // Productos de limpieza
            new ClaveProductoServicio { Clave = "47131700", Descripcion = "Detergentes para ropa", Categoria = "Limpieza" },
            new ClaveProductoServicio { Clave = "47131800", Descripcion = "Productos de limpieza del hogar", Categoria = "Limpieza" },
            new ClaveProductoServicio { Clave = "53131600", Descripcion = "Jabones y productos de baño", Categoria = "Higiene" },

            // Productos de uso general
            new ClaveProductoServicio { Clave = "44121600", Descripcion = "Papelería y artículos de oficina", Categoria = "Papelería" },
            new ClaveProductoServicio { Clave = "52151500", Descripcion = "Artículos de cocina", Categoria = "Hogar" },

            // Clave genérica para productos no especificados
            new ClaveProductoServicio { Clave = "01010101", Descripcion = "No existe en el catálogo", Categoria = "General" },
        };

        #endregion

        #region Unidades de Medida comunes (c_ClaveUnidad)

        public class UnidadMedida
        {
            public string Clave { get; set; }
            public string Nombre { get; set; }
            public string Descripcion { get; set; }

            public override string ToString() => $"{Clave} - {Nombre}";
        }

        public static List<UnidadMedida> UnidadesMedida { get; } = new List<UnidadMedida>
        {
            new UnidadMedida { Clave = "H87", Nombre = "Pieza", Descripcion = "Unidad" },
            new UnidadMedida { Clave = "EA", Nombre = "Elemento", Descripcion = "Cada uno" },
            new UnidadMedida { Clave = "E48", Nombre = "Servicio", Descripcion = "Unidad de servicio" },
            new UnidadMedida { Clave = "ACT", Nombre = "Actividad", Descripcion = "Actividad" },
            new UnidadMedida { Clave = "KGM", Nombre = "Kilogramo", Descripcion = "Kilogramo" },
            new UnidadMedida { Clave = "GRM", Nombre = "Gramo", Descripcion = "Gramo" },
            new UnidadMedida { Clave = "LTR", Nombre = "Litro", Descripcion = "Litro" },
            new UnidadMedida { Clave = "MLT", Nombre = "Mililitro", Descripcion = "Mililitro" },
            new UnidadMedida { Clave = "MTR", Nombre = "Metro", Descripcion = "Metro" },
            new UnidadMedida { Clave = "MTK", Nombre = "Metro cuadrado", Descripcion = "Metro cuadrado" },
            new UnidadMedida { Clave = "MTQ", Nombre = "Metro cúbico", Descripcion = "Metro cúbico" },
            new UnidadMedida { Clave = "XBX", Nombre = "Caja", Descripcion = "Caja" },
            new UnidadMedida { Clave = "XPK", Nombre = "Paquete", Descripcion = "Paquete" },
            new UnidadMedida { Clave = "SET", Nombre = "Juego", Descripcion = "Conjunto" },
            new UnidadMedida { Clave = "DZN", Nombre = "Docena", Descripcion = "Docena" },
            new UnidadMedida { Clave = "PR", Nombre = "Par", Descripcion = "Par" },
        };

        #endregion

        #region Métodos de ayuda

        public static RegimenFiscal GetRegimenFiscalPorClave(string clave) =>
            RegimenesFiscales.FirstOrDefault(r => r.Clave == clave);

        public static UsoCFDI GetUsoCFDIPorClave(string clave) =>
            UsosCFDI.FirstOrDefault(u => u.Clave == clave);

        public static FormaPago GetFormaPagoPorClave(string clave) =>
            FormasPago.FirstOrDefault(f => f.Clave == clave);

        public static MetodoPago GetMetodoPagoPorClave(string clave) =>
            MetodosPago.FirstOrDefault(m => m.Clave == clave);

        public static ClaveProductoServicio GetClaveProductoPorClave(string clave) =>
            ClavesProductosComunes.FirstOrDefault(p => p.Clave == clave);

        public static UnidadMedida GetUnidadMedidaPorClave(string clave) =>
            UnidadesMedida.FirstOrDefault(u => u.Clave == clave);

        /// <summary>
        /// Valida si un RFC tiene formato válido
        /// </summary>
        public static bool ValidarFormatoRFC(string rfc)
        {
            if (string.IsNullOrWhiteSpace(rfc))
                return false;

            rfc = rfc.ToUpper().Trim();

            // RFC de persona física: 13 caracteres
            // RFC de persona moral: 12 caracteres
            if (rfc.Length != 12 && rfc.Length != 13)
                return false;

            // Validación básica con regex
            string patronPersonaFisica = @"^[A-ZÑ&]{4}\d{6}[A-Z0-9]{3}$";
            string patronPersonaMoral = @"^[A-ZÑ&]{3}\d{6}[A-Z0-9]{3}$";

            return System.Text.RegularExpressions.Regex.IsMatch(rfc, patronPersonaFisica) ||
                   System.Text.RegularExpressions.Regex.IsMatch(rfc, patronPersonaMoral);
        }

        /// <summary>
        /// Determina si el RFC corresponde a persona física o moral
        /// </summary>
        public static bool EsPersonaFisica(string rfc)
        {
            if (string.IsNullOrWhiteSpace(rfc))
                return false;
            return rfc.Trim().Length == 13;
        }

        /// <summary>
        /// RFC genérico para público en general (ventas globales)
        /// </summary>
        public static string RFCPublicoGeneral => "XAXX010101000";

        /// <summary>
        /// RFC para extranjeros
        /// </summary>
        public static string RFCExtranjero => "XEXX010101000";

        #endregion
    }
}
