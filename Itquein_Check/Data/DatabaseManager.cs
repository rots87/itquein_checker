using Itqueuein_Check.Models;
using Microsoft.Data.SqlClient;

namespace Itqueuein_Check.Data
{
    public class DatabaseManager
    {
        private readonly string _connectionString;

        public DatabaseManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        // 1. OBTENER PENDIENTES (Solo AA/AE o los que ya maduraron 6 min)
        public List<CheckRecord> ObtenerPendientes()
        {
            var pendientes = new List<CheckRecord>();

            // Dentro de tu método ObtenerPendientes()
            string query = @"
                SELECT 
                    iqc_id, 
                    iqc_num_msj, 
                    ISNULL(iqc_sis_id, 'SIN_ID') AS iqc_sis_id, -- Extraemos el nuevo campo
                    iqc_msj, 
                    iqc_intentos,
                    DATEDIFF(MINUTE, iqc_fechahora, GETDATE()) as Minutos
                FROM itqueue_inCheck WITH (NOLOCK)
                WHERE iqc_pro_checker = 0 
                  AND iqc_intentos <= 5 
                  AND (
                      iqc_estado_original IN ('AA', 'AE') 
                      OR DATEDIFF(MINUTE, iqc_fechahora, GETDATE()) >= 6
                  )";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var orden = new CheckRecord
                        {
                            Id = Convert.ToInt32(reader["iqc_id"]),
                            NumMsj = reader["iqc_num_msj"].ToString(),
                            SisId = reader["iqc_sis_id"].ToString(), // <-- ASIGNAMOS EL DATO
                            MensajeHl7 = reader["iqc_msj"].ToString(),
                            Intentos = Convert.ToInt32(reader["iqc_intentos"]),
                            MinutosTranscurridos = Convert.ToInt32(reader["Minutos"])
                        };
                        pendientes.Add(orden);
                    }
                }
            }
            return pendientes;
        }

        // 2. VALIDACIÓN DE ÚLTIMO MILISEGUNDO
        public bool VerificarExisteOrden(string numMsj, string sisId)
        {
            bool existe = false;

            // Validamos NumMsj obligatoriamente, y el sisId solo si es válido.
            string query = @"
        SELECT TOP 1 1 
        FROM Ordenes WITH (NOLOCK) 
        WHERE o_num_mj = @numMsj 
          AND (
              @sisId = 'SIN_ID' 
              OR o_siap_numero = @sisId 
              OR @sisId IS NULL
          )";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@numMsj", numMsj);
                    cmd.Parameters.AddWithValue("@sisId", (object)sisId ?? DBNull.Value);

                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        existe = true;
                    }
                }
            }
            return existe;
        }

        // 3. LIMPIEZA DE TUBERÍA (Regla de Oro antes de inyectar)
        public void EliminarCadaverRestlink(string numMsj, string sisId)
        {
            // Usamos una lógica defensiva en el SQL: 
            // Si tenemos un sisId real, lo usamos para ser precisos. 
            // Si es "SIN_ID" o nulo, borramos usando solo el numMsj como respaldo.
            string query = @"
        DELETE FROM TablaErroresRestlink 
        WHERE num_msj_columna = @numMsj 
        AND (
            @sisId = 'SIN_ID' 
            OR sis_id_columna = @sisId 
            OR @sisId IS NULL
        )";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@numMsj", numMsj);
                    cmd.Parameters.AddWithValue("@sisId", (object)sisId ?? DBNull.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 4. ÉXITO: MARCAR COMO PROCESADO
        public void MarcarComoProcesado(int idCheck)
        {
            string query = "UPDATE itqueue_inCheck SET iqc_pro_checker = 1 WHERE iqc_id = @id";
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", idCheck);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // 5. FALLO: ACTUALIZAR INTENTOS Y LOG DE ERROR
        public void RegistrarIntentoFallido(int idCheck, string mensajeError)
        {
            string query = @"
                UPDATE itqueue_inCheck 
                SET iqc_intentos = iqc_intentos + 1, 
                    iqc_ultima_respuesta = @resp 
                WHERE iqc_id = @id";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", idCheck);
                cmd.Parameters.AddWithValue("@resp", (object)mensajeError ?? DBNull.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        // 6. INYECCIÓN EXITOSA: Resetear el reloj para darle tiempo a Labcore de procesarla
        public void RegistrarInyeccionEnIIS(int idCheck)
        {
            string query = @"
                UPDATE itqueue_inCheck 
                SET iqc_intentos = iqc_intentos + 1, 
                    iqc_fechahora = GETDATE(), -- ¡Magia! Reseteamos el contador de 6 minutos
                    iqc_ultima_respuesta = '200 OK (Esperando inserción en BD)' 
                WHERE iqc_id = @id";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", idCheck);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}