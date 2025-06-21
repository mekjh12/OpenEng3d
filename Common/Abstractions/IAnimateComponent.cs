namespace Common.Abstractions
{
    public interface IAnimateComponent
    {
        int BoneIndexOnlyOneJoint { get; set; }

        bool IsOnlyOneJointWeight { get; set; }
    }
}
