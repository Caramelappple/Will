using _Scripts.LSO.Will;
using UnityEngine;

namespace _Scripts.LSO.UI.Text
{
    /// <summary>
    /// 유언 문구를 물어보는 창구. LSO_AbilityText와 같은 모양이다.
    ///
    /// ── 특성과 다른 점 ────────────────────────────────────────
    /// 특성은 순수 C# 클래스라 문구를 담아둘 데이터 자리가 아예 없었다.
    /// 그래서 사전 에셋(LSO_AbilityCatalog)을 따로 만들었다.
    ///
    /// 유언은 이미 DLJ_WillDataSO가 설명·아이콘·이펙트를 들고 있다.
    /// 이름만 다른 곳에 두면 같은 유언의 문구가 두 파일로 갈린다.
    /// 그래서 이름도 그 에셋에 넣고, 여기는 찾아다 주기만 한다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 유언 에셋을 이미 손에 쥔 곳(LSO_WillNote, DLJ_InfoPanelData)은
    /// 여기를 거치지 말고 will.DisplayName을 바로 읽는 편이 낫다.
    /// 이 창구는 enum만 아는 곳(LSO_AnimalInfoPanel)을 위한 것이다.
    /// </summary>
    public static class LSO_WillText
    {
        /// <summary>DLJ_WillSystem·DLJ_InfoPanel이 쓰는 것과 같은 경로다. 바뀌면 같이 고칠 것.</summary>
        private const string DatabasePath = "DLJ/DLJ_WillDatabase";

        private static DLJ_WillDatabaseSO _database;
        private static bool _searched;

        /// <summary>화면에 띄울 이름. 못 찾으면 영문 enum 이름을 돌려준다.</summary>
        public static string NameOf(LSO_WillType type)
        {
            if (type == LSO_WillType.None) return "없음";

            DLJ_WillDataSO data = DataOf(type);

            return data != null ? data.DisplayName : type.ToString();
        }

        /// <summary>유언 설명. 못 찾거나 안 적어뒀으면 빈 문자열이다.</summary>
        public static string DescriptionOf(LSO_WillType type)
        {
            DLJ_WillDataSO data = DataOf(type);

            return data != null && data.description != null ? data.description : string.Empty;
        }

        /// <summary>유언 아이콘. 없으면 null.</summary>
        public static Sprite IconOf(LSO_WillType type)
        {
            DLJ_WillDataSO data = DataOf(type);

            return data != null ? data.icon : null;
        }

        /// <summary>
        /// 유언 에셋 자체. 없으면 null.
        ///
        /// DLJ_WillDatabaseSO.Get은 못 찾으면 LogError를 낸다. 표시용 조회까지
        /// 에러로 번지지 않게 None은 여기서 먼저 걸러낸다.
        /// </summary>
        public static DLJ_WillDataSO DataOf(LSO_WillType type)
        {
            if (type == LSO_WillType.None) return null;

            LoadIfNeeded();

            return _database != null ? _database.Get(type) : null;
        }

        private static void LoadIfNeeded()
        {
            if (_searched) return;

            _searched = true;
            _database = Resources.Load<DLJ_WillDatabaseSO>(DatabasePath);

            if (_database == null)
            {
                Debug.LogWarning(
                    "유언 데이터베이스를 찾지 못해 영문 이름으로 표시합니다.\n" +
                    $"Assets/Resources/{DatabasePath}.asset 을 확인하세요.");
            }
        }

        /// <summary>
        /// 도메인 리로드를 끈 에디터에서는 static이 지난 플레이의 값을 그대로 들고 있다.
        /// 에셋을 고쳤는데도 옛 값이 나오지 않도록 플레이할 때마다 비운다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            _database = null;
            _searched = false;
        }
    }
}
