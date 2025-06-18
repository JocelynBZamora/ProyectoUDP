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

        // Asegúrate de que tu propiedad Registrado y PuedeRegistrar estén definidas así:
        [ObservableProperty]
        private bool registrado = false;

        // Esta propiedad se recalcula automáticamente cuando 'Registrado' cambia
        public bool PuedeRegistrar => !Registrado;

        [ObservableProperty]
        private string mensajeEstado = ""; // Nueva propiedad para mensajes en la vista

        private ClientUDP? clienteUDP;
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
                RespuestasHabilitadas = false;
                RespuestaCorrecta = pregunta.RespuestaCorrecta;
                MensajeEstado = "Nueva pregunta recibida. Esperando para responder..."; // Actualizar mensaje de estado
            });
            await Task.Delay(3000);

            App.Current.Dispatcher.Invoke(() =>
            {
                RespuestasHabilitadas = true;
                MensajeEstado = "¡Tiempo para responder!"; // Actualizar mensaje de estado
            });
        }
        [RelayCommand]
        private void SeleccionarRespuesta(string opcionSeleccionada)
        {
            if (string.IsNullOrWhiteSpace(opcionSeleccionada)) return; // Evitar enviar respuestas vacías

            if (opcionSeleccionada == RespuestaCorrecta)
            {
                Puntaje++;
            }
            
            clienteUDP?.EnviarRespuesta(nombre, opcionSeleccionada, Puntaje);
            //limpiar para siguiente pregunta
            Opciones.Clear();
            MensajeEstado = "Respuesta enviada. Esperando la siguiente pregunta."; // Actualizar mensaje de estado
            RespuestasHabilitadas = false; // Deshabilitar después de enviar respuesta
            
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
                // Suscribirse al nuevo evento de error
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
            }

            clienteUDP.EnviarRegistro(nombre.Trim());
            Registrado = true;
            OnPropertyChanged(nameof(PuedeRegistrar));
            MensajeEstado = "Registro enviado. Espera confirmación o el inicio del quiz."; // Mensaje en la vista

        }
        


        //public event PropertyChangedEventHandler? PropertyChanged;
        //private void OnPropertyChanged(string propertyName) =>
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
