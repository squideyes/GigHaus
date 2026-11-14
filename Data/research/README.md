# Venue and artist research

Paused on 2026-11-14. This folder holds a first pass at finding house concerts
and intimate venues that GigHaus could serve, and the artists who play them.
It was built from `Data/Troubador Concert Rooms.csv` (1,579 rows), which is
**not** in the repository; copy it into `Data/` by hand to re-run the crawl.

## Where it stands

| Step | Count |
|---|---|
| House Concert and Intimate Venue rows with a website | 693 |
| Facebook or Instagram only (not looked at yet) | 180 |
| Own websites crawled | 513 |
| Reachable | 477 |
| Reviewed | 447 |
| **Shortlist** | **230** |

The shortlist is 109 house concerts and 121 intimate venues.

- **House concerts:** every reviewed one that looks active, whatever it says
  about money. 78 are donation-based, 15 sell tickets, 16 don't say.
- **Intimate venues:** active, and the site mentions donations in some form.
  Only 14 take donations or tips for the artist. The other 107 just have
  donation wording somewhere, and most of those sell tickets and ask for
  donations to the venue itself. Filter on `Donation basis` = yes or likely
  for the strict set.
- **Artists:** 3,137 appearances by 2,686 distinct artists, mostly 2024 onward.
  1,575 appearances (1,355 artists) are at shortlisted venues.
- **Payments:** shortlisted sites mention cash, cheques, Venmo and PayPal.
  One mentions Stripe and none mention Ko-fi.

## Files

| File | What it is |
|---|---|
| `shortlist.csv` | The 230 venues, house concerts and clearest donation cases first |
| `all_reviewed.csv` | All 447 reviewed venues with the same columns |
| `artists.csv` | One row per artist appearance, with a `Venue on shortlist` flag |
| `summary.txt` | Counts printed by the last merge |
| `crawl.json` | Per-site crawl record (`crawled`) and the 180 social-only venues (`social_only`) |
| `text/` | Visible text scraped from each site, one file per venue |
| `bundles/` | The text condensed into 16 batches for review |
| `review/` | One JSON file per batch with the reviewers' verdicts |
| `crawl.py`, `merge.py` | The crawler and the merge script |

## How it was obtained

1. **Filter.** Keep House Concert and Intimate Venue rows that have a website.
   Set aside those whose website is a Facebook or Instagram address.
2. **Crawl** (`python crawl.py "../Troubador Concert Rooms.csv" .`). For each
   site, fetch the home page and up to five same-site links whose address or
   label looks relevant (concert, show, schedule, calendar, past, artist,
   donate, about, ticket). Keep the visible text, capped at 9,000 characters
   per page. Record the years seen, donation wording, ticketing wording and
   signs of a parked domain. Standard library only; certificate errors are
   ignored so that small sites with broken certificates can still be read.
3. **Bundle.** Drop parked domains and sites with under 400 characters of
   text. Condense each venue to about 9,000 characters (3,500 from the home
   page, 2,200 from each further page) plus up to six snippets around money
   words. Batches 1-8 hold all house concerts and the intimate venues whose
   text matched the donation wording (239 venues). Batches 9-16 hold the
   remaining intimate venues (208).
4. **Review.** Each batch was read by a Claude subagent (Sonnet) that judged
   every venue from the text alone and wrote `review/batch_N.json` with:
   `active` (yes, likely, no, unknown), `latest_show`, `donation_basis` (yes,
   likely, no, unknown), `donation_evidence`, `pay_methods`, `recent_artists`
   (name and date, 2024 onward) and `notes`.
   - *active*: "yes" for a show dated 2026 or late 2025, "likely" for 2025
     dates or an undated but current-looking schedule, "no" for defunct or
     nothing after 2023.
   - *donation_basis*: "yes" for suggested donation, pass the hat, tip jar or
     pay-what-you-can with the money going to the artist; "no" for fixed-price
     tickets, covers, or a nonprofit asking for donations to itself.
5. **Merge** (`python merge.py .`). Joins the reviews to the crawl records and
   writes the three CSVs and `summary.txt`. The shortlist rule lives in
   `listed()` in `merge.py`.

## How far to trust it

- No verdict has been checked by hand against the live site.
- Long pages were cut off, so some past-show archives are incomplete.
- About 30 venues had no show calendar in the scraped text. Their activity
  rating rests on things like a current copyright year.
- Sites that build their pages with JavaScript give thin text and may be
  wrongly rated inactive.
- Reviewers inferred the year where a site gave only day and month, and
  `Latest show` can be a future scheduled date.
- Distinct artists are counted by exact name, so spelling variants of one act
  count separately.
- 21 reachable intimate venues were never reviewed: 4 parked domains and 17
  with almost no text. 36 sites did not answer at all.

## What comes next

The 180 social-only venues are the main gap, and they are mostly what GigHaus
cares about: 162 house concerts and 15 intimate venues on Facebook (21 of them
Facebook groups) and 3 house concerts on Instagram. They are listed in
`crawl.json` under `social_only`.

1. **Look for another source first.** Web-search each venue name and city for
   its own website, a house-concert directory listing or event pages. No
   sign-in needed.
2. **Test signed-out reading.** Fetch about 10 Facebook pages without signing
   in to see how much is visible.
3. **Read the rest signed in**, in the owner's browser, read only, slowly and
   in small batches, house concerts first. Record the same fields as the
   review step. Stop at the first checkpoint or CAPTCHA. Meta's terms forbid
   automated collection, so the alternative is a checklist of the links,
   sorted by likelihood, to skim by hand. This choice is still open.
4. **Private groups** that can't be seen are recorded as such. No join
   requests.
5. **Merge** the results as further `review/batch_N.json` files so the
   shortlist and artist list pick them up.

Other loose ends:

- Spot-check a sample of shortlist verdicts against the live sites.
- Decide whether the loose rule for intimate venues is what is wanted, or
  whether to go back to donation basis only.
- Retry the 36 unreachable sites and the JavaScript-built ones with a real
  browser.
- Merge spelling variants in the artist list.
