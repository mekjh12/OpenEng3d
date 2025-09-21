using System;
using OpenGL;

/// <summary>
/// Matrix4x4f를 위한 확장 메서드를 제공하는 정적 클래스
/// </summary>
public static class Matrix4x4fExtensions
{
    /// <summary>
    /// 행렬의 지정된 열을 제자리에서(in-place) 단위벡터로 정규화합니다.
    /// </summary>
    /// <param name="matrix">정규화할 행렬</param>
    /// <param name="columnIndex">정규화할 열의 인덱스 (0-3)</param>
    /// <exception cref="ArgumentOutOfRangeException">columnIndex가 0-3 범위를 벗어날 때</exception>
    /// <exception cref="InvalidOperationException">열의 크기가 0에 가까워 정규화할 수 없을 때</exception>
    public static void NormalizeColumn(this ref Matrix4x4f matrix, int columnIndex)
    {
        if (columnIndex < 0 || columnIndex > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(columnIndex), "열 인덱스는 0부터 3까지의 값이어야 합니다.");
        }

        // 열의 각 성분 가져오기
        float x = matrix[(uint)columnIndex, 0];
        float y = matrix[(uint)columnIndex, 1];
        float z = matrix[(uint)columnIndex, 2];
        float w = matrix[(uint)columnIndex, 3];

        // 열의 크기(길이) 계산
        float magnitude = (float)Math.Sqrt(x * x + y * y + z * z + w * w);

        // 0에 가까운 크기인 경우 정규화 불가능
        if (Math.Abs(magnitude) < 1e-6f)
        {
            throw new InvalidOperationException($"열 {columnIndex}의 크기가 0에 가까워 정규화할 수 없습니다.");
        }

        // 각 성분을 크기로 나누어 정규화
        matrix[(uint)columnIndex, 0] = x / magnitude;
        matrix[(uint)columnIndex, 1] = y / magnitude;
        matrix[(uint)columnIndex, 2] = z / magnitude;
        matrix[(uint)columnIndex, 3] = w / magnitude;
    }

    /// <summary>
    /// 지정된 열의 크기(길이)를 계산합니다.
    /// </summary>
    /// <param name="matrix">행렬</param>
    /// <param name="columnIndex">열 인덱스 (0-3)</param>
    /// <returns>열의 크기</returns>
    /// <exception cref="ArgumentOutOfRangeException">columnIndex가 0-3 범위를 벗어날 때</exception>
    public static float GetColumnMagnitude(this Matrix4x4f matrix, int columnIndex)
    {
        if (columnIndex < 0 || columnIndex > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(columnIndex), "열 인덱스는 0부터 3까지의 값이어야 합니다.");
        }

        float x = matrix[(uint)columnIndex, 0];
        float y = matrix[(uint)columnIndex, 1];
        float z = matrix[(uint)columnIndex, 2];
        float w = matrix[(uint)columnIndex, 3];

        return (float)Math.Sqrt(x * x + y * y + z * z + w * w);
    }

    /// <summary>
    /// 지정된 열이 단위벡터인지 확인합니다.
    /// </summary>
    /// <param name="matrix">확인할 행렬</param>
    /// <param name="columnIndex">확인할 열의 인덱스 (0-3)</param>
    /// <param name="tolerance">허용 오차 (기본값: 1e-6f)</param>
    /// <returns>열이 단위벡터이면 true, 아니면 false</returns>
    /// <exception cref="ArgumentOutOfRangeException">columnIndex가 0-3 범위를 벗어날 때</exception>
    public static bool IsColumnNormalized(this Matrix4x4f matrix, int columnIndex, float tolerance = 1e-6f)
    {
        float magnitude = matrix.GetColumnMagnitude(columnIndex);
        return Math.Abs(magnitude - 1.0f) <= tolerance;
    }
}
