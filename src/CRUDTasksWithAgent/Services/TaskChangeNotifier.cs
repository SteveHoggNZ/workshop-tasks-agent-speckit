namespace CRUDTasksWithAgent.Services
{
    public class TaskChangeNotifier
    {
        public event Action? Changed;

        public void NotifyChanged() => Changed?.Invoke();
    }
}
