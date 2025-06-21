using System;
using System.Collections.Generic;
using Geometry;
using Occlusion;
using ZetaExt;

namespace Common.Geometry
{
    /// <summary>
    /// BVH 트리의 균형성을 분석하여 다양한 균형 지표를 포함한 보고서를 생성하는 클래스입니다.
    /// 균형성은 세 가지 주요 측면에서 평가됩니다:
    /// 1. 높이 균형성 (Height Balance)
    /// 2. 노드 분포 균형성 (Node Distribution Balance)
    /// 3. 공간 분할 균형성 (Spatial Balance)
    /// </summary>
    public class TreeBalanceInfo
    {
        /// <summary>트리의 높이 균형성 지표 (0~1, 1이 완벽한 균형)</summary>
        public float HeightBalanceFactor { get; set; }

        /// <summary>노드 분포의 균형성 지표 (0~1, 1이 완벽한 균형)</summary>
        public float NodeDistributionFactor { get; set; }

        /// <summary>공간 분할의 균형성 지표 (0~1, 1이 완벽한 균형)</summary>
        public float SpatialBalanceFactor { get; set; }

        /// <summary>전체 균형성 지표 (0~1, 1이 완벽한 균형)</summary>
        public float OverallBalanceFactor { get; set; }

        /// <summary>트리의 최대 깊이</summary>
        public int MaxDepth { get; set; }

        /// <summary>트리의 최소 깊이</summary>
        public int MinDepth { get; set; }

        /// <summary>전체 노드 수</summary>
        public int TotalNodes { get; set; }

        /// <summary>리프 노드 수</summary>
        public int LeafNodes { get; set; }
    }

    public static class BVHBalanceAnalyzer
    {
        /// <summary>
        /// BVH 트리의 균형성을 분석하여 상세한 균형성 정보를 반환합니다.
        /// </summary>
        /// <remarks>
        /// 이 분석은 다음 세 가지 주요 측면에서 트리의 균형성을 평가합니다:
        /// 
        /// 1. 높이 균형성 (HeightBalanceFactor)
        ///    - 트리의 최대 깊이와 최소 깊이의 차이를 분석
        ///    - 이상적인 이진 트리의 이론적 최소 깊이와 비교
        ///    - 깊이 차이가 작을수록 높은 점수 (0~1)
        ///    
        /// 2. 노드 분포 균형성 (NodeDistributionFactor)
        ///    - 각 노드의 왼쪽/오른쪽 서브트리의 노드 수 비율 분석
        ///    - 50:50 비율에 가까울수록 높은 점수
        ///    - 노드 분포가 균등할수록 높은 점수 (0~1)
        ///    
        /// 3. 공간 분할 균형성 (SpatialBalanceFactor)
        ///    - AABB의 면적 비율을 기반으로 공간 분할의 균형성 분석
        ///    - 자식 노드들의 AABB 면적이 비슷할수록 높은 점수
        ///    - 공간이 균등하게 분할될수록 높은 점수 (0~1)
        ///    
        /// 전체 균형성 점수(OverallBalanceFactor)는 위 세 가지 요소의 가중 평균으로 계산됩니다:
        /// - 높이 균형성: 40% 가중치
        /// - 노드 분포: 30% 가중치
        /// - 공간 분할: 30% 가중치
        /// </remarks>
        /// <returns>트리의 균형성 분석 정보를 포함한 TreeBalanceInfo 객체</returns>
        /// <example>
        /// <code>
        /// BVH bvh = new BVH();
        /// // ... BVH 트리 구성 ...
        /// TreeBalanceInfo balanceInfo = bvh.AnalyzeBalance();
        /// Console.WriteLine(balanceInfo.GetBalanceReport());
        /// </code>
        /// </example>
        public static TreeBalanceInfo AnalyzeBalance(this BVH bvh)
        {
            if (bvh.IsEmpty)
                return new TreeBalanceInfo();

            var info = new TreeBalanceInfo();
            info.TotalNodes = (int)bvh.NodeCount;
            info.LeafNodes = (int)bvh.CountLeaf;

            // 1. 높이 균형성 분석
            AnalyzeHeightBalance(bvh.Root, info);

            // 2. 노드 분포 균형성 분석
            AnalyzeNodeDistribution(bvh.Root, info);

            // 3. 공간 분할 균형성 분석
            AnalyzeSpatialBalance(bvh.Root, info);

            // 전체 균형성 점수 계산 (각 요소의 가중 평균)
            info.OverallBalanceFactor = (
                info.HeightBalanceFactor * 0.4f +    // 높이 균형성 가중치
                info.NodeDistributionFactor * 0.3f + // 노드 분포 가중치
                info.SpatialBalanceFactor * 0.3f     // 공간 분할 가중치
            );

            return info;
        }

