// components/Header.tsx
import { Clocks } from "./Clocks"

export function Header() {
  return (
    <div className="w-full bg-[var(--color-bg)] px-[5vw] py-4">
      <div className="grid w-full grid-cols-1 items-center gap-4 py-4 2xl:grid-cols-[auto_minmax(0,1fr)_auto]">
        {/* Only show at >= 1920px (2xl) */}
        <div className="hidden items-center justify-start 2xl:flex">
          <Clocks
            showSeconds={false}
            cities={[
              { label: "New York", zone: "America/New_York" },
              { label: "London", zone: "Europe/London" },
              { label: "Dubai", zone: "Asia/Dubai" },
              { label: "Hong Kong", zone: "Asia/Hong_Kong" },
            ]}
          />
        </div>

        <h1 className="hbc-title min-w-0 w-full text-center text-white whitespace-normal break-normal hyphens-none">
          HBC TSY Analytical Dashboard
        </h1>

        {/* Only show at >= 1920px (2xl) */}
        <nav className="hidden items-center justify-end gap-4 text-xl font-normal 2xl:flex">
          <a
            href="http://localhost:5047/swagger/index.html"
            className="rounded-md bg-[color:var(--color-link-surface)] px-3 py-2 text-2xl text-[color:var(--color-text)] transition-[text-decoration-color] hover:underline hover:decoration-2 hover:underline-offset-4 hover:decoration-[var(--color-accent)]"
          >
            API (Swagger)
          </a>
          <a
            href="https://github.com/a4shubert/hbc_tsy_enhanced"
            className="rounded-md bg-[color:var(--color-link-surface)] px-3 py-2 text-2xl text-[color:var(--color-text)] transition-[text-decoration-color] hover:underline hover:decoration-2 hover:underline-offset-4 hover:decoration-[var(--color-accent)]"
          >
            Documentation
          </a>
        </nav>
      </div>
    </div>
  )
}
