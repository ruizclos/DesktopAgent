using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LocalAIAgent.ViewModels
{
    public class AppointmentViewModel : INotifyPropertyChanged
    {
        private string _clientName = "";
        public string ClientName
        {
            get => _clientName;
            set { _clientName = value; OnPropertyChanged(); }
        }

        private DateTime _date = DateTime.Now.Date;
        public DateTime Date
        {
            get => _date;
            set { _date = value; OnPropertyChanged(); }
        }

        private TimeSpan _time = new TimeSpan(14, 0, 0); // Default 2 PM
        public TimeSpan Time
        {
            get => _time;
            set { _time = value; OnPropertyChanged(); }
        }

        private int _durationMinutes = 30;
        public int DurationMinutes
        {
            get => _durationMinutes;
            set { _durationMinutes = value; OnPropertyChanged(); }
        }

        private string _purpose = "";
        public string Purpose
        {
            get => _purpose;
            set { _purpose = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
