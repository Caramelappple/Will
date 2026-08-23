using UnityEngine;

namespace _Scripts.LDY.UI
{
    /// <summary>
    /// 픽셀화 머티리얼 한 장의 수명만 책임진다.
    /// (누구에게 꽂을지는 <see cref="LDY_UIPixelizer"/>, 블록 크기는 전역 셰이더 값의 몫)
    ///
    /// 항상 인스턴스를 새로 굽되 "딱 한 장"만 만든다.
    ///  - 에셋을 직접 쓰면 블록 크기를 만질 때마다 .mat 파일이 더러워진다.
    ///  - 그래픽마다 굽으면 배칭이 깨지고 드로우콜이 폭발한다.
    /// </summary>
    internal sealed class LDY_UIPixelMaterial
    {
        public Material Material { get; private set; }

        public bool IsValid => Material != null;

        public LDY_UIPixelMaterial(Material template, Shader shader)
        {
            if (template != null)
            {
                Material = new Material(template);
            }
            else if (shader != null)
            {
                Material = new Material(shader);
            }
            else
            {
                return;
            }

            // 씬/프리팹에 딸려 저장되면 안 되는 런타임 전용 인스턴스
            Material.hideFlags = HideFlags.HideAndDontSave;
            Material.name = "LDY_UIPixelate (Runtime)";
        }

        public void Dispose()
        {
            if (Material == null) return;

            if (Application.isPlaying) Object.Destroy(Material);
            else Object.DestroyImmediate(Material);

            Material = null;
        }
    }
}
