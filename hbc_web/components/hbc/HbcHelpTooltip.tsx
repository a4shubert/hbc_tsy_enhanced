// components/HbcHelpTooltip.tsx
"use client"

export type HbcHelpTooltipProps = {
  title?: string
  ariaLabel?: string
  widthClassName?: string
  items: string[]
}

function InfoIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <path
        d="M12 22c5.523 0 10-4.477 10-10S17.523 2 12 2 2 6.477 2 12s4.477 10 10 10z"
        stroke="currentColor"
        strokeWidth="2"
      />
      <path d="M12 10v7" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
      <path d="M12 7h.01" stroke="currentColor" strokeWidth="3" strokeLinecap="round" />
    </svg>
  )
}

export function HbcHelpTooltip({
  title = "Grid shortcuts",
  ariaLabel = "Grid help",
  widthClassName = "w-[420px]",
  items,
}: HbcHelpTooltipProps) {
  return (
    <div className="relative inline-flex items-center">
      <button
        type="button"
        aria-label={ariaLabel}
        className="peer inline-flex items-center justify-center rounded-md border border-transparent bg-transparent p-1 text-[color:var(--color-muted)] hover:text-[color:var(--color-text)]"
      >
        <InfoIcon />
      </button>
      <div
        className={[
          "pointer-events-none absolute right-full top-full z-20 mr-2 mt-2 hidden rounded-md border border-[color:var(--color-accent)] [background:var(--color-card)] p-3 text-sm text-[color:var(--color-text)] shadow-lg peer-hover:block",
          widthClassName,
        ].join(" ")}
      >
        <div className="font-semibold text-[color:var(--color-accent)]">{title}</div>
        <ul className="mt-2 list-disc space-y-1 pl-5 text-[color:var(--color-muted)]">
          {items.map((x) => (
            <li key={x}>{x}</li>
          ))}
        </ul>
      </div>
    </div>
  )
}

