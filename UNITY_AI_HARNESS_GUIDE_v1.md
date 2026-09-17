# Unity AI Harness 운영 가이드 v1

> 대상: 개인 ~ 6인 이하의 소규모 Unity 게임 개발 팀  
> 목적: Codex, Claude Code, Antigravity, 로컬 모델을 혼용하면서 토큰 낭비와 코드 탐색 시간을 줄이고, 비개발자 게임 기획자도 운영 가능한 AI 개발 프로세스를 만든다.  
> 기준일: 2026-09-17

---

# 0. 최종 추천 한 줄

**Harness를 개발 시스템의 중심으로 두고, VS Code를 기본 작업대로 사용하며, 한 작업에는 기본적으로 AI 1개만 사용한다. Orca는 여러 독립 작업을 병렬 처리하거나 어려운 문제를 여러 모델로 비교할 때만 선택적으로 사용한다.**

```text
기획자 / 개발자
      ↓
  요구사항(Task)
      ↓
   Harness
 ┌────┼──────────────┐
 │    │              │
지도  작업 절차       자동 검사
 │    │              │
 ↓    ↓              ↓
Domain Skill        Tests
 Map
      ↓
Codex / Claude / Antigravity / Local 중 1개
      ↓
구현
      ↓
자동 검사
      ↓
Unity 실제 검증
      ↓
완료
```

Orca는 이 구조의 위에 항상 존재하지 않는다.

```text
평상시
Harness → AI 1개 → Test → Unity

병렬 작업이나 난제
Harness → Orca → 여러 AI / 여러 Worktree → 결과 선택 → Unity
```

---

# 1. 각 도구의 역할

## Harness
프로젝트의 **AI 개발 업무 규칙과 검사 시스템**이다.

- AI가 어디를 먼저 봐야 하는지 안내
- 게임 시스템별 관련 파일 지도 제공
- 반복 작업 절차 제공
- AI가 수정할 수 있는 범위 제한
- 테스트 실행 방법 제공
- 작업 결과 기록
- 모델이 달라져도 같은 개발 절차 유지

Harness가 핵심이고 Codex, Claude, Antigravity, Orca는 Harness를 이용하는 도구다.

## VS Code
기본 작업대.

- Codex, Claude Code, Antigravity를 필요할 때 골라 사용
- Git / Terminal / Worktree 관리
- Unity C# 개발
- 특정 AI IDE에 프로젝트 구조를 종속시키지 않음

원칙: **한 번에 필요한 AI만 켠다.**

## Codex / Claude Code / Antigravity / Local Model
실제 작업자.

기본 원칙:

```text
1 Task = 1 Main Agent
```

예:
- 일반 구현 → Codex
- 복잡한 분석 / 설계 검토 → Claude
- Antigravity가 유리한 작업 → Antigravity
- 단순 분류 / 요약 / 문서화 → 로컬 모델

## Orca
상시 개발환경이 아니라 **선택적 병렬 작업 도구**로 사용한다.

사용:
- 독립 기능 2개 이상 동시 진행
- Worktree 여러 개 관리
- 한 AI가 반복 실패한 난제
- 여러 모델의 해결책 비교가 가치 있음

사용하지 않음:
- 단순 버그
- UI 텍스트 수정
- 명확한 기능
- 문서 작업
- 한 AI로 충분한 작업

---

# 2. 프로젝트 권장 구조

```text
UnityProject/
│
├─ AGENTS.md
├─ CLAUDE.md
│
├─ docs/
│  ├─ architecture.md
│  └─ domains/
│     ├─ INDEX.md
│     ├─ player.md
│     ├─ combat.md
│     ├─ enemy.md
│     ├─ ui.md
│     └─ save.md
│
├─ tasks/
│  ├─ active/
│  └─ done/
│
├─ .agents/
│  ├─ skills/
│  │  ├─ implement-feature/
│  │  │  └─ SKILL.md
│  │  ├─ investigate-bug/
│  │  │  └─ SKILL.md
│  │  ├─ review-change/
│  │  │  └─ SKILL.md
│  │  ├─ unity-test/
│  │  │  └─ SKILL.md
│  │  └─ update-domain-map/
│  │     └─ SKILL.md
│  │
│  └─ mcp_config.json
│
├─ .claude/
│  ├─ settings.json
│  ├─ settings.local.json       # Git 제외
│  └─ skills                    # 로컬 symlink, Git 제외
│
├─ .codex/
│  └─ config.toml
│
├─ .mcp.json                    # Claude 프로젝트 MCP
│
├─ harness/
│  ├─ README.md
│  └─ mcp/
│     └─ README.md
│
├─ scripts/
│  ├─ setup-links.ps1
│  ├─ load-env.ps1
│  ├─ verify-env.ps1
│  ├─ verify-fast.ps1
│  └─ verify-unity.ps1
│
├─ .env                         # Git 제외
├─ .env.example
└─ .gitignore
```

