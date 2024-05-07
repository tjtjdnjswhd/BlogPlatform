# BlogPlatform

ASP.NET Core로 구현하는 블로그 플랫폼 서비스 API

## 기능

- 회원가입/로그인/로그아웃/탈퇴
- 유저 이름 설정
- 카테고리 작성/수정/삭제
- 블로그 이름 설정
- 게시글 작성/수정/삭제
- 댓글 작성/수정/삭제
- 태그 작성/수정/삭제
- 게시글 검색
  - 카테고리
  - 제목
  - 내용
  - 댓글
  - 작성일
  - 수정일
  - 태그
- 댓글 검색
  - 작성자
  - 내용
  - 게시글
  - 작성일
  - 수정일

## 기술 스택

- ASP.NET Core 8.0
- Entity Framework Core 8.0
- MySQL
- Redis
- Docker

## ERD

```mermaid
erDiagram
    __EFMigrationsHistory {
        varchar MigrationId PK
        varchar ProductVersion
    }
    BasicAccounts {
        int Id PK
        datetime CreatedAt
        datetime DeletedAt
        timestamp Version
        varchar AccountId
        longtext PasswordHash
    }
    OAuthProvider {
        int Id PK
        datetime CreatedAt
        datetime DeletedAt
        timestamp Version
        varchar Name
    }
    Role {
        int Id PK
        datetime CreatedAt
        datetime DeletedAt
        timestamp Version
        varchar Name
        int Priority
    }
    User {
        int Id PK
        datetime CreatedAt
        datetime DeletedAt
        timestamp Version
        varchar UserName
        varchar Email
        int BasicLoginAccountId FK
    }
    Blog {
        int Id PK
        datetime CreatedAt
        datetime DeletedAt
        timestamp Version
        varchar Name
        longtext Description
        int UserId FK
    }
    OAuthAccount {
        int Id PK
        datetime CreatedAt
        datetime DeletedAt
        timestamp Version
        varchar NameIdentifier
        int ProviderId FK
        int UserId FK
    }
    RoleUser {
        int RolesId PK
        int UsersId PK
    }
    Category {
        int Id PK
        datetime CreatedAt
        datetime DeletedAt
        timestamp Version
        longtext Name
        int BlogId FK
    }
    Post {
        int Id PK
        datetime CreatedAt
        datetime DeletedAt
        timestamp Version
        longtext Title
        text Content
        json Tags
        datetime LastUpdatedAt
        int CategoryId FK
    }
    Comment {
        int Id PK
        datetime CreatedAt
        datetime DeletedAt
        timestamp Version
        longtext Content
        datetime LastUpdatedAt
        int PostId FK
        int UserId FK
        int ParentCommentId FK
    }

    User ||--o{ BasicAccounts : has
    Blog ||--o{ User : has
    OAuthAccount ||--o{ OAuthProvider : has
    OAuthAccount ||--o{ User : has
    RoleUser ||--o{ Role : has
    RoleUser ||--o{ User : has
    Category ||--o{ Blog : has
    Post ||--o{ Category : has
    Comment ||--o{ Post : has
    Comment ||--o{ User : has
    Comment ||--|| Comment : has
```
