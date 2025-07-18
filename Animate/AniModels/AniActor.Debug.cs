using OpenGL;

namespace Animate
{
	public abstract partial class AniActor
	{
		#region 디버깅용 멤버 변수
		PolygonMode _polygonMode = PolygonMode.Fill; // 폴리곤 모드
		RenderingMode _renderingMode = RenderingMode.Animation; // 렌더링 모드
		int _selectedBoneIndex = 0; // 선택된 본 인덱스
		float _axisLength = 10.3f; // 축 길이
		float _drawThick = 1.0f; // 그리기 두께
		#endregion

		#region 디버깅용 열거형
		/// <summary>
		/// 렌더링 모드
		/// </summary>
		public enum RenderingMode { Animation, BoneWeight, Static, None, Count };
		#endregion

		#region 디버깅용 속성
		/// <summary>
		/// 본 개수
		/// </summary>
		public int BoneCount => _aniRig.DicBones.Count;

		/// <summary>
		/// 선택된 본 인덱스
		/// </summary>
		public int SelectedBoneIndex
		{
			get => _selectedBoneIndex;
			set => _selectedBoneIndex = value;
		}

		/// <summary>
		/// 폴리곤 모드
		/// </summary>
		public PolygonMode PolygonMode
		{
			get => _polygonMode;
			set => _polygonMode = value;
		}

		/// <summary>
		/// 렌더링 모드
		/// </summary>
		public RenderingMode RenderMode
		{
			get => _renderingMode;
			set => _renderingMode = value;
		}
		#endregion

		#region 디버깅용 메서드
		/// <summary>
		/// 폴리곤 모드를 순환한다.
		/// </summary>
		public void PopPolygonMode()
		{
			_polygonMode++;
			if (_polygonMode >= (PolygonMode)6915) _polygonMode = (PolygonMode)6912;
		}

		/// <summary>
		/// 렌더링 모드를 순환한다.
		/// </summary>
		public void PopPolygonModeMode()
		{
			_renderingMode++;
			if (_renderingMode == RenderingMode.Count - 1) _renderingMode = 0;
		}
		#endregion
	}
}