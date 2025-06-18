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
        private string preguntaActual;
        public ObservableCollection<string> Opciones { get; set; } = new();

        // Asegúrate de que tu propiedad Registrado y PuedeRegistrar estén definidas así:
        [ObservableProperty]
        private bool registrado = false;

        // Esta propiedad se recalcula automáticamente cuando 'Registrado' cambia
        public bool PuedeRegistrar => !Registrado;


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
            });
            await Task.Delay(3000);

            App.Current.Dispatcher.Invoke(() =>
            {
                RespuestasHabilitadas = true; 
            });
        }
        [RelayCommand]
        private void SeleccionarRespuesta(string opcionSeleccionada)
        {

            
            
            if (opcionSeleccionada == RespuestaCorrecta)
            {
                Puntaje++;
            }
            
            clienteUDP?.EnviarRespuesta(nombre, opcionSeleccionada, Puntaje);
            //limpiar para siguiente pregunta
            Opciones.Clear();
            MessageBox.Show("Respuesta enviada");
            
        }


        [RelayCommand]
        public void Registrar()
        {

            if (string.IsNullOrWhiteSpace(ipServidor) || string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show("Ingresa la IP del servidor y tu nombre.");
                return;
            }

            if (clienteUDP == null)
            {
                clienteUDP = new(ipServidor.Trim(), 65000, 65001);
                // Cuando se recibe un nombre duplicado del servidor
                clienteUDP.DuplicadoRecibido += () =>
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Nombre existente, ingrese otro");
                        // Si el nombre es duplicado, re-habilitar el registro
                        Registrado = false;
                        OnPropertyChanged(nameof(PuedeRegistrar));
                    });
                };
                clienteUDP.PreguntaRecibida += MostrarPregunta;
            }

            // Aquí enviamos el registro.
            // La deshabilitación ocurre *después* de enviar, asumiendo éxito
            // a menos que recibamos un mensaje de duplicado.
            clienteUDP.EnviarRegistro(nombre.Trim());

            // Asumimos que el registro fue enviado. Deshabilitamos los campos.
            Registrado = true; // Establecer a true para deshabilitar
            OnPropertyChanged(nameof(PuedeRegistrar)); // Notificar a la UI
            MessageBox.Show("Registro enviado. Espera confirmación o el inicio del quiz.");

        }
        


        //public event PropertyChangedEventHandler? PropertyChanged;
        //private void OnPropertyChanged(string propertyName) =>
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
