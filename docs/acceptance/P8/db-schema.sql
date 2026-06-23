PRAGMA journal_mode = WAL;
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS pet_profile (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    name TEXT NOT NULL,
    created_at_utc TEXT NOT NULL,
    updated_at_utc TEXT NOT NULL,
    interaction_count INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS pet_status (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    state TEXT NOT NULL,
    window_left REAL NOT NULL,
    window_top REAL NOT NULL,
    updated_at_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS interaction_log (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    interaction_type TEXT NOT NULL,
    message TEXT NULL,
    state TEXT NOT NULL,
    window_left REAL NOT NULL,
    window_top REAL NOT NULL,
    created_at_utc TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_interaction_log_created_at_utc
    ON interaction_log (created_at_utc);
