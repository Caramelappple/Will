using UnityEngine;

namespace _Scripts.LSO.UI.Popup
{
    /// <summary>
    /// 월드 좌표 위에 짧은 문구를 띄우는 창구.
    ///
    /// 요청하는 쪽(기물, 특성, 유언)은 풀링도 좌표 변환도 몰라야 한다.
    /// "여기에 이 글자를 이 색으로 띄워줘"까지만 안다.
    /// </summary>
    public interface LSO_IDamagePopupSpawner
    {
        void Spawn(Vector3 worldPosition, string text, Color color);
    }
}
