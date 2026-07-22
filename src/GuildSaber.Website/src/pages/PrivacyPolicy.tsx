import type { ReactNode } from "react"

const CONTROLLER_NAME = "Kuurama, operator of GuildSaber"
const PRIVACY_EMAIL = "privacy@guildsaber.com"

interface SectionProps {
  id: string
  title: string
  children: ReactNode
}

const Section = ({ id, title, children }: SectionProps) => (
  <section aria-labelledby={`${id}-heading`} className="scroll-mt-24 space-y-4" id={id}>
    <h2 className="text-2xl font-semibold" id={`${id}-heading`}>
      {title}
    </h2>
    {children}
  </section>
)

interface ExternalLinkProps {
  href: string
  children: ReactNode
}

const ExternalLink = ({ href, children }: ExternalLinkProps) => (
  <a className="text-primary underline underline-offset-4" href={href} rel="noreferrer" target="_blank">
    {children}
  </a>
)

const PrivacyPolicy = () => (
  <main className="mx-auto w-full max-w-4xl py-8 md:py-12">
    <article className="space-y-10">
      <header className="space-y-4">
        <p className="text-primary text-sm font-medium tracking-wide uppercase">Legal information</p>
        <h1 className="text-4xl font-bold tracking-tight">Privacy policy</h1>
        <p className="text-muted-foreground">Last updated: 22 July 2026</p>
        <p className="text-lg leading-8">
          This policy explains how GuildSaber processes personal data when you use its website, API, game mod,
          authentication features, Discord bot, or open-source project. It also explains which information is public and
          how to exercise your rights under the General Data Protection Regulation (GDPR, or RGPD in French).
        </p>
      </header>

      <nav aria-label="Privacy policy contents" className="bg-card border-border rounded-lg border p-5">
        <h2 className="font-semibold">Contents</h2>
        <ul className="text-muted-foreground mt-3 grid gap-x-6 gap-y-2 text-sm sm:grid-cols-2">
          <li>
            <a className="hover:text-foreground" href="#controller">
              Controller and contact
            </a>
          </li>
          <li>
            <a className="hover:text-foreground" href="#data">
              Data we process
            </a>
          </li>
          <li>
            <a className="hover:text-foreground" href="#purposes">
              Purposes and legal bases
            </a>
          </li>
          <li>
            <a className="hover:text-foreground" href="#public-data">
              Public data
            </a>
          </li>
          <li>
            <a className="hover:text-foreground" href="#recipients">
              Recipients and providers
            </a>
          </li>
          <li>
            <a className="hover:text-foreground" href="#retention">
              Retention
            </a>
          </li>
          <li>
            <a className="hover:text-foreground" href="#cookies">
              Cookies and local storage
            </a>
          </li>
          <li>
            <a className="hover:text-foreground" href="#rights">
              Your rights
            </a>
          </li>
        </ul>
      </nav>

      <Section id="controller" title="1. Controller and contact">
        <p className="leading-7">
          GuildSaber is operated from France by <strong>{CONTROLLER_NAME}</strong>, who determines why and how the
          personal data described in this policy is processed and is therefore the data controller.
        </p>
        <p className="leading-7">
          For privacy questions or requests, email{" "}
          <a className="text-primary underline underline-offset-4" href={`mailto:${PRIVACY_EMAIL}`}>
            {PRIVACY_EMAIL}
          </a>
          . GuildSaber has not appointed a data protection officer.
        </p>
        <p className="leading-7">
          GuildSaber&apos;s source code is publicly available in the{" "}
          <ExternalLink href="https://github.com/GuildSaber/GuildSaber">GuildSaber GitHub repository</ExternalLink>{" "}
          under an open-source licence. The public source code does not make GuildSaber&apos;s production database,
          authentication credentials, or infrastructure logs public.
        </p>
      </Section>

      <Section id="data" title="2. Data we process and where it comes from">
        <div className="space-y-5 leading-7">
          <div>
            <h3 className="font-semibold">Account, profile, and linked accounts</h3>
            <p className="text-muted-foreground mt-1">
              Player identifiers, username, avatar URL, country, account creation date, headset and game platform,
              manager status, subscription tier, and linked BeatLeader, ScoreSaber, Steam, Meta/Oculus, and Discord
              identifiers. GuildSaber currently obtains Steam and Meta/Oculus identifiers through BeatLeader; direct
              Steam or Meta/Oculus ticket authentication is not currently enabled.
            </p>
          </div>
          <div>
            <h3 className="font-semibold">Scores, gameplay, and rankings</h3>
            <p className="text-muted-foreground mt-1">
              Maps played, scores, modifiers, ranks, combo, misses, bad cuts, headset or controller information,
              submission times, and detailed performance statistics such as accuracy, pauses, jump distance, height,
              head position, and score progression when those fields are supplied by a ranking provider. GuildSaber uses
              this data to calculate guild-specific rankings, points, progression, and requirement results.
            </p>
          </div>
          <div>
            <h3 className="font-semibold">Guild and community activity</h3>
            <p className="text-muted-foreground mt-1">
              Guild membership, roles, permissions, join status, progression and ranking records, and relevant Discord
              user or server identifiers and bot interactions.
            </p>
          </div>
          <div>
            <h3 className="font-semibold">Sessions, devices, and security</h3>
            <p className="text-muted-foreground mt-1">
              Session identifiers and validity dates, browser name and version, operating platform, and request and
              security information such as IP address, timestamps, HTTP metadata, response status, and Cloudflare
              security signals. Cloudflare processes IP addresses and request metadata before traffic reaches the
              GuildSaber API.
            </p>
          </div>
          <div>
            <h3 className="font-semibold">Maps and creators</h3>
            <p className="text-muted-foreground mt-1">
              Beatmap identifiers, hashes, versions, names, artwork, song information, mapper or creator names, and
              related metadata obtained from BeatSaver and ranking providers.
            </p>
          </div>
          <div>
            <h3 className="font-semibold">Preferences and correspondence</h3>
            <p className="text-muted-foreground mt-1">
              The website stores your theme and selected guild locally in your browser. If you contact GuildSaber, your
              email address, message, and any information you include are processed to handle the request.
            </p>
          </div>
          <div>
            <h3 className="font-semibold">Open-source participation</h3>
            <p className="text-muted-foreground mt-1">
              If you participate through GitHub, your GitHub profile, issues, pull requests, reviews, discussions,
              contribution content, and commit attribution may be processed as part of maintaining the project. Git
              commit metadata can include the author name and email address configured in your Git client.
            </p>
          </div>
        </div>
        <p className="leading-7">
          This information comes from you and your browser, GuildSaber clients and the Discord bot, public or authorised
          provider APIs including BeatLeader, ScoreSaber, and BeatSaver, and GuildSaber&apos;s hosting and security
          infrastructure. Open-source participation data comes from your activity on GitHub. GuildSaber does not receive
          or store your provider password. OAuth access tokens used during BeatLeader or Discord authentication are not
          retained after the authentication flow.
        </p>
      </Section>

      <Section id="purposes" title="3. Why we process data and our legal bases">
        <div className="overflow-x-auto">
          <table className="border-border w-full min-w-160 border-collapse text-left text-sm">
            <thead>
              <tr className="bg-muted">
                <th className="border-border border p-3 font-semibold">Purpose</th>
                <th className="border-border border p-3 font-semibold">Legal basis</th>
              </tr>
            </thead>
            <tbody className="text-muted-foreground">
              <tr>
                <td className="border-border border p-3">
                  Create and authenticate accounts, link providers, maintain sessions, and provide requested website,
                  API, mod, and bot features.
                </td>
                <td className="border-border border p-3">
                  Performance of a contract or steps taken at your request (Article 6(1)(b) GDPR).
                </td>
              </tr>
              <tr>
                <td className="border-border border p-3">
                  Import and display profiles, maps, scores, guild rankings, progression, and community features.
                </td>
                <td className="border-border border p-3">
                  Performance of the service and GuildSaber&apos;s legitimate interests in operating a public
                  competitive ranking and community platform (Articles 6(1)(b) and 6(1)(f) GDPR).
                </td>
              </tr>
              <tr>
                <td className="border-border border p-3">
                  Protect accounts and leaderboard integrity, prevent abuse, apply rate limits, diagnose failures, and
                  keep the service reliable.
                </td>
                <td className="border-border border p-3">
                  GuildSaber&apos;s legitimate interests in security, abuse prevention, and reliable operation (Article
                  6(1)(f)).
                </td>
              </tr>
              <tr>
                <td className="border-border border p-3">
                  Develop, review, secure, document, and maintain GuildSaber as an open-source project and preserve its
                  contribution history.
                </td>
                <td className="border-border border p-3">
                  GuildSaber&apos;s legitimate interests in collaborative software development, security, attribution,
                  and project governance (Article 6(1)(f)).
                </td>
              </tr>
              <tr>
                <td className="border-border border p-3">
                  Respond to privacy requests, disputes, valid legal demands, and other legal obligations.
                </td>
                <td className="border-border border p-3">
                  Compliance with legal obligations and, where applicable, legitimate interests in establishing or
                  defending legal claims (Articles 6(1)(c) and 6(1)(f)).
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <p className="leading-7">
          You may object to processing based on legitimate interests. GuildSaber will assess your request against the
          relevant security, integrity, and community interests. Where consent is required for a future optional
          feature, it will be requested separately and may be withdrawn at any time.
        </p>
      </Section>

      <Section id="public-data" title="4. Information that is public">
        <p className="leading-7">
          GuildSaber is a public ranking and guild service. The website and public API may expose player identifiers,
          usernames, avatars, country, linked platform and Discord identifiers, headset and platform information,
          manager and subscription status, scores, performance statistics, ranks, points, guild memberships, roles, and
          progression. Guild and map information, including creator names, is also public.
        </p>
        <p className="leading-7">
          Public information can be viewed, copied, indexed, archived, or redistributed by others outside
          GuildSaber&apos;s control. GuildSaber does not intentionally publish raw IP addresses, authentication cookies,
          OAuth tokens, or private session and browser records.
        </p>
        <p className="leading-7">
          The source repository is also public. GitHub usernames and profiles, issues, pull requests, reviews,
          discussions, commit contents, and commit attribution may be permanently visible on GitHub and in distributed
          copies of the Git repository. Do not include authentication secrets, private tickets, or unnecessary personal
          data in a public contribution.
        </p>
      </Section>

      <Section id="recipients" title="5. Recipients, service providers, and transfers">
        <ul className="text-muted-foreground list-disc space-y-3 pl-6 leading-7">
          <li>
            <strong className="text-foreground">The public</strong>, for data returned by public pages and API
            endpoints.
          </li>
          <li>
            <strong className="text-foreground">Guild managers and authorised operators</strong>, where access is needed
            to administer guilds, rankings, accounts, or the service.
          </li>
          <li>
            <strong className="text-foreground">OVHcloud</strong>, which hosts GuildSaber&apos;s server and database in
            France. See the{" "}
            <ExternalLink href="https://www.ovhcloud.com/fr/terms-and-conditions/privacy-policy/">
              OVHcloud privacy policy
            </ExternalLink>
            .
          </li>
          <li>
            <strong className="text-foreground">Cloudflare</strong>, which provides DNS, reverse proxy, traffic
            security, and related metrics. Cloudflare may process network data outside the European Economic Area and
            describes its transfer safeguards in its{" "}
            <ExternalLink href="https://www.cloudflare.com/policies/privacy/">privacy policy</ExternalLink> and{" "}
            <ExternalLink href="https://www.cloudflare.com/trust-hub/gdpr/">GDPR information</ExternalLink>.
          </li>
          <li>
            <strong className="text-foreground">Connected services and data sources</strong>: BeatLeader, ScoreSaber,
            BeatSaver, and Discord process data under their own terms when GuildSaber uses their APIs, redirects you for
            authentication, or operates bot features. Review the{" "}
            <ExternalLink href="https://beatleader.com/privacy">BeatLeader privacy policy</ExternalLink>,{" "}
            <ExternalLink href="https://scoresaber.com/legal/privacy">ScoreSaber privacy policy</ExternalLink>,{" "}
            <ExternalLink href="https://scoresaber.com/legal/cookies-policy">ScoreSaber cookies policy</ExternalLink>,{" "}
            <ExternalLink href="https://beatsaver.com/policy/privacy">BeatSaver privacy policy</ExternalLink>, and{" "}
            <ExternalLink href="https://discord.com/privacy">Discord privacy policy</ExternalLink>.
          </li>
          <li>
            <strong className="text-foreground">GitHub</strong>, which hosts GuildSaber&apos;s public source repository
            and its contribution and collaboration features. GitHub processes your use of its platform under the{" "}
            <ExternalLink href="https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement">
              GitHub privacy statement
            </ExternalLink>
            .
          </li>
          <li>
            <strong className="text-foreground">Courts, regulators, or public authorities</strong>, when disclosure is
            required by law or necessary to protect rights, safety, or service security.
          </li>
        </ul>
        <p className="leading-7">GuildSaber does not sell or rent personal data and does not use it for advertising.</p>
      </Section>

      <Section id="retention" title="6. How long we keep data">
        <ul className="text-muted-foreground list-disc space-y-3 pl-6 leading-7">
          <li>
            Account, profile, linked-account, guild, score, and progression information is retained while the account
            and associated public ranking records are needed to provide GuildSaber. Following a valid deletion request,
            it will be deleted or anonymised unless limited retention is necessary for a legal obligation, dispute,
            abuse investigation, or ranking and community integrity.
          </li>
          <li>
            The GuildSaber authentication cookie expires after seven days. Temporary BeatLeader and Discord
            authentication cookies expire after up to ten minutes. Server-side session metadata is retained for account
            session management and security and is deleted with the account.
          </li>
          <li>
            Cloudflare retains security events and analytics according to the retention period for GuildSaber&apos;s
            Cloudflare service plan. GuildSaber does not export those logs to another log-storage service.
          </li>
          <li>
            Website theme and selected-guild preferences remain in browser local storage until you clear the site&apos;s
            data.
          </li>
          <li>
            Open-source contributions and their attribution may remain in Git history for the life of the project and in
            independently distributed copies. Requests concerning personal data in project history will be assessed
            against legal obligations, attribution requirements, and repository integrity, with removal or
            de-identification used where appropriate and reasonably possible.
          </li>
          <li>
            Privacy correspondence is retained for the time needed to answer the request and demonstrate that it was
            handled. Information required for legal claims or obligations may be restricted and retained for the
            applicable legal period.
          </li>
        </ul>
      </Section>

      <Section id="cookies" title="7. Cookies and browser storage">
        <p className="leading-7">
          GuildSaber currently uses only storage needed for authentication, security, and user-requested preferences. It
          does not use advertising cookies or non-essential analytics cookies, so GuildSaber does not currently display
          a cookie-consent banner. If that changes, consent will be requested where required.
        </p>
        <div className="overflow-x-auto">
          <table className="border-border w-full min-w-180 border-collapse text-left text-sm">
            <thead>
              <tr className="bg-muted">
                <th className="border-border border p-3 font-semibold">Storage</th>
                <th className="border-border border p-3 font-semibold">Purpose</th>
                <th className="border-border border p-3 font-semibold">Duration</th>
              </tr>
            </thead>
            <tbody className="text-muted-foreground">
              <tr>
                <td className="border-border border p-3 font-mono">__Host-GuildSaber.Session</td>
                <td className="border-border border p-3">Securely maintain your authenticated API session.</td>
                <td className="border-border border p-3">Seven days, or until logout or invalidation.</td>
              </tr>
              <tr>
                <td className="border-border border p-3">BeatLeader, Discord, and OAuth correlation cookies</td>
                <td className="border-border border p-3">
                  Complete authentication and protect the redirect against forgery.
                </td>
                <td className="border-border border p-3">Up to ten minutes or the end of the login flow.</td>
              </tr>
              <tr>
                <td className="border-border border p-3">Cloudflare security cookies</td>
                <td className="border-border border p-3">
                  Distinguish legitimate traffic and remember successful security challenges when Cloudflare enables
                  them.
                </td>
                <td className="border-border border p-3">Depends on the security feature and cookie.</td>
              </tr>
              <tr>
                <td className="border-border border p-3 font-mono">theme, guilds (local storage)</td>
                <td className="border-border border p-3">Remember your display theme and selected guild.</td>
                <td className="border-border border p-3">Until you clear the site&apos;s browser data.</td>
              </tr>
            </tbody>
          </table>
        </div>
        <p className="leading-7">
          Authentication and security cookies are necessary for the requested service. Blocking them can prevent sign-in
          or API features from working. Cloudflare documents the cookies its security services may set in its{" "}
          <ExternalLink href="https://developers.cloudflare.com/fundamentals/reference/policies-compliances/cloudflare-cookies/">
            cookie documentation
          </ExternalLink>
          .
        </p>
      </Section>

      <Section id="security" title="8. Security">
        <p className="leading-7">
          GuildSaber uses measures intended to protect personal data, including encrypted network transport, signed
          HTTP-only and secure session cookies in production, access controls, session invalidation, request
          verification for state-changing cookie-authenticated API calls, rate limiting, and Cloudflare traffic
          security. No internet service can guarantee absolute security.
        </p>
      </Section>

      <Section id="automated-processing" title="9. Rankings and automated processing">
        <p className="leading-7">
          GuildSaber automatically calculates rankings, points, progression, and whether scores meet guild rules. These
          calculations affect presentation and participation within GuildSaber but are not intended to produce legal or
          similarly significant effects as described by Article 22 GDPR. Contact GuildSaber if you believe data or a
          calculated result about you is incorrect.
        </p>
      </Section>

      <Section id="rights" title="10. Your data-protection rights">
        <p className="leading-7">
          Depending on the circumstances, you may request access to, correction of, deletion of, restriction of, or a
          portable copy of your personal data. You may object to processing based on legitimate interests and withdraw
          consent at any time where consent is used. These rights are not absolute; for example, the law may allow
          limited retention to protect other users, comply with a legal obligation, or preserve service integrity.
        </p>
        <p className="leading-7">
          Send requests to{" "}
          <a className="text-primary underline underline-offset-4" href={`mailto:${PRIVACY_EMAIL}`}>
            {PRIVACY_EMAIL}
          </a>
          . Include enough information to identify the relevant account or record, but do not send passwords or
          authentication tickets. GuildSaber may request proportionate proof of identity and will respond without undue
          delay, normally within one month. That period may be extended by up to two further months for complex or
          numerous requests, in which case you will be informed during the first month.
        </p>
        <p className="leading-7">
          You also have the right to lodge a complaint with the French data-protection authority, the{" "}
          <ExternalLink href="https://www.cnil.fr/fr/plaintes">
            Commission nationale de l&apos;informatique et des libertés (CNIL)
          </ExternalLink>
          .
        </p>
      </Section>

      <Section id="children" title="11. Children">
        <p className="leading-7">
          GuildSaber does not intentionally request sensitive information from children. If you are a parent or legal
          guardian and believe that a child&apos;s data is being processed unlawfully or contrary to the requirements of
          a connected provider, contact GuildSaber so the situation can be reviewed.
        </p>
      </Section>

      <Section id="future" title="12. Future integrations">
        <p className="leading-7">
          Direct Steam authentication tickets, Meta/Oculus PC game authentication tickets, and Patreon subscription
          integration are not currently processed by GuildSaber. Before enabling those features, this policy will be
          updated to describe the data exchanged, purpose, legal basis, recipients, and retention. Existing Steam and
          Meta/Oculus account identifiers obtained from BeatLeader are already covered above.
        </p>
      </Section>

      <Section id="changes" title="13. Changes to this policy">
        <p className="leading-7">
          This policy may be updated when GuildSaber&apos;s features, providers, or legal obligations change. The date
          at the top identifies the current version. Material changes will be highlighted through the service or another
          appropriate channel where practical.
        </p>
      </Section>
    </article>
  </main>
)

export default PrivacyPolicy
