"use client";

import { useEffect, useState } from "react";

type CityClock = {
  label: string;
  zone: string;
};

type ClocksProps = {
  cities?: CityClock[];
  showSeconds?: boolean;
};

const defaultCities: CityClock[] = [
  { label: "New York", zone: "America/New_York" },
  { label: "London", zone: "Europe/London" },
  { label: "Dubai", zone: "Asia/Dubai" },
  { label: "Hong Kong", zone: "Asia/Hong_Kong" },
];

export function Clocks({ cities = defaultCities, showSeconds = false }: ClocksProps) {
  const [times, setTimes] = useState<Record<string, string>>({});

  useEffect(() => {
    const update = () => {
      const next: Record<string, string> = {};
      cities.forEach((c) => {
        next[c.label] = new Intl.DateTimeFormat("en-GB", {
          hour: "2-digit",
          minute: "2-digit",
          second: showSeconds ? "2-digit" : undefined,
          hour12: false,
          timeZone: c.zone,
        }).format(new Date());
      });
      setTimes(next);
    };

    update();
    const id = setInterval(update, 1000);
    return () => clearInterval(id);
  }, []);

  return (
    <div className="flex flex-wrap items-center gap-3 text-sm text-slate-200 sm:text-base">
      {cities.map((c) => (
        <div
          key={c.label}
          className="flex flex-col items-center rounded-md border border-slate-800 bg-slate-900/60 px-3 py-2 shadow-sm"
        >
          <div className="font-mono text-indigo-200 text-lg text-center">
            {times[c.label] ?? "--:--:--"}
          </div>
          <div className="font-semibold text-white text-center mt-1">{c.label}</div>
        </div>
      ))}
    </div>
  );
}
