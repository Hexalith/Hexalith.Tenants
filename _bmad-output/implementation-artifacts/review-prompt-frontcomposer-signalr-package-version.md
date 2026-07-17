---
title: 'Review duplicate SignalR package version fix'
type: 'review-prompt'
status: 'pending-external-review'
---

# Review prompt

Review the uncommitted change in the `references/Hexalith.FrontComposer`
submodule with extreme skepticism. The intended fix removes the duplicate
`Microsoft.AspNetCore.SignalR.Client` `PackageVersion` declaration from
`Directory.Packages.props`; the package remains centrally defined by the
imported `Hexalith.Builds` props at version `10.0.10`.

Changed file:

- `references/Hexalith.FrontComposer/Directory.Packages.props`

Validation already performed:

- `dotnet restore src/Hexalith.FrontComposer.Testing/Hexalith.FrontComposer.Testing.csproj`
- Result: all projects are up-to-date for restore.

Find at least ten actionable issues or explicitly explain why fewer than ten
exist. Check import precedence, package-version ownership, other projects that
consume the package, line-ending/formatting integrity, and whether the change
could alter release or consumer restore behavior. Return a Markdown list of
findings only, classified as patch, defer, or reject.
