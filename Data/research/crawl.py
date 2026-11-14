"""Crawl house-concert and intimate-venue websites from the Troubador CSV.

For each venue with its own website (not Facebook/Instagram) fetch the home
page plus a few likely pages (shows, schedule, past concerts, donations) and
save the visible text, so the sites can be judged for activity and donation
basis. Standard library only.

Usage: python crawl.py <csv> <outdir>
"""
import csv, json, re, ssl, sys, os, hashlib
import urllib.request, urllib.parse
from html.parser import HTMLParser
from concurrent.futures import ThreadPoolExecutor, as_completed

UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0 Safari/537.36"
LINK_WORDS = re.compile(r"concert|show|schedul|event|calendar|past|artist|upcoming|perform|donat|about|faq|season|archive|lineup|music|ticket|reserv", re.I)
DONATION = re.compile(r"suggested donation|donation|pass the hat|tip jar|love offering|pay what you|100% (of|goes)|all proceeds go|goes? (directly )?to the (artist|musician|performer)", re.I)
TICKET = re.compile(r"buy tickets|tickets? (are )?\$|eventbrite|ticketmaster|tixr|admission \$|cover charge", re.I)
MAX_PAGES = 6
MAX_CHARS = 9000
CTX = ssl.create_default_context()
CTX.check_hostname = False
CTX.verify_mode = ssl.CERT_NONE


class Text(HTMLParser):
    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.out, self.links, self.skip, self.title, self._in_title = [], [], 0, "", False
        self._href = None

    def handle_starttag(self, tag, attrs):
        if tag in ("script", "style", "noscript", "svg"):
            self.skip += 1
        if tag == "title":
            self._in_title = True
        if tag == "a":
            self._href = dict(attrs).get("href")
            self._atext = []
        if tag in ("p", "div", "br", "li", "tr", "h1", "h2", "h3", "h4", "td"):
            self.out.append("\n")

    def handle_endtag(self, tag):
        if tag in ("script", "style", "noscript", "svg") and self.skip:
            self.skip -= 1
        if tag == "title":
            self._in_title = False
        if tag == "a" and self._href:
            self.links.append((self._href, " ".join(self._atext).strip()))
            self._href = None

    def handle_data(self, data):
        if self._in_title:
            self.title += data
        if self.skip:
            return
        self.out.append(data)
        if self._href is not None:
            self._atext.append(data)

    def text(self):
        t = "".join(self.out)
        t = re.sub(r"[ \t\r\f\v ]+", " ", t)
        t = re.sub(r"\n\s*\n+", "\n", t)
        return t.strip()


def fetch(url, timeout=20):
    req = urllib.request.Request(url, headers={"User-Agent": UA, "Accept": "text/html,*/*", "Accept-Language": "en-US,en"})
    with urllib.request.urlopen(req, timeout=timeout, context=CTX) as r:
        ctype = r.headers.get("Content-Type", "")
        if "html" not in ctype and "text" not in ctype:
            return r.geturl(), "", r.headers.get("Last-Modified", "")
        raw = r.read(1_500_000)
        enc = r.headers.get_content_charset() or "utf-8"
        return r.geturl(), raw.decode(enc, errors="replace"), r.headers.get("Last-Modified", "")


