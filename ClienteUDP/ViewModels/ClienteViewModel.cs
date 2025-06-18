using ClienteUDP.Models;
using ClienteUDP.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading; // Necesario para DispatcherTimer


namespace ClienteUDP.ViewModels
{
    public partial class ClienteViewModel:ObservableObject
    {
        [ObservableProperty]
        public string ipServidor = "0.0.0.0";
        [ObservableProperty]
        public string nombre = "Ingresa nombre";

        [ObservableProperty]
        private string respuestaCorrecta = "";

        [ObservableProperty]
        private int puntaje = 0;
        [ObservableProperty]
        private bool respuestasHabilitadas = false;
        [ObservableProperty]
        private bool registroHabilitado = true;
        [ObservableProperty]
        private string preguntaActual = ""; //Inicializar para evitar null
        public ObservableCollection<string> Opciones { get; set; } = new();

        [ObservableProperty]
        private bool registrado = false;

        public bool PuedeRegistrar => !Registrado;

        [ObservableProperty]
        private string mensajeEstado = ""; // Nueva propiedad para mensajes en la vista

        // PROPIEDADES Y OBJETOS DEL TEMPORIZADOR AÑADIDOS
        [ObservableProperty]
        private int tiempoRestante = 0; // Propiedad para mostrar el tiempo restante
        private DispatcherTimer? clientTimer; // Objeto temporizador

        private ClientUDP? clienteUDP;

        // CONSTRUCTOR: Aquí inicializamos el temporizador y lo configuramos
        public ClienteViewModel()
        {
            clientTimer = new DispatcherTimer();
            clientTimer.Interval = TimeSpan.FromSeconds(1);
            clientTimer.Tick += ClientTimer_Tick;
        }

        private async void MostrarPregunta(PreguntaModel pregunta)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                PreguntaActual = pregunta.Texto;
                Opciones.Clear();
                foreach (var opcion in pregunta.Opciones)
                {
                    Opciones.Add(opcion);
                }
                // Las respuestas NO se habilitan aquí, se habilitan con la señal del servidor (IniciarConteoTiempo)
                RespuestasHabilitadas = false; // Asegurar que al mostrar una nueva pregunta, estén deshabilitadas por defecto
                RespuestaCorrecta = pregunta.RespuestaCorrecta;
                MensajeEstado = "Nueva pregunta recibida. Esperando señal del servidor..."; // Actualizar mensaje de estado
                TiempoRestante = 0; // Reiniciar o mostrar "0" hasta que el servidor inicie el conteo
            });

            // ELIMINAR ESTE DELAY: El servidor ahora controlará cuándo se habilitan las respuestas a través de IniciarConteoTiempo
            // int delayTime = pregunta.TiempoRetardoMs > 0 ? pregunta.TiempoRetardoMs : 3000;
            // await Task.Delay(delayTime);

            // Esta parte ya no va aquí, se mueve a IniciarTemporizadorCliente que es llamado por el servicio
            // App.Current.Dispatcher.Invoke(() =>
            // {
            //     RespuestasHabilitadas = true;
            //     MensajeEstado = "¡Tiempo para responder!";
            // });
        }

        private void IniciarTemporizadorCliente(int tiempoTotalSegundos)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                TiempoRestante = tiempoTotalSegundos;
                RespuestasHabilitadas = true; // Habilitar respuestas cuando el servidor lo indique
                MensajeEstado = $"¡Tiempo para responder! Tienes {tiempoTotalSegundos} segundos.";
                clientTimer?.Stop(); // Asegúrate de detenerlo antes de iniciar si ya estaba corriendo
                clientTimer?.Start();
            });
        }

        private void ClientTimer_Tick(object? sender, EventArgs e)
        {
            if (TiempoRestante > 0)
            {
                TiempoRestante--;
            }
            else
            {
                clientTimer?.Stop();
                RespuestasHabilitadas = false; // Deshabilitar respuestas cuando el tiempo se acaba
                MensajeEstado = "¡Tiempo terminado! Esperando la siguiente pregunta.";
                // Opcional: Podrías enviar una respuesta "vacía" al servidor aquí si el tiempo expira
                clienteUDP?.EnviarRespuesta(nombre, "Tiempo expirado", Puntaje); // Notificar al servidor que el tiempo se acabó
            }
        }

        [RelayCommand]
        private void SeleccionarRespuesta(string opcionSeleccionada)
        {
            if (!RespuestasHabilitadas) return; // No permitir respuestas si están deshabilitadas

            if (string.IsNullOrWhiteSpace(opcionSeleccionada)) return; // Evitar enviar respuestas vacías

            if (opcionSeleccionada == RespuestaCorrecta)
            {
                Puntaje++;
            }

            clienteUDP?.EnviarRespuesta(nombre, opcionSeleccionada, Puntaje);
            RespuestasHabilitadas = false; // Deshabilitar después de enviar respuesta
            clientTimer?.Stop(); // Detener el temporizador del cliente una vez que se envía la respuesta
            MensajeEstado = "Respuesta enviada. Esperando la siguiente pregunta."; // Actualizar mensaje de estado
        }

        [RelayCommand]
        public void Registrar()
        {
            if (string.IsNullOrWhiteSpace(ipServidor) || string.IsNullOrWhiteSpace(nombre))
            {
                MensajeEstado = "Ingresa la IP del servidor y tu nombre."; // Mensaje en la vista
                return;
            }

            if (clienteUDP == null)
            {
                clienteUDP = new(ipServidor.Trim(), 65000, 65001);
                // Suscribirse al evento de error
                clienteUDP.MensajeErrorRecibido += (msg) =>
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        MensajeEstado = msg; // Mostrar el mensaje de error del servicio
                        Registrado = false;
                        OnPropertyChanged(nameof(PuedeRegistrar));
                    });
                };
                clienteUDP.PreguntaRecibida += MostrarPregunta;
                // ¡IMPORTANTE! Suscribirse al evento de inicio de conteo de tiempo del servidor
                clienteUDP.IniciarConteoTiempo += IniciarTemporizadorCliente;
            }

            clienteUDP.EnviarRegistro(nombre.Trim());
            Registrado = true;
            OnPropertyChanged(nameof(PuedeRegistrar));
            MensajeEstado = "Registro enviado. Espera confirmación o el inicio del quiz."; // Mensaje en la vista
        }
    }
}