        /// <summary>
        /// 트리의 높이 균형성을 분석합니다.
        /// </summary>
        private static void AnalyzeHeightBalance(Node root, TreeBalanceInfo info)
        {
            // 최대/최소 깊이 초기화
            info.MaxDepth = 0;
            info.MinDepth = int.MaxValue;

            // 깊이 분석을 위한 DFS
            AnalyzeDepth(root, 0, info);

            // 높이 균형성 계산
            // 완벽한 이진트리의 경우 MaxDepth와 MinDepth가 같거나 1 차이
            float theoreticalMinDepth = (float)Math.Floor(Math.Log(info.LeafNodes, 2));
            float depthDifferenceRatio = (info.MaxDepth - theoreticalMinDepth) / theoreticalMinDepth;
            info.HeightBalanceFactor = 1.0f / (1.0f + depthDifferenceRatio);
        }

        private static void AnalyzeDepth(Node node, int currentDepth, TreeBalanceInfo info)
        {
            if (node == null) return;

            if (node.IsLeaf)
            {
                info.MaxDepth = Math.Max(info.MaxDepth, currentDepth);
                info.MinDepth = Math.Min(info.MinDepth, currentDepth);
                return;
            }

            AnalyzeDepth(node.Child1, currentDepth + 1, info);
            AnalyzeDepth(node.Child2, currentDepth + 1, info);
        }

        /// <summary>
        /// 노드의 분포 균형성을 분석합니다.
        /// </summary>
        private static void AnalyzeNodeDistribution(Node root, TreeBalanceInfo info)
        {
            if (root == null) return;

            // 각 서브트리의 노드 수 계산
            int leftCount = CountNodes(root.Child1);
            int rightCount = CountNodes(root.Child2);
            float totalNodes = leftCount + rightCount;

            if (totalNodes > 0)
            {
                // 이상적인 분포는 50:50
                float idealRatio = 0.5f;
                float actualRatio = Math.Min(leftCount, rightCount) / totalNodes;
                info.NodeDistributionFactor = actualRatio / idealRatio;
            }
            else
            {
                info.NodeDistributionFactor = 1.0f; // 리프 노드의 경우
            }
        }

        private static int CountNodes(Node node)
        {
            if (node == null) return 0;
            return 1 + CountNodes(node.Child1) + CountNodes(node.Child2);
        }

        /// <summary>
        /// 공간 분할의 균형성을 분석합니다.
        /// </summary>
        private static void AnalyzeSpatialBalance(Node root, TreeBalanceInfo info)
        {
            if (root == null) return;

            // 공간 분할의 균형성 계산
            if (!root.IsLeaf && root.Child1 != null && root.Child2 != null)
            {
                float parent_area = root.AABB.Area;
                float child1_area = root.Child1.AABB.Area;
                float child2_area = root.Child2.AABB.Area;

                // 자식 노드들의 면적 비율 계산
                float areaRatio = Math.Min(child1_area, child2_area) / Math.Max(child1_area, child2_area);
                info.SpatialBalanceFactor = areaRatio;
            }
            else
            {
                info.SpatialBalanceFactor = 1.0f; // 리프 노드의 경우
            }
        }

        public static string GetBalanceReport(this TreeBalanceInfo info)
        {
            return $@"BVH Tree Balance Analysis Report:
----------------------------------------
Total Nodes: {info.TotalNodes}
Leaf Nodes: {info.LeafNodes}
Tree Depth: {info.MinDepth} ~ {info.MaxDepth}

Balance Factors (0 = unbalanced, 1 = perfectly balanced):
- Height Balance: {info.HeightBalanceFactor:F3}
- Node Distribution: {info.NodeDistributionFactor:F3}
- Spatial Balance: {info.SpatialBalanceFactor:F3}
- Overall Balance: {info.OverallBalanceFactor:F3}

Assessment: {GetBalanceAssessment(info.OverallBalanceFactor)}";
        }

        private static string GetBalanceAssessment(float overallBalance)
        {
            if (overallBalance >= 0.9f) return "Excellent balance";
            if (overallBalance >= 0.7f) return "Good balance";
            if (overallBalance >= 0.5f) return "Fair balance";
            if (overallBalance >= 0.3f) return "Poor balance";
            return "Severe imbalance";
        }
    }
}