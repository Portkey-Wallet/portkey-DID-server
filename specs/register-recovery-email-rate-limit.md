# Register/Recovery Email Rate Limit Spec

## Background

The DID server sends email verification codes for multiple business flows.
The abuse case addressed by this change is repeated registration or recovery attempts from the same client IP.

This spec introduces a hard IP rate limit for registration and recovery email verification-code requests.
It does not introduce hard limits for normal in-product flows such as transfer approval or secondary email verification.

## Scope

### In Scope

- `POST /api/app/account/sendVerificationRequest`
- Requests with `Type == "Email"`
- Requests with `OperationType == CreateCAHolder`
- Requests with `OperationType == SocialRecovery`
- Config-controlled feature enablement
- Header-based client IP extraction from `X-Forwarded-For` and `X-Real-IP` for the hard limiter only
- Redis-backed fixed-window counters
- `400 Bad Request` when both IP headers are missing
- `429 Too Many Requests` with `Retry-After` when the hard limit is exceeded

### Non-goals

- No hard rate limit for `Approve`
- No hard rate limit for `GuardianApproveTransfer`
- No hard rate limit for `SetSecondaryEmail`
- No email-address-based rate limiting in v1
- No trusted-proxy hardening in v1

## Operation Types In Scope

- `CreateCAHolder`
- `SocialRecovery`

All other operation types are explicitly excluded from this hard limiter in v1.

## IP Extraction Rules

### Hard Limiter Path

The registration/recovery hard limiter reads the client IP from HTTP headers only.

1. Read `X-Forwarded-For`
2. Split by comma
3. Use the first non-empty trimmed value
4. If `X-Forwarded-For` is empty or missing, read `X-Real-IP`
5. Split by comma and use the first non-empty trimmed value
6. If both headers are missing or empty, reject the request with `400 Bad Request`

### Legacy Flows

Existing controller flows outside the new hard limiter keep the previous best-effort behavior:

1. Read the first IP from `X-Forwarded-For`
2. If missing, fallback to `RemoteIpAddress`

This preserves historical behavior for captcha, `isGoogleRecaptchaOpen`, secondary email, and other existing request paths.

The hard-limiter behavior assumes the upstream gateway or proxy writes trusted forwarding headers.

## Rate Limit Model

The limiter uses Redis-backed fixed windows keyed by:

`RegistrationEmailRateLimit:{OperationType}:{WindowName}:{WindowStartUtc}:{ClientIp}`

Two fixed windows are enforced per operation:

- 10-minute window
- 1-hour window

The request is rejected as soon as one window exceeds its configured threshold.

## Thresholds

### CreateCAHolder

- `10 requests / 10 minutes / IP`
- `30 requests / hour / IP`

### SocialRecovery

- `15 requests / 10 minutes / IP`
- `45 requests / hour / IP`

These thresholds are intentionally more permissive for `SocialRecovery` because it is a normal-user recovery flow with more legitimate retries.

If both thresholds for one operation are configured as non-positive values, that operation is treated as disabled for the hard limiter and bypasses the header-only enforcement path.

## Error Handling

### Missing IP Headers

- HTTP status: `400 Bad Request`
- Response body: empty `VerifierServerResponse`
- Reason: the hard limiter cannot evaluate the required forwarding-header-based IP policy

### Rate Limit Exceeded

- HTTP status: `429 Too Many Requests`
- Response header: `Retry-After`
- Response body: empty `VerifierServerResponse`

### Redis Failure

- Fail open
- Log the error with `traceId`, `operationType`, and `clientIp`
- Do not block the business flow when Redis is temporarily unavailable

## Configuration

The host configuration section is:

```json
{
  "RegistrationEmailRateLimit": {
    "IsEnabled": false,
    "CreateCAHolder": {
      "Per10Minutes": 10,
      "PerHour": 30
    },
    "SocialRecovery": {
      "Per10Minutes": 15,
      "PerHour": 45
    }
  }
}
```

`IsEnabled` controls the feature globally. For each operation, at least one of `Per10Minutes` or `PerHour` must be a positive value for the limiter to apply.

## Observability

The server logs:

- `traceId`
- `clientIp`
- `operationType`
- matched rate-limit window
- configured limit
- current count
- remaining quota
- retry-after seconds when blocked

The server must not log raw email values as part of this feature.

## Rollout Notes

- This limiter is independent from the existing captcha/check-switch logic.
- Captcha remains a separate risk-control layer.
- The hard limiter is evaluated only when `RegistrationEmailRateLimit:IsEnabled` is set to `true`.
- The recommended rollout is to deploy code first with `IsEnabled = false`, verify that registration and recovery requests still behave normally, and then enable the feature through configuration.
- Existing non-limiter flows keep their original `RemoteIpAddress` fallback behavior.
- If future abuse patterns change, business-flow-specific policies can be added separately for transfer or approval flows.

## Verification Strategy

- With `IsEnabled = false`, registration and recovery email requests should follow the original path with no `400` or `429` introduced by this feature.
- With `IsEnabled = true`, a normal registration or recovery request with valid forwarding headers should still succeed.
- With `IsEnabled = true`, the server should log a successful rate-limit check that includes `traceId`, `clientIp`, `operationType`, and window information.
- Production verification should avoid intentionally spamming real email sends. The block-path (`429`) behavior is covered by automated tests and should only be manually verified with temporary low thresholds and a controlled egress IP if operationally necessary.
