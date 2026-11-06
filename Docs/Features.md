# GigHaus features

Everything GigHaus does today, by audience, followed by what isn't built yet and what's needed before launch. For how the pieces fit together, see [Concepts.md](Concepts.md).

## Marketing site

Public pages built to sell hosting to prospective venues. They're static pages, so they load fast and search engines can read them.

| Page | Route | Purpose |
|---|---|---|
| Home | `/` | The pitch to hosts: what GigHaus does today, five steps to a show, what's on the way, and why it's free |
| How it works | `/hosts` | Who can host, who does what, and how a private home is protected |
| FAQ | `/faq` | Cost, privacy, approval, money, tax and what to check before hosting |
| Contact | `/contact` | Lead form for people not ready to sign up |

GigHaus is for hosts alone, so all the selling is aimed at them. The pitch is that GigHaus makes house concerts dead simple. It's about managing shows and guests, and it doesn't find or book artists.

The site only describes planned features in the home page's "On the way" section, which says they aren't ready. It has no invented testimonials or numbers. Please keep it that way as it grows, and move each feature out of that section when it ships.

### The mascot

The house with the guitar appears on every marketing page. The `Mascot` component renders a still image. Only the home page hero sets `Animated`, and there `wwwroot/js/mascot.js` plays the video three times and leaves it stopped on its last frame.

| Situation | What people see in the home page hero |
|---|---|
| Chrome, Edge and other Chromium browsers | The transparent video three times, stopping on its last frame |
| Safari, Firefox and anything on iOS | The still image |
| Visitors who prefer reduced motion | The still image |

The swap is limited to Chromium because a browser that ignores WebM transparency shows a solid green square. Only Chromium has been tested.

`Mascot/` holds the source files. `Core/GigHaus.Web/wwwroot/mascot/` holds the versions the site serves: the 128, 256 and 512 pixel videos, stills at 96, 320 and 640 pixels generated from `mascot-4096.png`, and `mascot-hero.webp`, the first frame of the 512 pixel video, so the hero doesn't jump when the video starts. To add a size, follow `Mascot/MAKING_EXTRA_SIZES.md` and copy the result in.

## Accounts

| Page | Route | Purpose |
|---|---|---|
| Sign up | `/signup` | Creates a host account and venue, then signs the host in |
| Sign in | `/login` | Sends hosts to the portal and the admin to the admin area |
| Forgot password | `/forgot-password` | Emails a reset link. Shows the same message whether or not the account exists. |
| Reset password | `/reset-password` | Sets a new password from the emailed link |
| Change password | `/account/password` | For signed-in users |

## Public show page

`/shows/{id}` is the link a host shares. It shows the title, artist, date, city and contribution policy, with the legal disclosure beside a reservation form. It's only available for published shows from approved hosts.

The street address never appears on this page.

## Host portal

Signed-in hosts only. The portal works on a phone, including the guest list at the door.

| Page | Route | What hosts do |
|---|---|---|
| Dashboard | `/portal` | See their status, upcoming shows, waiting requests and confirmed guests |
| Shows | `/portal/events` | List all their shows |
| Add or edit a show | `/portal/events/new`, `/portal/events/{id}/edit` | Set the artist, date, guest limit and contribution policy |
| Manage a show | `/portal/events/{id}` | Publish, copy the share link, confirm guests, check them in, cancel |
| Your venue | `/portal/venue` | Update their address, capacity and description |

## Admin

For you alone, as the person who runs GigHaus.

| Page | Route | What you do |
|---|---|---|
| Dashboard | `/admin` | See new leads, hosts waiting for review and overall counts |
| Leads | `/admin/leads` | Work the sales pipeline: set a stage and keep notes |
| Hosts | `/admin/hosts` | See all hosts, with pending ones first |
| Host detail | `/admin/hosts/{id}` | Review a venue, approve or suspend it, and see its shows |
| Artists | `/admin/artists`, `/admin/artists/{id}` | See artists and their shows |
| Shows | `/admin/events` | See every show on the platform |

## Emails

| Email | Sent when | Sent to |
|---|---|---|
| Your venue is approved | You approve a host | The host |
| You're confirmed | A host confirms a guest. Includes the street address. | The guest |
| Reset your password | Someone asks for a reset | The account holder |

