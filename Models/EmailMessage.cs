using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LocalAIAgent.Models
{
    /// <summary>Structured email for display in the Inbox tab.</summary>
    public class EmailMessage : INotifyPropertyChanged
    {
        public string From    { get; init; } = "";
        public string Subject { get; init; } = "(No Subject)";
        public DateTime Date  { get; init; }
        public string Body    { get; init; } = "";
        public string Preview => Body.Length > 120 ? Body[..120].Replace('\n', ' ') + "…" : Body.Replace('\n', ' ');

        // Raw sender address for Reply-To
        public string FromAddress { get; init; } = "";

        private bool _isRead;
        public bool IsRead
        {
            get => _isRead;
            set { _isRead = value; OnPropertyChanged(); OnPropertyChanged(nameof(FontWeight)); }
        }

        public string FontWeight => IsRead ? "Normal" : "SemiBold";

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
