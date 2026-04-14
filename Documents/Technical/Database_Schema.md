**[문서 내비게이션 바]**
**[Technical]** [Architecture](./Architecture.md) | [API Reference](./API_Reference.md) | [Setup Guide](./Setup_Guide.md) | [Database Schema](./Database_Schema.md)
**[UserGuide]** [Introduction](../UserGuide/Introduction.md) | [Installation](../UserGuide/Installation.md) | [How to Use](../UserGuide/How_to_Use.md) | [Troubleshooting](../UserGuide/Troubleshooting.md)
---

# 데이터베이스 스키마 (Database Schema)

> **⚠️ 알림 (추가 정보 필요):**
> 현재 코드베이스 분석 결과, `DatabaseConnector.cs`를 통한 MySQL 연동 코드가 존재하나, 전체 스키마 정의를 담은 `.sql` 덤프 파일이나 ORM(Entity Framework) 모델 클래스가 명시적으로 제공되지 않았습니다.
> 아래 스키마는 `LoginProtocol` 및 의존성 주입 코드(`Database=unity3d`)를 기반으로 역공학하여 추론한 구조입니다.

## 1. 데이터베이스 연결 정보
*   **DBMS**: MySQL
*   **Target Database**: `unity3d`
*   **접근 방식**: ADO.NET (`MySql.Data`) 기반 커넥션 풀링 사용 (최근 보안 패치를 통해 SQL Injection 취약점 점검 및 Parameterized Query 도입 중)

## 2. 추론된 테이블 스키마 (Inferred Schema)

### Table: `users` (추정)
계정 인증 및 권한 관리를 위한 기본 테이블입니다.

| 컬럼명 | 데이터 타입 | 제약 조건 | 설명 |
| :--- | :--- | :--- | :--- |
| `id` | `INT` | PK, AUTO_INCREMENT | 유저 고유 식별자 |
| `user_id` | `VARCHAR(50)` | UNIQUE, NOT NULL | 로그인용 아이디 (`LoginProtocol.UserId`) |
| `password_hash` | `VARCHAR(255)`| NOT NULL | 보안을 위해 해시 처리된 비밀번호 |
| `created_at` | `DATETIME` | DEFAULT CURRENT_TIMESTAMP | 계정 생성 일자 |

### Table: `player_data` (추정)
`PlayerData` 구조체 직렬화 로직을 기반으로 추정한 게임 내 상태 저장 테이블입니다.

| 컬럼명 | 데이터 타입 | 제약 조건 | 설명 |
| :--- | :--- | :--- | :--- |
| `user_id` | `VARCHAR(50)` | FK(users.user_id) | 소유 유저 아이디 |
| `position_x` | `FLOAT` | | 마지막 위치 X |
| `position_y` | `FLOAT` | | 마지막 위치 Y |
| `position_z` | `FLOAT` | | 마지막 위치 Z |
| `skin_id` | `INT` | | 장착 중인 스킨/캐릭터 ID |

## 3. 아키텍처적 개선 사항 (Phase 3 권고)
현재 ADO.NET을 활용한 하드코딩된 쿼리 문자열 방식은 유지보수가 어렵습니다. 향후 Dapper 또는 Entity Framework Core를 도입하여 객체 지향적인 스키마 관리를 권장합니다.