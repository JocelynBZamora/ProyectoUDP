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
        private async Task Recibir()
        {
            while (true)
            {
                var responce = await clienteUDP.ReceiveAsync();
                var mensaje = Encoding.UTF8.GetString(responce.Buffer);
                if (mensaje.StartsWith("ERROR|DUPLICADO"))
                {
                    DuplicadoRecibido?.Invoke();
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
