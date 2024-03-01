namespace VRMarionette.ForceTask
{
    public interface IForceTaskExecutor
    {
        public SingleForceTask ExecuteTask(IForceTask task);
    }
}