using ProyectoUDP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProyectoUDP.Services
{
    public class ServerUdp
    {
        private UdpClient? udpClient;
        private Thread? hiloEscucha;
        private int port = 6500;


        public event Action<string>? AlTenerError;
        public event Action? AlTenerDetenerse;
        public event Action? AlIniciar;



        public event Action<RespuestaModel>? Respuesta;

        private bool enEjecucion;


        public List<IPEndPoint> RegistroClientes { get; set; } = new List<IPEndPoint>();
        public HashSet<string> nombresRegistrados = new HashSet<string>();

        public void Iniciar()
        {
            udpClient = new();
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));

        }

        private async Task ResivirMensaje()
        {
            while (true)
            {
                if (udpClient == null)
                {
                    return;
                } 
                var result = await udpClient.ReceiveAsync();
                string mensaje = Encoding.UTF8.GetString(result.Buffer);
                IPEndPoint endPoint = result.RemoteEndPoint;
                //siempre imprimimos lo que llega
                System.Diagnostics.Debug.WriteLine($"[Servidor] Datagram recibido de {endPoint}: {mensaje}");


                if (mensaje.StartsWith("REGISTRO|"))
                {

                    string nombre = mensaje.Substring("REGISTRO|".Length);


                    if (!RegistroClientes.Any(c =>
                            c.Address.Equals(endPoint.Address) &&
                            c.Port == endPoint.Port))
                    {
                        RegistroClientes.Add(endPoint);
                        System.Diagnostics.Debug.WriteLine($"[Servidor] Cliente registrado correctamente: {nombre} ({endPoint})");


                        Respuesta?.Invoke(new RespuestaModel
                        {
                            Nombre = nombre,
                            Opcion = '-',
                            ClienteEndPoint = endPoint
                        });
                    }
                    continue;
                }
            }
        }
        public void Detener()
        {
            try
            {
                enEjecucion = false;
                udpClient?.Close();
                hiloEscucha?.Join(500);
                AlTenerDetenerse?.Invoke();
            }
            catch (Exception ex)
            {
                AlTenerError?.Invoke($"Error al detener servidor: {ex.Message}");
            }
        }
        public void EnviarMensaje(string mensaje, IPEndPoint destino)
        {
            var datos = Encoding.UTF8.GetBytes(mensaje);
            udpClient?.Send(datos, datos.Length, destino);
        }
    }
}
