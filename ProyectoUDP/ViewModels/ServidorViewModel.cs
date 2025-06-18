// ProyectoUDP/ViewModels/ServidorViewModel.cs

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProyectoUDP.Controls;
using ProyectoUDP.Models;
using ProyectoUDP.Services;
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
using System.Windows;

namespace ProyectoUDP.ViewModels
{
    public partial class ServidorViewModel : ObservableObject
    {
        private ServerUdp? serverUdp;

        [ObservableProperty]
        private string ipServidor = "0.0.0.0";

        // Nueva propiedad para controlar la habilitación/deshabilitación
        [ObservableProperty]
        private bool cuestionarioIniciado = false;

        // Propiedad calculada para simplificar el binding en el XAML
        public bool PuedeIniciarCuestionario => !CuestionarioIniciado;


        public ObservableCollection<RespuestaModel> UsuariosConectados { get; } = new();

        private readonly HashSet<IPEndPoint> ipsRespondidas = new();
        private readonly HashSet<string> nombresRespondidos = new();
        public ObservableCollection<string> MensajesRecibidos { get; set; } = new();
        public ObservableCollection<string> MensajeOpcion { get; set; } = new();
        public bool GetParticipantesConectados() => serverUdp?.RegistroClientes.Count > 0;

        public ServidorViewModel()
        {
            ipServidor = Dns.GetHostEntry(Dns.GetHostName())
                                .AddressList
                                .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                ?.ToString() ?? "Desconocido";
            serverUdp = new();
            serverUdp.Respuesta += RegistarRespuesta;
            serverUdp.Iniciar();
            CargarPreguntasDesdeJson();
        }

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

        private int indicePregunta = 0;
        public string PreguntaActual { get => preguntaActual; set { preguntaActual = value; OnPropertyChanged(); } }
        private string preguntaActual = "";

        [RelayCommand]
        private void IniciarCuestionario()
        {
            // Establecer a true para deshabilitar los controles una vez que el quiz inicia
            CuestionarioIniciado = true;
            OnPropertyChanged(nameof(PuedeIniciarCuestionario)); // Notificar a la UI

            // Si es la primera vez que se inicia el cuestionario
            if (PreguntaActual == "")
            {
                indicePregunta = 0;
                // Al iniciar el quiz, también deshabilitamos el ComboBox del TimerControl
                // Esto se hace a través de una propiedad en el TimerControl, que crearemos a continuación
                if (timerControl != null)
                {
                    timerControl.HabilitarSeleccionTiempo = false; // Nueva propiedad en TimerControl
                }
            }
            else
            {
                // Avanzar a la siguiente pregunta
                indicePregunta++;
                if (indicePregunta >= preguntas.Length)
                {
                    // Si llegamos al final, detener el timer
                    timerControl?.StopTimer();
                    MessageBox.Show("¡Fin del Quiz! Revisa las puntuaciones finales."); // Mensaje de fin
                    CuestionarioIniciado = false; // Opcional: Re-habilitar si quieres un "nuevo quiz"
                    OnPropertyChanged(nameof(PuedeIniciarCuestionario));
                    if (timerControl != null)
                    {
                        timerControl.HabilitarSeleccionTiempo = true; // Opcional: Re-habilitar
                    }
                    return;
                }
            }
            MostrarPregunta(indicePregunta);
        }

        public PreguntasModel PreguntaEnCurso { get; private set; }
        public ObservableCollection<string> Opciones { get; set; } = new();
        public string ResultadosPreguntaActual { get => resultadosPreguntaActual; set { resultadosPreguntaActual = value; OnPropertyChanged(); } }
        private string resultadosPreguntaActual = "";
        private string respuestaCorrectaActual = "";
        public static class ServidorViewModelAccessor
        {
            public static string RespuestaCorrectaActual { get; set; } = "";
        }
        private void MostrarPregunta(int indice)
        {
            if (indice < 0 || indice >= preguntas.Length) return;

            var p = preguntas[indice];
            PreguntaActual = p.Texto;
            ResultadosPreguntaActual = "";

            Opciones.Clear();
            foreach (var op in p.Opciones)
                Opciones.Add(op);
            PreguntaEnCurso = new PreguntasModel();

            PreguntaEnCurso.Texto = p.Texto;
            PreguntaEnCurso.RespuestaCorrecta = p.RespuestaCorrecta;
            PreguntaEnCurso.Opciones = p.Opciones;


            respuestaCorrectaActual = p.RespuestaCorrecta;
            serverUdp?.EnviarPreguntas(PreguntaEnCurso);
        }

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

        private Dictionary<string, int> puntajesPorAlumno = new();

        private void RegistarRespuesta(RespuestaModel responce)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (responce.Opcion == '-')
                {
                    OnPropertyChanged(nameof(GetParticipantesConectados));
                    return;
                }

                if (!nombresRespondidos.Contains(responce.Nombre))
                {
                    nombresRespondidos.Add(responce.Nombre);
                    ipsRespondidas.Add(responce.ClienteEndPoint);
                }

                // Guardar puntaje
                puntajesPorAlumno[responce.Nombre] = responce.Puntaje;

                // Limpiar respuesta anterior
                var existentes = MensajesRecibidos.Where(m => m.StartsWith(responce.Nombre)).ToList();
                foreach (var item in existentes)
                    MensajesRecibidos.Remove(item);

                MensajesRecibidos.Add($"{responce.Nombre} Puntos: {responce.Puntaje}/15");
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propiedad) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
    }
}