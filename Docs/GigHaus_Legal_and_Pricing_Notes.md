# GigHaus Legal and Pricing Notes

_Last updated: November 6, 2026_

## Update: GigHaus is free (November 6, 2026)

GigHaus is now a free service, which changes some of what follows:

- **No platform fee.** GigHaus doesn't charge hosts, artists or guests, and takes no cut of contributions. Mentions of a platform fee below are historical.
- **No online payments yet.** Guests contribute at the door, so GigHaus doesn't currently collect or hold money for anyone. The sections on Stripe Connect and money transmission apply if online payments are added.
- **Wording to review.** The site says GigHaus takes "no cut" and is "free for hosts". Those claims are accurate while contributions stay at the door. If online payments launch, processor fees would reduce what artists receive, so the wording must change. The guidance below against "no-fee donation" still applies.
- **Planned features to review.** Texting guests needs their consent, and using a guest's review in publicity needs their permission. Merch sales and extra donations stay outside this note's money concerns only if GigHaus coordinates them without collecting or holding the money. Ko-fi is optional and pays artists directly.

The disclosure and suggested-donation guidance below still applies, and the show page follows it. The site needs terms of service and a privacy policy before launch, and this whole approach still needs review by an attorney and CPA.

## Purpose

These notes update the GigHaus product direction in two important ways:

1. Money for shows should be framed as a **suggested donation / contribution to support the artist**, not as a conventional ticket fee.
2. The product must be designed with care around **how GigHaus collects money on an artist's behalf and later distributes it**, because that creates legal, tax, disclosure, payments, and marketplace-platform issues.

This is a product/legal design note, **not legal advice**. Before launch, GigHaus should be reviewed by a U.S. attorney and CPA familiar with marketplaces, live events, and payments compliance.

## Core conclusion

GigHaus should **not** describe attendee payments as charitable donations unless the recipient is a qualified charitable organization and the facts support charitable treatment. Under IRS guidance, charitable deductions generally apply only for contributions to qualified organizations, and gifts to individuals are not deductible. citeturn261082search3turn261082search12

For ordinary house concerts where money is meant for the performer, GigHaus should instead use language such as:

- **Suggested donation**
- **Suggested contribution to support the artist**
- **Pay what you can**
- **Recommended contribution**

And, unless a real nonprofit structure applies, GigHaus should clearly state:

- **Not tax-deductible**
- **Supports the artist / performer**
- **General admission; no assigned seating**

## Why this matters

If a user sees the word "donation," they may assume:

- the payment is charitable,
- it may be tax-deductible,
- GigHaus is acting like a nonprofit fundraiser,
- or the money is being handled under rules that do not actually apply.

That creates unnecessary legal and consumer-protection risk.

The IRS says deductible charitable contributions generally must be made to a **qualified organization**, and gifts to individuals are not deductible. citeturn261082search3turn261082search12turn261082search15

## Product-language recommendation

### Public-facing pricing language

Use wording like:

- **Suggested donation: $25**
- **Suggested donation to support the artist: $25**
- **Recommended contribution: $25**
- **Pay what you can; suggested donation is $25**

### Required disclosure near payment/reservation

Include plain language such as:

> Payments support the artist and event hosting. These payments are not charitable donations and are not tax-deductible unless explicitly stated otherwise.

That disclosure should be clear and conspicuous. FTC guidance repeatedly emphasizes that disclosures needed to avoid deception should be clear and conspicuous, and since May 12, 2025, the FTC's unfair-or-deceptive-fees rule has specifically targeted misleading total-price practices for live-event tickets. citeturn261082search1turn261082search4turn261082search7turn261082search10

## Important nuance: “suggested donation” versus “ticket fee” versus “required payment”

GigHaus needs to support different event policies.

### Option A — True pay-what-you-can / suggested contribution
The host presents an amount as suggested, but the attendee may reserve or attend without paying that exact amount.

### Option B — Required minimum contribution
The host says a contribution is required to attend.

### Option C — Reservation now, contribute at the door
The attendee reserves in advance and pays later.

From a consumer-protection perspective, GigHaus should avoid disguising a mandatory price as something optional. If attendance really requires a minimum amount, the UI should say so plainly. The FTC's current live-event fee rule is aimed at obscuring or misrepresenting total prices. citeturn261082search1turn261082search4

## Recommendation for GigHaus pricing model

GigHaus should model money using the concept of a **Suggested Donation Policy**, not a fee schedule.

Suggested event fields:

- `SuggestedDonationAmount`
- `MinimumContributionAmount` nullable
- `AllowPayWhatYouCan`
- `AllowReserveNowPayLater`
- `AllowCashAtDoor`
- `SuggestedDonationLabel`
- `PricingDisclosureText`

Behavior:

- If no minimum is required, label it **Suggested donation**.
- If attendance requires payment, disclose the minimum clearly.
- If reserve-now-pay-later is allowed, show that plainly.
- If cash at door is allowed, show that plainly.

## Collecting money on the artist’s behalf

This is the bigger structural issue.

If GigHaus collects funds from attendees and then later routes money to artists, GigHaus starts to look like a **platform / marketplace handling third-party funds**.

That matters because:

- the payment processor integration must support platform money movement,
- onboarding/verification of payees may be required,
- tax reporting may be implicated,
- statement descriptors and merchant-of-record issues matter,
- refund/dispute handling must be defined,
- and state money-transmission questions can arise depending on the exact model and facts.

