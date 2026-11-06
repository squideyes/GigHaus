-- GigHaus consolidated schema

CREATE TABLE IF NOT EXISTS artists (
    id              UUID PRIMARY KEY,
    name            VARCHAR(200) NOT NULL,
    bio             TEXT,
    email           VARCHAR(200) NOT NULL,
    phone           VARCHAR(50),
    website         VARCHAR(500),
    stripe_connect_account_id VARCHAR(200),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_artists_email ON artists (email);

CREATE TABLE IF NOT EXISTS hosts (
    id                  UUID PRIMARY KEY,
    name                VARCHAR(200) NOT NULL,
    email               VARCHAR(200) NOT NULL,
    phone               VARCHAR(50),
    address             VARCHAR(500),
    city                VARCHAR(100),
    state               VARCHAR(50),
    zip_code            VARCHAR(20),
    max_capacity        INT NOT NULL DEFAULT 0,
    venue_description   TEXT,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_hosts_email ON hosts (email);

CREATE TABLE IF NOT EXISTS events (
    id                          UUID PRIMARY KEY,
    title                       VARCHAR(300) NOT NULL,
    description                 TEXT,
    artist_id                   UUID NOT NULL REFERENCES artists(id),
    host_id                     UUID NOT NULL REFERENCES hosts(id),
    event_date                  DATE NOT NULL,
    start_time                  TIME NOT NULL,
    end_time                    TIME,
    max_attendees               INT NOT NULL DEFAULT 0,
    pricing_mode                VARCHAR(50) NOT NULL DEFAULT 'SuggestedOnly',
    suggested_donation_amount   NUMERIC(18,2) NOT NULL DEFAULT 0,
    minimum_contribution_amount NUMERIC(18,2),
    allow_pay_what_you_can      BOOLEAN NOT NULL DEFAULT TRUE,
    allow_reserve_now_pay_later BOOLEAN NOT NULL DEFAULT FALSE,
    allow_cash_at_door          BOOLEAN NOT NULL DEFAULT TRUE,
    allow_online_payment        BOOLEAN NOT NULL DEFAULT TRUE,
    suggested_donation_label    VARCHAR(500) NOT NULL DEFAULT 'Suggested donation to support the artist',
    pricing_disclosure_text     VARCHAR(2000) NOT NULL DEFAULT 'Payments support the artist and event hosting. These payments are not charitable donations and are not tax-deductible unless explicitly stated otherwise.',
    is_published                BOOLEAN NOT NULL DEFAULT FALSE,
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS reservations (
    id              UUID PRIMARY KEY,
    event_id        UUID NOT NULL REFERENCES events(id),
    attendee_name   VARCHAR(200) NOT NULL,
    attendee_email  VARCHAR(200) NOT NULL,
    attendee_phone  VARCHAR(50),
    number_of_guests INT NOT NULL DEFAULT 1,
    status          VARCHAR(50) NOT NULL DEFAULT 'Pending',
    note            TEXT,
    confirmed_at    TIMESTAMPTZ,
    cancelled_at    TIMESTAMPTZ,
    checked_in_at   TIMESTAMPTZ,
    cancellation_reason TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS ix_reservations_event_status ON reservations (event_id, status);
CREATE INDEX IF NOT EXISTS ix_reservations_attendee_email ON reservations (attendee_email);

CREATE TABLE IF NOT EXISTS payment_records (
    id                          UUID PRIMARY KEY,
    reservation_id              UUID NOT NULL REFERENCES reservations(id),
    gross_amount                NUMERIC(18,2) NOT NULL DEFAULT 0,
    processor_fee_amount        NUMERIC(18,2) NOT NULL DEFAULT 0,
    platform_fee_amount         NUMERIC(18,2) NOT NULL DEFAULT 0,
    net_amount                  NUMERIC(18,2) NOT NULL DEFAULT 0,
    attributable_to_artist_amount NUMERIC(18,2) NOT NULL DEFAULT 0,
    payment_method_type         VARCHAR(50) NOT NULL DEFAULT 'Online',
    status                      VARCHAR(50) NOT NULL DEFAULT 'Pending',
    provider                    VARCHAR(100),
    provider_reference          VARCHAR(500),
    stripe_payment_intent_id    VARCHAR(500),
    stripe_connected_account_id VARCHAR(200),
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS ix_payment_records_stripe_intent ON payment_records (stripe_payment_intent_id)
    WHERE stripe_payment_intent_id IS NOT NULL;

-- Settlement summaries — per-event financial rollup
CREATE TABLE IF NOT EXISTS settlement_summaries (
    id                          UUID PRIMARY KEY,
    event_id                    UUID NOT NULL REFERENCES events(id),
    total_online_collected      NUMERIC(18,2) NOT NULL DEFAULT 0,
    total_door_cash_collected   NUMERIC(18,2) NOT NULL DEFAULT 0,
    total_other_collected       NUMERIC(18,2) NOT NULL DEFAULT 0,
    total_gross_collected       NUMERIC(18,2) NOT NULL DEFAULT 0,
    total_processor_fees        NUMERIC(18,2) NOT NULL DEFAULT 0,
    total_platform_fees         NUMERIC(18,2) NOT NULL DEFAULT 0,
    total_fees                  NUMERIC(18,2) NOT NULL DEFAULT 0,
    net_attributable_to_artist  NUMERIC(18,2) NOT NULL DEFAULT 0,
    total_reservations          INT NOT NULL DEFAULT 0,
    total_checked_in            INT NOT NULL DEFAULT 0,
    total_guests                INT NOT NULL DEFAULT 0,
    total_comps                 INT NOT NULL DEFAULT 0,
    is_finalized                BOOLEAN NOT NULL DEFAULT FALSE,
    payout_status               VARCHAR(50) NOT NULL DEFAULT 'Pending',
    stripe_payout_id            VARCHAR(500),
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_settlement_summaries_event ON settlement_summaries (event_id);

