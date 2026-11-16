# FCL-P2: preserve audited publication bytes

The guard now remembers the original text and writes only when the exact shared normalization changes it. Unsafe scanning still runs for every selected file before publication, and all prior refusal/failure semantics remain intact. The shared policy and strict validator bytes are unchanged.

The new regression executes the real validator against an explicitly synthetic bounded fixture, verifies its OutputSha256 against actual report bytes, then runs aggregate preparation twice and compares every selected artifact byte. It fails before the repair and passes afterward. All ten publication methods pass:21 actual PowerShell executions, including the validator and all16 existing unsafe/missing/linked/directory refusals. Full raw logs/process records and exact input hashes are retained in the two uniquely named attempts.

The source delta is limited to the guard and its Python controls, separately authorized by the parent; fcl-p2.patch is reviewable. Both private wrapper roots were removed. Finite technical triage is preserved in its separate packet. No wider tests, mutation run, native score/current-source qualification, Git/remote or authority update occurred.
