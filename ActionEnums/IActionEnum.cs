namespace FormTools.Actions
{
    // 공통 인터페이스 정의
    public interface IActionEnum
    {
        int GetValue();
        string GetName();
        bool IsCommonAction(); // 공통 액션인지 확인
    }
}
