using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoUDP.Models
{
    public class RespuestaModel
    {
        public string Nombre { get; set; } = null!;
        public char Opcion { get; set; }
        public IPEndPoint ClienteEndPoint { get; set; } = null!;
        public bool EsCorrecta { get; set; }
    }
}
