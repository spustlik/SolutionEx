using System.Windows;

namespace SolutionExtensions
{
    public class MessageBoxEx
    {
        public static MessageBoxEx Instance { get; private set; } = new MessageBoxEx();
    }
    public static class MessageBoxExtensions
    {
        public static MessageBoxResult Show(this MessageBoxEx mbox, string message, string caption = null,
            MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None)
        {
            return MessageBox.Show(message, caption, button, icon);
        }
        public static MessageBoxResult ShowQuestion(this MessageBoxEx mbox,
            string message,
            string caption = null,
            MessageBoxButton button = MessageBoxButton.YesNoCancel, MessageBoxImage icon = MessageBoxImage.Question)
        {
            return mbox.Show(message, caption ?? "Question", button, icon);
        }
        public static MessageBoxResult ShowInformation(this MessageBoxEx mbox,
            string message,
            string caption = null,
            MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Information)
        {
            return mbox.Show(message, "Information", button, icon);
        }
        public static MessageBoxResult ShowError(this MessageBoxEx mbox,
            string message,
            string caption = null,
            MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Stop)
        {
            return mbox.Show(message, "Error", button, icon);
        }
    }
}