macOS/Linux 팀이면 `.sh` 버전의 스크립트를 추가한다.

---

# 3. AGENTS.md

`AGENTS.md`는 긴 설명서가 아니라 **AI가 어디로 가야 하는지 알려주는 지도**다.

예:

```md
# Project AI Guide

## Start Here
작업 시작 전 관련 Domain 문서를 확인한다.

- Player → docs/domains/player.md
- Combat → docs/domains/combat.md
- Enemy → docs/domains/enemy.md
- UI → docs/domains/ui.md
- Save → docs/domains/save.md

전체 목록:
docs/domains/INDEX.md

## Rules
1. 관련 Domain 문서를 먼저 읽는다.
2. Domain 문서로 부족할 때만 코드 검색을 한다.
3. 요청 범위 밖의 리팩터링을 하지 않는다.
4. Scene / Prefab / ProjectSettings 수정은 꼭 필요한 경우만 한다.
5. 작업 완료 전 테스트한다.
6. 테스트 실패 시 완료라고 보고하지 않는다.
7. 핵심 구조가 바뀌면 Domain Map 갱신 여부를 확인한다.

## Workflow
반복 작업에는 `.agents/skills/`의 Skill을 사용한다.

## Secrets
실제 API Key, Token, Password를 Git 추적 파일에 기록하지 않는다.
비밀값은 프로젝트 루트 `.env`에서 관리한다.
```

---

# 4. Domain Map

가장 중요한 토큰 절약 장치 중 하나다.

```text
Task
 ↓
AGENTS.md
 ↓
관련 Domain 확인
 ↓
Domain Map
 ↓
핵심 파일 확인
 ↓
필요할 때만 좁은 검색
```

검색을 없애는 것이 아니라 **검색 범위를 먼저 줄이는 것**이 목적이다.

## 예: docs/domains/player.md

```md
# Player Domain

## 역할
플레이어 이동, 점프, 입력 및 기본 상태를 담당한다.

## 핵심 파일
| 역할 | 경로 |
|---|---|
| 이동 | Assets/Scripts/Player/PlayerMovement.cs |
| 점프 | Assets/Scripts/Player/PlayerJump.cs |
| 입력 | Assets/Scripts/Player/PlayerInput.cs |
| 설정 | Assets/Data/PlayerConfig.asset |
| Prefab | Assets/Prefabs/Player.prefab |
| 테스트 | Assets/Tests/Player/ |

## 관련 Domain
- Combat
- Input
- Animation

## 수정 주의
- Player.prefab 변경은 Unity 검증 필요
- Input System 구조를 임의로 바꾸지 않는다
- ProjectSettings 수정 금지

## 검색이 필요한 경우
위 핵심 파일에서 필요한 정보를 찾지 못할 때만
`Assets/Scripts/Player/`와 관련 테스트부터 검색한다.
전체 프로젝트 검색은 마지막 수단으로 사용한다.
```

관리 원칙:
- 사람이 역할, 관계, 핵심 파일, 주의점을 관리
- 자동 검사는 문서에 적힌 파일의 존재 여부를 확인
- 모든 파일을 적지 말고 핵심만 기록

---

# 5. Skill 구조

Skill은 **게임 지식이 아니라 반복 작업 방법**을 담는다.

추천:
```text
implement-feature
investigate-bug
review-change
unity-test
update-domain-map
build-android
```

## 예: implement-feature/SKILL.md

