using System;
using System.Collections.Generic;

namespace ZetaExt
{
    /// <summary>
    /// List 컬렉션에 대한 확장 메서드를 제공하는 클래스입니다.
    /// </summary>
    public static class ListExtensions
    {
        private static Random _random = new Random();

        /// <summary>
        /// List의 모든 아이템을 랜덤하게 섞습니다.
        /// </summary>
        /// <typeparam name="T">리스트 아이템의 타입</typeparam>
        /// <param name="list">섞을 리스트</param>
        /// <remarks>
        /// Fisher-Yates shuffle 알고리즘을 사용하여 O(n) 시간 복잡도로 구현되었습니다.
        /// </remarks>
        public static void Shuffle<T>(this List<T> list)
        {
            int n = list.Count;
            for (int i = n - 1; i > 0; i--)
            {
                // 0부터 i까지의 랜덤한 인덱스 선택
                int randomIndex = _random.Next(i + 1);

                // 현재 인덱스와 랜덤 인덱스의 아이템을 교환
                T temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        /// <summary>
        /// List의 모든 아이템을 랜덤하게 섞은 새로운 리스트를 반환합니다.
        /// </summary>
        /// <typeparam name="T">리스트 아이템의 타입</typeparam>
        /// <param name="list">섞을 리스트</param>
        /// <returns>섞인 새로운 리스트</returns>
        /// <remarks>
        /// 원본 리스트는 변경되지 않고 새로운 리스트를 반환합니다.
        /// </remarks>
        public static List<T> ToShuffled<T>(this List<T> list)
        {
            List<T> shuffledList = new List<T>(list);
            shuffledList.Shuffle();
            return shuffledList;
        }
    }
}
