# Expose task codes in CLI listing

Add `task_code` to every task in `wtodo list`, using the shared SHA8 location
code generator. Preserve existing fields, ordering, project filtering, and
read-only behavior. Document the field in SPEC0019, SPEC0023, and README.

Verify root and completed nested task codes in CLI JSON output, run repository
tests, and refresh the code graph.
