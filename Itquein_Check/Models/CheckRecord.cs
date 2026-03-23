namespace Itqueuein_Check.Models
{
    public class CheckRecord
    {
        public int Id { get; set; }
        public string NumMsj { get; set; }
        public string SisId { get; set; } // <-- NUEVA PROPIEDAD
        public string MensajeHl7 { get; set; }
        public int Intentos { get; set; }
        public int MinutosTranscurridos { get; set; }
        public bool ExisteEnOrdenes { get; set; }
    }
}