# Publication Report

## Version

Requested version: `3.0.0-rc.1`.

Publication status: Blocked before publication.

## Candidate commit

Local HEAD at RC.6 start: `b4e0323bee471e0758e2fcc73c69d414e719dcca`.

The required publication authorization file
`.sdd/release-3.0.0-rc.1/PUBLISH-AUTHORIZATION.md` was not present, so the
authorized candidate commit could not be validated.

Existing SDD evidence also contains remote artifact metadata for
`44f690195f9a06703e04c051411047b993644186`, while the local release branch
HEAD is `b4e0323bee471e0758e2fcc73c69d414e719dcca`. This requires explicit
authorization before any publication action.

## Tag

Not created.

Required tag: `v3.0.0-rc.1`.

## Workflow run

No final RC.6 workflow was started.

Existing documented remote qualification run: `30476842589`, associated with
commit `44f690195f9a06703e04c051411047b993644186`.

## NuGet packages

No packages were published.

Expected package IDs:

- `Dapper.FluentMap`
- `Dapper.FluentMap.Analyzers`
- `Dapper.FluentMap.Generators`
- `Dapper.FluentMap.DependencyInjection`
- `Dapper.FluentMap.Dommel`

## Package hashes

No final RC.6 package hashes were produced or published.

The versioned `.sdd/release-3.0.0-rc.1/artifacts.json` still records hashes for
the earlier remote qualification commit
`44f690195f9a06703e04c051411047b993644186`; these were not reused for
publication.

## SourceLink

Not revalidated for RC.6 because the mandatory authorization gate failed before
remote final qualification.

## Provenance

Not revalidated for RC.6 because the mandatory authorization gate failed before
remote final qualification.

## GitHub Release

Not created.

## Verification

Performed before stopping:

- Confirmed required authorization file is missing:
  `.sdd/release-3.0.0-rc.1/PUBLISH-AUTHORIZATION.md`.
- Confirmed local branch: `release/3.0.0-rc.1`.
- Confirmed local HEAD: `b4e0323bee471e0758e2fcc73c69d414e719dcca`.
- Confirmed working tree was clean before this documentation update.
- Confirmed local branch was ahead of `origin/release/3.0.0-rc.1`.
- Confirmed existing artifact manifest references commit
  `44f690195f9a06703e04c051411047b993644186`.

Not executed because publication authorization was absent:

- final restore/build/test gate;
- consumer smoke;
- pack from final candidate commit;
- artifact validation;
- vulnerability audit;
- push;
- final remote workflow;
- NuGet publication;
- tag creation;
- GitHub pre-release creation.

## Incidents

Critical authorization gate failed: missing
`.sdd/release-3.0.0-rc.1/PUBLISH-AUTHORIZATION.md`.

Additional operational note: querying `origin` through the configured fetch URL
failed with SSH public-key authentication, so no remote mutation was attempted.

## Result

Blocked. `3.0.0-rc.1` was not published.
