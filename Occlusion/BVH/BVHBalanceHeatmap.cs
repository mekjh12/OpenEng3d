using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace Occlusion.Visualization
{
    /// <summary>
    /// BVH 트리의 균형도를 히트맵으로 시각화
    /// 각 리프 노드를 하나의 픽셀로 표현하여 깊이에 따라 색상 표시
    /// </summary>
    public class BVHBalanceHeatmap
    {
        /// <summary>
        /// 리프 노드 정보 (순서 + 깊이)
        /// </summary>
        private class LeafInfo
        {
            public int Index { get; set; }      // 리프의 순서 (0, 1, 2, ...)
            public int Depth { get; set; }      // 해당 리프의 깊이
            public Node3f Node { get; set; }    // 원본 노드 참조
        }

        /// <summary>
        /// BVH 트리를 히트맵 이미지로 저장
        /// </summary>
        /// <param name="root">BVH 루트 노드</param>
        /// <param name="filename">저장할 파일명</param>
        /// <param name="width">이미지 가로 크기 (픽셀 수)</param>
        /// <param name="height">이미지 세로 크기 (픽셀 수)</param>
        public static void SaveHeatmap(Node3f root, string filename, int width = 800, int height = 600)
        {
            if (root == null)
            {
                Console.WriteLine("[ERROR] 루트 노드가 null입니다.");
                return;
            }

            // 1. 모든 리프 노드 추출 (왼쪽 -> 오른쪽 순서)
            List<LeafInfo> leaves = ExtractLeavesInOrder(root);
            int leafCount = leaves.Count;

            if (leafCount == 0)
            {
                Console.WriteLine("[ERROR] 리프 노드가 없습니다.");
                return;
            }

            // 2. 통계 계산
            int minDepth = leaves.Min(l => l.Depth);
            int maxDepth = leaves.Max(l => l.Depth);
            double avgDepth = leaves.Average(l => l.Depth);
            double idealDepth = Math.Log(leafCount, 2);
            double imbalance = (maxDepth - idealDepth) / Math.Max(idealDepth, 1.0);

            Console.WriteLine("====== BVH 균형도 분석 ======");
            Console.WriteLine($"총 리프 수: {leafCount}");
            Console.WriteLine($"최소 깊이: {minDepth}");
            Console.WriteLine($"최대 깊이: {maxDepth}");
            Console.WriteLine($"평균 깊이: {avgDepth:F2}");
            Console.WriteLine($"이상적 깊이: {idealDepth:F2}");
            Console.WriteLine($"불균형 지수: {imbalance:F3}");
            Console.WriteLine("============================\n");

            // 3. 이미지 생성
            using (Bitmap bitmap = new Bitmap(width, height))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);

                // 4. 히트맵 그리기
                DrawHeatmap(g, leaves, width, height, minDepth, maxDepth);

                // 7. 저장
                bitmap.Save(filename, ImageFormat.Png);
                Console.WriteLine($"[SUCCESS] 히트맵 저장: {filename}");
            }
        }

        /// <summary>
        /// 트리를 중위 순회하여 리프 노드를 순서대로 추출
        /// </summary>
        private static List<LeafInfo> ExtractLeavesInOrder(Node3f root)
        {
            List<LeafInfo> leaves = new List<LeafInfo>();
            int index = 0;
            InOrderTraversal(root, leaves, ref index, 0);
            return leaves;
        }

        /// <summary>
        /// 중위 순회 (In-order traversal)
        /// 왼쪽 -> 현재 -> 오른쪽
        /// </summary>
        private static void InOrderTraversal(Node3f node, List<LeafInfo> leaves, ref int index, int depth)
        {
            if (node == null) return;

            if (node.IsLeaf)
            {
                leaves.Add(new LeafInfo
                {
                    Index = index++,
                    Depth = depth,
                    Node = node
                });
            }
            else
            {
                if (node.Child1 != null)
                    InOrderTraversal(node.Child1, leaves, ref index, depth + 1);

                if (node.Child2 != null)
                    InOrderTraversal(node.Child2, leaves, ref index, depth + 1);
            }
        }


        /// <summary>
        /// 히트맵 그리기 (각 리프를 2D 그리드의 픽셀로 표현)
        /// </summary>
        private static void DrawHeatmap(Graphics g, List<LeafInfo> leaves, int width, int height, int minDepth, int maxDepth)
        {
            int leafCount = leaves.Count;

            // 2D 그리드 크기 계산 (정사각형에 가깝게)
            int cols = (int)Math.Ceiling(Math.Sqrt(leafCount));
            int rows = (int)Math.Ceiling((double)leafCount / cols);

            // 픽셀 크기 계산
            float pixelWidth = (float)width / cols;
            float pixelHeight = (float)height / rows;
            float pixelSize = Math.Min(pixelWidth, pixelHeight);

            // 중앙 정렬을 위한 오프셋
            float offsetx = (width - (cols * pixelSize)) / 2;
            float offsetY = (height - (rows * pixelSize)) / 2;

            // 각 리프를 2D 그리드의 픽셀로 그리기
            for (int i = 0; i < leafCount; i++)
            {
                LeafInfo leaf = leaves[i];

                int row = i / cols;
                int col = i % cols;

                float x = offsetx + col * pixelSize;
                float y = offsetY + row * pixelSize;

                // 깊이에 따른 색상 계산
                Color color = GetDepthColor(leaf.Depth, minDepth, maxDepth);

                using (SolidBrush brush = new SolidBrush(color))
                {
                    g.FillRectangle(brush, x, y, pixelSize + 1, pixelSize + 1);
                }
            }
        }

        /// <summary>
        /// 깊이를 색상으로 변환 (파란색 -> 초록 -> 노랑 -> 빨강)
        /// </summary>
        private static Color GetDepthColor(int depth, int minDepth, int maxDepth)
        {
            if (maxDepth == minDepth)
                return Color.FromArgb(0, 255, 0); // 모두 같은 깊이면 초록

            // 정규화 (0.0 ~ 1.0)
            float normalized = (float)(depth - minDepth) / (maxDepth - minDepth);

            // HSV 색상 공간 사용 (Hue: 240° -> 0°)
            // 240° = 파란색 (얕은 깊이)
            // 0° = 빨간색 (깊은 깊이)
            float hue = 240f * (1f - normalized); // 240 -> 0

            return ColorFromHSV(hue, 1.0f, 1.0f);
        }

        /// <summary>
        /// HSV를 RGB로 변환
        /// </summary>
        private static Color ColorFromHSV(float hue, float saturation, float value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            float f = hue / 60 - (float)Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            switch (hi)
            {
                case 0: return Color.FromArgb(255, v, t, p);
                case 1: return Color.FromArgb(255, q, v, p);
                case 2: return Color.FromArgb(255, p, v, t);
                case 3: return Color.FromArgb(255, p, q, v);
                case 4: return Color.FromArgb(255, t, p, v);
                default: return Color.FromArgb(255, v, p, q);
            }
        }

        /// <summary>
        /// 컬러바 그리기
        /// </summary>
        private static void DrawColorBar(Graphics g, int width, int height, int minDepth, int maxDepth)
        {
            int barx = 50;
            int barY = height - 120;
            int barWidth = width - 100;
            int barHeight = 30;

            // 그라데이션 컬러바
            for (int i = 0; i < barWidth; i++)
            {
                float normalized = (float)i / barWidth;
                int depth = (int)(minDepth + normalized * (maxDepth - minDepth));
                Color color = GetDepthColor(depth, minDepth, maxDepth);

                using (Pen pen = new Pen(color, 1))
                {
                    g.DrawLine(pen, barx + i, barY, barx + i, barY + barHeight);
                }
            }

            // 테두리
            using (Pen borderPen = new Pen(Color.Black, 2))
            {
                g.DrawRectangle(borderPen, barx, barY, barWidth, barHeight);
            }

            // 레이블
            using (Font font = new Font("Arial", 10, FontStyle.Bold))
            using (SolidBrush brush = new SolidBrush(Color.Black))
            {
                g.DrawString($"Depth: {minDepth}", font, brush, barx, barY + barHeight + 5);
                g.DrawString($"{maxDepth}", font, brush, barx + barWidth - 30, barY + barHeight + 5);

                // 중간값들
                for (int i = 1; i <= 3; i++)
                {
                    int depth = minDepth + (maxDepth - minDepth) * i / 4;
                    float x = barx + barWidth * i / 4f;
                    g.DrawString($"{depth}", font, brush, x - 10, barY + barHeight + 5);
                    g.DrawLine(Pens.Black, x, barY + barHeight, x, barY + barHeight + 5);
                }
            }
        }

        /// <summary>
        /// 통계 정보 그리기
        /// </summary>
        private static void DrawStatistics(Graphics g, int leafCount, int minDepth, int maxDepth,
            double avgDepth, double idealDepth, double imbalance, int width, int height)
        {
            using (Font titleFont = new Font("Arial", 14, FontStyle.Bold))
            using (Font textFont = new Font("Arial", 11))
            using (SolidBrush brush = new SolidBrush(Color.Black))
            {
                int startY = height - 80;

                string title = "BVH Tree Balance Heatmap";
                SizeF titleSize = g.MeasureString(title, titleFont);
                g.DrawString(title, titleFont, brush, (width - titleSize.Width) / 2, startY - 30);

                string stats = $"Leaves: {leafCount}  |  Min/Max/Avg Depth: {minDepth}/{maxDepth}/{avgDepth:F1}  |  " +
                              $"Ideal: {idealDepth:F1}  |  Imbalance: {imbalance:F3}";

                SizeF statsSize = g.MeasureString(stats, textFont);
                g.DrawString(stats, textFont, brush, (width - statsSize.Width) / 2, startY);

                // 불균형 평가
                string evaluation = GetImbalanceEvaluation(imbalance);
                Color evalColor = GetEvaluationColor(imbalance);
                using (SolidBrush evalBrush = new SolidBrush(evalColor))
                {
                    SizeF evalSize = g.MeasureString(evaluation, textFont);
                    g.DrawString(evaluation, textFont, evalBrush, (width - evalSize.Width) / 2, startY + 20);
                }
            }
        }

        /// <summary>
        /// 불균형 지수에 따른 평가 문구
        /// </summary>
        private static string GetImbalanceEvaluation(double imbalance)
        {
            if (imbalance < 0.1) return "⭐ Excellent Balance";
            if (imbalance < 0.2) return "✓ Good Balance";
            if (imbalance < 0.4) return "⚠ Moderate Imbalance";
            if (imbalance < 0.6) return "⚠⚠ Poor Balance";
            return "❌ Critical Imbalance - Rebuild Recommended";
        }

        /// <summary>
        /// 불균형 지수에 따른 평가 색상
        /// </summary>
        private static Color GetEvaluationColor(double imbalance)
        {
            if (imbalance < 0.1) return Color.FromArgb(76, 175, 80);   // 초록
            if (imbalance < 0.2) return Color.FromArgb(139, 195, 74);  // 연두
            if (imbalance < 0.4) return Color.FromArgb(255, 193, 7);   // 노랑
            if (imbalance < 0.6) return Color.FromArgb(255, 152, 0);   // 주황
            return Color.FromArgb(244, 67, 54);                        // 빨강
        }

        /// <summary>
        /// 여러 BVH 트리를 비교하는 히트맵 생성
        /// </summary>
        public static void SaveComparisonHeatmap(List<Node3f> roots, List<string> labels,
            string filename, int width = 1200, int height = 800)
        {
            if (roots == null || roots.Count == 0)
            {
                Console.WriteLine("[ERROR] 비교할 트리가 없습니다.");
                return;
            }

            int treeCount = roots.Count;
            int rowHeight = (height - 100) / treeCount;

            using (Bitmap bitmap = new Bitmap(width, height))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);

                for (int i = 0; i < treeCount; i++)
                {
                    Node3f root = roots[i];
                    string label = (labels != null && i < labels.Count) ? labels[i] : $"Tree {i + 1}";

                    if (root == null) continue;

                    List<LeafInfo> leaves = ExtractLeavesInOrder(root);
                    int minDepth = leaves.Min(l => l.Depth);
                    int maxDepth = leaves.Max(l => l.Depth);

                    int yOffset = i * rowHeight;

                    // 레이블
                    using (Font font = new Font("Arial", 12, FontStyle.Bold))
                    using (SolidBrush brush = new SolidBrush(Color.Black))
                    {
                        g.DrawString(label, font, brush, 10, yOffset + 5);
                    }

                    // 히트맵
                    int leafCount = leaves.Count;
                    float pixelWidth = (float)(width - 150) / leafCount;
                    float pixelHeight = rowHeight - 10;

                    for (int j = 0; j < leafCount; j++)
                    {
                        LeafInfo leaf = leaves[j];
                        float x = 120 + j * pixelWidth;
                        Color color = GetDepthColor(leaf.Depth, minDepth, maxDepth);

                        using (SolidBrush brush = new SolidBrush(color))
                        {
                            g.FillRectangle(brush, x, yOffset + 5, pixelWidth + 1, pixelHeight);
                        }
                    }

                    // 통계
                    using (Font font = new Font("Arial", 9))
                    using (SolidBrush brush = new SolidBrush(Color.Black))
                    {
                        double avgDepth = leaves.Average(l => l.Depth);
                        double idealDepth = Math.Log(leafCount, 2);
                        double imbalance = (maxDepth - idealDepth) / Math.Max(idealDepth, 1.0);
                        string stats = $"Depth: {minDepth}-{maxDepth} (Avg: {avgDepth:F1}), Imbalance: {imbalance:F3}";
                        g.DrawString(stats, font, brush, 120, yOffset + rowHeight - 20);
                    }
                }

                bitmap.Save(filename, ImageFormat.Png);
                Console.WriteLine($"[SUCCESS] 비교 히트맵 저장: {filename}");
            }
        }
    }
}