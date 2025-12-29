// app/layout.tsx
import "./globals.css";
import type { Metadata } from "next";
import { DM_Sans } from "next/font/google";

import { Header } from "@/components/Header";
import { Footer } from "@/components/Footer";

export const metadata: Metadata = {
  title: "HBC TSY Analytical Dashboard",
  description: "Frontend for HBC REST API datasets.",
};

const dmSans = DM_Sans({
  subsets: ["latin"],
  weight: ["100", "300", "400", "500", "700"],
  variable: "--font-dm-sans",
  display: "swap",
});

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" className={dmSans.variable} data-theme="hbc">
      <body className="antialiased border-0 [background:var(--color-bg)] text-[var(--color-text)] overflow-hidden">
        <div id="app-scroll" className="flex h-[100dvh] min-h-[100dvh] flex-col overflow-hidden">
          <header className="sticky top-0 z-50 w-full [background:var(--color-bg)] border-b border-[color:var(--color-border)]">
            <Header />
          </header>
          <main className="flex-1 min-h-0 overflow-auto p-[5vw] py-5">{children}</main>
          <Footer />
        </div>
      </body>
    </html>
  );
}