```md
---
name: implement-feature
description: 새 Unity 게임 기능을 구현할 때 사용하는 표준 절차.
---

# Implement Feature
1. AGENTS.md를 확인한다.
2. 관련 Domain 문서를 읽는다.
3. Domain Map의 핵심 파일부터 확인한다.
4. 부족할 때만 좁은 검색을 한다.
5. 수정 파일과 영향을 짧게 정리한다.
6. 요청 범위 안에서 구현한다.
7. 관련 테스트를 추가/수정한다.
8. 빠른 검증을 실행한다.
9. Unity 검증 필요 여부를 판단한다.
10. 핵심 구조가 바뀌었으면 Domain Map을 갱신한다.
11. 변경 파일, 테스트 결과, 남은 위험을 Task에 기록한다.
```

긴 자료는 `references/`, `scripts/`, `examples/`로 분리한다.

---

# 6. Skill 공통 원본과 symlink

공통 Skill 원본:

```text
.agents/skills/
```

Codex와 Antigravity는 이 위치를 직접 사용한다.

Claude는 `.claude/skills/`를 사용하므로 symlink:

```text
.claude/skills
      └────────────→ ../.agents/skills
```

실제 Skill은 `.agents/skills`에만 존재한다.

## Windows PowerShell

```powershell
New-Item -ItemType Directory -Force ".claude" | Out-Null

New-Item -ItemType SymbolicLink `
  -Path ".claude\skills" `
  -Target "..\.agents\skills"
```

권한 오류 시 Windows Developer Mode 또는 관리자 PowerShell을 사용한다.

## setup-links.ps1

```powershell
$claudeDir = ".claude"
$link = ".claude\skills"
$target = "..\.agents\skills"

New-Item -ItemType Directory -Force $claudeDir | Out-Null

if (-not (Test-Path $link)) {
    New-Item -ItemType SymbolicLink `
        -Path $link `
        -Target $target | Out-Null

    Write-Host "Claude skills symlink created."
}
else {
    Write-Host "Claude skills path already exists."
}
```

팀원은 clone 후:

```powershell
.\scripts\setup-links.ps1
```

---

# 7. 모델별 설정 원칙

```text
docs/
= 게임에 관한 사실

AGENTS.md
= 어디를 봐야 하는지

.agents/skills/
= 일을 어떻게 해야 하는지

.claude / .codex / .agents 설정
= 해당 프로그램을 어떻게 실행할지
```

## Claude
`CLAUDE.md`는 얇은 어댑터로:

```md
# Claude Project Instructions

@AGENTS.md
```

공용:
```text
.claude/settings.json
```

개인:
```text
.claude/settings.local.json
```

## Codex

공통 지식:
```text
AGENTS.md
```

프로젝트 설정:
```text
.codex/config.toml
```

개인 설정:
```text
~/.codex/config.toml
```

## Antigravity

공통 지식:
```text
AGENTS.md
```

공통 Skill:
```text
.agents/skills/
```

Workspace MCP:
```text
.agents/mcp_config.json
```

---

# 8. Git에 올릴 것 / 제외할 것

원칙:

**팀이 똑같이 재현해야 하는 것 = Git  
개인 취향, 비밀, 실험 = Local**

## Git에 올림

```text
AGENTS.md
CLAUDE.md
docs/**
.agents/skills/**
.agents/mcp_config.json
.claude/settings.json
.codex/config.toml
.mcp.json
.env.example
harness/**
scripts/**
```

## Git에서 제외

```text
.env
.claude/settings.local.json
.claude/skills
개인 로그
Harness cache
임시 출력
```

---

# 9. 팀 Skill 변경

개인 실험 Skill은 먼저 개인 global 위치에서 시험한다.

```text
개인 실험
 ↓
실제 작업 검증
 ↓
팀 공용 가치 있음
 ↓
.agents/skills/
 ↓
PR
 ↓
검토
 ↓
Merge
```

공용 Skill 수정도 코드 수정처럼 PR로 관리한다.

---

# 10. MCP 운영

```text
Domain Map = 무엇을 알고 있는가
Skill      = 어떻게 일하는가
MCP        = 무엇을 실제로 조작할 수 있는가
```

## 팀 MCP
팀 작업에 필요한 MCP는 프로젝트 설정에 두고 Git에 올린다.
단, 비밀값은 넣지 않고 변수 이름만 둔다.

## 개인 MCP
개인 Notion, 개인 DB, 실험용 Browser 등은 각 도구의 user/global 설정에 둔다.

## 자동 생성기
초기에는 만들지 않는다.

