# API architecture

This doc covers how `src/SkillCert.Api` is put together and the template every endpoint
copies. Product rules live in the functional spec, and phases and tasks in
`docs/development-plan.md`. For the web side, see `docs/web-architecture.md`.

Reference implementation: `src/SkillCert.Api/Features/Auth/Login.cs` and its tests in
`tests/SkillCert.Api.Tests/Features/Auth/LoginTests.cs`.

## Projects

| Project | Owns | Must not |
| --- | --- | --- |
| `SkillCert.Domain` | Entities, enums, pure rules (currency, compliance, completion date) | Reference EF Core, ASP.NET or anything with I/O |
| `SkillCert.Infrastructure` | `SkillCertDbContext`, entity configurations, migrations, Identity, seeders, blob store, PDF renderer | Contain HTTP concerns |
| `SkillCert.Api` | Feature slices (HTTP endpoints), auth setup, the nightly job | Put business rules in handlers that belong in Domain |
| `SkillCert.Migrator` | Applies migrations, then seeds demo data (users, AFA list, groups, review history) when `Seed:DemoData` is true | Run in the API process |

## The feature slice

One folder per feature area, one file per use case:

```
Features/<Area>/
  <Area>Feature.cs        # MapGroup("/api/...").WithTags("<Area>") and calls each endpoint's Map
  <UseCase>.cs            # request record, validator, endpoint (Map + HandleAsync)
```

A use-case file holds, in this order:

1. **Request / response records**, named for the use case (`LoginRequest`,
   `CurrentUserResponse`). They are top-level types, because OpenAPI schema names come from
   type names and two nested `Request` types would collide.
2. **Validator**, `<Request>Validator : AbstractValidator<<Request>>`, for shape and format
   rules only (required, email, lengths). Rules that need the database or the domain belong in
   the handler or the domain.
3. **Endpoint**, `public static class <UseCase>Endpoint` with:
   - `Map(RouteGroupBuilder group)`, which sets the route, `.WithName("<UseCase>")` (this
     becomes the OpenAPI `operationId`, and the web hook `use<UseCase>`), `.WithValidation<Request>()`
     when the use case has a body, every non-2xx status the client must handle
     (`.Produces(StatusCodes.Status401Unauthorized)`), and the authorization requirement.
   - `internal static async Task<Results<...>> HandleAsync(...)`, the handler. Dependencies
     are method parameters (DbContext, `SignInManager`, `TimeProvider`, `CancellationToken`).
     It returns `TypedResults`, so every outcome is in the signature and in OpenAPI.

Register the area once in `Program.cs` (`app.Map<Area>Feature()`). Validators are picked up
automatically (`AddValidatorsFromAssemblyContaining<Program>`).

**The OpenAPI tag is the contract with the web app.** Each tag maps to exactly one web
feature in `web/orval.config.ts`, and web generation fails on an unmapped tag. A new area
therefore needs a matching `featureByTag` entry.

## Validation

`Common/ValidationFilter.cs` runs the request's validator before the handler.
- **On failure:** it returns `400` as an `HttpValidationProblemDetails`, with `errors` keyed
  by camelCase property name (`email`, `password`).
- **On success:** the handler can assume a well-formed request.
- **Error messages** are English and for developers. The web app maps *which* field failed
  to its own translated copy (web architecture §3).

## Responses and errors

| Situation | Status | Body |
| --- | --- | --- |
| Malformed request | 400 | ValidationProblem from the filter |
| Not signed in | 401 | none, from the cookie handler (never a redirect) |
| Signed in, not allowed | 403 | none |
| Not found | 404 | none |
| Business rule refused (e.g. duplicate competency in a basket) | 409 or 422 | `TypedResults.Problem` with a stable `type` the web app can branch on |

Auth failures stay bare on purpose: login answers 401 for a wrong password, an unknown
email, a locked account and a deactivated user alike.

## Time

Inject `TimeProvider` (registered as `TimeProvider.System`) and never call `DateTime.Now`.
Server time is authoritative for `ReviewedAt`, expiry and the nightly job. Tests swap in a
fake clock.
An instant that is both stored and returned (e.g. `ReviewedAt`) comes from
`time.GetUtcNowForStorage()`, truncated to Postgres's microsecond precision, so the response matches
what a later read returns.

## Testing

`tests/SkillCert.Api.Tests` runs the real API through `WebApplicationFactory<Program>`
against a throwaway Postgres 18.3 container (`Infrastructure/ApiFactory.cs`):
- It applies the real migrations and seeds the demo users with the password
  `ApiFactory.DemoPassword`.
- One container serves the whole run (`[Collection(ApiCollection.Name)]`).
- A test that changes shared data restores it in `finally` (see
  `Deactivated_user_cannot_sign_in`).

Per slice:
- **Test file:** one per use case, mirroring the slice path (`Features/<Area>/<UseCase>Tests.cs`).
- **Test names:** sentences, e.g. `Wrong_password_or_unknown_email_both_return_a_bare_401`.
- **Coverage:** the happy path, each validation tier the client relies on, each auth outcome
  (401 / 403), and each business-rule refusal.
- **Calls:** over HTTP only (`client.LoginAsync(email)`, then requests). Use
  `api.WithDbAsync` only to arrange or inspect state the API has no endpoint for.

Pure domain rules (currency, compliance) are tested in `SkillCert.Domain.Tests` without a
database.

Docker must be running for `dotnet test`.

## Adding a use case: checklist

1. Create `Features/<Area>/<UseCase>.cs` from `Login.cs`'s shape.
2. If the area is new, add `<Area>Feature.cs`, call it from `Program.cs`, and add the tag to
   `web/orval.config.ts`.
3. Write `tests/SkillCert.Api.Tests/Features/<Area>/<UseCase>Tests.cs`.
4. Run `dotnet build`. It rewrites `web/openapi/skillcert.json`; commit it. Then run
   `npm run gen` in `web/` for the typed hook.
5. Run `dotnet test`.
