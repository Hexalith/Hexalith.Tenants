# Private documented column-filter contract

`ColumnModel.cs:90–100` explicitly documents public `SupportsFilter` results: true for Text/Numeric/Enum/DateTime and false for Boolean/Collection/Unsupported. The existing ordinary test asserts Unsupported=false. This separate seven-category `MutationErrorRegression` candidate tests that existing public IR/getter contract directly. It does not claim a generated UI effect or introduce filtering behavior.

On exact b34 tracked source plus the owner's four preserved reviewed test files, the private baseline passes7/7. Exact literal controls14139–14143 all compile and fail the appropriate cases;14143 fails the three documented false categories. No numeric hashes, arbitrary category extensions, product changes, gate changes or accepted triage are included.

`private-column-filter-proposal.patch` contains one owner-style file. `attempt-01/verification.json` retains exact source/test hashes, commands, raw output and TRX; `package-check-02/` retains structural/style and read-only apply checks. All useful evidence is outside disposable scratch. The owner remains unchanged at completion of this first sequential task.

```text
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/run-controls.py
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/package-proposal.py
python3 <this-directory>/final-audit.py
```

Fresh owned `/dev/shm` workspaces/caches use read-only b34/Builds73e60af8 archives, MTP/Release, two processors and disabled server reuse. Final summary/manifests bind the results and cleanup. Parent owns any source integration and full qualification.
