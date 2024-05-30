# IdentityController sequence diagram

## BasicLogin

```mermaid
sequenceDiagram
    actor User
    participant PasswordChangeRequiredFilter
    participant Controller
    participant IIdentityService
    participant Database
    participant LoginResult
    participant IJwtService

    User->>PasswordChangeRequiredFilter: id/pw로 로그인 요청
    activate PasswordChangeRequiredFilter
    Note right of User: 쿠키에 토큰 설정 시 헤더에 token-set-cookie: true 설정
    alt if 비밀번호 변경이 필요
        PasswordChangeRequiredFilter-->>User: 403 반환
    end
    deactivate PasswordChangeRequiredFilter
    PasswordChangeRequiredFilter->>Controller: 
    activate Controller
    Controller->>IIdentityService: 계정 검증 요청
    activate IIdentityService
    IIdentityService->>Database: 비밀번호 해시와 유저 데이터 요청
    activate Database
    Database-->>IIdentityService: 비밀번호 해시와 유저 데이터 반환
    deactivate Database
    IIdentityService-->>Controller: 검증 실패 시 에러코드 반환
    deactivate IIdentityService
    alt If 에러코드
        Controller-->>User: 에러코드에 따라 401 or 404 반환
    end

    Controller->>LoginResult: 로그인 결과 생성
    deactivate Controller
    activate LoginResult
    LoginResult->>IJwtService: 토큰 요청
    activate IJwtService
    IJwtService-->>LoginResult: 토큰 반환
    deactivate IJwtService
    alt if 쿠키에 저장
        LoginResult-->>User: response 쿠키에 토큰 저장
    else
        LoginResult-->>User: response body에 토큰 저장
    end
    deactivate LoginResult
```

## BasicSignUp

```mermaid
sequenceDiagram
    actor User
    participant SignUpEmailVerificationFilter
    participant Controller
    participant IIdentityService
    participant Database
    participant LoginResult
    participant IJwtService

    User->>SignUpEmailVerificationFilter: id/pw/email/name으로 로그인 요청
    activate SignUpEmailVerificationFilter
    Note right of User: 쿠키에 토큰 설정 시 헤더에 token-set-cookie: true 설정
    alt if 미인증된 이메일
        SignUpEmailVerificationFilter-->>User: 403 반환
    end

    deactivate SignUpEmailVerificationFilter
    SignUpEmailVerificationFilter->>Controller: 
    activate Controller
    Controller->>IIdentityService: 계정 추가 요청
    activate IIdentityService
    IIdentityService->>Database: id, email, name 중복 여부 요청, 신규 유저 삽입
    activate Database
    Database-->>IIdentityService: 중복 여부 반환
    deactivate Database
    IIdentityService-->>Controller: 중복시 에러코드 반환
    deactivate IIdentityService
    alt If 에러코드
        Controller-->>User: 에러코드에 따라 404 or 409 반환
    end

    Controller->>LoginResult: 가입한 유저의 로그인 결과 생성
    deactivate Controller
    activate LoginResult
    LoginResult->>IJwtService: 토큰 요청
    activate IJwtService
    IJwtService-->>LoginResult: 토큰 반환
    deactivate IJwtService
    alt if 쿠키에 저장
        LoginResult-->>User: response 쿠키에 토큰 저장
    else
        LoginResult-->>User: response body에 토큰 저장
    end
    deactivate LoginResult
```

## SendVerifyEmail

```mermaid
sequenceDiagram
    actor User
    participant Controller
    participant IVerifyEmailService
    participant IDistributedCache
    participant IMailSender

    User->>Controller: 요청 이메일로 인증 메일 전송 요청
    activate Controller
    Controller->>IVerifyEmailService: 인증 메일 전송 요청
    activate IVerifyEmailService
    IVerifyEmailService->>IVerifyEmailService: 인증 코드 발행
    IVerifyEmailService->>IDistributedCache: 인증 코드 저장
    IVerifyEmailService->>IMailSender: 인증 메일 전송 요청
    IVerifyEmailService-->>Controller: 
    deactivate IVerifyEmailService
    Controller-->>User: 200 반환
```

## VerifyEmail

```mermaid
sequenceDiagram
    actor User
    participant Controller
    participant IVerifyEmailService
    participant IDistributedCache

    User->>Controller: 인증 코드로 확인 요청
    activate Controller
    Controller->>IVerifyEmailService: 인증 코드로 확인 요청
    activate IVerifyEmailService
    IVerifyEmailService->>IDistributedCache: 코드 저장 여부 확인
    activate IDistributedCache
    IDistributedCache-->>IVerifyEmailService: 코드 저장 여부 반환
    deactivate IDistributedCache
    IVerifyEmailService-->>Controller: 코드 저장 여부 반환
    deactivate IVerifyEmailService
    alt if 저장됨
        Controller-->>User: 200 반환
    else
        Controller-->>User: 404 반환
    end
```

## OAuthLogin

