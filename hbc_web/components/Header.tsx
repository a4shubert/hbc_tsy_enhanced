import { Roboto } from "next/font/google";

const roboto = Roboto({
  subsets: ["latin"],
  weight: ["100", "300"],
});

export function Header() {
  return (
    <div className="w-full rounded-xl border-2 border-red-500">
      <header className="flex w-full items-center justify-between px-6 py-4">
        <div className="flex-1" />
        <h1
          className={`${roboto.className} flex-1 text-center text-4xl font-semibold text-white whitespace-nowrap`}
        >
          HBC TSY: Analytical Dashboard
        </h1>
        <nav className="flex flex-1 items-center justify-end gap-4 text-xl font-medium text-indigo-200">
          <a
            href="https://localhost:5047/swagger/index.html"
            className="rounded-md bg-indigo-500/10 px-3 py-2 hover:bg-indigo-500/20"
          >
            Documentation
          </a>
        </nav>
      </header>
    </div>
  );
}
