using System;
using System.IO;
using UnityEngine;

namespace _Scripts.LDY.Save
{
    /// <summary>
    /// JsonUtility로 직렬화해 Application.persistentDataPath 아래에 파일로 담는다.
    ///
    /// 쓰기는 항상 임시 파일을 거친 뒤 한 번에 교체한다.
    /// 저장 도중 강제 종료되더라도 기존 파일이 반쯤 쓰인 채로 깨지지 않게 하기 위해서다.
    /// </summary>
    public sealed class LDY_FileSaveRepository : LDY_ISaveRepository
    {
        private const string Extension = ".json";
        private const string TempExtension = ".tmp";

        private readonly string _rootPath;

        public LDY_FileSaveRepository() : this(Application.persistentDataPath) { }

        public LDY_FileSaveRepository(string rootPath)
        {
            _rootPath = rootPath;
        }

        public bool Exists(string key)
        {
            return File.Exists(PathFor(key));
        }

        public T Load<T>(string key) where T : class
        {
            string path = PathFor(key);
            if (!File.Exists(path)) return null;

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return null;

                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                // 깨진 세이브에 예외를 던지면 게임이 아예 켜지지 않는다.
                // null을 돌려주고 새 게임 흐름으로 빠지게 한다.
                Debug.LogError($"[LDY_FileSaveRepository] '{key}' 를 읽지 못했습니다: {e.Message}");
                return null;
            }
        }

        public void Save<T>(string key, T data)
        {
            if (data == null)
            {
                Debug.LogWarning($"[LDY_FileSaveRepository] '{key}' 에 null을 저장하려 했습니다. 무시합니다.");
                return;
            }

            string path = PathFor(key);
            string tempPath = path + TempExtension;

            try
            {
                Directory.CreateDirectory(_rootPath);

                File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));

                // 여기까지 왔으면 임시 파일은 온전하다. 이제 한 번의 교체로 갈아끼운다.
                ReplaceFile(tempPath, path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LDY_FileSaveRepository] '{key}' 를 저장하지 못했습니다: {e.Message}");
                TryDelete(tempPath);
            }
        }

        public void Delete(string key)
        {
            string path = PathFor(key);

            TryDelete(path);
            TryDelete(path + TempExtension);
        }

        /// <summary>
        /// File.Replace는 대상 파일이 없으면 예외를 던진다. 그래서 첫 저장은 Move로 간다.
        /// Replace를 지원하지 않는 플랫폼에서는 삭제 후 Move로 물러선다(이 경로는 원자적이지 않다).
        /// </summary>
        private static void ReplaceFile(string tempPath, string path)
        {
            if (!File.Exists(path))
            {
                File.Move(tempPath, path);
                return;
            }

            try
            {
                File.Replace(tempPath, path, null);
            }
            catch (Exception e) when (e is PlatformNotSupportedException || e is IOException)
            {
                Debug.LogWarning($"[LDY_FileSaveRepository] File.Replace 실패, 삭제 후 교체로 대체합니다: {e.Message}");

                File.Delete(path);
                File.Move(tempPath, path);
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LDY_FileSaveRepository] '{path}' 삭제 실패: {e.Message}");
            }
        }

        private string PathFor(string key)
        {
            return Path.Combine(_rootPath, key + Extension);
        }
    }
}
