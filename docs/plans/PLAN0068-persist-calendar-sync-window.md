# Persistent calendar sync window

Implement a rolling calendar window covering today and seven days either side.

- Load matching disk-cached agendas before starting background synchronization.
- Persist window entries atomically in application state, separate from todos.
- Retain entries during refresh and on failure; replace successful day results.
- Refresh the window at startup, date rollover, config reload, and manual refresh.
- Limit concurrent requests and cancel obsolete work when configuration changes.
- Verify window boundaries, restart persistence, failed refreshes, deleted events,
  and corrupt or mismatched caches with automated tests.
