using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClienteUDP.Models
{
    public class PreguntaModel
    {
        public string Texto { get; set; } = null!;
        public List<string> Opciones { get; set; } = null!;
        public string RespuestaCorrecta { get; set; } = null!;
        public int TiempoRetardoMs { get; set; } // Nueva propiedad para el tiempo de retardo en milisegundos

    }
}
