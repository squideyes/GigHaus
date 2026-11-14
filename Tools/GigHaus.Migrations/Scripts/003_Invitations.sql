-- GigHaus is invite only. A lead is now a request for an invitation, and the
-- admin answers it by emailing a single-use sign-up link.

ALTER TABLE leads ADD COLUMN IF NOT EXISTS invite_token_hash VARCHAR(64);
ALTER TABLE leads ADD COLUMN IF NOT EXISTS invited_at TIMESTAMPTZ;
ALTER TABLE leads ADD COLUMN IF NOT EXISTS invite_expires_at TIMESTAMPTZ;

CREATE UNIQUE INDEX IF NOT EXISTS ix_leads_invite_token_hash ON leads (invite_token_hash)
    WHERE invite_token_hash IS NOT NULL;
