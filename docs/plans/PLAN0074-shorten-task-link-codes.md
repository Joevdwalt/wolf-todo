# Shorten task link codes

Use the first eight lowercase hexadecimal digits of SHA-256 after `wt1-`.
Update validation, examples, and the task-link spec. Reject codes matching
multiple current locations during generation and opening; keep navigation
unchanged on failure. Verify a known SHA8 value and a real collision fixture,
run repository tests, and refresh the code graph.