## Strong product recommendation

Do **not** build GigHaus as an informal “collect money in our account and manually send it later” system.

Instead, if GigHaus will collect online payments for artists, it should use a true **platform payments architecture**, with Stripe Connect as the primary design target.

Stripe documents that platform/marketplace flows can use **destination charges** or **separate charges and transfers**, and that with destination charges or separate charges and transfers, the platform is generally the merchant of record for those payments. citeturn261082search5turn261082search8turn261082search14

Stripe also documents that platforms can retain funds in the platform balance and manage transfers/payouts to connected accounts in appropriate Connect configurations. citeturn261082search17turn261082search20

## Practical interpretation for GigHaus

GigHaus should assume this model:

- **GigHaus is a platform**
- **Hosts and/or artists are connected recipients / payees**
- **Funds flow through Stripe Connect-style rails**
- **GigHaus maintains internal ledger records for reservations, payments, adjustments, fees, and amounts attributable to the artist**

That is much safer and cleaner than running everything through one generic Stripe account and manually pushing money around.

## Merchant-of-record and disclosure implications

Stripe notes that, depending on Connect charge type, the platform can be the merchant of record. citeturn261082search14turn261082search5

For GigHaus, that means the product must eventually make explicit choices about:

- who appears on the attendee’s card statement,
- who handles refunds,
- who handles disputes/chargebacks,
- whether the artist, host, or platform bears processing fees,
- and what terms the attendee agrees to.

These are not merely engineering details. They affect customer support, trust, and legal risk.

## Tax-deductibility and receipting

GigHaus should **not** issue charitable-style receipts by default.

Instead, it should issue standard payment confirmations / reservation confirmations that say things like:

- reservation confirmed,
- amount paid,
- event details,
- support of artist,
- not tax-deductible unless explicitly stated.

If a host is genuinely partnering with a qualified nonprofit and wants to run a truly charitable flow, that should be treated as a separate future mode with separate review.

## State law and money-transmission caution

Whether GigHaus would need money-transmitter analysis depends heavily on the exact operational model, contracts, and payment flow. I have **not** concluded from public web research alone that GigHaus is or is not a money transmitter.

What I can say is:

- once a platform collects money for third parties and later distributes it,
- especially across many hosts/artists,
- legal review becomes important.

Using Stripe Connect does **not by itself** answer every legal question, but it is a much more appropriate starting point than an ad hoc collect-and-forward model.

## Recommended legal/product stance for v1

For v1, GigHaus should adopt the following rules:

1. **Call it suggested donation / contribution, not charitable donation.**
2. **Do not imply tax deductibility.**
3. **Disclose clearly whether payment is optional, suggested, or required.**
4. **Use Stripe Connect-style architecture if GigHaus collects online money destined for artists.**
5. **Maintain an internal ledger of what is attributable to the artist.**
6. **Do not promise instant or off-platform payouts without a real payout workflow.**
7. **Have counsel review the payout model before public launch.**

## Domain-model impact

The system should reflect suggested-donation logic directly.

Recommended concepts:

### Event pricing / contribution policy
- `SuggestedDonationAmount`
- `MinimumContributionAmount`
- `PricingMode` (`SuggestedOnly`, `MinimumRequired`, `ReserveNowPayLater`, `DoorOnly`, etc.)
- `AllowCashAtDoor`
- `AllowOnlinePayment`
- `PricingDisclosureText`

### Payment record
- `GrossAmount`
- `ProcessorFeeAmount`
- `PlatformFeeAmount` if ever used
- `NetAmount`
- `PaymentMethodType`
- `Provider`
- `ProviderReference`
- `AttributableToArtistAmount`

### Settlement summary
- total suggested contributions collected
- total online collected
- total door cash/manual collected
- total fees
- total comps / waived
- net attributable to artist
- payout status

## UX copy recommendations

### Good examples
- “Suggested donation: $25”
- “Support the artist with a suggested donation of $25”
- “Pay what you can. Suggested donation is $25.”
- “Reservation confirmed. Your contribution supports the artist and is not tax-deductible.”

### Avoid
- “Tax receipt”
- “Charitable donation” unless truly applicable
- “No-fee donation” if fees still affect settlement
- “Optional” when the amount is actually required for entry

## Engineering recommendation for the next build pass

The next GigHaus build/spec pass should:

1. Replace any “fee” terminology with **suggested donation / contribution** terminology.
2. Add explicit event pricing-policy fields around optional versus required contribution.
3. Design payments around **Stripe Connect** abstractions, not a plain single-merchant charge flow.
4. Keep artist distributions inside a real payout/ledger model.
5. Add clear disclosure text to public event and checkout/reservation screens.
6. Keep tax-deductible wording entirely out of the product unless a special nonprofit mode is introduced later.

## Bottom line

GigHaus can absolutely support house concerts funded by attendee contributions for artists.

But the safest framing is:

- **suggested donation / contribution to support the artist**
- **not a charitable donation by default**
- **clear disclosure of whether payment is optional or required**
- **platform-style payment routing for any collect-and-distribute flow**

That product posture is much more consistent with current IRS deductibility rules, FTC disclosure expectations, and Stripe’s documented marketplace/platform payment patterns. citeturn261082search3turn261082search12turn261082search1turn261082search5turn261082search14
