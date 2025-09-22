using System;
using System.Windows.Threading;

namespace SolutionExtensions.UI
{
    /// <summary>
    /// Executes action after timeout. If previous call is waiting, it is disposed.
    /// </summary>
    public class ThrottleTimer
    {
        private Action action;
        private readonly DispatcherTimer timer;

        public ThrottleTimer(TimeSpan timeout)
        {
            timer = new DispatcherTimer(DispatcherPriority.Input);
            timer.Interval = timeout;
            timer.Tick += Timer_Tick;
        }
        public void Invoke(Action action)
        {
            timer.Stop();
            this.action = action;
            timer.Start();

        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            action();
        }
    }
}
