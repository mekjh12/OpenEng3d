namespace Common.Abstractions
{
    public interface ICamera : 
        ICameraTransformation,
        ICameraMovement,
        ICameraProperties,
        ICameraLifecycle
    {
        string Direction { get; }
    }
}
