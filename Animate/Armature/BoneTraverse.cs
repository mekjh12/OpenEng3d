using System;
using System.Collections.Generic;

namespace Animate
{
    /// <summary>
    /// Bone 클래스의 확장 메서드들
    /// BFS(너비 우선 탐색)와 DFS(깊이 우선 탐색) 순회 기능을 제공한다.
    /// </summary>
    public static class BoneTraverse
    {
        // 성능 향상을 위한 재사용 컬렉션들
        private static Queue<Bone> _queue = new Queue<Bone>(); // 뼈대 업데이트를 위한 큐(성능 향상을 위해 재사용 큐 사용)
        private static Stack<Bone> _stack = new Stack<Bone>(); // DFS를 위한 스택(재사용)

        /// <summary>
        /// 너비 우선 탐색(BFS)으로 현재 뼈대부터 시작하여 모든 하위 뼈대를 List로 반환한다.
        /// 성능이 중요한 경우 기존 큐를 재사용하여 메모리 할당을 최소화한다.
        /// </summary>
        /// <param name="bone">시작 뼈대</param>
        /// <returns>BFS 순서로 정렬된 뼈대들의 리스트</returns>
        public static List<Bone> ToBFSList(this Bone bone)
        {
            if (bone == null)
                throw new ArgumentNullException(nameof(bone));

            List<Bone> result = new List<Bone>();
            _queue.Clear(); // 기존 큐 재사용
            _queue.Enqueue(bone);

            while (_queue.Count > 0)
            {
                Bone current = _queue.Dequeue();

                result.Add(current);

                // 현재 뼈대의 모든 자식들을 큐에 추가
                foreach (Bone child in current.Children)
                {
                    _queue.Enqueue(child);
                }
            }

            return result;
        }

        /// <summary>
        /// 특정 뼈대를 제외하고 BFS로 순회하여 List로 반환한다.
        /// </summary>
        /// <param name="bone">시작 뼈대</param>
        /// <param name="exceptBone">순회에서 제외할 뼈대 (null이면 모든 뼈대 포함)</param>
        /// <returns>BFS 순서로 정렬된 뼈대들의 리스트</returns>
        public static List<Bone> ToBFSList(this Bone bone, Bone exceptBone)
        {
            if (bone == null)
                throw new ArgumentNullException(nameof(bone));

            List<Bone> result = new List<Bone>();
            _queue.Clear(); // 기존 큐 재사용
            _queue.Enqueue(bone);

            while (_queue.Count > 0)
            {
                Bone current = _queue.Dequeue();

                // 제외 대상 뼈대가 아닌 경우에만 결과에 추가
                if (current != exceptBone && current.Index>=0)
                {
                    result.Add(current);
                }

                // 현재 뼈대의 모든 자식들을 큐에 추가 (제외 뼈대의 자식들도 순회)
                foreach (Bone child in current.Children)
                {
                    _queue.Enqueue(child);
                }
            }

            return result;
        }

        /// <summary>
        /// 깊이 우선 탐색(DFS)으로 현재 뼈대부터 시작하여 모든 하위 뼈대를 List로 반환한다.
        /// 한 브랜치를 끝까지 탐색한 후 다른 브랜치로 이동한다. (스택 기반 구현)
        /// </summary>
        /// <param name="bone">시작 뼈대</param>
        /// <returns>DFS 순서로 정렬된 뼈대들의 리스트</returns>
        public static List<Bone> ToDFSList(this Bone bone)
        {
            if (bone == null)
                throw new ArgumentNullException(nameof(bone));

            List<Bone> result = new List<Bone>();
            _stack.Clear(); // 기존 스택 재사용
            _stack.Push(bone);

            while (_stack.Count > 0)
            {
                Bone current = _stack.Pop();
                result.Add(current);

                // 자식들을 역순으로 스택에 추가 (왼쪽부터 탐색하기 위해)
                // Children이 IReadOnlyList이므로 역순 접근
                for (int i = current.Children.Count - 1; i >= 0; i--)
                {
                    _stack.Push(current.Children[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// 특정 뼈대를 제외하고 DFS로 순회하여 List로 반환한다.
        /// </summary>
        /// <param name="bone">시작 뼈대</param>
        /// <param name="exceptBone">순회에서 제외할 뼈대 (null이면 모든 뼈대 포함)</param>
        /// <returns>DFS 순서로 정렬된 뼈대들의 리스트</returns>
        public static List<Bone> ToDFSList(this Bone bone, Bone exceptBone)
        {
            if (bone == null)
                throw new ArgumentNullException(nameof(bone));

            List<Bone> result = new List<Bone>();
            _stack.Clear(); // 기존 스택 재사용
            _stack.Push(bone);

            while (_stack.Count > 0)
            {
                Bone current = _stack.Pop();

                // 제외 대상 뼈대가 아닌 경우에만 결과에 추가
                if (current != exceptBone)
                    result.Add(current);

                // 자식들을 역순으로 스택에 추가 (제외 뼈대의 자식들도 순회)
                for (int i = current.Children.Count - 1; i >= 0; i--)
                {
                    _stack.Push(current.Children[i]);
                }
            }

            return result;
        }


    }
}