// ProyectoUDP/Controls/TimerControl.xaml.cs

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ProyectoUDP.Controls
{
    /// <summary>
    /// Lógica de interacción para TimerControl.xaml
    /// </summary>
    public partial class TimerControl : UserControl
    {
        private DispatcherTimer timer;
        private int currentTime;
        private int selectedTime;
        private bool isAutoCycling = false;

        // Nueva propiedad de dependencia para controlar la habilitación del ComboBox
        public static readonly DependencyProperty HabilitarSeleccionTiempoProperty =
            DependencyProperty.Register("HabilitarSeleccionTiempo", typeof(bool), typeof(TimerControl), new PropertyMetadata(true, OnHabilitarSeleccionTiempoChanged));

        public bool HabilitarSeleccionTiempo
        {
            get { return (bool)GetValue(HabilitarSeleccionTiempoProperty); }
            set { SetValue(HabilitarSeleccionTiempoProperty, value); }
        }

        private static void OnHabilitarSeleccionTiempoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TimerControl)d;
            control.TimerComboBox.IsEnabled = (bool)e.NewValue;
        }


        public event EventHandler TimerCompleted;

        public TimerControl()
        {
            InitializeComponent();
            InitializeTimer();
            // Asegurarse de que el ComboBox inicie habilitado
            TimerComboBox.IsEnabled = HabilitarSeleccionTiempo;
        }

        private void InitializeTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (currentTime > 0)
            {
                currentTime--;
                TimeDisplay.Text = currentTime.ToString();
            }
            else
            {
                TimeDisplay.Text = "0";
                TimerCompleted?.Invoke(this, EventArgs.Empty);

                // Reiniciar automáticamente el timer si está en modo auto-ciclo
                if (isAutoCycling)
                {
                    currentTime = selectedTime;
                    TimeDisplay.Text = currentTime.ToString();
                }
                else
                {
                    timer.Stop();
                }
            }
        }

        private void TimerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TimerComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string timeText = selectedItem.Content.ToString();
                selectedTime = int.Parse(timeText.Split(' ')[0]);
                currentTime = selectedTime;
                TimeDisplay.Text = currentTime.ToString();

                // Iniciar el timer y activar el auto-ciclo
                isAutoCycling = true;
                timer.Start();
            }
        }

        public void StartTimer()
        {
            if (TimerComboBox.SelectedItem == null)
            {
                TimerComboBox.SelectedIndex = 0;
            }
            isAutoCycling = true;
            timer.Start();
        }

        public void StopTimer()
        {
            isAutoCycling = false;
            timer.Stop();
            currentTime = selectedTime;
            TimeDisplay.Text = currentTime.ToString();
        }

        public void PauseTimer()
        {
            isAutoCycling = false;
            timer.Stop();
        }
    }
}