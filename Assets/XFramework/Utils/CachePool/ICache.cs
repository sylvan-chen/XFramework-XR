namespace XGame.Core
{
    /// <summary>
    /// 可缓存对象
    /// </summary>
    /// <remarks>
    /// 要被缓存池管理的对象必须实现此接口，该接口能够保证对象必须实现一个清空自身的方法，
    /// 缓存池将自动调用清空方法，以防外部放回到缓存池时忘记清空。
    /// </remarks>
    public interface ICache
    {
        /// <summary>
        /// 清空对象（置为初始状态）
        /// </summary>
        public void Clear();
    }
}