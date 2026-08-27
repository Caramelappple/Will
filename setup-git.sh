#!/usr/bin/env bash
# =============================================================
# 팀 git 1회 설정 스크립트
# =============================================================
# 클론한 뒤 한 번만 실행하면 된다. 여러 번 실행해도 안전하다.
#
# 실행 방법 (Windows):
#   프로젝트 폴더에서 우클릭 > "Git Bash Here" > 아래 입력
#     ./setup-git.sh
#
# 하는 일:
#   1. Unity SmartMerge를 머지 드라이버로 등록 (씬/프리팹 충돌 완화)
#   2. TMP 폰트 폴백 에셋을 로컬 변경 무시로 설정
#
# 자세한 배경은 GIT_협업규칙.md 참고.

set -u

cd "$(dirname "$0")" || exit 1

echo "=============================================="
echo " Will 프로젝트 git 설정"
echo "=============================================="
echo

# -------------------------------------------------------------
# 1. Unity SmartMerge 등록
# -------------------------------------------------------------

echo "[1/2] Unity SmartMerge 등록"

# 프로젝트가 쓰는 유니티 버전을 먼저 찾는다.
UNITY_VERSION=""
if [ -f ProjectSettings/ProjectVersion.txt ]; then
    UNITY_VERSION=$(grep '^m_EditorVersion:' ProjectSettings/ProjectVersion.txt | awk '{print $2}' | tr -d '\r')
fi

MERGE_EXE=""

# 프로젝트 버전을 우선 확인하고, 없으면 설치된 아무 버전이나 쓴다.
# (SmartMerge는 버전 간 호환되므로 정확히 일치하지 않아도 동작한다.)
CANDIDATES=""
if [ -n "$UNITY_VERSION" ]; then
    CANDIDATES="/c/Program Files/Unity/Hub/Editor/$UNITY_VERSION/Editor/Data/Tools/UnityYAMLMerge.exe"
fi

for d in "/c/Program Files/Unity/Hub/Editor"/*/Editor/Data/Tools/UnityYAMLMerge.exe \
         "/c/Program Files/Unity/Editor/Data/Tools/UnityYAMLMerge.exe"; do
    CANDIDATES="$CANDIDATES
$d"
done

while IFS= read -r c; do
    [ -z "$c" ] && continue
    if [ -f "$c" ]; then
        MERGE_EXE="$c"
        break
    fi
done <<EOF
$CANDIDATES
EOF

if [ -z "$MERGE_EXE" ]; then
    echo "  ! UnityYAMLMerge.exe를 찾지 못했습니다."
    echo "    유니티가 기본 경로에 설치되어 있지 않은 것 같습니다."
    echo "    직접 찾아서 아래를 실행하세요:"
    echo
    echo "      git config merge.unityyamlmerge.name \"Unity SmartMerge\""
    echo "      git config merge.unityyamlmerge.driver '\"<경로>/UnityYAMLMerge.exe\" merge -p %O %A %B %A'"
    echo "      git config merge.unityyamlmerge.recursive binary"
    echo
else
    # git config에는 윈도우 형식 경로(C:/...)로 넣어야 한다.
    WIN_PATH=$(printf '%s' "$MERGE_EXE" | sed -E 's#^/([a-zA-Z])/#\U\1:/#')

    git config merge.unityyamlmerge.name "Unity SmartMerge"
    git config merge.unityyamlmerge.driver "\"$WIN_PATH\" merge -p %O %A %B %A"
    git config merge.unityyamlmerge.recursive binary

    echo "  OK: $WIN_PATH"
fi

echo

# -------------------------------------------------------------
# 2. TMP 폰트 폴백 에셋 로컬 변경 무시
# -------------------------------------------------------------

echo "[2/2] TMP 폰트 폴백 에셋 추적 제외"

TMP_ASSET="Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset"

if [ ! -f "$TMP_ASSET" ]; then
    echo "  - 파일이 없습니다. 건너뜁니다."
else
    # 이미 로컬에서 재생성되어 더러워져 있으면 먼저 되돌린다.
    # (더러운 채로 skip-worktree를 걸면 나중에 checkout/merge가 막힌다.)
    if ! git diff --quiet -- "$TMP_ASSET" 2>/dev/null; then
        echo "  - 로컬 변경이 있어 저장소 버전으로 되돌립니다."
        git update-index --no-skip-worktree "$TMP_ASSET" 2>/dev/null
        git checkout -- "$TMP_ASSET"
    fi

    git update-index --skip-worktree "$TMP_ASSET"
    echo "  OK: 이제 이 파일의 로컬 변경은 git이 무시합니다."
fi

echo
echo "=============================================="
echo " 완료"
echo "=============================================="
echo
echo "확인:"
git config --get-regexp '^merge\.unityyamlmerge' | sed 's/^/  /'
echo
echo "되돌리려면:"
echo "  git config --remove-section merge.unityyamlmerge"
echo "  git update-index --no-skip-worktree \"$TMP_ASSET\""