def crawl(row):
    site = row["Website"].strip()
    res = {"name": row["Concert Presenter"], "city": row["City"], "st": row["ST"], "type": row["Type"],
           "website": site, "contact": row["Booking Contact"], "notes": row["Also..."], "pages": [], "error": ""}
    start = site if site.startswith("http") else "https://" + site
    html = None
    for attempt in (start, start.replace("https://", "http://", 1)):
        try:
            final, html, lastmod = fetch(attempt)
            break
        except Exception as e:  # noqa
            res["error"] = f"{type(e).__name__}: {e}"[:200]
    if html is None:
        return res, ""
    res["error"] = ""
    res["final_url"] = final
    res["last_modified"] = lastmod
    p = Text()
    try:
        p.feed(html)
    except Exception:
        pass
    res["title"] = p.title.strip()[:200]
    texts = [(final, p.text())]
    host = urllib.parse.urlparse(final).netloc.lower().removeprefix("www.")
    seen, cands = {final.rstrip("/")}, []
    for href, label in p.links:
        if not href or href.startswith(("mailto:", "tel:", "javascript:", "#")):
            continue
        u = urllib.parse.urljoin(final, href).split("#")[0]
        pu = urllib.parse.urlparse(u)
        if pu.netloc.lower().removeprefix("www.") != host:
            continue
        if re.search(r"\.(jpg|jpeg|png|gif|pdf|mp3|mp4|zip|ics)$", pu.path, re.I):
            continue
        key = u.rstrip("/")
        if key in seen:
            continue
        if LINK_WORDS.search(pu.path + " " + label):
            seen.add(key)
            cands.append(u)
    for u in cands[:MAX_PAGES - 1]:
        try:
            f2, h2, _ = fetch(u, timeout=15)
            q = Text()
            q.feed(h2)
            texts.append((f2, q.text()))
        except Exception:
            continue
    alltext = "\n".join(t for _, t in texts)
    res["pages"] = [u for u, _ in texts]
    res["chars"] = len(alltext)
    years = re.findall(r"\b(20[12]\d)\b", alltext)
    res["years"] = {y: years.count(y) for y in sorted(set(years))}
    res["donation_hits"] = sorted({m.group(0).lower() for m in DONATION.finditer(alltext)})[:8]
    res["ticket_hits"] = sorted({m.group(0).lower() for m in TICKET.finditer(alltext)})[:8]
    res["parked"] = bool(re.search(r"domain (is )?for sale|buy this domain|parked|godaddy|hugedomains|sedo|expired", alltext[:3000], re.I))
    body = "\n".join(f"=== PAGE {u}\n{t[:MAX_CHARS]}" for u, t in texts)
    return res, body


def main():
    src, out = sys.argv[1], sys.argv[2]
    os.makedirs(os.path.join(out, "text"), exist_ok=True)
    with open(src, newline="", encoding="utf-8-sig") as f:
        rows = [r for r in csv.DictReader(f)]
    rows = [r for r in rows if r["Type"] in ("House Concert", "Intimate Venue") and r["Website"].strip()]
    social = [r for r in rows if re.search(r"facebook\.com|instagram\.com", r["Website"], re.I)]
    own = [r for r in rows if r not in social]
    results = []
    with ThreadPoolExecutor(max_workers=24) as ex:
        futs = {ex.submit(crawl, r): r for r in own}
        for i, fut in enumerate(as_completed(futs), 1):
            r = futs[fut]
            try:
                res, body = fut.result()
            except Exception as e:  # noqa
                res, body = {"name": r["Concert Presenter"], "website": r["Website"], "type": r["Type"], "error": str(e)[:200]}, ""
            slug = re.sub(r"[^a-z0-9]+", "-", res["name"].lower()).strip("-")[:50] + "-" + hashlib.md5(res["website"].encode()).hexdigest()[:4]
            res["slug"] = slug
            if body:
                with open(os.path.join(out, "text", slug + ".txt"), "w", encoding="utf-8") as f:
                    f.write(f"VENUE: {res['name']} | {res.get('city')}, {res.get('st')} | {res['type']}\nSITE: {res['website']}\nCSV NOTES: {res.get('notes')}\n\n{body}")
            results.append(res)
            if i % 25 == 0:
                print(f"{i}/{len(own)}", flush=True)
    results.sort(key=lambda r: r["name"].lower())
    with open(os.path.join(out, "crawl.json"), "w", encoding="utf-8") as f:
        json.dump({"crawled": results, "social_only": [{"name": r["Concert Presenter"], "city": r["City"], "st": r["ST"], "type": r["Type"], "website": r["Website"], "notes": r["Also..."]} for r in social]}, f, indent=1)
    ok = [r for r in results if not r.get("error")]
    print(f"done: {len(ok)} reachable of {len(results)}; {len(social)} social-only skipped")


if __name__ == "__main__":
    main()
