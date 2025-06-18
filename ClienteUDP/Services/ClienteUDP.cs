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

        public event Action? DuplicadoRecibido; // Este evento no se usa actualmente en tu VM, pero se mantiene.
        public event Action<PreguntaModel>? PreguntaRecibida;
        public event Action<string>? MensajeErrorRecibido; // Evento para errores

        // --- NUEVO EVENTO: IniciarConteoTiempo ---
        // Este evento se disparará cuando el servidor envíe una señal para iniciar el temporizador.
        // Pasa un entero, que será el tiempo total en segundos para el temporizador del cliente.
        public event Action<int>? IniciarConteoTiempo;

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
                try
                {
                    var response = await clienteUDP.ReceiveAsync();
                    var mensaje = Encoding.UTF8.GetString(response.Buffer);

                    if (mensaje.StartsWith("ERROR|DUPLICADO"))
                    {
                        string errorMessage = "Nombre de usuario ya existe.";
                        MensajeErrorRecibido?.Invoke(errorMessage);
                    }
                    else if (mensaje.StartsWith("INICIAR_CONTEO|")) // --- NUEVA CONDICIÓN ---
                    {
                        // Formato esperado: "INICIAR_CONTEO|60" (donde 60 son los segundos)
                        string[] parts = mensaje.Split('|');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int tiempoSegundos))
                        {
                            IniciarConteoTiempo?.Invoke(tiempoSegundos); // Dispara el nuevo evento
                            // Opcional: actualizar un mensaje de estado o registrar en consola
                            Console.WriteLine($"[CLIENTE UDP] Señal de inicio de conteo recibida: {tiempoSegundos} segundos.");
                        }
                        else
                        {
                            Console.WriteLine($"[CLIENTE UDP] Formato de INICIAR_CONTEO incorrecto: {mensaje}");
                        }
                    }
                    else if (mensaje.Contains("\"Tipo\":\"Pregunta\""))
                    {
                        var pregunta = JsonSerializer.Deserialize<PreguntaModel>(mensaje);
                        if (pregunta != null) // Añadir verificación de nulidad para la deserialización
                        {
                            PreguntaRecibida?.Invoke(pregunta);
                        }
                    }
                    else
                    {
                        Console.WriteLine("[CLIENTE] Mensaje recibido del servidor: " + mensaje);
                        // Si deseas usar AlRecibirMensaje para mensajes generales, invócalo aquí:
                        // AlRecibirMensaje?.Invoke(mensaje);
                    }
                }
                catch (SocketException ex)
                {
                    // Manejar errores específicos de socket, por ejemplo, conexión reiniciada, host inalcanzable
                    MensajeErrorRecibido?.Invoke($"Error de conexión: {ex.Message}");
                    break; // Salir del bucle en errores críticos
                }
                catch (Exception ex)
                {
                    // Capturar otros errores inesperados durante la recepción
                    MensajeErrorRecibido?.Invoke($"Error al recibir mensaje: {ex.Message}");
                }
            }
        }

        // El evento AlRecibirMensaje se declara pero no se usa en la lógica del método Recibir proporcionado.
        // Si quieres usarlo para mensajes generales, deberías invocarlo en el bloque else de arriba.
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
                MessageBox.Show(ex.ToString()); // Considera usar MensajeErrorRecibido para esto también
            }

        }
}
