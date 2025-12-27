import { Clocks } from "./Clocks";

export function Header() {
  return (
    <div className="w-full rounded-xl border-2 border-[color:var(--color-accent)] bg-[color:var(--color-bg)]">
      <header className="grid w-full items-center gap-4 px-6 py-4 grid-cols-1 2xl:grid-cols-[auto_minmax(0,1fr)_auto]">
        <div className="hidden 2xl:flex items-center justify-start">
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

        <h1 className="hbc-title min-w-0 w-full text-center text-white whitespace-normal break-words">
          HBC TSY Analytical Dashboard
        </h1>

        <nav className="hidden 2xl:flex items-center justify-end gap-4 text-xl font-normal">
          <a
            href="http://localhost:5047/swagger/index.html"
            className="rounded-md px-3 py-2 text-[color:var(--color-text)] bg-[color:var(--color-link-surface)] hover:bg-[color:var(--color-link-surface-hover)]"
          >
            API (Swagger)
          </a>
          <a
            href="https://github.com/a4shubert/hbc_tsy_enhanced"
            className="rounded-md px-3 py-2 text-[color:var(--color-text)] bg-[color:var(--color-link-surface)] hover:bg-[color:var(--color-link-surface-hover)]"
          >
            Documentation
          </a>
        </nav>
      </header>
    </div>
  );
}
