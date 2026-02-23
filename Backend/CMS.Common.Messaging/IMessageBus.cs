namespace CMS.Common.Messaging
{
    /// <summary>
    /// Interface for message bus operations
    /// </summary>
    public interface IMessageBus
    {
        /// <summary>
        /// Publish a message to an exchange
        /// </summary>
        Task PublishAsync<T>(T message, string exchangeName) where T : class;
    }
}
