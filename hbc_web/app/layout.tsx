import "./globals.css";
import type { Metadata } from "next";
import { DM_Sans } from "next/font/google";

import { Header } from "@/components/Header";
import { Footer } from "@/components/Footer";

export const metadata: Metadata = {
  title: "HBC Data Portal",
  description: "Frontend for HBC REST API datasets.",
};

const dmSans = DM_Sans({
  subsets: ["latin"],
  weight: ["100", "300", "400", "500", "700"], // load 300 so font-light isn't faked
  variable: "--font-dm-sans",
  display: "swap",
});

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" className={dmSans.variable} data-theme="hbc">
      <body className="antialiased bg-[var(--color-bg)] text-[var(--color-text)]">
        <div className="flex min-h-screen flex-col">
          <div className="sticky top-0 z-20 px-[5vw] py-4 backdrop-blur bg-[var(--color-bg)]">
            <div className="w-full">
              <Header />
            </div>
          </div>

          <main className="flex-1 px-[5vw] py-10">{children}</main>
          <Footer />
        </div>
      </body>
    </html>
  );
}