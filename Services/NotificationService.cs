using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace LocalAIAgent.Services
{
    public class NotificationService
    {
        private const string APP_ID = "LocalAIAgent";

        public void ShowNotification(string title, string message)
        {
            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);

            var stringElements = toastXml.GetElementsByTagName("text");
            stringElements[0].AppendChild(toastXml.CreateTextNode(title));
            stringElements[1].AppendChild(toastXml.CreateTextNode(message));

            var toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier(APP_ID).Show(toast);
        }
    }
}
