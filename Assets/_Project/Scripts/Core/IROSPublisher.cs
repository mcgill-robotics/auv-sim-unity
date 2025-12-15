/// <summary>
/// Interface for all ROS publishers. Provides the contract for ROS message publishing.
/// Publishers can implement this directly for full control, or extend ROSPublisher for default behavior.
/// </summary>
public interface IROSPublisher
{
    /// <summary>
    /// The ROS topic name this publisher sends messages to.
    /// </summary>
    string Topic { get; }
    
    /// <summary>
    /// Target publish rate in Hz.
    /// </summary>
    float PublishRate { get; set; }
    
    /// <summary>
    /// Whether this publisher is currently active and should publish.
    /// </summary>
    bool IsActive { get; }
    
    /// <summary>
    /// Enable or disable publishing.
    /// </summary>
    void SetActive(bool active);
    
    /// <summary>
    /// Publish a message to the ROS topic. Called by the timing system or manually.
    /// </summary>
    void PublishMessage();
}
