using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClienteUDP.Models
{
    public class RespuestaClienteModel
    {
        public int puntuacion { get; set; }
        public List<string> Respuestas { get; set; } = null!;
    }
}