```mermaid
sequenceDiagram
    actor User
    participant Controller
    participant OAuthProvider
    participant IIdentityService
    participant Database
    participant LoginResult
    participant IJwtService

    User->>Controller: OAuth 로그인 요청
    activate Controller
    Note right of User: 쿠키에 토큰 설정 시 헤더에 token-set-cookie: true 설정
    Controller->>OAuthProvider: OAuth 제공자로 리디렉션
    OAuthProvider->>User: 로그인 페이지 표시
    User->>OAuthProvider: 자격증명 입력
    OAuthProvider-->>Controller: 인증코드와 함께 리디렉션
    Controller->>OAuthProvider: 인증코드를 액세스 토큰으로 교환 요청
    OAuthProvider-->>Controller: 액세스 토큰 반환
    Controller->>IIdentityService: OAuth 유저 검증 요청
    activate IIdentityService
    IIdentityService->>Database: 유저 데이터 요청
    activate Database
    Database-->>IIdentityService: 유저 데이터 반환
    deactivate Database
    IIdentityService-->>Controller: 검증 실패 시 에러코드 반환
    deactivate IIdentityService
    alt If 에러코드
        Controller-->>User: 404 반환
    end

    Controller->>LoginResult: 로그인 결과 생성
    deactivate Controller
    activate LoginResult
    LoginResult->>IJwtService: 토큰 요청
    activate IJwtService
    IJwtService-->>LoginResult: 토큰 반환
    deactivate IJwtService
    alt if 쿠키에 저장
        LoginResult-->>User: 200 반환. response 쿠키에 토큰 저장
    else
        LoginResult-->>User: 200 반환. response body에 토큰 저장
    end
    deactivate LoginResult
```

## OAuthSignUp

```mermaid
sequenceDiagram
    actor User
    participant Controller
    participant OAuthProvider
    participant IIdentityService
    participant Database
    participant LoginResult
    participant IJwtService

    User->>Controller: OAuth 회원가입 요청
    activate Controller
    Note right of User: 쿠키에 토큰 설정 시 헤더에 token-set-cookie: true 설정
    Controller->>OAuthProvider: OAuth 제공자로 리디렉션
    OAuthProvider->>User: 로그인 페이지 표시
    User->>OAuthProvider: 자격증명 입력
    OAuthProvider-->>Controller: 인증코드와 함께 리디렉션
    Controller->>OAuthProvider: 인증코드를 액세스 토큰으로 교환 요청
    OAuthProvider-->>Controller: 액세스 토큰 반환
    Controller->>IIdentityService: OAuth 유저 가입 요청
    activate IIdentityService
    IIdentityService->>Database: 중복 유저 여부 요청
    activate Database
    Database-->>IIdentityService: 중복 유저 여부 반환
    deactivate Database
    IIdentityService-->>Controller: 중복 시 에러코드 반환
    deactivate IIdentityService
    alt If 에러코드
        Controller-->>User: 에러코드에 따라 404 or 409 반환
    end

    Controller->>LoginResult: 가입한 유저의 로그인 결과 생성
    deactivate Controller
    activate LoginResult
    LoginResult->>IJwtService: 토큰 요청
    activate IJwtService
    IJwtService-->>LoginResult: 토큰 반환
    deactivate IJwtService
    alt if 쿠키에 저장
        LoginResult-->>User: 200 반환. response 쿠키에 토큰 저장
    else
        LoginResult-->>User: 200 반환. response body에 토큰 저장
    end
    deactivate LoginResult
```

## AddOAuth

```mermaid
sequenceDiagram
    actor User
    participant Controller
    participant OAuthProvider
    participant IIdentityService
    participant Database

    User->>Controller: 현재 계정에 OAuth 추가 요청
    activate Controller
    Controller->>OAuthProvider: OAuth 제공자로 리디렉션
    OAuthProvider->>User: 로그인 페이지 표시
    User->>OAuthProvider: 자격증명 입력
    OAuthProvider-->>Controller: 인증코드와 함께 리디렉션
    Controller->>OAuthProvider: 인증코드를 액세스 토큰으로 교환 요청
    OAuthProvider-->>Controller: 액세스 토큰 반환
    Controller->>IIdentityService: OAuth 계정 추가 요청
    activate IIdentityService
    IIdentityService->>Database: 중복 계정 여부 요청, 신규 계정 삽입
    activate Database
    Database-->>IIdentityService: 중복 계정 여부 반환
    deactivate Database
    IIdentityService-->>Controller: 중복 시 에러코드 반환
    deactivate IIdentityService
    alt If 에러코드
        Controller-->>User: 에러코드에 따라 401 or 404 or 409 반환
    else
        Controller-->>User: 200 반환
    end
```

## RemoveOAuth

```mermaid
sequenceDiagram
    actor User
    participant Controller
    participant IIdentityService
    participant Database

    User->>Controller: 현제 계정의 OAuth 제거 요청
    activate Controller
    Controller->>IIdentityService: OAuth 계정 제거 요청
    activate IIdentityService
    IIdentityService->>Database: 해당 계정 존재 여부 확인, 계정 제거 요청
    activate Database
    Database-->>IIdentityService: 해당 계정 존재 여부 반환
    deactivate Database
    IIdentityService-->>Controller: 삭제 결과 반환
    deactivate IIdentityService
    alt If 에러코드
        Controller-->>User: 에러코드에 따라 401 or 404 or 409 반환
    else
        Controller-->>User: 200 반환
    end
```

