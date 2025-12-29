"use client"

import { useState } from "react"

type DatasetSidebarItem<K extends string> = {
  key: K
  label: string
  moniker: string
}

function MenuIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <path d="M4 6h16" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
      <path d="M4 12h16" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
      <path d="M4 18h16" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
  )
}

function CloseIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <path d="M18 6L6 18" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
      <path d="M6 6l12 12" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
  )
}

function SidebarContent<K extends string>({
  datasets,
  selected,
  onSelect,
  groupName,
}: {
  datasets: DatasetSidebarItem<K>[]
  selected: K
  onSelect: (key: K) => void
  groupName: string
}) {
  return (
    <div className="flex h-full flex-col gap-4 p-4 [background:var(--color-bg)]">
      <div className="text-2xl font-medium text-[color:var(--color-accent)]">DataSets:</div>
      <div className="flex flex-col gap-3">
        {datasets.map((d) => (
          <label
            key={d.key}
            className="flex cursor-pointer items-start gap-2 rounded-md border border-[color:var(--color-border)] [background:var(--color-card)] px-3 py-2 text-[color:var(--color-text)] hover:border-[color:var(--color-accent)]"
          >
            <input
              type="radio"
              name={groupName}
              value={d.key}
              checked={selected === d.key}
              onChange={() => onSelect(d.key)}
              className="mt-1 accent-[color:var(--color-accent)]"
            />
            <span className="min-w-0">
              <div className="text-lg font-medium">{d.label}</div>
              <div className="text-m leading-snug text-[color:var(--color-muted)] break-words whitespace-normal">
                {d.moniker}
              </div>
            </span>
          </label>
        ))}
      </div>
    </div>
  )
}

export function DatasetSidebar<K extends string>({
  datasets,
  selected,
  onSelect,
}: {
  datasets: DatasetSidebarItem<K>[]
  selected: K
  onSelect: (key: K) => void
}) {
  const [drawerOpen, setDrawerOpen] = useState(false)

  return (
    <>
      {/* Static sidebar for >=1920px */}
      <aside className="hbc-dataset-sidebar-static h-full w-[15vw] shrink-0 rounded-lg border border-[color:var(--color-border)] [background:var(--color-card)]">
        <SidebarContent datasets={datasets} selected={selected} onSelect={onSelect} groupName="dataset-static" />
      </aside>

      {/* Hamburger / drawer for <1920px */}
      <div className="hbc-dataset-sidebar-hamburger">
        <button
          type="button"
          aria-label="Open dataset menu"
          onClick={() => setDrawerOpen(true)}
          className="inline-flex items-center justify-center rounded-md border border-[color:var(--color-border)] [background:var(--color-bg)] p-2 text-[color:var(--color-text)] hover:border-[color:var(--color-accent)]"
        >
          <MenuIcon />
        </button>

        {drawerOpen ? (
          <div className="fixed inset-0 z-[60]">
            <button
              type="button"
              aria-label="Close dataset menu"
              onClick={() => setDrawerOpen(false)}
              className="absolute inset-0 cursor-default bg-black/50"
            />
            <div className="absolute left-0 top-0 h-full w-[15vw] min-w-[260px] border-r border-[color:var(--color-border)] [background:var(--color-card)]">
              <div className="flex items-center justify-end border-b border-[color:var(--color-border)] p-3">
                <button
                  type="button"
                  aria-label="Close"
                  onClick={() => setDrawerOpen(false)}
                  className="inline-flex items-center justify-center rounded-md border border-[color:var(--color-border)] [background:var(--color-bg)] p-2 text-[color:var(--color-text)] hover:border-[color:var(--color-accent)]"
                >
                  <CloseIcon />
                </button>
              </div>
              <SidebarContent
                datasets={datasets}
                selected={selected}
                onSelect={(k) => {
                  onSelect(k)
                }}
                groupName="dataset-drawer"
              />
            </div>
          </div>
        ) : null}
      </div>
    </>
  )
}
