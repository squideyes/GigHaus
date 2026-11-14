"""Merge the per-batch site reviews into a venue shortlist and an artist list.

Usage: python merge.py <research dir>
Writes shortlist.csv, all_reviewed.csv, artists.csv and summary.txt there.
"""
import csv, glob, json, os, sys, collections

out = sys.argv[1]
crawl = json.load(open(os.path.join(out, "crawl.json"), encoding="utf-8"))
by_slug = {r.get("slug"): r for r in crawl["crawled"]}
rows = []
for f in sorted(glob.glob(os.path.join(out, "review", "batch_*.json"))):
    rows += json.load(open(f, encoding="utf-8"))

GOOD = ("yes", "likely")
RANK = {"yes": 0, "likely": 1}


def flat(r):
    c = by_slug.get(r["slug"], {})
    arts = r.get("recent_artists") or []
    return {
        "Venue": r.get("name") or c.get("name", ""),
        "City": r.get("city") or c.get("city", ""),
        "ST": r.get("st") or c.get("st", ""),
        "Type": c.get("type") or r.get("type", ""),
        "Website": c.get("website") or r.get("site", ""),
        "Booking Contact": c.get("contact", ""),
        "Active": r.get("active", ""),
        "Latest show": r.get("latest_show", ""),
        "Donation basis": r.get("donation_basis", ""),
        "Donation evidence": r.get("donation_evidence", ""),
        "Site donation wording": ", ".join(c.get("donation_hits") or []),
        "Pay methods": ", ".join(r.get("pay_methods") or []),
        "Artists found": len(arts),
        "Notes": r.get("notes", ""),
        "CSV notes": c.get("notes", ""),
    }


flat_rows = [flat(r) for r in rows]
cols = list(flat_rows[0].keys())


def write(name, data, fields):
    with open(os.path.join(out, name), "w", newline="", encoding="utf-8-sig") as f:
        w = csv.DictWriter(f, fieldnames=fields)
        w.writeheader()
        w.writerows(data)




def listed(r):
    """House concerts: any that look active. Intimate venues: active, and the
    site mentions donations in some form (donation basis, or donation wording)."""
    if r["Active"] not in GOOD:
        return False
    if r["Type"] == "House Concert":
        return True
    return r["Donation basis"] in GOOD or bool(r["Site donation wording"])


short = [r for r in flat_rows if listed(r)]
short.sort(key=lambda r: (r["Type"] != "House Concert", RANK.get(r["Donation basis"], 2), RANK[r["Active"]], r["Venue"].lower()))
write("shortlist.csv", short, cols)
write("all_reviewed.csv", sorted(flat_rows, key=lambda r: r["Venue"].lower()), cols)

short_slugs = {r["slug"] for r, fr in zip(rows, flat_rows) if listed(fr)}
arts = []
for r in rows:
    c = by_slug.get(r["slug"], {})
    for a in r.get("recent_artists") or []:
        name = (a.get("name") or "").strip()
        if name:
            arts.append({"Artist": name, "Date": a.get("date", ""), "Venue": r.get("name", ""), "City": r.get("city", ""),
                         "ST": r.get("st", ""), "Venue type": c.get("type", ""),
                         "Venue on shortlist": "yes" if r["slug"] in short_slugs else "no"})
arts.sort(key=lambda a: (a["Artist"].lower(), a["Date"]))
write("artists.csv", arts, list(arts[0].keys()))

uniq = collections.Counter(a["Artist"].lower() for a in arts)
pay = collections.Counter(m.strip() for r in short for m in r["Pay methods"].split(",") if m.strip())
lines = [
    f"reviewed venues: {len(rows)}",
    f"shortlist (active house concerts + active intimate venues mentioning donations): {len(short)}",
    f"  house concerts: {sum(r['Type'] == 'House Concert' for r in short)}",
    f"  intimate venues: {sum(r['Type'] == 'Intimate Venue' for r in short)}",
    "  donation basis on shortlist: " + str(dict(collections.Counter((r['Type'], r['Donation basis']) for r in short).most_common())),
    f"artist appearances: {len(arts)}; distinct artists: {len(uniq)}",
    f"artists at 3+ venues: {', '.join(n for n, c in uniq.most_common(40) if c >= 3)}",
    f"pay methods on shortlist: {dict(pay.most_common())}",
    "house concerts reviewed by active/donation: " + str(dict(collections.Counter(
        (r['Active'], r['Donation basis']) for r in flat_rows if r['Type'] == 'House Concert').most_common())),
]
open(os.path.join(out, "summary.txt"), "w", encoding="utf-8").write("\n".join(lines))
print("\n".join(lines))
