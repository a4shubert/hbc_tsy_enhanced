import "./globals.css";
import type { Metadata } from "next";
import { Header } from "@/components/Header";
import { Footer } from "@/components/Footer";

export const metadata: Metadata = {
  title: "HBC Data Portal",
  description: "Frontend for HBC REST API datasets.",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <head>
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="" />
        <link
          href="https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@400;500;600&display=swap"
          rel="stylesheet"
        />
      </head>
      <body className="font-sans text-[color:var(--color-text)] bg-[color:var(--color-bg)]">
        <div className="flex min-h-screen flex-col">
          <div className="sticky top-0 z-20 px-[5vw] py-4 backdrop-blur bg-slate-950/90">
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
