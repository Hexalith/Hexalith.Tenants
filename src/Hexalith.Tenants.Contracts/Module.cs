using Hexalith.McpCli.Abstractions;

// Platform operations declare their request scope individually. Single-tenant reads use the
// caller's explicit canonical tenant; the Gateway maps authorized reads to retained storage.
[assembly: HexalithModule(
    "tenants",
    "Manage tenant lifecycle, memberships, roles, configuration and audit. Commands and cross-tenant reads use their declared platform scope; single-tenant reads require an explicit canonical tenant. A global administrator may manage every tenant.",
    IdentifierKind.String)]
