"use client"

import { useEffect, useState } from "react"

type CityClock = {
  label: string
  zone: string
}

type ClocksProps = {
  cities?: CityClock[]
  showSeconds?: boolean
}

const defaultCities: CityClock[] = [
  { label: "New York", zone: "America/New_York" },
  { label: "London", zone: "Europe/London" },
  { label: "Dubai", zone: "Asia/Dubai" },
  { label: "Hong Kong", zone: "Asia/Hong_Kong" },
]

export function Clocks({ cities = defaultCities, showSeconds = false }: ClocksProps) {
  const [times, setTimes] = useState<Record<string, string>>({})

  useEffect(() => {
    const update = () => {
      const next: Record<string, string> = {}
      for (const c of cities) {
        next[c.label] = new Intl.DateTimeFormat("en-GB", {
          hour: "2-digit",
          minute: "2-digit",
          second: showSeconds ? "2-digit" : undefined,
          hour12: false,
          timeZone: c.zone,
        }).format(new Date())
      }
      setTimes(next)
    }

    update()
    const id = setInterval(update, 1000)
    return () => clearInterval(id)
  }, [cities, showSeconds])

  return (
    <div className="flex min-w-0 flex-nowrap items-center gap-4 text-base text-slate-200">
      {cities.map((c) => (
        <div
          key={c.label}
          className="min-w-0 flex flex-col items-center rounded-md border border-slate-800 bg-[color:var(--color-bg)] px-4 py-3 shadow-sm"
        >
          <div className="w-full text-center font-normal text-indigo-100 tabular-nums text-2xl">
            {times[c.label] ?? (showSeconds ? "--:--:--" : "--:--")}
          </div>

          <div className="mt-1 w-full text-center font-medium text-white truncate text-base 2xl:text-lg">
            {c.label}
          </div>
        </div>
      ))}
    </div>
  )
}
