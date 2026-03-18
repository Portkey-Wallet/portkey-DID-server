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
- Guardian existence gating for `SocialRecovery` before quota consumption when the hard limiter is enabled
- Header-based client IP extraction from the configured forwarded header source with fallback to `X-Forwarded-For` and `X-Real-IP` for the hard limiter only
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

### Shared Client IP Resolver

The HTTP layer uses a shared transport-side client IP resolver with two explicit modes:

- `GetForwardedClientIp()`: strict forwarded-header-only resolution for the hard limiter
- `GetBestEffortClientIp()`: legacy best-effort resolution for captcha, whitelist, secondary email, and existing flows

This change removes the PR-local duplicate IP helper and keeps the resolver semantics centralized without pushing HTTP header parsing into the application contract boundary.

### Hard Limiter Path

The registration/recovery hard limiter uses `GetForwardedClientIp()` and reads the client IP from HTTP headers only.

1. Read `RealIpOptions.HeaderKey` when configured
2. Split by comma and use the first non-empty trimmed value
3. If the configured header is empty or missing, fallback to `X-Forwarded-For`
4. If `X-Forwarded-For` is empty or missing, fallback to `X-Real-IP`
5. Duplicate header names are ignored during fallback resolution
6. If all forwarded headers are missing or empty, reject the request with `400 Bad Request`

### Legacy Flows

Existing flows outside the new hard limiter use `GetBestEffortClientIp()` and keep the previous best-effort behavior:

1. Reuse a request-scoped resolved IP when one was already established by the hard limiter path
2. Otherwise read the first IP from `RealIpOptions.HeaderKey`
3. If missing, fallback to `X-Forwarded-For`
4. If still missing, fallback to `X-Real-IP`
5. If still missing, fallback to `RemoteIpAddress`

This preserves historical behavior for captcha, `isGoogleRecaptchaOpen`, secondary email, and other existing request paths.

The hard-limiter behavior assumes the upstream gateway or proxy writes trusted forwarding headers.

## Rate Limit Model

The limiter uses Redis-backed fixed windows keyed by:

`RegistrationEmailRateLimit:{OperationType}:{WindowName}:{WindowStartUtc}:{ClientIp}`

Two fixed windows are enforced per operation:

- 10-minute window
- 1-hour window

All enabled windows are evaluated with the same request timestamp.
If multiple windows exceed their thresholds in the same request, the limiter returns the longest remaining TTL as `Retry-After`.

## Thresholds

### CreateCAHolder

- `10 requests / 10 minutes / IP`
- `30 requests / hour / IP`

### SocialRecovery

- `15 requests / 10 minutes / IP`
- `45 requests / hour / IP`

These thresholds are intentionally more permissive for `SocialRecovery` because it is a normal-user recovery flow with more legitimate retries.

For the default `SocialRecovery` policy, quota is consumed only after `GuardianExistsAsync(...)` confirms that the recovery target exists.
Requests for non-existent guardians bypass the hard limiter and do not consume quota.
If a future policy explicitly sets `RequireGuardianExistsBeforeConsume = false`, guardian existence is still checked before send/risk-control, but quota may already have been consumed.

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
- If both the 10-minute and 1-hour windows are exceeded, `Retry-After` reflects the longer blocking window

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
    "Policies": {
      "CreateCAHolder": {
        "GuardianType": "Email",
        "Per10Minutes": 10,
        "PerHour": 30,
        "RequireGuardianExistsBeforeConsume": false
      },
      "SocialRecovery": {
        "GuardianType": "Email",
        "Per10Minutes": 15,
        "PerHour": 45,
        "RequireGuardianExistsBeforeConsume": true
      }
    }
  }
}
```

`IsEnabled` controls the feature globally. Each entry in `Policies` is keyed by `OperationType`.
`appsettings.json` is the single default source of truth for these policies; the application code does not embed fallback thresholds.
For each policy, the limiter applies only when:

- the request guardian type matches `GuardianType` after trimmed, case-insensitive normalization
- at least one of `Per10Minutes` or `PerHour` is positive

`GuardianType` must be a valid named `GuardianIdentifierType` value such as `Email`.
Undefined numeric enum values are rejected during configuration validation.
`RequireGuardianExistsBeforeConsume` controls whether guardian existence must be confirmed before quota consumption, not whether guardian existence is checked at all.
For `SocialRecovery`, guardian existence is always checked when the limiter policy applies; the flag only controls whether that check happens before or after quota consumption.
If `IsEnabled = true`, the configuration is validated so that policies are present, `GuardianType` is not blank, `GuardianType` is valid, and thresholds are not negative.

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

- This limiter is independent from the existing captcha/check-switch logic and does not re-route `CreateCAHolder` back into a captcha path.
- Existing captcha behavior remains unchanged for flows that already use captcha or app-check today.
- The hard limiter is evaluated only when `RegistrationEmailRateLimit:IsEnabled` is set to `true`.
- `CreateCAHolder` and `SocialRecovery` are dispatched through dedicated operation handlers so the controller remains a thin HTTP adapter.
- Guardian verification-code flow and secondary-email verification now share the same risk-control orchestration, while the controller remains responsible for writing HTTP status codes.
- The recommended rollout is to deploy code first with `IsEnabled = false`, verify that registration and recovery requests preserve the current baseline behavior, and then enable the feature through configuration.
- When `IsEnabled = false`, `SocialRecovery` preserves the current `master` baseline behavior, including the `CheckSwitch = false` fast path that does not introduce guardian existence gating.
- When `CheckSwitch = true`, `SocialRecovery` preserves the current `master` recovery baseline and does not introduce a new login requirement.
- Existing non-limiter flows keep their best-effort IP behavior, including `X-Real-IP` and `RemoteIpAddress` fallback.
- In `SocialRecovery`, when the hard limiter resolves a forwarded client IP, the same request reuses that IP for downstream whitelist/captcha/count logic.
- If future abuse patterns change, business-flow-specific policies can be added separately for transfer or approval flows.

## Verification Strategy

- With `IsEnabled = false`, registration and recovery email requests should preserve the current baseline behavior with no new `400` or `429` introduced by this feature.
- With `IsEnabled = false` and `CheckSwitch = false`, `SocialRecovery` should still short-circuit to `SendVerificationRequestAsync(...)` without invoking `GuardianExistsAsync(...)`.
- With `IsEnabled = false` and `CheckSwitch = true`, anonymous `SocialRecovery` should still follow the existing recovery risk-control path and must not return `401` only because the caller is unauthenticated.
- With `IsEnabled = true`, a normal registration or recovery request with valid forwarding headers should still succeed.
- With `IsEnabled = true`, the server should log a successful rate-limit check that includes `traceId`, `clientIp`, `operationType`, and window information.
- With `IsEnabled = true`, if a later window hits a Redis error after an earlier window already proved the request should be blocked, the request should still return the previously computed block result.
- Production verification should avoid intentionally spamming real email sends. The block-path (`429`) behavior is covered by automated tests and should only be manually verified with temporary low thresholds and a controlled egress IP if operationally necessary.
