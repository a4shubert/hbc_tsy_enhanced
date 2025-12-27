// app/page.tsx
import {
  DM_Sans,
  Manrope,
  Plus_Jakarta_Sans,
  Sora,
  Space_Grotesk,
  Montserrat,
} from "next/font/google";

const fontDM = DM_Sans({ subsets: ["latin"], weight: ["400", "500", "600"] });
const fontManrope = Manrope({
  subsets: ["latin"],
  weight: ["400", "500", "600", "700"],
});
const fontJakarta = Plus_Jakarta_Sans({
  subsets: ["latin"],
  weight: ["400", "500", "600", "700"],
});
const fontSora = Sora({
  subsets: ["latin"],
  weight: ["400", "500", "600", "700"],
});
const fontSpace = Space_Grotesk({
  subsets: ["latin"],
  weight: ["400", "500", "600", "700"],
});
const fontMontserrat = Montserrat({
  subsets: ["latin"],
  weight: ["500", "600", "700"],
});

const samples: Array<
  | { label: string; className: string }
  | { label: string; style: React.CSSProperties }
> = [
    { label: "Space Grotesk (current)", className: fontSpace.className },
    { label: "DM Sans", className: fontDM.className },
    { label: "Manrope", className: fontManrope.className },
    { label: "Plus Jakarta Sans", className: fontJakarta.className },
    { label: "Sora", className: fontSora.className },
    { label: "Montserrat", className: fontMontserrat.className },
    {
      label: "Calibri Light",
      style: { fontFamily: "'Calibri Light', Calibri, 'Segoe UI', sans-serif" },
    },
    { label: "Segoe UI", style: { fontFamily: "'Segoe UI', system-ui, sans-serif" } },
    { label: "Inter", style: { fontFamily: "Inter, system-ui, sans-serif" } },
    { label: "Roboto", style: { fontFamily: "Roboto, system-ui, sans-serif" } },
    { label: "Helvetica Neue", style: { fontFamily: "'Helvetica Neue', Arial, sans-serif" } },
    { label: "Arial", style: { fontFamily: "Arial, sans-serif" } },
  ];

export default function Home() {
  return (
    <div className="w-full rounded-lg border-2 border-[color:var(--color-accent)] bg-[color:var(--color-bg)] p-6 text-[color:var(--color-text)]">
      <h1 className="text-xl text-[color:var(--color-accent)]">Main Contents</h1>

      <div className="mt-8 space-y-4">
        {samples.map((s) => {
          const className = "className" in s ? s.className : "";
          const style = "style" in s ? s.style : undefined;

          return (
            <div
              key={s.label}
              className="rounded-lg border border-white/10 bg-white/5 p-4"
              style={style}
            >
              <div className="text-sm text-white/70">{s.label}</div>
              <div className={`mt-1 text-xl ${className}`}>
                The quick brown fox jumps over the lazy dog (text-xl)
              </div>
            </div>
          );
        })}
      </div>

      <div className="mt-12 h-[140vh] rounded-lg border border-white/10 bg-white/5 p-4">
        Scroll test area (extra height so you can verify the header does NOT show content “behind” it)
      </div>
    </div>
  );
}
