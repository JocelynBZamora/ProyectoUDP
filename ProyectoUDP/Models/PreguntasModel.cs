using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoUDP.Models
{
    public class PreguntasModel
    {
        public string Texto { get; set; } = null!;
        public List<string> Opciones { get; set; } = null!;
        public string RespuestaCorrecta { get; set; } = null!;
        public int TiempoRetardoMs { get; set; } // Nueva propiedad, coincidente con el modelo del cliente

    }
}