## Logout

```mermaid
sequenceDiagram
    actor User
    participant Controller
    participant LogoutResult
    participant IJwtService
    participant IDistributedCache

    User->>Controller: 로그아웃 요청
    Controller->>LogoutResult: 로그아웃 결과 생성
    activate LogoutResult
    LogoutResult->>IJwtService: 쿠키의 토큰 제거 요청
    activate IJwtService
    IJwtService-->>LogoutResult: 쿠키의 토큰 제거 결과 반환
    deactivate IJwtService
    alt if 제거 성공 시
        LogoutResult->>IJwtService: 캐시의 토큰 제거 요청
        activate IJwtService
        IJwtService->>IDistributedCache: 캐시의 토큰 제거
        deactivate IJwtService
        LogoutResult-->>User: 200 반환
    else
        LogoutResult-->>User: 204 반환
    end
    deactivate LogoutResult
```

## Refresh

```mermaid
sequenceDiagram
    actor User
    participant Controller
    participant RefreshResult
    participant IJwtService
    participant IDistributedCache

    User->>Controller: 토큰 refresh 요청
    Controller->>RefreshResult: refresh 결과 생성
    activate RefreshResult
    RefreshResult->>IJwtService: 유저 request의 토큰 요청
    activate IJwtService
    IJwtService-->>RefreshResult: 유저 request의 토큰 반환
    alt if 토큰이 없을 시
        RefreshResult-->>User: 404 반환
    end

    RefreshResult->>IJwtService: 토큰 refresh 요청
    IJwtService->>IDistributedCache: 저장된 토큰 요청
    IDistributedCache-->>IJwtService: 저장된 토큰 반환
    alt if 저장된 토큰이 없을 시
        IJwtService-->>RefreshResult: null 반환
        RefreshResult-->>User: 204 반환
    else
        IJwtService->>IJwtService: 신규 토큰 발행
        IJwtService-->>RefreshResult: 신규 토큰 반환
        alt if 쿠키에 토큰 저장 시
            RefreshResult->>IJwtService: 쿠키에 토큰 설정 요청
        else
            RefreshResult->>IJwtService: response body에 토큰 설정 요청
        end
        deactivate IJwtService
        RefreshResult-->>User: 200 반환
        deactivate RefreshResult
    end
```

## ChangePassword

```mermaid
sequenceDiagram
    actor User
    participant Controller
    participant IIdentityService
    participant Database

    User->>Controller: 현재 계정의 비밀번호 변경 요청
    activate Controller
    Controller->>IIdentityService: 비밀번호 변경 요청
    activate IIdentityService
    IIdentityService->>IIdentityService: 비밀번호 해시 생성
    IIdentityService->>Database: 유저 비밀번호 변경 요청
    Databse-->>IIdentityService: 변경 결과 반환
    IIdentityService-->>Controller: 변경 결과 반환
    deactivate IIdentityService
    alt if 유저 데이터가 DB에 없을 시
        Controller-->>User: 401 반환
    else
        Controller-->>User: 200 반환
    end
    deactivate Controller
```

## ChangeName

```mermaid
sequenceDiagram
    actor User
    participant Controller
    participant IIdentityService
    participant Database

    User->>Controller: 현재 계정의 이름 변경 요청
    activate Controller
    Controller->>IIdentityService: 이름 변경 요청
    activate IIdentityService
    IIdentityService->>Database: 유저 이름 변경 요청
    Databse-->>IIdentityService: 변경 결과 반환
    IIdentityService-->>Controller: 변경 결과 반환
    deactivate IIdentityService
    alt if 유저 데이터가 DB에 없을 시
        Controller-->>User: 401 반환
    else
        Controller-->>User: 200 반환
    end
    deactivate Controller
```

## ResetPassword

```mermaid
sequenceDiagram
    actor User
    participant Controller
    participant IIdentityService
    participant Database
    participant IPasswordResetMailService

    User->>Controller: 해당 이메일의 비밀번호 초기화 요청
    activate Controller
    Controller->>IIdentityService: 비밀번호 초기화 요청
    activate IIdentityService
    IIdentityService->>IIdentityService: 임시 비밀번호 생성
    IIdentityService->>Database: 유저 비밀번호 변경 요청
    activate Database
    Database-->>IIdentityService: 변경 결과 반환
    deactivate Database
    IIdentityService-->>Controller: 변경 결과 반환
    deactivate IIdentityService
    alt if 해당 이메일의 유저가 DB에 없을 시
        Controller-->>User: 404 반환
    else
        Controller->>IPasswordResetMailService: 비밀번호 초기화 안내 메일 전송 요청
        Controller-->>User: 200 반환
    end
    deactivate Controller
```
