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

const samples = [
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
    <div className="w-full rounded-lg border-2 border-[color:var(--color-success)] bg-[color:var(--color-bg)] p-6 text-[color:var(--color-text)]">
      <h2 className="mb-4 text-3xl text-[color:var(--color-accent)]">
        Title font samples
      </h2>
      <div className="space-y-3 text-4xl">
        {samples.map((sample) => (
          <div
            key={sample.label}
            className={`${sample.className ?? ""} rounded-md bg-[color:var(--color-card)]/60 px-4 py-3 ring-1 ring-[color:var(--color-link-surface)]`}
            style={sample.style}
          >
            HBC TSY Analytical Dashboard — {sample.label}
          </div>
        ))}
      </div>
    </div>
  );
}
