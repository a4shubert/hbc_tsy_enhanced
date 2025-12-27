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
    <div className="w-full h-screen rounded-lg border-2 border-[color:var(--color-accent)] bg-[color:var(--color-bg)] p-6 text-[color:var(--color-text)]">
      <h1 className='text-3xl text-[color:var(--color-accent)]'> Main Contents </h1>
    </div>
  );
}
