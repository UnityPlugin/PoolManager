namespace UnityPlugin.PoolManager
{
    public interface IPoolable
    {
        void Recycle();

        void OnCreate();
        void OnSpawn();

        void BeforeRecycle();
        void OnRecycle();
    }
}
