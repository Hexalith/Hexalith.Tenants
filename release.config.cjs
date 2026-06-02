const expectedPackageIds = [
  'Hexalith.Tenants.Contracts',
  'Hexalith.Tenants.Client',
  'Hexalith.Tenants.Server',
  'Hexalith.Tenants.Testing',
];

module.exports = {
  branches: ['main'],
  plugins: [
    '@semantic-release/commit-analyzer',
    '@semantic-release/release-notes-generator',
    [
      '@semantic-release/exec',
      {
        prepareCmd: [
          'rm -rf ./nupkgs',
          'python3 scripts/pack-release-packages.py ./nupkgs ${nextRelease.version}',
          'python3 scripts/validate-nuget-packages.py ./nupkgs',
          'python3 scripts/validate-consumer-package-references.py ./nupkgs',
        ].join(' && '),
        publishCmd: [
          'python3 scripts/validate-nuget-packages.py ./nupkgs',
          'python3 scripts/validate-consumer-package-references.py ./nupkgs',
          'dotnet nuget push ./nupkgs/*.nupkg --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json --skip-duplicate',
        ].join(' && '),
      },
    ],
    [
      '@semantic-release/github',
      {
        assets: ['nupkgs/*.nupkg'],
      },
    ],
  ],
};
