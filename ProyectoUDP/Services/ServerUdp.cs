﻿using ProyectoUDP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ProyectoUDP.Services
{
    public class ServerUdp
    {
        private UdpClient? udpClient;
        private Thread? hiloEscucha;
        private int port = 65000;


        public event Action<string>? AlTenerError;



        public event Action<RespuestaModel>? Respuesta;

        private bool enEjecucion;


        public List<IPEndPoint> RegistroClientes { get; set; } = new List<IPEndPoint>();
        private HashSet<string> nombresRegistrados = new();

        public void Iniciar()
        {
            udpClient = new();
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            Task.Run(ResivirMensaje);
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

                    if (!RegistroClientes.Any(c => c.Equals(endPoint)))
                        if (nombresRegistrados.Contains(nombre))
                        {
                            EnviarMensaje("ERROR|DUPLICADO", endPoint);
                        }
                        else
                        {
                            nombresRegistrados.Add(nombre);
                            RegistroClientes.Add(endPoint);
                            Respuesta?.Invoke(new RespuestaModel { Nombre = nombre, Opcion = '-', ClienteEndPoint = endPoint });

                            EnviarMensaje("REGISTRO_OK", endPoint);
                        }
                    continue;

                }
                else if (mensaje.Contains("\"Tipo\":\"Respuesta\""))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(mensaje);
                        var root = doc.RootElement;

                        string nombre = root.GetProperty("Nombre").GetString() ?? "";
                        string respuesta = root.GetProperty("Respuesta").GetString() ?? "";
                        int puntaje = root.GetProperty("Puntaje").GetInt32();
                        Respuesta?.Invoke(new RespuestaModel
                        {
                            Nombre = nombre,
                            Opcion = respuesta.FirstOrDefault(),
                            ClienteEndPoint = result.RemoteEndPoint,
                            Puntaje = puntaje
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[ERROR] Al procesar respuesta: " + ex.Message);
                    }
                }
            }
        }

        public void EnviarPreguntas(PreguntasModel p)
        {
            var paquete = new
            {
                Tipo = "Pregunta",
                Texto = p.Texto,
                Opciones = p.Opciones,
                RespuestaCorrecta = p.RespuestaCorrecta
            };
            var json = JsonSerializer.Serialize(paquete);

            var datos = Encoding.UTF8.GetBytes(json);

            foreach (var c in RegistroClientes)
                udpClient?.Send(datos, datos.Length, c);
        
        }
        public void EnviarMensaje(string mensaje, IPEndPoint destino)
        {
            var datos = Encoding.UTF8.GetBytes(mensaje);
            udpClient?.Send(datos, datos.Length, destino);
        }
    }
}