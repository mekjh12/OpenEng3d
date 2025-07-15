using Common.Abstractions;

/// <summary>
/// 완전한 변환 기능을 제공한다. (복잡한 3D 객체용)
/// </summary>
public interface ITransformable : IBasicTransformable, IRotatable, IMatrixTransformable
{

}