우선 네이티브 설정:
```text
.mcp.json
.codex/config.toml
.agents/mcp_config.json
```

을 관리하고 `harness/mcp/README.md`에:
- MCP 이름
- 목적
- 필요한 `.env` 변수
- 사용 모델

만 기록한다.

설정 드리프트가 실제 문제가 될 때만 `catalog.yaml → generator`를 추가한다.

---

# 11. .env

프로젝트 루트:

```text
.env
.env.example
```

`.env.example`:

```dotenv
GITHUB_TOKEN=
FIGMA_TOKEN=
UNITY_MCP_TOKEN=
OTHER_SECRET=
```

각 개인은 복사 후 `.env`에 실제 값을 입력한다.

`.env`는 Git 제외.

중요: `.env` 파일이 있다고 모든 CLI가 자동으로 읽는 것은 아니다.

Harness launcher가:

```text
.env
 ↓
process 환경 변수로 load
 ↓
AI / MCP 실행
```

하게 만든다.

---

# 12. 추천 .gitignore 추가분

```gitignore
# Secrets
.env
.env.*
!.env.example

# Claude local
.claude/settings.local.json

# Local generated symlink
.claude/skills

# Harness local state
.harness/local/
.harness/cache/
.harness/logs/

# Agent logs
.codex-log/
*.agent.log
```

`.agents/skills/`는 ignore하지 않는다.

---

# 13. Task 파일

Agent 간 전체 대화를 넘기지 않고 작은 상태 파일을 사용한다.

예:

```md
# Double Jump

## Goal
플레이어가 공중에서 추가 점프를 한 번 사용할 수 있다.

## Acceptance Criteria
- 공중 추가 점프 정확히 1회
- 착지하면 초기화
- 기존 점프 정상
- 관련 테스트 통과

## Allowed Scope
- Assets/Scripts/Player/**
- Assets/Tests/Player/**

## Do Not Modify
- Assets/Scenes/**
- Assets/Prefabs/**
- ProjectSettings/**

## Status
IMPLEMENTING

## Changed
- none

## Verification
- Fast Test: pending
- Unity Test: pending

## Notes
-
```

다른 AI에게 넘길 때:

```text
Task 파일 + git diff + 핵심 실패 로그
```

만 제공한다.

---

# 14. 기본 개발 Flow

1. 기획자가 목표와 완성 조건 작성
2. `tasks/active/`에 Task 생성
3. Main Agent 1개 선택
4. AGENTS.md → 관련 Domain Map 확인
5. Domain Map으로 부족할 때만 좁은 검색
6. 구현
7. Fast Test
8. 필요할 때만 별도 Reviewer
9. Unity 실제 검증
10. 필요할 때만 Android build
11. Task에 결과 기록
12. `tasks/done/`으로 이동

---

# 15. Worktree

모든 작업에서 쓰지 않는다.

```text
작업 1개
→ 일반 branch

독립 작업 2개 이상
→ Worktree
```

예:

```text
main

worktree/player-jump
→ Codex

worktree/enemy-ai
→ Claude
```

Unity 특화 원칙:

```text
Code Worktree A ─┐
Code Worktree B ─┼→ Integration → Unity Editor 검증
Code Worktree C ─┘
```

같은 Scene / Prefab을 여러 Agent가 동시에 수정하지 않는다.

Unity `Library/`를 여러 Worktree가 함께 쓰는 구조는 기본값으로 사용하지 않는다.

---

# 16. 토큰 절약 규칙

1. **1 Task = 기본 1 Agent**
2. Race는 원인 불명 난제, 반복 실패, 중요한 설계 비교에서만 사용
3. Domain Map 먼저, 검색은 그 다음
4. MCP 결과를 통째로 가져오지 말고 필요한 범위만 조회
5. Agent 간 전체 대화 대신 Task + diff + 핵심 로그만 전달
6. Skill은 짧게 유지하고 상세 자료는 on-demand 파일로 분리

---

# 17. Orca 사용 기준

## 평상시

```text
VS Code
 ↓
Harness
 ↓
Codex OR Claude OR Antigravity
 ↓
Test
 ↓
Unity
```

## Orca

다음 중 하나 이상일 때만:
- 독립 기능 여러 개 병렬 개발
- 한 Agent가 2회 이상 실패
- 여러 해결책을 비교할 가치가 큼
- 여러 Worktree 관리가 번거로움

