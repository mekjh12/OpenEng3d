using System;
using System.Collections.Generic;
using System.Xml;

namespace ZetaExt
{
    public static class XMLNode
    {
        /// <summary>
        /// XML 노드를 깊이 우선 순회하여 모든 하위 노드를 반환합니다.
        /// <code>
        /// 파일 구조 그대로의 순서대로 순회한다.
        /// </code>
        /// </summary>
        /// <param name="node">순회할 루트 노드</param>
        /// <returns>현재 노드부터 시작하여 모든 하위 노드를 깊이 우선 순서로 반환하는 열거자</returns>
        public static IEnumerable<(XmlNode parent, XmlNode node)> TraverseXmlNodesWithParent(this XmlNode node)
        {
            return TraverseXmlNodesWithParentInternal(null, node);
        }

        private static IEnumerable<(XmlNode parent, XmlNode node)> TraverseXmlNodesWithParentInternal(XmlNode parent, XmlNode node)
        {
            // 속성이 있는 노드만 반환
            if (node.Attributes != null)
            {
                yield return (parent, node);
            }

            // 자식 노드들은 계속 순회 (속성 유무와 관계없이)
            foreach (XmlNode child in node.ChildNodes)
            {
                foreach (var item in TraverseXmlNodesWithParentInternal(node, child))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// 지정된 속성이 존재하는지 확인합니다.
        /// </summary>
        /// <param name="node">XML 노드</param>
        /// <param name="attributeName">속성 이름</param>
        /// <returns>속성이 존재하면 true, 없으면 false</returns>
        public static bool HasAttribute(this XmlNode node, string attributeName)
        {
            try
            {
                return node.Attributes[attributeName] != null;
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }

        /// <summary>
        /// 지정된 속성 값을 안전하게 가져옵니다.
        /// </summary>
        /// <param name="node">XML 노드</param>
        /// <param name="attributeName">속성 이름</param>
        /// <returns>속성 값 (없으면 null)</returns>
        public static string GetAttributeValueSafe(this XmlNode node, string attributeName)
        {
            try
            {
                return node.Attributes[attributeName]?.Value;
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }
    }
}
