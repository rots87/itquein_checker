using Itqueuein_Check.Core;

namespace Itquein_Check
{
    public partial class Form1 : Form
    {
        private CheckerEngine _engine;

        public Form1()
        {
            InitializeComponent();

            // Cadena de conexión a Labcore
            string connectionString = "Server=localhost;Database=Labcore;User ID=Labcore;Password=1Sys.@#;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True;";

            // Inicializamos el motor inyectando el método con la nueva firma (string, Color)
            _engine = new CheckerEngine(connectionString, MostrarLogEnPantalla);

            // Timer a 30 segundos (30000 ms)
            timer1.Interval = 30000;

            btn_detener.Enabled = false;
        }

        private void btn_iniciar_Click(object sender, EventArgs e)
        {
            // Usamos un color neutro (Azul o Gris) para los mensajes del sistema
            MostrarLogEnPantalla("--- SERVICIO CHECKER INICIADO ---", Color.DodgerBlue);

            btn_iniciar.Enabled = false;
            btn_detener.Enabled = true;

            _ = EjecutarCicloAsync();
        }

        private void btn_detener_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            MostrarLogEnPantalla("--- SERVICIO DETENIDO ---", Color.DodgerBlue);

            btn_iniciar.Enabled = true;
            btn_detener.Enabled = false;
        }

        private async void timer1_Tick(object sender, EventArgs e)
        {
            await EjecutarCicloAsync();
        }

        private async Task EjecutarCicloAsync()
        {
            timer1.Stop();

            try
            {
                await _engine.ProcesarColaPendientesAsync();
            }
            catch (Exception ex)
            {
                MostrarLogEnPantalla($"Error fatal en el ciclo principal: {ex.Message}", Color.Tomato);
            }
            finally
            {
                if (btn_detener.Enabled)
                {
                    timer1.Start();
                }
            }
        }

        // --- MOTOR DE UI ACTUALIZADO (Colores y Límite de 200 Líneas) ---
        private void MostrarLogEnPantalla(string mensaje, Color color)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, Color>(MostrarLogEnPantalla), mensaje, color);
                return;
            }

            // 1. Escribir con color
            richTextBox1.SelectionStart = richTextBox1.TextLength;
            richTextBox1.SelectionLength = 0;
            richTextBox1.SelectionColor = color;
            richTextBox1.AppendText($"{DateTime.Now:HH:mm:ss} - {mensaje}{Environment.NewLine}");
            richTextBox1.SelectionColor = richTextBox1.ForeColor; // Restaurar color por defecto

            // 2. Motor de Autolimpieza (Máximo 200 líneas)
            const int MAX_LINES = 200;
            if (richTextBox1.Lines.Length > MAX_LINES)
            {
                // Suspendemos el pintado de la UI por un milisegundo para evitar parpadeos (flickering) al borrar
                richTextBox1.SuspendLayout();

                int linesToRemove = richTextBox1.Lines.Length - MAX_LINES;
                int indexToCut = richTextBox1.GetFirstCharIndexFromLine(linesToRemove);

                richTextBox1.Select(0, indexToCut);
                richTextBox1.SelectedText = "";

                richTextBox1.ResumeLayout();
            }

            // 3. Auto-scroll al final
            richTextBox1.SelectionStart = richTextBox1.TextLength;
            richTextBox1.ScrollToCaret();
        }
    }
}