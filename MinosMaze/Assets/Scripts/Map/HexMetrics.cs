using UnityEngine;

// 六边形几何常量定义，供 HexGrid 和 HexPathfinder 使用
public static class HexMetrics {

	// 六边形外接圆半径（顶点到中心），控制六角格的大小
	public const float outerRadius = 4.5f;

	// 六边形内切圆半径（边中点到中心）= outerRadius * √3/2（≈0.866）
	// 用于将六角格整齐排列（水平方向两列之间的间距）
	public const float innerRadius = outerRadius * 0.866025404f;
    
}
