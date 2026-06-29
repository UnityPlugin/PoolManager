namespace UnityPlugin
{
    public interface IPoolable
    {
        void OnSpawn();
        void OnRecycle();
    }
}
