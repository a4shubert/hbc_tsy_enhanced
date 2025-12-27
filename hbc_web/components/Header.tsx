import { Clocks } from "./Clocks";

export function Header() {
  return (
    <div className="w-full rounded-xl border-2 border-[color:var(--color-accent)] bg-[color:var(--color-bg)]">
      <header className="flex w-full items-center justify-between gap-4 px-6 py-4">
        <div className="flex flex-1 items-center justify-start overflow-x-auto">
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

        <h1 className="flex-1 whitespace-nowrap text-center text-7xl font-[100] tracking-tight text-white">
          HBC TSY: Analytical Dashboard
        </h1>

        <nav className="flex flex-1 items-center justify-end gap-4 text-xl font-normal">
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