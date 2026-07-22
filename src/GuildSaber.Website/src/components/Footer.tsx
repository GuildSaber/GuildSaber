import { Link } from "react-router"

const Footer = () => (
  <footer className="border-border border-t">
    <div className="text-muted-foreground mx-auto flex w-full max-w-7xl items-center justify-between gap-4 px-3 py-5 text-sm">
      <span>GuildSaber</span>
      <nav aria-label="Footer navigation" className="flex items-center gap-4">
        <a
          className="hover:text-foreground underline underline-offset-4"
          href="https://github.com/GuildSaber/GuildSaber"
          rel="noreferrer"
          target="_blank"
        >
          Source code
        </a>
        <Link className="hover:text-foreground underline underline-offset-4" to="/privacy-policy">
          Privacy policy
        </Link>
      </nav>
    </div>
  </footer>
)

export default Footer
