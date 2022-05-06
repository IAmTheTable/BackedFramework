namespace BackedFramework.Resources.Extensions
{
    internal class GarbageCollection
    {
        public static GarbageCollection s_instance;
        
        public GarbageCollection()
        {
            if (s_instance is not null)
                throw new Exception("Only one instance of GarbageCollection can be created");

            s_instance = this;
        }
        
        public void StartRoutine()
        {
            new Thread(() =>
            {
                while (true)
                {
                    GC.Collect();
                    Thread.Sleep((1000 * 60) * 300);
                }
            }).Start();
        }
    }
}
