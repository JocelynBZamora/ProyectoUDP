using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProyectoUDP.Models;
using ProyectoUDP.Services;
using ProyectoUDP.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProyectoUDP.ViewModels
{
    public partial class ServidorViewModel : ObservableObject
    {
        private ServerUdp? serverUdp;

        public string Estado { get => estado; set { estado = value; OnPropertyChanged(); } }
        private string estado = "Servidor detenido";

        [ObservableProperty]
        private string ipServidor = "0.0.0.0";
        [ObservableProperty]
        private string nombreUsuario = "";

        public ObservableCollection<string> MensajesRecibidos { get; set; } = new();

        public bool GetParticipantesConectados() => serverUdp?.RegistroClientes.Count > 0;

        public ServidorViewModel()
        {
             ipServidor = Dns.GetHostEntry(Dns.GetHostName())
                             .AddressList
                             .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                             ?.ToString() ?? "Desconocido";
            serverUdp = new();
            
            CargarPreguntasDesdeJson();

        }


        //tiempo para contestar y cambiar de pregunta
        private TimerControl? timerControl;
        public void SetTimerControl(TimerControl control)
        {
            timerControl = control;
            timerControl.TimerCompleted += TimerControl_TimerCompleted;
        }

        private void TimerControl_TimerCompleted(object? sender, EventArgs e)
        {
            IniciarCuestionario();
        }

        //iniciar y mostrar las preguntas 
        private int indicePregunta = 0;
        public string PreguntaActual { get => preguntaActual; set { preguntaActual = value; OnPropertyChanged(); } }
        private string preguntaActual = "";

        [RelayCommand]
        private void IniciarCuestionario()
        {
            // Si es la primera vez que se inicia el cuestionario
            if (PreguntaActual == "")
            {
                indicePregunta = 0;
            }
            else
            {
                // Avanzar a la siguiente pregunta
                indicePregunta++;
                if (indicePregunta >= preguntas.Length)
                {
                    // Si llegamos al final, detener el timer
                    timerControl?.StopTimer();
                    return;
                }
            }
            MostrarPregunta(indicePregunta);
        }


        public ObservableCollection<string> Opciones { get; set; } = new();
        public string ResultadosPreguntaActual { get => resultadosPreguntaActual; set { resultadosPreguntaActual = value; OnPropertyChanged(); } }
        private string resultadosPreguntaActual = "";
        private string respuestaCorrectaActual = "";
        private void MostrarPregunta(int indice)
        {
            if (indice < 0 || indice >= preguntas.Length) return;

            var p = preguntas[indice];
            PreguntaActual = p.Texto;
            ResultadosPreguntaActual = "";

            Opciones.Clear();
            foreach (var op in p.Opciones)
                Opciones.Add(op);
            respuestaCorrectaActual = p.RespuestaCorrecta;

            // Enviar JSON con pregunta, opciones y respuesta correcta
            //if (servidor != null)
            //{
            //    var paquete = new
            //    {
            //        Tipo = "Pregunta",
            //        Texto = p.Texto,
            //        Opciones = p.Opciones,
            //        RespuestaCorrecta = p.RespuestaCorrecta
            //    };
            //    ServidorViewModelAccessor.RespuestaCorrectaActual = p.RespuestaCorrecta;

            //    string mensajeJson = JsonSerializer.Serialize(paquete);
            //    servidor.EnviarBroadcast(mensajeJson);
            //}
        }


        //muestra las preguntas del Json
        private PreguntasModel[] preguntas = Array.Empty<PreguntasModel>();
        private void CargarPreguntasDesdeJson()
        {
            try
            {
                string ruta = @"..\..\..\Resources\preguntas.json";

                if (File.Exists(ruta))
                {
                    string json = File.ReadAllText(ruta);
                    preguntas = JsonSerializer.Deserialize<PreguntasModel[]>(json) ?? Array.Empty<PreguntasModel>();

                }
                else
                {
                    MensajesRecibidos.Add("[ERROR] No se encontró el archivo de preguntas.");
                }
            }
            catch (Exception ex)
            {
                MensajesRecibidos.Add($"[ERROR] Al leer JSON: {ex.Message}");
            }
        }

        private void RegistarRespuesta(RespuestaModel responce)
        {
            App.Current.Dispatcher.Invoke(() => {
                if (responce.Opcion == '-')
                {
                    OnPropertyChanged(nameof(GetParticipantesConectados));
                    return;
                }
            });
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propiedad) =>
           PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
    }
}
