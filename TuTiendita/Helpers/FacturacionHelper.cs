using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace TuTiendita.Helpers
{
    #region Modelos de Datos para Facturación

    /// <summary>
    /// Configuración fiscal del emisor (empresa)
    /// </summary>
    public class ConfiguracionFiscal : INotifyPropertyChanged
    {
        public int Id { get; set; } = 1;
        public string RFC { get; set; }
        public string RazonSocial { get; set; }
        public string RegimenFiscalClave { get; set; }
        public string CodigoPostal { get; set; }
        public string Calle { get; set; }
        public string NumeroExterior { get; set; }
        public string NumeroInterior { get; set; }
        public string Colonia { get; set; }
        public string Municipio { get; set; }
        public string Estado { get; set; }
        public string Pais { get; set; } = "MEX";

        // Certificados y PAC
        public string CertificadoCSD { get; set; } // Ruta al certificado .cer
        public string LlaveCSD { get; set; } // Ruta a la llave .key
        public string ContrasenaLlaveCSD { get; set; }
        public string PAC { get; set; } // Nombre del PAC
        public string PACUsuario { get; set; }
        public string PACContrasena { get; set; }
        public bool PACModoProduccion { get; set; }

        // Configuración de folios
        public string LugarExpedicion { get; set; }
        public string SerieFactura { get; set; } = "A";
        public int UltimoFolio { get; set; }
        public byte[] LogoEmpresa { get; set; }
        public bool Activo { get; set; } = true;
        public string FechaActualizacion { get; set; }

        // Propiedades calculadas
        public string RegimenFiscalDescripcion =>
            CatalogosSAT.GetRegimenFiscalPorClave(RegimenFiscalClave)?.Descripcion ?? "";

        public string DireccionCompleta =>
            $"{Calle} {NumeroExterior} {NumeroInterior}, {Colonia}, {Municipio}, {Estado}, CP {CodigoPostal}".Trim();

        public bool ConfiguracionCompleta =>
            !string.IsNullOrWhiteSpace(RFC) &&
            !string.IsNullOrWhiteSpace(RazonSocial) &&
            !string.IsNullOrWhiteSpace(RegimenFiscalClave) &&
            !string.IsNullOrWhiteSpace(CodigoPostal);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Modelo de Comprobante Fiscal Digital por Internet (CFDI)
    /// </summary>
    public class CFDI : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string UUID { get; set; } // Folio fiscal (asignado por el SAT)
        public string Serie { get; set; }
        public string Folio { get; set; }
        public string Fecha { get; set; }
        public string FormaPagoClave { get; set; }
        public string MetodoPagoClave { get; set; }
        public string TipoComprobante { get; set; } = "I"; // I=Ingreso, E=Egreso, T=Traslado, N=Nómina, P=Pago
        public string Exportacion { get; set; } = "01"; // 01=No aplica
        public string Moneda { get; set; } = "MXN";
        public decimal TipoCambio { get; set; } = 1;
        public string LugarExpedicion { get; set; }

        // Montos
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal IVATrasladado { get; set; }
        public decimal IVARetenido { get; set; }
        public decimal ISRRetenido { get; set; }
        public decimal Total { get; set; }

        // Emisor
        public string EmisorRFC { get; set; }
        public string EmisorNombre { get; set; }
        public string EmisorRegimenFiscal { get; set; }

        // Receptor
        public string ReceptorRFC { get; set; }
        public string ReceptorNombre { get; set; }
        public string ReceptorRegimenFiscal { get; set; }
        public string ReceptorDomicilioFiscalCP { get; set; }
        public string ReceptorUsoCFDI { get; set; }

        // Referencias
        public int? VentaId { get; set; }
        public int? ClienteId { get; set; }
        public int? UsuarioId { get; set; }

        // Timbrado
        public string CadenaOriginal { get; set; }
        public string SelloDigitalCFDI { get; set; }
        public string SelloSAT { get; set; }
        public string NoCertificadoEmisor { get; set; }
        public string NoCertificadoSAT { get; set; }
        public string FechaTimbrado { get; set; }
        public string XMLOriginal { get; set; }
        public string XMLTimbrado { get; set; }

        // Estado
        private string _estado = "Pendiente";
        public string Estado
        {
            get => _estado;
            set { _estado = value; OnPropertyChanged(nameof(Estado)); OnPropertyChanged(nameof(EstadoColor)); }
        }
        public string MotivoCancelacion { get; set; }
        public string FechaCancelacion { get; set; }
        public string UUIDSustituto { get; set; }
        public string Notas { get; set; }
        public string FechaCreacion { get; set; }

        // Conceptos/Detalles
        public List<DetalleCFDI> Conceptos { get; set; } = new List<DetalleCFDI>();

        // Propiedades calculadas para UI
        public string FolioCompleto => $"{Serie}{Folio}";
        public string FormaPagoDescripcion =>
            CatalogosSAT.GetFormaPagoPorClave(FormaPagoClave)?.Descripcion ?? FormaPagoClave;
        public string MetodoPagoDescripcion =>
            CatalogosSAT.GetMetodoPagoPorClave(MetodoPagoClave)?.Descripcion ?? MetodoPagoClave;
        public string UsoCFDIDescripcion =>
            CatalogosSAT.GetUsoCFDIPorClave(ReceptorUsoCFDI)?.Descripcion ?? ReceptorUsoCFDI;
        public string TipoComprobanteDescripcion
        {
            get
            {
                return TipoComprobante switch
                {
                    "I" => "Ingreso",
                    "E" => "Egreso",
                    "T" => "Traslado",
                    "N" => "Nómina",
                    "P" => "Pago",
                    _ => TipoComprobante
                };
            }
        }

        public string EstadoColor
        {
            get
            {
                return Estado switch
                {
                    "Timbrado" => "#4CAF50", // Verde
                    "Pendiente" => "#FF9800", // Naranja
                    "Cancelado" => "#F44336", // Rojo
                    "Error" => "#E91E63", // Rosa/Magenta
                    _ => "#9E9E9E" // Gris
                };
            }
        }

        public string TotalFormateado => Total.ToString("C");
        public string SubtotalFormateado => Subtotal.ToString("C");
        public string IVAFormateado => IVATrasladado.ToString("C");

        public bool PuedeCancelarse => Estado == "Timbrado" && !string.IsNullOrEmpty(UUID);
        public bool EstaTimbrado => Estado == "Timbrado" && !string.IsNullOrEmpty(UUID);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Detalle/Concepto de un CFDI
    /// </summary>
    public class DetalleCFDI : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public int CFDIId { get; set; }
        public string ClaveProdServ { get; set; } = "01010101";
        public string NoIdentificacion { get; set; } // Código del producto
        public string ClaveUnidad { get; set; } = "H87"; // Pieza por defecto
        public string Unidad { get; set; } = "Pieza";
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal Importe { get; set; }
        public decimal Descuento { get; set; }
        public string ObjetoImpClave { get; set; } = "02"; // Sí objeto de impuesto
        public string ImpuestoTrasladado { get; set; } = "002"; // IVA
        public decimal TasaOCuota { get; set; } = 0.16m;
        public string TipoFactor { get; set; } = "Tasa";
        public decimal ImporteImpuesto { get; set; }

        // Calculadas
        public decimal ImporteConImpuesto => Importe + ImporteImpuesto - Descuento;
        public string ClaveProdServDescripcion =>
            CatalogosSAT.GetClaveProductoPorClave(ClaveProdServ)?.Descripcion ?? ClaveProdServ;
        public string ClaveUnidadDescripcion =>
            CatalogosSAT.GetUnidadMedidaPorClave(ClaveUnidad)?.Nombre ?? ClaveUnidad;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    #endregion

    /// <summary>
    /// Helper para operaciones de facturación electrónica CFDI 4.0
    /// </summary>
    public static class FacturacionHelper
    {
        #region Configuración Fiscal

        public static ConfiguracionFiscal ObtenerConfiguracionFiscal()
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                string query = "SELECT * FROM ConfiguracionFiscal WHERE Id = 1";
                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new ConfiguracionFiscal
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            RFC = reader["RFC"]?.ToString(),
                            RazonSocial = reader["RazonSocial"]?.ToString(),
                            RegimenFiscalClave = reader["RegimenFiscalClave"]?.ToString(),
                            CodigoPostal = reader["CodigoPostal"]?.ToString(),
                            Calle = reader["Calle"]?.ToString(),
                            NumeroExterior = reader["NumeroExterior"]?.ToString(),
                            NumeroInterior = reader["NumeroInterior"]?.ToString(),
                            Colonia = reader["Colonia"]?.ToString(),
                            Municipio = reader["Municipio"]?.ToString(),
                            Estado = reader["Estado"]?.ToString(),
                            Pais = reader["Pais"]?.ToString() ?? "MEX",
                            CertificadoCSD = reader["CertificadoCSD"]?.ToString(),
                            LlaveCSD = reader["LlaveCSD"]?.ToString(),
                            ContrasenaLlaveCSD = reader["ContrasenaLlaveCSD"]?.ToString(),
                            PAC = reader["PAC"]?.ToString(),
                            PACUsuario = reader["PACUsuario"]?.ToString(),
                            PACContrasena = reader["PACContrasena"]?.ToString(),
                            PACModoProduccion = Convert.ToInt32(reader["PACModoProduccion"] ?? 0) == 1,
                            LugarExpedicion = reader["LugarExpedicion"]?.ToString(),
                            SerieFactura = reader["SerieFactura"]?.ToString() ?? "A",
                            UltimoFolio = Convert.ToInt32(reader["UltimoFolio"] ?? 0),
                            LogoEmpresa = reader["LogoEmpresa"] as byte[],
                            Activo = Convert.ToInt32(reader["Activo"] ?? 1) == 1,
                            FechaActualizacion = reader["FechaActualizacion"]?.ToString()
                        };
                    }
                }
            }
            return null;
        }

        public static void GuardarConfiguracionFiscal(ConfiguracionFiscal config)
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();

                // Check if exists
                string checkQuery = "SELECT COUNT(*) FROM ConfiguracionFiscal WHERE Id = 1";
                bool exists = false;
                using (var cmd = new SQLiteCommand(checkQuery, conn))
                {
                    exists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }

                string query;
                if (exists)
                {
                    query = @"UPDATE ConfiguracionFiscal SET
                        RFC = @RFC,
                        RazonSocial = @RazonSocial,
                        RegimenFiscalClave = @RegimenFiscalClave,
                        CodigoPostal = @CodigoPostal,
                        Calle = @Calle,
                        NumeroExterior = @NumeroExterior,
                        NumeroInterior = @NumeroInterior,
                        Colonia = @Colonia,
                        Municipio = @Municipio,
                        Estado = @Estado,
                        Pais = @Pais,
                        CertificadoCSD = @CertificadoCSD,
                        LlaveCSD = @LlaveCSD,
                        ContrasenaLlaveCSD = @ContrasenaLlaveCSD,
                        PAC = @PAC,
                        PACUsuario = @PACUsuario,
                        PACContrasena = @PACContrasena,
                        PACModoProduccion = @PACModoProduccion,
                        LugarExpedicion = @LugarExpedicion,
                        SerieFactura = @SerieFactura,
                        UltimoFolio = @UltimoFolio,
                        LogoEmpresa = @LogoEmpresa,
                        Activo = @Activo,
                        FechaActualizacion = @FechaActualizacion
                        WHERE Id = 1";
                }
                else
                {
                    query = @"INSERT INTO ConfiguracionFiscal
                        (Id, RFC, RazonSocial, RegimenFiscalClave, CodigoPostal, Calle, NumeroExterior,
                         NumeroInterior, Colonia, Municipio, Estado, Pais, CertificadoCSD, LlaveCSD,
                         ContrasenaLlaveCSD, PAC, PACUsuario, PACContrasena, PACModoProduccion,
                         LugarExpedicion, SerieFactura, UltimoFolio, LogoEmpresa, Activo, FechaActualizacion)
                        VALUES (1, @RFC, @RazonSocial, @RegimenFiscalClave, @CodigoPostal, @Calle, @NumeroExterior,
                         @NumeroInterior, @Colonia, @Municipio, @Estado, @Pais, @CertificadoCSD, @LlaveCSD,
                         @ContrasenaLlaveCSD, @PAC, @PACUsuario, @PACContrasena, @PACModoProduccion,
                         @LugarExpedicion, @SerieFactura, @UltimoFolio, @LogoEmpresa, @Activo, @FechaActualizacion)";
                }

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@RFC", config.RFC ?? "");
                    cmd.Parameters.AddWithValue("@RazonSocial", config.RazonSocial ?? "");
                    cmd.Parameters.AddWithValue("@RegimenFiscalClave", config.RegimenFiscalClave ?? "");
                    cmd.Parameters.AddWithValue("@CodigoPostal", config.CodigoPostal ?? "");
                    cmd.Parameters.AddWithValue("@Calle", config.Calle ?? "");
                    cmd.Parameters.AddWithValue("@NumeroExterior", config.NumeroExterior ?? "");
                    cmd.Parameters.AddWithValue("@NumeroInterior", config.NumeroInterior ?? "");
                    cmd.Parameters.AddWithValue("@Colonia", config.Colonia ?? "");
                    cmd.Parameters.AddWithValue("@Municipio", config.Municipio ?? "");
                    cmd.Parameters.AddWithValue("@Estado", config.Estado ?? "");
                    cmd.Parameters.AddWithValue("@Pais", config.Pais ?? "MEX");
                    cmd.Parameters.AddWithValue("@CertificadoCSD", config.CertificadoCSD ?? "");
                    cmd.Parameters.AddWithValue("@LlaveCSD", config.LlaveCSD ?? "");
                    cmd.Parameters.AddWithValue("@ContrasenaLlaveCSD", config.ContrasenaLlaveCSD ?? "");
                    cmd.Parameters.AddWithValue("@PAC", config.PAC ?? "");
                    cmd.Parameters.AddWithValue("@PACUsuario", config.PACUsuario ?? "");
                    cmd.Parameters.AddWithValue("@PACContrasena", config.PACContrasena ?? "");
                    cmd.Parameters.AddWithValue("@PACModoProduccion", config.PACModoProduccion ? 1 : 0);
                    cmd.Parameters.AddWithValue("@LugarExpedicion", config.LugarExpedicion ?? config.CodigoPostal ?? "");
                    cmd.Parameters.AddWithValue("@SerieFactura", config.SerieFactura ?? "A");
                    cmd.Parameters.AddWithValue("@UltimoFolio", config.UltimoFolio);
                    cmd.Parameters.AddWithValue("@LogoEmpresa", (object)config.LogoEmpresa ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Activo", config.Activo ? 1 : 0);
                    cmd.Parameters.AddWithValue("@FechaActualizacion", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static int ObtenerSiguienteFolio()
        {
            var config = ObtenerConfiguracionFiscal();
            int nuevoFolio = (config?.UltimoFolio ?? 0) + 1;

            using (var conn = Database.GetConnection())
            {
                conn.Open();
                string query = "UPDATE ConfiguracionFiscal SET UltimoFolio = @Folio WHERE Id = 1";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Folio", nuevoFolio);
                    cmd.ExecuteNonQuery();
                }
            }

            return nuevoFolio;
        }

        #endregion

        #region CFDI CRUD

        public static int GuardarCFDI(CFDI cfdi)
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string query;
                        if (cfdi.Id == 0)
                        {
                            query = @"INSERT INTO CFDI
                                (UUID, Serie, Folio, Fecha, FormaPagoClave, MetodoPagoClave, TipoComprobante,
                                 Exportacion, Moneda, TipoCambio, LugarExpedicion, Subtotal, Descuento,
                                 IVATrasladado, IVARetenido, ISRRetenido, Total, EmisorRFC, EmisorNombre,
                                 EmisorRegimenFiscal, ReceptorRFC, ReceptorNombre, ReceptorRegimenFiscal,
                                 ReceptorDomicilioFiscalCP, ReceptorUsoCFDI, VentaId, ClienteId, CadenaOriginal,
                                 SelloDigitalCFDI, SelloSAT, NoCertificadoEmisor, NoCertificadoSAT, FechaTimbrado,
                                 XMLOriginal, XMLTimbrado, Estado, MotivoCancelacion, FechaCancelacion,
                                 UUIDSustituto, Notas, UsuarioId, FechaCreacion)
                                VALUES
                                (@UUID, @Serie, @Folio, @Fecha, @FormaPagoClave, @MetodoPagoClave, @TipoComprobante,
                                 @Exportacion, @Moneda, @TipoCambio, @LugarExpedicion, @Subtotal, @Descuento,
                                 @IVATrasladado, @IVARetenido, @ISRRetenido, @Total, @EmisorRFC, @EmisorNombre,
                                 @EmisorRegimenFiscal, @ReceptorRFC, @ReceptorNombre, @ReceptorRegimenFiscal,
                                 @ReceptorDomicilioFiscalCP, @ReceptorUsoCFDI, @VentaId, @ClienteId, @CadenaOriginal,
                                 @SelloDigitalCFDI, @SelloSAT, @NoCertificadoEmisor, @NoCertificadoSAT, @FechaTimbrado,
                                 @XMLOriginal, @XMLTimbrado, @Estado, @MotivoCancelacion, @FechaCancelacion,
                                 @UUIDSustituto, @Notas, @UsuarioId, @FechaCreacion);
                                SELECT last_insert_rowid();";
                        }
                        else
                        {
                            query = @"UPDATE CFDI SET
                                UUID = @UUID, Serie = @Serie, Folio = @Folio, Fecha = @Fecha,
                                FormaPagoClave = @FormaPagoClave, MetodoPagoClave = @MetodoPagoClave,
                                TipoComprobante = @TipoComprobante, Exportacion = @Exportacion,
                                Moneda = @Moneda, TipoCambio = @TipoCambio, LugarExpedicion = @LugarExpedicion,
                                Subtotal = @Subtotal, Descuento = @Descuento, IVATrasladado = @IVATrasladado,
                                IVARetenido = @IVARetenido, ISRRetenido = @ISRRetenido, Total = @Total,
                                EmisorRFC = @EmisorRFC, EmisorNombre = @EmisorNombre,
                                EmisorRegimenFiscal = @EmisorRegimenFiscal, ReceptorRFC = @ReceptorRFC,
                                ReceptorNombre = @ReceptorNombre, ReceptorRegimenFiscal = @ReceptorRegimenFiscal,
                                ReceptorDomicilioFiscalCP = @ReceptorDomicilioFiscalCP, ReceptorUsoCFDI = @ReceptorUsoCFDI,
                                VentaId = @VentaId, ClienteId = @ClienteId, CadenaOriginal = @CadenaOriginal,
                                SelloDigitalCFDI = @SelloDigitalCFDI, SelloSAT = @SelloSAT,
                                NoCertificadoEmisor = @NoCertificadoEmisor, NoCertificadoSAT = @NoCertificadoSAT,
                                FechaTimbrado = @FechaTimbrado, XMLOriginal = @XMLOriginal, XMLTimbrado = @XMLTimbrado,
                                Estado = @Estado, MotivoCancelacion = @MotivoCancelacion,
                                FechaCancelacion = @FechaCancelacion, UUIDSustituto = @UUIDSustituto,
                                Notas = @Notas, UsuarioId = @UsuarioId
                                WHERE Id = @Id;
                                SELECT @Id;";
                        }

                        int cfdiId;
                        using (var cmd = new SQLiteCommand(query, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Id", cfdi.Id);
                            cmd.Parameters.AddWithValue("@UUID", cfdi.UUID ?? "");
                            cmd.Parameters.AddWithValue("@Serie", cfdi.Serie ?? "");
                            cmd.Parameters.AddWithValue("@Folio", cfdi.Folio ?? "");
                            cmd.Parameters.AddWithValue("@Fecha", cfdi.Fecha ?? DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
                            cmd.Parameters.AddWithValue("@FormaPagoClave", cfdi.FormaPagoClave ?? "01");
                            cmd.Parameters.AddWithValue("@MetodoPagoClave", cfdi.MetodoPagoClave ?? "PUE");
                            cmd.Parameters.AddWithValue("@TipoComprobante", cfdi.TipoComprobante ?? "I");
                            cmd.Parameters.AddWithValue("@Exportacion", cfdi.Exportacion ?? "01");
                            cmd.Parameters.AddWithValue("@Moneda", cfdi.Moneda ?? "MXN");
                            cmd.Parameters.AddWithValue("@TipoCambio", cfdi.TipoCambio);
                            cmd.Parameters.AddWithValue("@LugarExpedicion", cfdi.LugarExpedicion ?? "");
                            cmd.Parameters.AddWithValue("@Subtotal", cfdi.Subtotal);
                            cmd.Parameters.AddWithValue("@Descuento", cfdi.Descuento);
                            cmd.Parameters.AddWithValue("@IVATrasladado", cfdi.IVATrasladado);
                            cmd.Parameters.AddWithValue("@IVARetenido", cfdi.IVARetenido);
                            cmd.Parameters.AddWithValue("@ISRRetenido", cfdi.ISRRetenido);
                            cmd.Parameters.AddWithValue("@Total", cfdi.Total);
                            cmd.Parameters.AddWithValue("@EmisorRFC", cfdi.EmisorRFC ?? "");
                            cmd.Parameters.AddWithValue("@EmisorNombre", cfdi.EmisorNombre ?? "");
                            cmd.Parameters.AddWithValue("@EmisorRegimenFiscal", cfdi.EmisorRegimenFiscal ?? "");
                            cmd.Parameters.AddWithValue("@ReceptorRFC", cfdi.ReceptorRFC ?? "");
                            cmd.Parameters.AddWithValue("@ReceptorNombre", cfdi.ReceptorNombre ?? "");
                            cmd.Parameters.AddWithValue("@ReceptorRegimenFiscal", cfdi.ReceptorRegimenFiscal ?? "");
                            cmd.Parameters.AddWithValue("@ReceptorDomicilioFiscalCP", cfdi.ReceptorDomicilioFiscalCP ?? "");
                            cmd.Parameters.AddWithValue("@ReceptorUsoCFDI", cfdi.ReceptorUsoCFDI ?? "G03");
                            cmd.Parameters.AddWithValue("@VentaId", (object)cfdi.VentaId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ClienteId", (object)cfdi.ClienteId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@CadenaOriginal", cfdi.CadenaOriginal ?? "");
                            cmd.Parameters.AddWithValue("@SelloDigitalCFDI", cfdi.SelloDigitalCFDI ?? "");
                            cmd.Parameters.AddWithValue("@SelloSAT", cfdi.SelloSAT ?? "");
                            cmd.Parameters.AddWithValue("@NoCertificadoEmisor", cfdi.NoCertificadoEmisor ?? "");
                            cmd.Parameters.AddWithValue("@NoCertificadoSAT", cfdi.NoCertificadoSAT ?? "");
                            cmd.Parameters.AddWithValue("@FechaTimbrado", cfdi.FechaTimbrado ?? "");
                            cmd.Parameters.AddWithValue("@XMLOriginal", cfdi.XMLOriginal ?? "");
                            cmd.Parameters.AddWithValue("@XMLTimbrado", cfdi.XMLTimbrado ?? "");
                            cmd.Parameters.AddWithValue("@Estado", cfdi.Estado ?? "Pendiente");
                            cmd.Parameters.AddWithValue("@MotivoCancelacion", cfdi.MotivoCancelacion ?? "");
                            cmd.Parameters.AddWithValue("@FechaCancelacion", cfdi.FechaCancelacion ?? "");
                            cmd.Parameters.AddWithValue("@UUIDSustituto", cfdi.UUIDSustituto ?? "");
                            cmd.Parameters.AddWithValue("@Notas", cfdi.Notas ?? "");
                            cmd.Parameters.AddWithValue("@UsuarioId", (object)cfdi.UsuarioId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@FechaCreacion", cfdi.FechaCreacion ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                            cfdiId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // Eliminar conceptos anteriores si es actualización
                        if (cfdi.Id > 0)
                        {
                            string deleteConceptos = "DELETE FROM DetalleCFDI WHERE CFDIId = @CFDIId";
                            using (var cmd = new SQLiteCommand(deleteConceptos, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@CFDIId", cfdiId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Insertar conceptos
                        foreach (var concepto in cfdi.Conceptos)
                        {
                            string insertConcepto = @"INSERT INTO DetalleCFDI
                                (CFDIId, ClaveProdServ, NoIdentificacion, ClaveUnidad, Unidad, Descripcion,
                                 Cantidad, ValorUnitario, Importe, Descuento, ObjetoImpClave, ImpuestoTrasladado,
                                 TasaOCuota, TipoFactor, ImporteImpuesto)
                                VALUES
                                (@CFDIId, @ClaveProdServ, @NoIdentificacion, @ClaveUnidad, @Unidad, @Descripcion,
                                 @Cantidad, @ValorUnitario, @Importe, @Descuento, @ObjetoImpClave, @ImpuestoTrasladado,
                                 @TasaOCuota, @TipoFactor, @ImporteImpuesto)";

                            using (var cmd = new SQLiteCommand(insertConcepto, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@CFDIId", cfdiId);
                                cmd.Parameters.AddWithValue("@ClaveProdServ", concepto.ClaveProdServ ?? "01010101");
                                cmd.Parameters.AddWithValue("@NoIdentificacion", concepto.NoIdentificacion ?? "");
                                cmd.Parameters.AddWithValue("@ClaveUnidad", concepto.ClaveUnidad ?? "H87");
                                cmd.Parameters.AddWithValue("@Unidad", concepto.Unidad ?? "Pieza");
                                cmd.Parameters.AddWithValue("@Descripcion", concepto.Descripcion ?? "");
                                cmd.Parameters.AddWithValue("@Cantidad", concepto.Cantidad);
                                cmd.Parameters.AddWithValue("@ValorUnitario", concepto.ValorUnitario);
                                cmd.Parameters.AddWithValue("@Importe", concepto.Importe);
                                cmd.Parameters.AddWithValue("@Descuento", concepto.Descuento);
                                cmd.Parameters.AddWithValue("@ObjetoImpClave", concepto.ObjetoImpClave ?? "02");
                                cmd.Parameters.AddWithValue("@ImpuestoTrasladado", concepto.ImpuestoTrasladado ?? "002");
                                cmd.Parameters.AddWithValue("@TasaOCuota", concepto.TasaOCuota);
                                cmd.Parameters.AddWithValue("@TipoFactor", concepto.TipoFactor ?? "Tasa");
                                cmd.Parameters.AddWithValue("@ImporteImpuesto", concepto.ImporteImpuesto);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return cfdiId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Actualiza un CFDI después de ser timbrado exitosamente
        /// </summary>
        public static bool ActualizarCFDITimbrado(CFDI cfdi)
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();
                    string query = @"UPDATE CFDI SET
                        UUID = @UUID,
                        FechaTimbrado = @FechaTimbrado,
                        XMLTimbrado = @XMLTimbrado,
                        SelloDigitalCFDI = @SelloDigitalCFDI,
                        SelloSAT = @SelloSAT,
                        NoCertificadoEmisor = @NoCertificadoEmisor,
                        NoCertificadoSAT = @NoCertificadoSAT,
                        CadenaOriginal = @CadenaOriginal,
                        Estado = @Estado
                        WHERE Id = @Id";

                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", cfdi.Id);
                        cmd.Parameters.AddWithValue("@UUID", cfdi.UUID ?? "");
                        cmd.Parameters.AddWithValue("@FechaTimbrado", cfdi.FechaTimbrado ?? "");
                        cmd.Parameters.AddWithValue("@XMLTimbrado", cfdi.XMLTimbrado ?? "");
                        cmd.Parameters.AddWithValue("@SelloDigitalCFDI", cfdi.SelloDigitalCFDI ?? "");
                        cmd.Parameters.AddWithValue("@SelloSAT", cfdi.SelloSAT ?? "");
                        cmd.Parameters.AddWithValue("@NoCertificadoEmisor", cfdi.NoCertificadoEmisor ?? "");
                        cmd.Parameters.AddWithValue("@NoCertificadoSAT", cfdi.NoCertificadoSAT ?? "");
                        cmd.Parameters.AddWithValue("@CadenaOriginal", cfdi.CadenaOriginal ?? "");
                        cmd.Parameters.AddWithValue("@Estado", cfdi.Estado ?? "Timbrado");

                        int rows = cmd.ExecuteNonQuery();
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar CFDI timbrado: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Actualiza el estado de un CFDI (para cancelaciones)
        /// </summary>
        public static bool ActualizarEstadoCFDI(int cfdiId, string nuevoEstado, string fechaCancelacion = null)
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();
                    string query = @"UPDATE CFDI SET
                        Estado = @Estado,
                        FechaCancelacion = @FechaCancelacion
                        WHERE Id = @Id";

                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", cfdiId);
                        cmd.Parameters.AddWithValue("@Estado", nuevoEstado);
                        cmd.Parameters.AddWithValue("@FechaCancelacion",
                            fechaCancelacion ?? DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));

                        int rows = cmd.ExecuteNonQuery();
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar estado CFDI: {ex.Message}");
                return false;
            }
        }

        public static CFDI ObtenerCFDIPorId(int id)
        {
            CFDI cfdi = null;
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                string query = "SELECT * FROM CFDI WHERE Id = @Id";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            cfdi = MapearCFDI(reader);
                        }
                    }
                }

                if (cfdi != null)
                {
                    cfdi.Conceptos = ObtenerConceptosCFDI(conn, cfdi.Id);
                }
            }
            return cfdi;
        }

        public static List<CFDI> ObtenerTodosCFDI(string filtroEstado = null, DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var lista = new List<CFDI>();
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                var sb = new StringBuilder("SELECT * FROM CFDI WHERE 1=1");
                var parameters = new List<SQLiteParameter>();

                if (!string.IsNullOrEmpty(filtroEstado))
                {
                    sb.Append(" AND Estado = @Estado");
                    parameters.Add(new SQLiteParameter("@Estado", filtroEstado));
                }

                if (fechaInicio.HasValue)
                {
                    sb.Append(" AND date(Fecha) >= date(@FechaInicio)");
                    parameters.Add(new SQLiteParameter("@FechaInicio", fechaInicio.Value.ToString("yyyy-MM-dd")));
                }

                if (fechaFin.HasValue)
                {
                    sb.Append(" AND date(Fecha) <= date(@FechaFin)");
                    parameters.Add(new SQLiteParameter("@FechaFin", fechaFin.Value.ToString("yyyy-MM-dd")));
                }

                sb.Append(" ORDER BY Id DESC");

                using (var cmd = new SQLiteCommand(sb.ToString(), conn))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(MapearCFDI(reader));
                        }
                    }
                }
            }
            return lista;
        }

        public static List<CFDI> BuscarCFDI(string busqueda)
        {
            var lista = new List<CFDI>();
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                string query = @"SELECT * FROM CFDI
                    WHERE UUID LIKE @Busqueda
                    OR Folio LIKE @Busqueda
                    OR ReceptorRFC LIKE @Busqueda
                    OR ReceptorNombre LIKE @Busqueda
                    ORDER BY Id DESC";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Busqueda", $"%{busqueda}%");
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(MapearCFDI(reader));
                        }
                    }
                }
            }
            return lista;
        }

        private static CFDI MapearCFDI(SQLiteDataReader reader)
        {
            return new CFDI
            {
                Id = Convert.ToInt32(reader["Id"]),
                UUID = reader["UUID"]?.ToString(),
                Serie = reader["Serie"]?.ToString(),
                Folio = reader["Folio"]?.ToString(),
                Fecha = reader["Fecha"]?.ToString(),
                FormaPagoClave = reader["FormaPagoClave"]?.ToString(),
                MetodoPagoClave = reader["MetodoPagoClave"]?.ToString(),
                TipoComprobante = reader["TipoComprobante"]?.ToString(),
                Exportacion = reader["Exportacion"]?.ToString(),
                Moneda = reader["Moneda"]?.ToString(),
                TipoCambio = Convert.ToDecimal(reader["TipoCambio"] ?? 1),
                LugarExpedicion = reader["LugarExpedicion"]?.ToString(),
                Subtotal = Convert.ToDecimal(reader["Subtotal"] ?? 0),
                Descuento = Convert.ToDecimal(reader["Descuento"] ?? 0),
                IVATrasladado = Convert.ToDecimal(reader["IVATrasladado"] ?? 0),
                IVARetenido = Convert.ToDecimal(reader["IVARetenido"] ?? 0),
                ISRRetenido = Convert.ToDecimal(reader["ISRRetenido"] ?? 0),
                Total = Convert.ToDecimal(reader["Total"] ?? 0),
                EmisorRFC = reader["EmisorRFC"]?.ToString(),
                EmisorNombre = reader["EmisorNombre"]?.ToString(),
                EmisorRegimenFiscal = reader["EmisorRegimenFiscal"]?.ToString(),
                ReceptorRFC = reader["ReceptorRFC"]?.ToString(),
                ReceptorNombre = reader["ReceptorNombre"]?.ToString(),
                ReceptorRegimenFiscal = reader["ReceptorRegimenFiscal"]?.ToString(),
                ReceptorDomicilioFiscalCP = reader["ReceptorDomicilioFiscalCP"]?.ToString(),
                ReceptorUsoCFDI = reader["ReceptorUsoCFDI"]?.ToString(),
                VentaId = reader["VentaId"] != DBNull.Value ? Convert.ToInt32(reader["VentaId"]) : (int?)null,
                ClienteId = reader["ClienteId"] != DBNull.Value ? Convert.ToInt32(reader["ClienteId"]) : (int?)null,
                CadenaOriginal = reader["CadenaOriginal"]?.ToString(),
                SelloDigitalCFDI = reader["SelloDigitalCFDI"]?.ToString(),
                SelloSAT = reader["SelloSAT"]?.ToString(),
                NoCertificadoEmisor = reader["NoCertificadoEmisor"]?.ToString(),
                NoCertificadoSAT = reader["NoCertificadoSAT"]?.ToString(),
                FechaTimbrado = reader["FechaTimbrado"]?.ToString(),
                XMLOriginal = reader["XMLOriginal"]?.ToString(),
                XMLTimbrado = reader["XMLTimbrado"]?.ToString(),
                Estado = reader["Estado"]?.ToString(),
                MotivoCancelacion = reader["MotivoCancelacion"]?.ToString(),
                FechaCancelacion = reader["FechaCancelacion"]?.ToString(),
                UUIDSustituto = reader["UUIDSustituto"]?.ToString(),
                Notas = reader["Notas"]?.ToString(),
                UsuarioId = reader["UsuarioId"] != DBNull.Value ? Convert.ToInt32(reader["UsuarioId"]) : (int?)null,
                FechaCreacion = reader["FechaCreacion"]?.ToString()
            };
        }

        private static List<DetalleCFDI> ObtenerConceptosCFDI(SQLiteConnection conn, int cfdiId)
        {
            var lista = new List<DetalleCFDI>();
            string query = "SELECT * FROM DetalleCFDI WHERE CFDIId = @CFDIId";
            using (var cmd = new SQLiteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@CFDIId", cfdiId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new DetalleCFDI
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            CFDIId = Convert.ToInt32(reader["CFDIId"]),
                            ClaveProdServ = reader["ClaveProdServ"]?.ToString(),
                            NoIdentificacion = reader["NoIdentificacion"]?.ToString(),
                            ClaveUnidad = reader["ClaveUnidad"]?.ToString(),
                            Unidad = reader["Unidad"]?.ToString(),
                            Descripcion = reader["Descripcion"]?.ToString(),
                            Cantidad = Convert.ToDecimal(reader["Cantidad"] ?? 0),
                            ValorUnitario = Convert.ToDecimal(reader["ValorUnitario"] ?? 0),
                            Importe = Convert.ToDecimal(reader["Importe"] ?? 0),
                            Descuento = Convert.ToDecimal(reader["Descuento"] ?? 0),
                            ObjetoImpClave = reader["ObjetoImpClave"]?.ToString(),
                            ImpuestoTrasladado = reader["ImpuestoTrasladado"]?.ToString(),
                            TasaOCuota = Convert.ToDecimal(reader["TasaOCuota"] ?? 0),
                            TipoFactor = reader["TipoFactor"]?.ToString(),
                            ImporteImpuesto = Convert.ToDecimal(reader["ImporteImpuesto"] ?? 0)
                        });
                    }
                }
            }
            return lista;
        }

        #endregion

        #region Generación de XML CFDI 4.0

        /// <summary>
        /// Genera el XML del CFDI según la estructura del Anexo 20 versión 4.0
        /// </summary>
        public static string GenerarXMLCFDI(CFDI cfdi)
        {
            XNamespace cfdiNs = "http://www.sat.gob.mx/cfd/4";
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(cfdiNs + "Comprobante",
                    new XAttribute(XNamespace.Xmlns + "cfdi", cfdiNs.NamespaceName),
                    new XAttribute(XNamespace.Xmlns + "xsi", xsi.NamespaceName),
                    new XAttribute(xsi + "schemaLocation", "http://www.sat.gob.mx/cfd/4 http://www.sat.gob.mx/sitio_internet/cfd/4/cfdv40.xsd"),
                    new XAttribute("Version", "4.0"),
                    new XAttribute("Serie", cfdi.Serie ?? ""),
                    new XAttribute("Folio", cfdi.Folio ?? ""),
                    new XAttribute("Fecha", cfdi.Fecha),
                    new XAttribute("FormaPago", cfdi.FormaPagoClave),
                    new XAttribute("SubTotal", cfdi.Subtotal.ToString("F2")),
                    cfdi.Descuento > 0 ? new XAttribute("Descuento", cfdi.Descuento.ToString("F2")) : null,
                    new XAttribute("Moneda", cfdi.Moneda),
                    cfdi.Moneda != "MXN" ? new XAttribute("TipoCambio", cfdi.TipoCambio.ToString("F4")) : null,
                    new XAttribute("Total", cfdi.Total.ToString("F2")),
                    new XAttribute("TipoDeComprobante", cfdi.TipoComprobante),
                    new XAttribute("Exportacion", cfdi.Exportacion),
                    new XAttribute("MetodoPago", cfdi.MetodoPagoClave),
                    new XAttribute("LugarExpedicion", cfdi.LugarExpedicion),

                    // Emisor
                    new XElement(cfdiNs + "Emisor",
                        new XAttribute("Rfc", cfdi.EmisorRFC),
                        new XAttribute("Nombre", cfdi.EmisorNombre),
                        new XAttribute("RegimenFiscal", cfdi.EmisorRegimenFiscal)
                    ),

                    // Receptor
                    new XElement(cfdiNs + "Receptor",
                        new XAttribute("Rfc", cfdi.ReceptorRFC),
                        new XAttribute("Nombre", cfdi.ReceptorNombre),
                        new XAttribute("DomicilioFiscalReceptor", cfdi.ReceptorDomicilioFiscalCP),
                        !string.IsNullOrEmpty(cfdi.ReceptorRegimenFiscal) ?
                            new XAttribute("RegimenFiscalReceptor", cfdi.ReceptorRegimenFiscal) : null,
                        new XAttribute("UsoCFDI", cfdi.ReceptorUsoCFDI)
                    ),

                    // Conceptos
                    new XElement(cfdiNs + "Conceptos",
                        cfdi.Conceptos.Select(c => GenerarConceptoXML(cfdiNs, c))
                    ),

                    // Impuestos
                    cfdi.IVATrasladado > 0 ? GenerarImpuestosXML(cfdiNs, cfdi) : null
                )
            );

            // Remover atributos nulos
            doc.Descendants().Attributes().Where(a => a.Value == null).Remove();

            return doc.ToString();
        }

        private static XElement GenerarConceptoXML(XNamespace cfdiNs, DetalleCFDI concepto)
        {
            var elemento = new XElement(cfdiNs + "Concepto",
                new XAttribute("ClaveProdServ", concepto.ClaveProdServ),
                !string.IsNullOrEmpty(concepto.NoIdentificacion) ?
                    new XAttribute("NoIdentificacion", concepto.NoIdentificacion) : null,
                new XAttribute("Cantidad", concepto.Cantidad.ToString("F2")),
                new XAttribute("ClaveUnidad", concepto.ClaveUnidad),
                !string.IsNullOrEmpty(concepto.Unidad) ?
                    new XAttribute("Unidad", concepto.Unidad) : null,
                new XAttribute("Descripcion", concepto.Descripcion),
                new XAttribute("ValorUnitario", concepto.ValorUnitario.ToString("F2")),
                new XAttribute("Importe", concepto.Importe.ToString("F2")),
                concepto.Descuento > 0 ?
                    new XAttribute("Descuento", concepto.Descuento.ToString("F2")) : null,
                new XAttribute("ObjetoImp", concepto.ObjetoImpClave)
            );

            // Agregar impuestos del concepto si aplica
            if (concepto.ObjetoImpClave == "02" && concepto.ImporteImpuesto > 0)
            {
                elemento.Add(
                    new XElement(cfdiNs + "Impuestos",
                        new XElement(cfdiNs + "Traslados",
                            new XElement(cfdiNs + "Traslado",
                                new XAttribute("Base", concepto.Importe.ToString("F2")),
                                new XAttribute("Impuesto", concepto.ImpuestoTrasladado),
                                new XAttribute("TipoFactor", concepto.TipoFactor),
                                new XAttribute("TasaOCuota", concepto.TasaOCuota.ToString("F6")),
                                new XAttribute("Importe", concepto.ImporteImpuesto.ToString("F2"))
                            )
                        )
                    )
                );
            }

            return elemento;
        }

        private static XElement GenerarImpuestosXML(XNamespace cfdiNs, CFDI cfdi)
        {
            return new XElement(cfdiNs + "Impuestos",
                new XAttribute("TotalImpuestosTrasladados", cfdi.IVATrasladado.ToString("F2")),
                new XElement(cfdiNs + "Traslados",
                    new XElement(cfdiNs + "Traslado",
                        new XAttribute("Base", cfdi.Subtotal.ToString("F2")),
                        new XAttribute("Impuesto", "002"), // IVA
                        new XAttribute("TipoFactor", "Tasa"),
                        new XAttribute("TasaOCuota", "0.160000"),
                        new XAttribute("Importe", cfdi.IVATrasladado.ToString("F2"))
                    )
                )
            );
        }

        #endregion

        #region Creación de CFDI desde Venta

        /// <summary>
        /// Crea un CFDI a partir de una venta existente
        /// </summary>
        public static CFDI CrearCFDIDesdeVenta(int ventaId, string receptorRFC, string receptorNombre,
            string receptorRegimenFiscal, string receptorCP, string usoCFDI, string formaPago, string metodoPago)
        {
            var config = ObtenerConfiguracionFiscal();
            if (config == null || !config.ConfiguracionCompleta)
            {
                throw new InvalidOperationException("La configuración fiscal no está completa. Configure los datos del emisor primero.");
            }

            // Obtener la venta y sus detalles
            var venta = ObtenerVenta(ventaId);
            if (venta == null)
            {
                throw new InvalidOperationException($"No se encontró la venta con ID {ventaId}");
            }

            var detallesVenta = ObtenerDetallesVenta(ventaId);

            // Crear el CFDI
            var cfdi = new CFDI
            {
                Serie = config.SerieFactura,
                Folio = ObtenerSiguienteFolio().ToString(),
                Fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                FormaPagoClave = formaPago,
                MetodoPagoClave = metodoPago,
                TipoComprobante = "I",
                Exportacion = "01",
                Moneda = "MXN",
                TipoCambio = 1,
                LugarExpedicion = config.LugarExpedicion ?? config.CodigoPostal,

                // Emisor
                EmisorRFC = config.RFC,
                EmisorNombre = config.RazonSocial,
                EmisorRegimenFiscal = config.RegimenFiscalClave,

                // Receptor
                ReceptorRFC = receptorRFC,
                ReceptorNombre = receptorNombre,
                ReceptorRegimenFiscal = receptorRegimenFiscal,
                ReceptorDomicilioFiscalCP = receptorCP,
                ReceptorUsoCFDI = usoCFDI,

                VentaId = ventaId,
                Estado = "Pendiente",
                FechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // Calcular montos
            decimal subtotal = 0;
            decimal totalIVA = 0;

            foreach (var detalle in detallesVenta)
            {
                var concepto = new DetalleCFDI
                {
                    ClaveProdServ = detalle.ClaveProdServSAT ?? "01010101",
                    NoIdentificacion = detalle.ProductoCodigo,
                    ClaveUnidad = detalle.ClaveUnidadSAT ?? "H87",
                    Unidad = "Pieza",
                    Descripcion = detalle.ProductoNombre,
                    Cantidad = detalle.Cantidad,
                    ValorUnitario = detalle.PrecioUnitario,
                    Importe = detalle.Cantidad * detalle.PrecioUnitario,
                    ObjetoImpClave = detalle.ObjetoImpClave ?? "02",
                    ImpuestoTrasladado = "002", // IVA
                    TasaOCuota = detalle.TasaIVA,
                    TipoFactor = "Tasa"
                };

                concepto.ImporteImpuesto = Math.Round(concepto.Importe * concepto.TasaOCuota, 2);
                subtotal += concepto.Importe;
                totalIVA += concepto.ImporteImpuesto;

                cfdi.Conceptos.Add(concepto);
            }

            cfdi.Subtotal = Math.Round(subtotal, 2);
            cfdi.IVATrasladado = Math.Round(totalIVA, 2);
            cfdi.Total = Math.Round(subtotal + totalIVA, 2);

            // Generar XML
            cfdi.XMLOriginal = GenerarXMLCFDI(cfdi);

            return cfdi;
        }

        private static dynamic ObtenerVenta(int ventaId)
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                string query = "SELECT * FROM Ventas WHERE Id = @Id";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", ventaId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Total = Convert.ToDecimal(reader["Total"]),
                                Fecha = reader["Fecha"]?.ToString(),
                                MetodoPago = reader["MetodoPago"]?.ToString(),
                                ClienteId = reader["ClienteId"] != DBNull.Value ? Convert.ToInt32(reader["ClienteId"]) : (int?)null
                            };
                        }
                    }
                }
            }
            return null;
        }

        private static List<DetalleVentaExtendido> ObtenerDetallesVenta(int ventaId)
        {
            var lista = new List<DetalleVentaExtendido>();
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                string query = @"SELECT dv.*,
                    COALESCE(p.ClaveProdServSAT, '01010101') as ClaveProdServSAT,
                    COALESCE(p.ClaveUnidadSAT, 'H87') as ClaveUnidadSAT,
                    COALESCE(p.ObjetoImpClave, '02') as ObjetoImpClave,
                    COALESCE(p.TasaIVA, 0.16) as TasaIVA
                    FROM DetalleVentas dv
                    LEFT JOIN Productos p ON dv.ProductoCodigo = p.Codigo
                    WHERE dv.VentaId = @VentaId";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@VentaId", ventaId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new DetalleVentaExtendido
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                VentaId = Convert.ToInt32(reader["VentaId"]),
                                ProductoCodigo = reader["ProductoCodigo"]?.ToString(),
                                ProductoNombre = reader["ProductoNombre"]?.ToString(),
                                Cantidad = Convert.ToInt32(reader["Cantidad"]),
                                PrecioUnitario = Convert.ToDecimal(reader["PrecioUnitario"]),
                                Subtotal = Convert.ToDecimal(reader["Subtotal"]),
                                ClaveProdServSAT = reader["ClaveProdServSAT"]?.ToString(),
                                ClaveUnidadSAT = reader["ClaveUnidadSAT"]?.ToString(),
                                ObjetoImpClave = reader["ObjetoImpClave"]?.ToString(),
                                TasaIVA = Convert.ToDecimal(reader["TasaIVA"])
                            });
                        }
                    }
                }
            }
            return lista;
        }

        private class DetalleVentaExtendido
        {
            public int Id { get; set; }
            public int VentaId { get; set; }
            public string ProductoCodigo { get; set; }
            public string ProductoNombre { get; set; }
            public int Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal Subtotal { get; set; }
            public string ClaveProdServSAT { get; set; }
            public string ClaveUnidadSAT { get; set; }
            public string ObjetoImpClave { get; set; }
            public decimal TasaIVA { get; set; }
        }

        #endregion

        #region Estadísticas

        public static (int Total, int Timbrados, int Pendientes, int Cancelados, decimal MontoTotal) ObtenerEstadisticasCFDI(DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                var sb = new StringBuilder(@"SELECT
                    COUNT(*) as Total,
                    SUM(CASE WHEN Estado = 'Timbrado' THEN 1 ELSE 0 END) as Timbrados,
                    SUM(CASE WHEN Estado = 'Pendiente' THEN 1 ELSE 0 END) as Pendientes,
                    SUM(CASE WHEN Estado = 'Cancelado' THEN 1 ELSE 0 END) as Cancelados,
                    COALESCE(SUM(CASE WHEN Estado = 'Timbrado' THEN Total ELSE 0 END), 0) as MontoTotal
                    FROM CFDI WHERE 1=1");

                var parameters = new List<SQLiteParameter>();

                if (fechaInicio.HasValue)
                {
                    sb.Append(" AND date(Fecha) >= date(@FechaInicio)");
                    parameters.Add(new SQLiteParameter("@FechaInicio", fechaInicio.Value.ToString("yyyy-MM-dd")));
                }

                if (fechaFin.HasValue)
                {
                    sb.Append(" AND date(Fecha) <= date(@FechaFin)");
                    parameters.Add(new SQLiteParameter("@FechaFin", fechaFin.Value.ToString("yyyy-MM-dd")));
                }

                using (var cmd = new SQLiteCommand(sb.ToString(), conn))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return (
                                Convert.ToInt32(reader["Total"]),
                                Convert.ToInt32(reader["Timbrados"]),
                                Convert.ToInt32(reader["Pendientes"]),
                                Convert.ToInt32(reader["Cancelados"]),
                                Convert.ToDecimal(reader["MontoTotal"])
                            );
                        }
                    }
                }
            }
            return (0, 0, 0, 0, 0);
        }

        #endregion

        #region Ventas sin facturar

        public static List<dynamic> ObtenerVentasSinFacturar()
        {
            var lista = new List<dynamic>();
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                string query = @"SELECT v.*, c.Nombre as ClienteNombre, c.RFC as ClienteRFC
                    FROM Ventas v
                    LEFT JOIN Clientes c ON v.ClienteId = c.Id
                    WHERE v.Id NOT IN (SELECT DISTINCT VentaId FROM CFDI WHERE VentaId IS NOT NULL AND Estado != 'Cancelado')
                    ORDER BY v.Id DESC
                    LIMIT 100";

                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Fecha = reader["Fecha"]?.ToString(),
                            Total = Convert.ToDecimal(reader["Total"]),
                            MetodoPago = reader["MetodoPago"]?.ToString(),
                            ClienteId = reader["ClienteId"] != DBNull.Value ? Convert.ToInt32(reader["ClienteId"]) : (int?)null,
                            ClienteNombre = reader["ClienteNombre"]?.ToString() ?? "Público en general",
                            ClienteRFC = reader["ClienteRFC"]?.ToString()
                        });
                    }
                }
            }
            return lista;
        }

        #endregion
    }
}
