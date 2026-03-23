using Itqueuein_Check.Data;
using Itqueuein_Check.Logging;
using Itqueuein_Check.Models;
using Itqueuein_Check.Network;

namespace Itqueuein_Check.Core
{
    public class CheckerEngine
    {
        private readonly DatabaseManager _dbManager;
        private readonly RestlinkClient _restClient;
        private readonly FileLogger _logger;
        private readonly Action<string, Color> _logUi;
        private bool _isProcessing = false;

        public CheckerEngine(string connectionString, Action<string, Color> logUi)
        {
            _dbManager = new DatabaseManager(connectionString);
            _restClient = new RestlinkClient();
            _logger = new FileLogger();
            _logUi = logUi;
        }

        public async Task ProcesarColaPendientesAsync(bool forzarCheck = false)
        {
            if (_isProcessing) return;
            _isProcessing = true;

            try
            {
                List<CheckRecord> pendientes = _dbManager.ObtenerPendientes();
                int enMaduracion = 0;
                List<string> idsMadurando = new List<string>();

                foreach (var orden in pendientes)
                {
                    bool tieneSisId = !string.IsNullOrWhiteSpace(orden.SisId) && orden.SisId != "SIN_ID";
                    string sisIdDisplay = tieneSisId ? orden.SisId : "NO_DISPONIBLE";

                    // 1. VALIDACIÓN EN LABCORE
                    if (_dbManager.VerificarExisteOrden(orden.NumMsj, orden.SisId))
                    {
                        _dbManager.MarcarComoProcesado(orden.Id);
                        _logUi?.Invoke($"[OK] {orden.NumMsj}: Sincronizada.", Color.LimeGreen);
                        // Opcional: _logger.LogInfo($"Orden {orden.NumMsj} sincronizada correctamente.");
                        continue;
                    }

                    // 2. VEREDICTO FINAL (Intento 5) - BLINDAJE SOLICITADO
                    if (orden.Intentos >= 5 && orden.MinutosTranscurridos >= 6)
                    {
                        _dbManager.MarcarComoProcesado(orden.Id);
                        string mensajeFatal = $"El mensaje numero {orden.NumMsj} con numero de SIAP {sisIdDisplay} fallo definitivamente tras 5 intentos.";

                        _logUi?.Invoke($"[FATAL] {mensajeFatal}", Color.Red);

                        // ✅ USANDO TU LOGGER:
                        _logger.LogError(mensajeFatal);
                        continue;
                    }

                    // 3. REGLA DE MADURACIÓN (6 Minutos)
                    // Si 'forzarCheck' es true (desde el botón de la Fase 3), ignoramos los 6 min
                    if (orden.MinutosTranscurridos < 6 && !forzarCheck)
                    {
                        enMaduracion++;
                        if (idsMadurando.Count < 5)
                        {
                            string tag = tieneSisId ? $"{orden.NumMsj}({orden.SisId})" : orden.NumMsj;
                            idsMadurando.Add($"{tag}[it:{orden.Intentos}]");
                        }
                        continue;
                    }

                    // 4. PROTOCOLO DE RESCATE
                    _dbManager.EliminarCadaverRestlink(orden.NumMsj, orden.SisId);

                    if (_dbManager.VerificarExisteOrden(orden.NumMsj, orden.SisId))
                    {
                        _dbManager.MarcarComoProcesado(orden.Id);
                        continue;
                    }

                    // 5. REINYECCIÓN
                    var resultado = await _restClient.ReenviarOrdenAsync(orden.MensajeHl7);

                    if (resultado.Exito)
                    {
                        _dbManager.RegistrarInyeccionEnIIS(orden.Id);
                        _logUi?.Invoke($"[REINYECTADA] {orden.NumMsj}: Intento {orden.Intentos + 1}.", Color.DodgerBlue);
                        _logger.LogInfo($"Reinyección exitosa: {orden.NumMsj} (Intento {orden.Intentos + 1})");
                    }
                    else
                    {
                        _dbManager.RegistrarIntentoFallido(orden.Id, resultado.MensajeRespuesta);
                        Color colorErr = (orden.Intentos + 1 >= 5) ? Color.Red : Color.Orange;
                        _logUi?.Invoke($"[FALLO] {orden.NumMsj}: {resultado.MensajeRespuesta}", colorErr);

                        // ✅ USANDO TU LOGGER para errores de red:
                        _logger.LogError($"Fallo POST {orden.NumMsj}: {resultado.MensajeRespuesta}");
                    }
                }

                if (enMaduracion > 0)
                {
                    string lista = string.Join(", ", idsMadurando);
                    _logUi?.Invoke($"[INFO] {enMaduracion} madurando: {lista}", Color.Gold);
                }
            }
            catch (Exception ex)
            {
                _logUi?.Invoke($"[ERROR CRÍTICO] {ex.Message}", Color.DarkRed);
                _logger.LogError("Error en el ciclo del motor", ex); // ✅ Coincide con tu firma (string, Exception)
            }
            finally
            {
                _isProcessing = false;
            }
        }
    }
}