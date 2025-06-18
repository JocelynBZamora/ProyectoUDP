using ClienteUDP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace ClienteUDP.Services
{
    public class ClientUDP
    {
        private UdpClient clienteUDP = new();
        private IPEndPoint IPEndPoint;

        public event Action? DuplicadoRecibido;
        public event Action<PreguntaModel>? PreguntaRecibida;


        public ClientUDP(string ServIP, int pServidor, int local)
        {
            clienteUDP = new UdpClient(local); 

            IPEndPoint = new IPEndPoint(IPAddress.Parse(ServIP), pServidor);
            Task.Run(Recibir);
           
        }
        public event Action<string>? MensajeErrorRecibido; // Nuevo evento para errores

        private async Task Recibir()
        {
            while (true)
            {
                var responce = await clienteUDP.ReceiveAsync();
                var mensaje = Encoding.UTF8.GetString(responce.Buffer);
                if (mensaje.StartsWith("ERROR|DUPLICADO"))
                {
                    // Extraer el mensaje específico si lo hubiera o un mensaje genérico
                    string errorMessage = "Nombre de usuario ya existe."; // Mensaje por defecto
                    MensajeErrorRecibido?.Invoke(errorMessage);
                }
                else if (mensaje.Contains("\"Tipo\":\"Pregunta\""))
                {
                    var pregunta = JsonSerializer.Deserialize<PreguntaModel>(mensaje);
                    PreguntaRecibida?.Invoke(pregunta);
                }
                else
                {
                    Console.WriteLine("[CLIENTE] Mensaje recibido del servidor: " + mensaje);
                }

            }
        }
        public event Action<string>? AlRecibirMensaje;

        public void EnviarRespuesta(string nombre, string respuesta, int puntaje)
        {
            var paquete = new
            {
                Tipo = "Respuesta",
                Nombre = nombre,
                Respuesta = respuesta,
                Puntaje = puntaje
            };

            string json = JsonSerializer.Serialize(paquete);
            var datos = Encoding.UTF8.GetBytes(json);
            clienteUDP.Send(datos, datos.Length, IPEndPoint);
        }

        public void EnviarRegistro(string nombre)
        {
            try
            {
                var d = Encoding.UTF8.GetBytes($"REGISTRO|{nombre}");
                clienteUDP.Send(d, d.Length, IPEndPoint);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

    }
}
