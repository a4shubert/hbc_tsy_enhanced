// components/Header.tsx
import { Clocks } from "./Clocks";

export function Header() {
  return (
    <div className="w-full px-[5vw] py-4">
      <div className="grid w-full grid-cols-1 items-center gap-4 py-4 lg:grid-cols-[auto_minmax(0,1fr)_auto]">
        <div className="hidden items-center justify-start lg:flex">
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

        <h1 className="hbc-title min-w-0 w-full whitespace-normal break-words text-center text-white">
          HBC TSY Analytical Dashboard
        </h1>

        <nav className="hidden items-center justify-end gap-4 text-xl font-normal lg:flex">
          <a
            href="http://localhost:5047/swagger/index.html"
            className="rounded-md bg-[color:var(--color-link-surface)] px-3 py-2 text-2xl text-[color:var(--color-text)] hover:bg-[color:var(--color-link-surface-hover)]"
          >
            API (Swagger)
          </a>
          <a
            href="https://github.com/a4shubert/hbc_tsy_enhanced"
            className="rounded-md bg-[color:var(--color-link-surface)] px-3 py-2 text-2xl text-[color:var(--color-text)] hover:bg-[color:var(--color-link-surface-hover)]"
          >
            Documentation
          </a>
        </nav>
      </div>
    </div>
  );
}