Orca 사용 시에도:

```text
Harness
 ↓
Orca
 ↓
Agents
```

Orca가 Harness를 대체하지 않는다.

---

# 18. 팀 운영 규칙

Repository 내부 설정은 기본적으로 팀 공용으로 본다.

```text
AGENTS.md
docs/
.agents/
.claude/settings.json
.codex/config.toml
.mcp.json
```

개인 설정은 각 프로그램의 user/global 위치를 사용한다.

예:
- 개인 Skill
- 개인 MCP
- 개인 모델 선호
- 개인 UI 설정

비밀값만 프로젝트 `.env`에 둔다.

다음 파일은 PR 검토 권장:

```text
AGENTS.md
docs/domains/**
.agents/skills/**
MCP 프로젝트 설정
scripts/**
```

---

# 19. 최소 도입 순서

## Phase 1

```text
AGENTS.md

docs/domains/
  INDEX.md
  player.md
  combat.md

.agents/skills/
  implement-feature/
  investigate-bug/
  unity-test/

tasks/

.env.example

scripts/
  setup-links.ps1
  verify-fast.ps1
```

## Phase 2
실제 불편이 발견되면 추가:
- review-change
- update-domain-map
- MCP
- Unity automation
- Android build
- Domain Map validator

## Phase 3
MCP 설정이 자주 어긋날 정도로 커질 때만:
```text
harness/mcp/catalog.yaml
        ↓
generator
        ↓
각 모델 설정
```

---

# 20. 팀원 초기 Setup

```text
1. 저장소 clone
2. .env.example → .env 복사
3. 개인 Token / Key 입력
4. setup-links 실행
5. 필요한 AI CLI 로그인
6. Harness 검증
7. Unity 실행
```

Windows:

```powershell
Copy-Item .env.example .env
.\scripts\setup-links.ps1
.\scripts\verify-env.ps1
.\scripts\verify-fast.ps1
```

---

# 21. Harness 성공 기준

Agent 수가 아니라 다음을 측정한다.

- 기능 1개 평균 완료 시간
- Agent 재시도 횟수
- 관련 파일 탐색 시간
- 전체 프로젝트 검색 횟수
- 평균 토큰 / 구독 사용량
- 첫 테스트 통과율
- Merge conflict 수
- QA 회귀 버그
- 기획 → Playable Build 리드타임
- 사람이 AI 결과를 수정해야 하는 횟수

목표:

**AI를 많이 쓰는 것이 아니라 적은 문맥과 적은 재작업으로 안정적인 결과를 만드는 것.**

---

# 22. 초기 Harness 구축 프롬프트

이 문서와 아래 프롬프트를 함께 Codex, Claude Code 또는 Antigravity에게 제공한다.

