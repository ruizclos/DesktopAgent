using System.Windows;
using LocalAIAgent.ViewModels;

namespace LocalAIAgent.Views
{
    public partial class AppointmentDialog : Window
    {
        public AppointmentViewModel ViewModel { get; }

        public AppointmentDialog()
        {
            InitializeComponent();
            ViewModel = new AppointmentViewModel();
            DataContext = ViewModel;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
