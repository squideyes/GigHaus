-- Sign-in accounts, host approval and the sales pipeline

ALTER TABLE hosts ADD COLUMN IF NOT EXISTS status VARCHAR(50) NOT NULL DEFAULT 'Pending';
ALTER TABLE hosts ADD COLUMN IF NOT EXISTS approved_at TIMESTAMPTZ;

-- Hosts that predate approval stay usable.
UPDATE hosts SET status = 'Approved', approved_at = NOW();

CREATE TABLE IF NOT EXISTS users (
    id                  UUID PRIMARY KEY,
    email               VARCHAR(256) NOT NULL,
    normalized_email    VARCHAR(256) NOT NULL,
    display_name        VARCHAR(200) NOT NULL,
    password_hash       TEXT,
    security_stamp      VARCHAR(100) NOT NULL,
    email_confirmed     BOOLEAN NOT NULL DEFAULT FALSE,
    lockout_end_utc     TIMESTAMPTZ,
    lockout_enabled     BOOLEAN NOT NULL DEFAULT TRUE,
    access_failed_count INT NOT NULL DEFAULT 0,
    role                VARCHAR(50) NOT NULL,
    host_id             UUID REFERENCES hosts(id),
    is_deleted          BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at          TIMESTAMPTZ,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_users_normalized_email ON users (normalized_email) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_users_role ON users (role) WHERE NOT is_deleted;

CREATE TABLE IF NOT EXISTS leads (
    id                  UUID PRIMARY KEY,
    name                VARCHAR(200) NOT NULL,
    email               VARCHAR(200) NOT NULL,
    phone               VARCHAR(50),
    city                VARCHAR(100),
    state               VARCHAR(50),
    estimated_capacity  INT,
    message             TEXT,
    status              VARCHAR(50) NOT NULL DEFAULT 'New',
    staff_notes         TEXT,
    is_deleted          BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at          TIMESTAMPTZ,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS ix_leads_status ON leads (status) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_leads_email ON leads (email) WHERE NOT is_deleted;

CREATE INDEX IF NOT EXISTS ix_events_host ON events (host_id);