```text
이 저장소에 첨부된 `Unity AI Harness 운영 가이드 v1`을 기준으로
현재 Unity 프로젝트에 AI 개발 Harness를 실제로 구축해줘.

이 작업의 목적은 게임 기능을 변경하는 것이 아니라
AI 개발 환경과 문서 구조를 구축하는 것이다.

먼저 프로젝트를 읽기 전용으로 조사하고,
현재 존재하는 AGENTS.md, CLAUDE.md, .claude, .codex, .agents,
MCP 설정, .gitignore, Unity 프로젝트 구조를 확인해라.

기존 설정이 있다면 무조건 덮어쓰지 말고
가이드와 비교해 유지해야 할 내용과 변경해야 할 내용을 판단해라.

반드시 다음 원칙을 지켜라.

1. Harness가 중심이다.
2. 프로젝트 지식을 모델별 설정 파일에 중복 작성하지 않는다.
3. AGENTS.md는 짧은 프로젝트 지도 역할만 한다.
4. 게임 시스템 지식은 docs/domains/에 둔다.
5. 기존 프로젝트를 분석하여 실제 Domain을 식별한다.
6. 처음부터 모든 Domain 문서를 만들지 말고 핵심 시스템부터 최소한으로 만든다.
7. Domain 문서에는 모든 파일이 아니라 핵심 파일, 관계, 수정 주의사항만 기록한다.
8. 검색은 Domain Map으로 범위를 좁힌 뒤 필요할 때만 사용하도록 규칙을 작성한다.
9. 팀 공용 Skill 원본은 `.agents/skills/`에 둔다.
10. Claude용 `.claude/skills` symlink를 만드는 setup script를 작성한다.
11. 기존 `.claude/skills`가 실제 파일로 존재하면 임의 삭제하지 말고 안전하게 병합한다.
12. 공통 Skill은 최소 다음부터 시작한다.
   - implement-feature
   - investigate-bug
   - unity-test
13. Skill은 짧게 유지하고 긴 자료는 references/ 또는 scripts/로 분리한다.
14. 프로젝트 공용 설정과 개인 설정을 분리한다.
15. `.env.example`은 Git 추적, `.env`는 Git 제외로 구성한다.
16. 실제 API Key, Token, Password를 Git 추적 파일에 기록하지 않는다.
17. MCP가 이미 있다면 유지하면서 팀 공용/개인용을 구분한다.
18. MCP config generator는 지금 당장 필요하지 않으면 만들지 않는다.
19. Unity Library, Temp, Obj 등의 기존 ignore 정책을 훼손하지 않는다.
20. 기존 게임 코드, Scene, Prefab, ProjectSettings는 Harness 구축에 반드시 필요하지 않으면 수정하지 않는다.

다음 순서로 진행한다.

A. 현재 저장소 구조 조사
B. 적용 계획 작성
C. 충돌 가능성이 있는 기존 설정 확인
D. 최소 Harness 생성
E. Domain Map 초안 생성
F. Skill 생성
G. Git 추적/ignore 정책 수정
H. setup / verify script 생성
I. Harness 자체 검증
J. 변경사항 최종 보고

최종 보고에는 반드시 다음을 포함한다.

- 생성한 파일
- 수정한 파일
- 유지한 기존 파일
- Git에 올라가는 설정
- 개인 로컬에만 남는 설정
- 아직 자동화하지 않은 부분과 이유
- 사람이 한 번 확인해야 하는 항목
- 이후 실제 개발 Task에서 사용하는 방법

작업 중 불필요한 전체 프로젝트 검색을 반복하지 말고,
초기 구조 파악 이후에는 탐색 범위를 좁혀라.

Harness 구축이 끝날 때까지 게임 기능 구현이나
요청되지 않은 리팩터링은 하지 마라.
```

---

# 23. Harness 구축 후 일반 작업 프롬프트

```text
다음 기능을 구현해줘.

목표:
플레이어가 공중에서 추가 점프를 1회 사용할 수 있다.

완성 조건:
- 공중 추가 점프 1회
- 착지하면 초기화
- 기존 점프가 깨지지 않음
- 관련 자동 테스트 통과

수정하지 않을 것:
- UI
- 사운드
- 이펙트

이 프로젝트의 AGENTS.md와 관련 Domain Map을 먼저 확인하고,
적절한 Skill 절차를 사용해 작업해줘.

Domain Map에 정보가 충분하면 전체 프로젝트 검색을 하지 말고,
부족한 경우에만 관련 영역으로 범위를 좁혀 검색해줘.

작업 후 다음만 보고해줘.
1. 변경 파일
2. 테스트 결과
3. Unity에서 추가 확인할 항목
4. 남은 위험
5. Domain Map 갱신 여부
```

---

# 24. 최종 요약

```text
Harness > IDE / Agent / Orca
```

```text
Project Knowledge → docs/domains
Agent Entry       → AGENTS.md
Repeatable Work   → .agents/skills
Claude Adapter    → .claude/skills symlink
Secrets           → .env
Team Secret Form  → .env.example
Default Task      → Agent 1개
Parallel / 난제   → 필요할 때만 Orca
```

AI 탐색:

```text
Domain Map
 ↓
관련 핵심 파일
 ↓
좁은 검색
 ↓
전체 검색은 마지막
```

Unity:

```text
코드는 필요할 때 병렬
Scene / Prefab / Editor 검증은 통합해서 안전하게
```

가장 중요한 목표:

> **AI가 프로젝트를 매번 처음부터 이해하게 만들지 않는다.  
> Harness가 이미 알고 있는 구조를 이용해 필요한 정보만 읽고, 필요한 작업만 수행하고, 자동 검증을 통과한 결과만 사람에게 보여준다.**
