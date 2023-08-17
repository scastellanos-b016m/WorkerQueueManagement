using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorkerQueueManagement.Models
{
    public class JsonAlumno
    {
        [JsonPropertyName("NoExpediente")]
        public string NoExpediente { get; set; }

        [JsonPropertyName("Ciclo")]
        public string Ciclo { get; set; }

        [JsonPropertyName("MesInicioPago")]
        public int MesInicioPago { get; set; }

        [JsonPropertyName("CarreraId")]
        public string CarreraId { get; set; }

        [JsonPropertyName("InscripcionCargoId")]
        public string InscripcionCargoId { get; set; }

        [JsonPropertyName("CarneCargoId")]
        public string CarneCargoId { get; set; }

        [JsonPropertyName("CargoMensualId")]
        public string CargoMensualId { get; set; }

        [JsonPropertyName("DiaPago")]
        public string DiaPago { get; set; }

        [JsonPropertyName("Email")]
        public string Email { get; set; }
    }
}