If an email fails to send, the action still succeeds and the failure is logged. GigHaus doesn't send a guest anything when they first request a spot, or when a host cancels them from the portal.

## API

`GigHaus.WebApi` is a separate REST API, originally built for the mobile app. The website doesn't use it.

| Endpoints | Access |
|---|---|
| `GET /api/events`, `GET /api/events/{id}` | Public, limited view of published shows only |
| `GET /api/payments/fees` | Public, returns a fee breakdown for an amount |
| `GET /api/hosts`, `GET /api/artists` and their detail routes | Admin |
| Creating, updating and deleting artists and hosts | Admin |
| `POST /api/events`, confirming and checking in reservations, door payments | Host or Admin |
| Reading reservations and settlements | Host or Admin |
| Creating and cancelling reservations, online payments, reading payments | Any signed-in user |
| `/hubs/gighaus` | SignalR hub for live updates. The website doesn't use it yet. |

The API's sign-in is a JSON Web Token, and the only way to get one is `/api/auth/dev-token`, which exists in development only. So in production today, only the public endpoints are usable.

Also, the API's Host role isn't tied to a specific host, so its reservation endpoints don't check ownership the way the portal does. Fix that before giving hosts API tokens.

## Not built yet

| Area | Current state |
|---|---|
| **Terms of service and privacy policy** | No pages exist, and the footer doesn't link to any. Needed before launch. |
| **Online payments** | `StripePaymentService` is a stub that only works in development mode. Contributions happen at the door. |
| **Recording door money** | Hosts can't record what was collected, so the "Gross collected" figures in admin stay at zero. Promised on the home page. |
| **Mailing lists** | Guest lists are per show. Hosts can't keep a list of guests across shows. Promised on the home page. |
| **Emails and texts** | Hosts can't email or text their guests from GigHaus, and GigHaus sends no texts at all. Promised on the home page. |
| **Reports** | Hosts have dashboard counts only. Promised on the home page. |
| **Reviews after shows** | GigHaus doesn't ask guests for reviews yet. Using them in publicity needs the guest's permission. Promised on the home page. |
| **Merch and extra donations** | No way to coordinate online merch sales or extra donations yet. Promised on the home page. |
| **Artist and show info** | No info feed or webhooks yet, and no optional Ko-fi connection for paying artists directly. Promised on the home page. |
| **Reservation changes for guests** | Guests can't view or cancel their own reservation, so they have to contact the host. Guests will never have accounts, so this would need an emailed link. |
| **Finding shows** | There's no public list of shows. Guests need the link. |
| **Mobile app** | `GigHaus.Mobile` is a skeleton. Its pages have TODOs where the API calls belong, it has no `Platforms` folder, and it doesn't build on Windows. |
| **Live updates** | The SignalR hub exists, but no page listens to it. |
| **Notifying you** | New leads and sign-ups show on the dashboard, but nobody is emailed. |

## Before launch

- [ ] Have an attorney and CPA review the legal notes, the "free" claims and the site copy
- [ ] Publish terms of service and a privacy policy, and link them from the sign-up form and footer
- [ ] Add a Dockerfile and deployment configuration. None exist yet.
- [ ] Set up production email: `Smtp:Host`, `Smtp:Port`, `Smtp:EnableSsl`, `Smtp:Username`, `Smtp:Password` and `Smtp:DefaultFrom`. The default sender is `noreply@gighaus.app`.
- [ ] Set `Admin:Email` and `Admin:Password` for the admin account, then change the password
- [ ] Store ASP.NET Core Data Protection keys somewhere shared and persistent. Otherwise sign-ins and reset links stop working on every deploy.
- [ ] Configure forwarded headers for the hosting proxy. Without them, the rate limit treats all visitors as one address.
- [ ] Set a real `Jwt:Key` for the API. It falls back to a development key when none is set.
- [ ] Add a database volume or use managed Postgres, and back it up before running migrations
- [ ] Decide whether new hosts must confirm their email address. Today it's not required, because you review every host.
- [ ] Consider self-hosting the two web fonts, which load from Google Fonts
- [ ] Test the mascot and site in Safari and Firefox, and on real phones
